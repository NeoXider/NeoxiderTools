using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Neo.Editor
{
    internal static class NeoxiderUpdateChecker
    {
        public enum UpdateStatus
        {
            Unknown = 0,
            Checking = 1,
            UpToDate = 2,
            UpdateAvailable = 3,
            Ahead = 4
        }

        private const int MinCheckIntervalSeconds = 5 * 60;

        private static readonly Dictionary<string, ActiveRequest> RequestsByPrefix = new();

        public static State Tick(string stateKeyPrefix, string currentVersion, string packageRootPath)
        {
            if (string.IsNullOrWhiteSpace(stateKeyPrefix))
            {
                stateKeyPrefix = "Neoxider.UpdateCheck";
            }

            ActiveRequest active = GetOrCreateActive(stateKeyPrefix);
            if (active.IsChecking)
            {
                return ReadCheckingState(stateKeyPrefix);
            }

            long lastTicks = GetLastCheckTicks(stateKeyPrefix);
            long nowTicks = DateTime.UtcNow.Ticks;
            long minIntervalTicks = TimeSpan.FromSeconds(MinCheckIntervalSeconds).Ticks;

            if (lastTicks > 0 && nowTicks - lastTicks < minIntervalTicks)
            {
                return ReadState(stateKeyPrefix);
            }

            TryStartCheck(stateKeyPrefix, currentVersion, packageRootPath);
            return active.IsChecking ? ReadCheckingState(stateKeyPrefix) : ReadState(stateKeyPrefix);
        }

        public static State Peek(string stateKeyPrefix)
        {
            if (string.IsNullOrWhiteSpace(stateKeyPrefix))
            {
                stateKeyPrefix = "Neoxider.UpdateCheck";
            }

            ActiveRequest active = GetOrCreateActive(stateKeyPrefix);
            return active.IsChecking ? ReadCheckingState(stateKeyPrefix) : ReadState(stateKeyPrefix);
        }

        public static void RequestImmediateCheck(string stateKeyPrefix, string currentVersion, string packageRootPath)
        {
            if (string.IsNullOrWhiteSpace(stateKeyPrefix))
            {
                stateKeyPrefix = "Neoxider.UpdateCheck";
            }

            ActiveRequest active = GetOrCreateActive(stateKeyPrefix);
            if (active.IsChecking)
            {
                return;
            }

            WriteString(stateKeyPrefix, "LastCheckTicks", "0");
            WriteInt(stateKeyPrefix, "Status", (int)UpdateStatus.Checking);
            TryStartCheck(stateKeyPrefix, currentVersion, packageRootPath);
        }

        private static ActiveRequest GetOrCreateActive(string prefix)
        {
            if (!RequestsByPrefix.TryGetValue(prefix, out ActiveRequest active) || active == null)
            {
                active = new ActiveRequest();
                RequestsByPrefix[prefix] = active;
            }

            return active;
        }

        private static string Key(string prefix, string suffix)
        {
            return $"{prefix}.{suffix}";
        }

        /// <summary>In-memory copy of one prefix's SessionState values; dropped by every write to that prefix.</summary>
        private sealed class CachedPrefix
        {
            public bool HasState;
            public State State;
            public bool HasCheckingState;
            public State CheckingState;
            public bool HasTicks;
            public long Ticks;
        }

        // WHY: module headers read their state on every IMGUI event; five SessionState reads, key formatting and
        // Enum.IsDefined boxing per event were pure garbage. SessionState is only written through this class.
        private static readonly Dictionary<string, CachedPrefix> s_cache = new();

        private static CachedPrefix GetCache(string prefix)
        {
            if (!s_cache.TryGetValue(prefix, out CachedPrefix cached))
            {
                cached = new CachedPrefix();
                s_cache[prefix] = cached;
            }

            return cached;
        }

        private static void WriteString(string prefix, string suffix, string value)
        {
            SessionState.SetString(Key(prefix, suffix), value);
            s_cache.Remove(prefix);
        }

        private static void WriteInt(string prefix, string suffix, int value)
        {
            SessionState.SetInt(Key(prefix, suffix), value);
            s_cache.Remove(prefix);
        }

        private static State ReadState(string prefix)
        {
            CachedPrefix cached = GetCache(prefix);
            if (cached.HasState)
            {
                return cached.State;
            }

            int statusInt = SessionState.GetInt(Key(prefix, "Status"), (int)UpdateStatus.Unknown);
            UpdateStatus status = statusInt >= (int)UpdateStatus.Unknown && statusInt <= (int)UpdateStatus.Ahead
                ? (UpdateStatus)statusInt
                : UpdateStatus.Unknown;

            string latest = SessionState.GetString(Key(prefix, "LatestVersion"), string.Empty);
            string url = SessionState.GetString(Key(prefix, "UpdateUrl"), string.Empty);
            string error = SessionState.GetString(Key(prefix, "Error"), string.Empty);
            cached.State = new State(status, latest, url, error);
            cached.HasState = true;
            return cached.State;
        }

        // WHY: Tick and Peek answer every IMGUI event of a module header while a check is in flight; the
        // "Checking" copy of the cached state reuses its labels instead of building two strings per event.
        private static State ReadCheckingState(string prefix)
        {
            State state = ReadState(prefix);
            CachedPrefix cached = GetCache(prefix);
            if (!cached.HasCheckingState)
            {
                cached.CheckingState = state.WithStatus(UpdateStatus.Checking);
                cached.HasCheckingState = true;
            }

            return cached.CheckingState;
        }

        private static long GetLastCheckTicks(string prefix)
        {
            CachedPrefix cached = GetCache(prefix);
            if (!cached.HasTicks)
            {
                string ticksStr = SessionState.GetString(Key(prefix, "LastCheckTicks"), string.Empty);
                cached.Ticks = long.TryParse(ticksStr, out long ticks) ? ticks : 0;
                cached.HasTicks = true;
            }

            return cached.Ticks;
        }

        private static void SetLastCheckTicks(string prefix, long ticks)
        {
            WriteString(prefix, "LastCheckTicks", ticks.ToString());
        }

        private static void TryStartCheck(string prefix, string currentVersion, string packageRootPath)
        {
            ActiveRequest active = GetOrCreateActive(prefix);

            try
            {
                if (!TryGetUpdateConfigFromPackageJson(packageRootPath, out string repoUrl, out string tagPrefix) ||
                    string.IsNullOrEmpty(repoUrl) ||
                    !TryParseGitHubOwnerRepo(repoUrl, out string owner, out string repo, out string repoWebUrl))
                {
                    SetLastCheckTicks(prefix, DateTime.UtcNow.Ticks);
                    return;
                }

                string updateUrl = repoWebUrl;

                SetLastCheckTicks(prefix, DateTime.UtcNow.Ticks);
                WriteInt(prefix, "Status", (int)UpdateStatus.Checking);

                // WHY: If tagPrefix is provided, we only consider tags that start with this prefix (module-specific versioning).
                // This avoids false "updates" when multiple modules share the same Git repo.
                if (!string.IsNullOrEmpty(tagPrefix))
                {
                    TryCheckTagsWithPrefix(prefix, currentVersion, owner, repo, repoWebUrl, tagPrefix);
                    return;
                }

                string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

                UnityWebRequest request = UnityWebRequest.Get(apiUrl);
                request.SetRequestHeader("User-Agent", "NeoxiderTools-UnityEditor");
                request.SetRequestHeader("Accept", "application/vnd.github+json");

                active.Request = request;
                active.IsChecking = true;

                UnityWebRequestAsyncOperation op = request.SendWebRequest();
                op.completed += _ =>
                {
                    try
                    {
                        HandleCompletedRequest(prefix, request, currentVersion, updateUrl, owner, repo, repoWebUrl);
                    }
                    finally
                    {
                        request.Dispose();
                        active.Request = null;
                        active.IsChecking = false;
                    }
                };
            }
            catch
            {
                active.IsChecking = false;
            }
        }

        private static void HandleCompletedRequest(string prefix,
            UnityWebRequest request,
            string currentVersion,
            string updateUrl,
            string owner,
            string repo,
            string repoWebUrl)
        {
            if (request == null)
            {
                return;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                TryCheckTagsFallback(prefix, currentVersion, owner, repo, repoWebUrl);
                return;
            }

            string json = request.downloadHandler?.text;
            string latest = TryExtractLatestVersionFromGitHubReleaseJson(json);
            ApplyResult(prefix, currentVersion, latest, updateUrl, null);
        }

        private static void TryCheckTagsFallback(string prefix, string currentVersion, string owner, string repo,
            string repoWebUrl)
        {
            try
            {
                string tagsApiUrl = $"https://api.github.com/repos/{owner}/{repo}/tags?per_page=30";
                string updateUrl = repoWebUrl;

                UnityWebRequest tagsRequest = UnityWebRequest.Get(tagsApiUrl);
                tagsRequest.SetRequestHeader("User-Agent", "NeoxiderTools-UnityEditor");
                tagsRequest.SetRequestHeader("Accept", "application/vnd.github+json");

                UnityWebRequestAsyncOperation op = tagsRequest.SendWebRequest();
                op.completed += _ =>
                {
                    try
                    {
                        if (tagsRequest.result != UnityWebRequest.Result.Success)
                        {
                            ApplyResult(prefix, currentVersion, null, updateUrl, tagsRequest.error);
                            return;
                        }

                        string json = tagsRequest.downloadHandler?.text;
                        string latest = TryExtractLatestVersionFromGitHubTagsJson(json);
                        ApplyResult(prefix, currentVersion, latest, updateUrl, null);
                    }
                    finally
                    {
                        tagsRequest.Dispose();
                    }
                };
            }
            catch
            {
                ApplyResult(prefix, currentVersion, null, repoWebUrl, "Update check failed");
            }
        }

        private static void ApplyResult(string prefix, string currentVersion, string latestVersion, string updateUrl,
            string error)
        {
            WriteString(prefix, "Error", error ?? string.Empty);

            if (string.IsNullOrEmpty(latestVersion))
            {
                WriteString(prefix, "LatestVersion", string.Empty);
                WriteString(prefix, "UpdateUrl", updateUrl ?? string.Empty);
                WriteInt(prefix, "Status", (int)UpdateStatus.Unknown);
                return;
            }

            WriteString(prefix, "LatestVersion", latestVersion);
            WriteString(prefix, "UpdateUrl", updateUrl ?? string.Empty);

            UpdateStatus status = CompareToPublished(currentVersion, latestVersion);
            WriteInt(prefix, "Status", (int)status);
        }

        private static UpdateStatus CompareToPublished(string current, string latest)
        {
            if (!TryParseVersion(latest, out Version latestV) || !TryParseVersion(current, out Version currentV))
            {
                return UpdateStatus.Unknown;
            }

            int cmp = currentV.CompareTo(latestV);
            if (cmp < 0)
            {
                return UpdateStatus.UpdateAvailable;
            }

            if (cmp == 0)
            {
                return UpdateStatus.UpToDate;
            }

            return UpdateStatus.Ahead;
        }

        private static bool TryParseVersion(string version, out Version result)
        {
            result = null;

            if (string.IsNullOrEmpty(version))
            {
                return false;
            }

            string v = version.Trim();
            if (v.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                v = v.Substring(1);
            }

            int dash = v.IndexOf('-');
            if (dash >= 0)
            {
                v = v.Substring(0, dash);
            }

            v = Regex.Replace(v, "[^0-9.]", string.Empty);

            string[] parts = v.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return false;
            }

            int major = parts.Length > 0 && int.TryParse(parts[0], out int ma) ? ma : 0;
            int minor = parts.Length > 1 && int.TryParse(parts[1], out int mi) ? mi : 0;
            int build = parts.Length > 2 && int.TryParse(parts[2], out int bu) ? bu : 0;

            result = new Version(major, minor, build);
            return true;
        }

        private static string TryExtractLatestVersionFromGitHubReleaseJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            Match m = Regex.Match(json, "\\\"tag_name\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase);
            return m.Success && m.Groups.Count > 1 ? m.Groups[1].Value : null;
        }

        private static string TryExtractLatestVersionFromGitHubTagsJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            Match m = Regex.Match(json, "\\\"name\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase);
            return m.Success && m.Groups.Count > 1 ? m.Groups[1].Value : null;
        }

        private static bool TryGetUpdateConfigFromPackageJson(string packageRootPath, out string repoUrl,
            out string tagPrefix)
        {
            repoUrl = null;
            tagPrefix = null;

            if (string.IsNullOrEmpty(packageRootPath))
            {
                return false;
            }

            try
            {
                string relative = packageRootPath.Replace('\\', '/');
                string packageJsonRelative = $"{relative}/package.json";

                string projectRoot = Directory.GetCurrentDirectory();
                string packageJsonAbsolute = Path.GetFullPath(Path.Combine(projectRoot, packageJsonRelative));

                if (!File.Exists(packageJsonAbsolute))
                {
                    TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(packageJsonRelative);
                    if (asset == null)
                    {
                        return false;
                    }

                    repoUrl = TryExtractRepoUrlFromJson(asset.text);
                    tagPrefix = TryExtractUpdateTagPrefixFromJson(asset.text);
                    return true;
                }

                string json = File.ReadAllText(packageJsonAbsolute);
                repoUrl = TryExtractRepoUrlFromJson(json);
                tagPrefix = TryExtractUpdateTagPrefixFromJson(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string TryExtractRepoUrlFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            Match m = Regex.Match(json,
                "\\\"repository\\\"\\s*:\\s*\\{[\\s\\S]*?\\\"url\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"",
                RegexOptions.IgnoreCase);
            return m.Success && m.Groups.Count > 1 ? m.Groups[1].Value : null;
        }

        private static string TryExtractUpdateTagPrefixFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            Match m = Regex.Match(json,
                "\\\"neoxiderUpdateTagPrefix\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"",
                RegexOptions.IgnoreCase);
            return m.Success && m.Groups.Count > 1 ? m.Groups[1].Value : null;
        }

        private static void TryCheckTagsWithPrefix(string prefix, string currentVersion, string owner, string repo,
            string repoWebUrl, string tagPrefix)
        {
            ActiveRequest active = GetOrCreateActive(prefix);

            try
            {
                string tagsApiUrl = $"https://api.github.com/repos/{owner}/{repo}/tags?per_page=100";
                string updateUrl = repoWebUrl;

                UnityWebRequest tagsRequest = UnityWebRequest.Get(tagsApiUrl);
                tagsRequest.SetRequestHeader("User-Agent", "NeoxiderTools-UnityEditor");
                tagsRequest.SetRequestHeader("Accept", "application/vnd.github+json");

                active.Request = tagsRequest;
                active.IsChecking = true;

                UnityWebRequestAsyncOperation op = tagsRequest.SendWebRequest();
                op.completed += _ =>
                {
                    try
                    {
                        if (tagsRequest.result != UnityWebRequest.Result.Success)
                        {
                            ApplyResult(prefix, currentVersion, null, updateUrl, tagsRequest.error);
                            return;
                        }

                        string json = tagsRequest.downloadHandler?.text;
                        string latest = TryExtractLatestVersionFromGitHubTagsJsonWithPrefix(json, tagPrefix);
                        ApplyResult(prefix, currentVersion, latest, updateUrl, null);
                    }
                    finally
                    {
                        tagsRequest.Dispose();
                        active.Request = null;
                        active.IsChecking = false;
                    }
                };
            }
            catch
            {
                active.Request = null;
                active.IsChecking = false;
            }
        }

        private static string TryExtractLatestVersionFromGitHubTagsJsonWithPrefix(string json, string tagPrefix)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(tagPrefix))
            {
                return null;
            }

            MatchCollection matches =
                Regex.Matches(json, "\\\"name\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase);
            if (matches == null || matches.Count == 0)
            {
                return null;
            }

            Version best = null;
            string bestRaw = null;

            for (int i = 0; i < matches.Count; i++)
            {
                Match m = matches[i];
                if (!m.Success || m.Groups.Count < 2)
                {
                    continue;
                }

                string tag = m.Groups[1].Value;
                if (string.IsNullOrEmpty(tag) || !tag.StartsWith(tagPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string remainder = tag.Substring(tagPrefix.Length);
                if (string.IsNullOrEmpty(remainder))
                {
                    continue;
                }

                // WHY: For display, keep "vX.Y.Z" format
                string display = remainder.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                    ? remainder
                    : $"v{remainder}";

                if (!TryParseVersion(display, out Version v))
                {
                    continue;
                }

                if (best == null || v.CompareTo(best) > 0)
                {
                    best = v;
                    bestRaw = display;
                }
            }

            return bestRaw;
        }

        private static bool TryParseGitHubOwnerRepo(string repoUrl, out string owner, out string repo,
            out string repoWebUrl)
        {
            owner = null;
            repo = null;
            repoWebUrl = null;

            if (string.IsNullOrEmpty(repoUrl))
            {
                return false;
            }

            string url = repoUrl.Trim();
            if (url.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            {
                url = url.Substring(0, url.Length - 4);
            }

            if (!url.Contains("github.com", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                Uri uri = new(url);
                string[] segments = uri.AbsolutePath.Trim('/').Split('/');
                if (segments.Length < 2)
                {
                    return false;
                }

                owner = segments[0];
                repo = segments[1];
                repoWebUrl = $"https://github.com/{owner}/{repo}";
                return true;
            }
            catch
            {
                return false;
            }
        }

        private sealed class ActiveRequest
        {
            public bool IsChecking;
            public UnityWebRequest Request;
        }

        public readonly struct State
        {
            public readonly UpdateStatus Status;
            public readonly string LatestVersion;
            public readonly string UpdateUrl;
            public readonly string Error;

            /// <summary>"Update available: x.y.z", built once per state.</summary>
            public readonly string UpdateLabel;

            /// <summary>"Ahead of published (latest: x.y.z)", built once per state.</summary>
            public readonly string AheadLabel;

            public State(UpdateStatus status, string latestVersion, string updateUrl, string error)
            {
                Status = status;
                LatestVersion = latestVersion;
                UpdateUrl = updateUrl;
                Error = error;
                UpdateLabel = string.IsNullOrEmpty(latestVersion) ? null : "Update available: " + latestVersion;
                AheadLabel = string.IsNullOrEmpty(latestVersion)
                    ? null
                    : "Ahead of published (latest: " + latestVersion + ")";
            }

            private State(UpdateStatus status, in State source)
            {
                Status = status;
                LatestVersion = source.LatestVersion;
                UpdateUrl = source.UpdateUrl;
                Error = source.Error;
                UpdateLabel = source.UpdateLabel;
                AheadLabel = source.AheadLabel;
            }

            /// <summary>This state with another status; the labels are shared, not rebuilt.</summary>
            public State WithStatus(UpdateStatus status)
            {
                return new State(status, in this);
            }

            public bool IsChecking => Status == UpdateStatus.Checking;
            public bool UpdateAvailable => Status == UpdateStatus.UpdateAvailable;
            public bool IsUpToDate => Status == UpdateStatus.UpToDate;
            public bool IsAhead => Status == UpdateStatus.Ahead;
        }
    }
}
