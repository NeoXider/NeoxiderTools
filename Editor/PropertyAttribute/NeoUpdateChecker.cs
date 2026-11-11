using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Neo.Editor
{
    /// <summary>
    ///     Throttled GitHub release check behind the version pill of the inspector header. State lives in
    ///     SessionState so it survives domain reloads; reads are served from memory because the header asks on every
    ///     IMGUI event.
    /// </summary>
    internal static class NeoUpdateChecker
    {
        public enum UpdateStatus
        {
            Unknown = 0,
            Checking = 1,
            UpToDate = 2,
            UpdateAvailable = 3,
            Ahead = 4
        }

        private const int MinCheckIntervalSeconds = 10 * 60;
        private const int MinManualCheckIntervalSeconds = 10;

        private const string SessionLastCheckTicks = "Neo.UpdateCheck.LastCheckTicks";
        private const string SessionLatestVersion = "Neo.UpdateCheck.LatestVersion";
        private const string SessionUpdateAvailable = "Neo.UpdateCheck.UpdateAvailable";
        private const string SessionUpdateUrl = "Neo.UpdateCheck.UpdateUrl";
        private const string SessionError = "Neo.UpdateCheck.Error";
        internal const string SessionStatus = "Neo.UpdateCheck.Status";
        private const string SessionLastManualCheckTicks = "Neo.UpdateCheck.LastManualCheckTicks";
        private const string VerbosePrefsKey = "Neo.UpdateCheck.Verbose";

        private static UnityWebRequest _request;
        private static bool _isChecking;
        private static bool _isFallbackChecking;

        // WHY: Tick() runs on every IMGUI event of every Neo inspector. Reading five SessionState strings, parsing
        // ticks and calling Enum.IsDefined each time allocated per event; SessionState is only written here, so
        // the last read stays valid until one of the writers below invalidates it.
        private static bool s_stateCached;
        private static State s_cachedState;
        private static bool s_ticksCached;
        private static long s_cachedLastTicks;
        private static string s_lastLoggedWarning;

        /// <summary>
        ///     When true (EditorPrefs <c>Neo.UpdateCheck.Verbose</c>), request/result details are logged; otherwise
        ///     only a changed warning is logged once, since the version pill already shows the status.
        /// </summary>
        internal static bool VerboseLogging => EditorPrefs.GetBool(VerbosePrefsKey, false);

        /// <summary>True while a request started in this domain (the releases call or its tags fallback) is in flight.</summary>
        internal static bool IsChecking => _isChecking || _isFallbackChecking;

        /// <summary>Drops the in-memory copy so the next read comes from SessionState.</summary>
        internal static void InvalidateCache()
        {
            s_stateCached = false;
            s_ticksCached = false;
        }

        private static void WriteString(string key, string value)
        {
            SessionState.SetString(key, value);
            InvalidateCache();
        }

        private static void WriteInt(string key, int value)
        {
            SessionState.SetInt(key, value);
            InvalidateCache();
        }

        private static void WriteBool(string key, bool value)
        {
            SessionState.SetBool(key, value);
            InvalidateCache();
        }

        private static void LogInfo(string message)
        {
            if (VerboseLogging)
            {
                Debug.Log(message);
            }
        }

        private static void LogWarningOnce(string message)
        {
            if (VerboseLogging || !string.Equals(message, s_lastLoggedWarning, StringComparison.Ordinal))
            {
                s_lastLoggedWarning = message;
                Debug.LogWarning(message);
            }
        }

        public static State Tick(string currentVersion, string packageRootPath)
        {
            if (IsChecking)
            {
                State s = ReadState();
                return new State(UpdateStatus.Checking, s.LatestVersion, s.UpdateUrl, s.Error);
            }

            long lastTicks = GetLastCheckTicks();
            long nowTicks = DateTime.UtcNow.Ticks;
            long minIntervalTicks = TimeSpan.FromSeconds(MinCheckIntervalSeconds).Ticks;

            if (lastTicks > 0 && nowTicks - lastTicks < minIntervalTicks)
            {
                return ReadState();
            }

            TryStartCheck(currentVersion, packageRootPath);
            State state = ReadState();
            return IsChecking
                ? new State(UpdateStatus.Checking, state.LatestVersion, state.UpdateUrl, state.Error)
                : state;
        }

        /// <summary>
        ///     Returns the current state without starting a check (cache read only).
        /// </summary>
        public static State Peek()
        {
            if (IsChecking)
            {
                State s = ReadState();
                return new State(UpdateStatus.Checking, s.LatestVersion, s.UpdateUrl, s.Error);
            }

            return ReadState();
        }

        public static void RequestImmediateCheck(string currentVersion, string packageRootPath)
        {
            if (IsChecking)
            {
                return;
            }

            string manualTicksStr = SessionState.GetString(SessionLastManualCheckTicks, string.Empty);
            if (long.TryParse(manualTicksStr, out long lastManualTicks))
            {
                long nowTicks = DateTime.UtcNow.Ticks;
                long cooldownTicks = TimeSpan.FromSeconds(MinManualCheckIntervalSeconds).Ticks;
                if (nowTicks - lastManualTicks < cooldownTicks)
                {
                    // WHY: verbose-only like every other info line; the pill already shows the current state.
                    if (VerboseLogging)
                    {
                        int secondsLeft =
                            (int)((cooldownTicks - (nowTicks - lastManualTicks)) / TimeSpan.TicksPerSecond) + 1;
                        LogInfo($"[NeoUpdateChecker] Next check available in {secondsLeft} s.");
                    }

                    return;
                }
            }

            WriteString(SessionLastManualCheckTicks, DateTime.UtcNow.Ticks.ToString());

            WriteString(SessionLastCheckTicks, "0");
            WriteInt(SessionStatus, (int)UpdateStatus.Checking);

            TryStartCheck(currentVersion, packageRootPath);
        }

        private static State ReadState()
        {
            if (!s_stateCached)
            {
                int statusInt = SessionState.GetInt(SessionStatus, (int)UpdateStatus.Unknown);
                UpdateStatus status = statusInt >= (int)UpdateStatus.Unknown && statusInt <= (int)UpdateStatus.Ahead
                    ? (UpdateStatus)statusInt
                    : UpdateStatus.Unknown;

                string latest = SessionState.GetString(SessionLatestVersion, string.Empty);
                string url = SessionState.GetString(SessionUpdateUrl, string.Empty);
                string error = SessionState.GetString(SessionError, string.Empty);
                s_cachedState = new State(status, latest, url, error);
                s_stateCached = true;
            }

            // WHY: a manual check persists "Checking" before its request starts. A domain reload mid-request drops
            // the request but not that status, which left the strip spinning (and live-repainting) until the next
            // scheduled check. Without a request in flight in this domain the status is simply unknown.
            if (s_cachedState.Status == UpdateStatus.Checking && !IsChecking)
            {
                return new State(UpdateStatus.Unknown, s_cachedState.LatestVersion, s_cachedState.UpdateUrl,
                    s_cachedState.Error);
            }

            return s_cachedState;
        }

        private static long GetLastCheckTicks()
        {
            if (!s_ticksCached)
            {
                string ticksStr = SessionState.GetString(SessionLastCheckTicks, string.Empty);
                s_cachedLastTicks = long.TryParse(ticksStr, out long ticks) ? ticks : 0;
                s_ticksCached = true;
            }

            return s_cachedLastTicks;
        }

        private static void SetLastCheckTicks(long ticks)
        {
            WriteString(SessionLastCheckTicks, ticks.ToString());
        }

        private static void TryStartCheck(string currentVersion, string packageRootPath)
        {
            try
            {
                string repoUrl = TryGetRepositoryUrlFromPackageJson(packageRootPath);

                if (string.IsNullOrEmpty(repoUrl))
                {
                    repoUrl = TryFindRepositoryUrlFallback();
                }

                if (string.IsNullOrEmpty(repoUrl))
                {
                    WriteString(SessionError, "repository URL not found in package.json");
                    WriteInt(SessionStatus, (int)UpdateStatus.Unknown);
                    SetLastCheckTicks(DateTime.UtcNow.Ticks);
                    LogWarningOnce("[NeoUpdateChecker] Could not find repository URL in package.json. " +
                                   $"packageRootPath=\"{packageRootPath ?? "null"}\"");
                    return;
                }

                if (!TryParseGitHubOwnerRepo(repoUrl, out string owner, out string repo, out string repoWebUrl))
                {
                    WriteString(SessionError, $"Cannot parse GitHub URL: {repoUrl}");
                    WriteInt(SessionStatus, (int)UpdateStatus.Unknown);
                    SetLastCheckTicks(DateTime.UtcNow.Ticks);
                    LogWarningOnce($"[NeoUpdateChecker] Could not parse GitHub URL: {repoUrl}");
                    return;
                }

                string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
                string updateUrl = repoWebUrl;

                _request = UnityWebRequest.Get(apiUrl);
                _request.SetRequestHeader("User-Agent", "NeoxiderTools-UnityEditor");
                _request.SetRequestHeader("Accept", "application/vnd.github+json");

                _isChecking = true;
                SetLastCheckTicks(DateTime.UtcNow.Ticks);
                LogInfo($"[NeoUpdateChecker] Update request: {apiUrl}");

                UnityWebRequestAsyncOperation op = _request.SendWebRequest();
                op.completed += _ =>
                {
                    try
                    {
                        HandleCompletedRequest(_request, currentVersion, updateUrl, owner, repo, repoWebUrl);
                    }
                    finally
                    {
                        _request?.Dispose();
                        _request = null;
                        _isChecking = false;
                    }
                };
            }
            catch (Exception ex)
            {
                _isChecking = false;
                LogWarningOnce($"[NeoUpdateChecker] Exception while checking for updates: {ex.Message}");
            }
        }

        private static void HandleCompletedRequest(UnityWebRequest request,
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

            // WHY: 403 — GitHub API rate limit. Do not try tags fallback; it would also return 403.
            if (request.responseCode == 403)
            {
                string error = "GitHub API rate limit (403). Try again later.";
                ApplyResult(currentVersion, null, repoWebUrl, error);
                return;
            }

            // WHY: If releases/latest fails (e.g. 404 — no releases), try tags
            if (request.result != UnityWebRequest.Result.Success)
            {
                TryCheckTagsFallback(currentVersion, owner, repo, repoWebUrl);
                return;
            }

            string json = request.downloadHandler?.text;
            string latest = TryExtractLatestVersionFromGitHubReleaseJson(json);
            ApplyResult(currentVersion, latest, updateUrl, null);
        }

        private static void TryCheckTagsFallback(string currentVersion, string owner, string repo, string repoWebUrl)
        {
            try
            {
                string tagsApiUrl = $"https://api.github.com/repos/{owner}/{repo}/tags?per_page=1";
                string updateUrl = repoWebUrl;

                UnityWebRequest tagsRequest = UnityWebRequest.Get(tagsApiUrl);
                tagsRequest.SetRequestHeader("User-Agent", "NeoxiderTools-UnityEditor");
                tagsRequest.SetRequestHeader("Accept", "application/vnd.github+json");

                // WHY: the first request's completion clears _isChecking before this one finishes; without a flag of
                // its own ReadState demoted the persisted "Checking" to Unknown halfway through the check and the
                // strip stopped its live repaint. A separate flag stays correct even if completion fires early.
                _isFallbackChecking = true;
                UnityWebRequestAsyncOperation op = tagsRequest.SendWebRequest();
                op.completed += _ =>
                {
                    try
                    {
                        if (tagsRequest.result != UnityWebRequest.Result.Success)
                        {
                            ApplyResult(currentVersion, null, updateUrl, tagsRequest.error);
                            return;
                        }

                        string json = tagsRequest.downloadHandler?.text;
                        string latest = TryExtractLatestVersionFromGitHubTagsJson(json);
                        ApplyResult(currentVersion, latest, updateUrl, null);
                    }
                    finally
                    {
                        tagsRequest.Dispose();
                        _isFallbackChecking = false;
                    }
                };
            }
            catch
            {
                _isFallbackChecking = false;
                ApplyResult(currentVersion, null, repoWebUrl, "Update check failed");
            }
        }

        private static void ApplyResult(string currentVersion, string latestVersion, string updateUrl, string error)
        {
            WriteString(SessionError, error ?? string.Empty);

            if (!string.IsNullOrEmpty(error))
            {
                LogWarningOnce($"[NeoUpdateChecker] Update check error: {error}");
            }

            if (string.IsNullOrEmpty(latestVersion))
            {
                WriteBool(SessionUpdateAvailable, false);
                WriteString(SessionLatestVersion, string.Empty);
                WriteString(SessionUpdateUrl, updateUrl ?? string.Empty);
                WriteInt(SessionStatus, (int)UpdateStatus.Unknown);
                return;
            }

            WriteString(SessionLatestVersion, latestVersion);
            WriteString(SessionUpdateUrl, updateUrl ?? string.Empty);

            UpdateStatus status = CompareToPublished(currentVersion, latestVersion);
            WriteInt(SessionStatus, (int)status);
            WriteBool(SessionUpdateAvailable, status == UpdateStatus.UpdateAvailable);

            LogInfo(
                $"[NeoUpdateChecker] Check finished: current={currentVersion}, latest={latestVersion}, status={status}");
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
            if (m.Success && m.Groups.Count > 1)
            {
                return m.Groups[1].Value;
            }

            return null;
        }

        private static string TryExtractLatestVersionFromGitHubTagsJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            // WHY: First array element: { "name": "vX.Y.Z", ... }
            Match m = Regex.Match(json, "\\\"name\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase);
            if (m.Success && m.Groups.Count > 1)
            {
                return m.Groups[1].Value;
            }

            return null;
        }

        private static string TryGetRepositoryUrlFromPackageJson(string packageRootPath)
        {
            if (string.IsNullOrEmpty(packageRootPath))
            {
                return null;
            }

            try
            {
                string relative = packageRootPath.Replace('\\', '/');
                string packageJsonRelative = $"{relative}/package.json";

                // WHY: packageRootPath may be Assets/... or Packages/...
                string projectRoot = Directory.GetCurrentDirectory();
                string packageJsonAbsolute = Path.GetFullPath(Path.Combine(projectRoot, packageJsonRelative));

                if (!File.Exists(packageJsonAbsolute))
                {
                    TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(packageJsonRelative);
                    if (asset != null)
                    {
                        return TryExtractRepoUrlFromJson(asset.text);
                    }

                    return null;
                }

                string json = File.ReadAllText(packageJsonAbsolute);
                return TryExtractRepoUrlFromJson(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     Fallback: find package.json via AssetDatabase using the known package name.
        /// </summary>
        private static string TryFindRepositoryUrlFallback()
        {
            try
            {
                string[] guids = AssetDatabase.FindAssets("package t:TextAsset");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith("package.json", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    if (asset == null)
                    {
                        continue;
                    }

                    string text = asset.text;
                    if (text.Contains("\"displayName\": \"NeoxiderTools\"") ||
                        text.Contains("\"displayName\": \"Neoxider Tools\"") ||
                        text.Contains("\"name\": \"com.neoxider.tools\""))
                    {
                        string url = TryExtractRepoUrlFromJson(text);
                        if (!string.IsNullOrEmpty(url))
                        {
                            return url;
                        }
                    }
                }
            }
            catch
            {
            }

            try
            {
                string[] knownPaths =
                {
                    "Assets/Neoxider/package.json",
                    "Packages/com.neoxider.tools/package.json"
                };

                foreach (string p in knownPaths)
                {
                    TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(p);
                    if (asset != null)
                    {
                        string url = TryExtractRepoUrlFromJson(asset.text);
                        if (!string.IsNullOrEmpty(url))
                        {
                            return url;
                        }
                    }
                }
            }
            catch
            {
            }

            return null;
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

            if (m.Success && m.Groups.Count > 1)
            {
                return m.Groups[1].Value;
            }

            return null;
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

        public readonly struct State
        {
            public readonly UpdateStatus Status;
            public readonly string LatestVersion;
            public readonly string UpdateUrl;
            public readonly string Error;

            public State(UpdateStatus status, string latestVersion, string updateUrl, string error)
            {
                Status = status;
                LatestVersion = latestVersion;
                UpdateUrl = updateUrl;
                Error = error;
            }

            public bool IsChecking => Status == UpdateStatus.Checking;
            public bool UpdateAvailable => Status == UpdateStatus.UpdateAvailable;
            public bool IsUpToDate => Status == UpdateStatus.UpToDate;
            public bool IsAhead => Status == UpdateStatus.Ahead;
        }
    }
}
