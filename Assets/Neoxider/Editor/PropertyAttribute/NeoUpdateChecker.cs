using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Neo.Editor
{
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
        private const string SessionStatus = "Neo.UpdateCheck.Status";
        private const string SessionLastManualCheckTicks = "Neo.UpdateCheck.LastManualCheckTicks";

        private static UnityWebRequest _request;
        private static bool _isChecking;

        public static State Tick(string currentVersion, string packageRootPath)
        {
            // Если уже в процессе — просто возвращаем текущее состояние
            if (_isChecking)
            {
                State s = ReadState();
                return new State(UpdateStatus.Checking, s.LatestVersion, s.UpdateUrl, s.Error);
            }

            long lastTicks = GetLastCheckTicks();
            long nowTicks = DateTime.UtcNow.Ticks;
            long minIntervalTicks = TimeSpan.FromSeconds(MinCheckIntervalSeconds).Ticks;

            // Если проверяли недавно — ничего не делаем
            if (lastTicks > 0 && nowTicks - lastTicks < minIntervalTicks)
            {
                return ReadState();
            }

            // Запускаем новую проверку
            TryStartCheck(currentVersion, packageRootPath);
            State state = ReadState();
            return _isChecking
                ? new State(UpdateStatus.Checking, state.LatestVersion, state.UpdateUrl, state.Error)
                : state;
        }

        /// <summary>
        ///     Возвращает текущее состояние без запуска проверки (только чтение кеша).
        /// </summary>
        public static State Peek()
        {
            if (_isChecking)
            {
                State s = ReadState();
                return new State(UpdateStatus.Checking, s.LatestVersion, s.UpdateUrl, s.Error);
            }

            return ReadState();
        }

        public static void RequestImmediateCheck(string currentVersion, string packageRootPath)
        {
            if (_isChecking)
            {
                return;
            }

            // Кулдаун ручной проверки (не чаще раз в 10 секунд)
            string manualTicksStr = SessionState.GetString(SessionLastManualCheckTicks, string.Empty);
            if (long.TryParse(manualTicksStr, out long lastManualTicks))
            {
                long nowTicks = DateTime.UtcNow.Ticks;
                long cooldownTicks = TimeSpan.FromSeconds(MinManualCheckIntervalSeconds).Ticks;
                if (nowTicks - lastManualTicks < cooldownTicks)
                {
                    int secondsLeft = (int)((cooldownTicks - (nowTicks - lastManualTicks)) / TimeSpan.TicksPerSecond) +
                                      1;
                    Debug.Log($"[NeoUpdateChecker] Повторная проверка доступна через {secondsLeft} сек.");
                    return;
                }
            }

            SessionState.SetString(SessionLastManualCheckTicks, DateTime.UtcNow.Ticks.ToString());

            // Сбрасываем интервал и помечаем как Checking сразу
            SessionState.SetString(SessionLastCheckTicks, "0");
            SessionState.SetInt(SessionStatus, (int)UpdateStatus.Checking);

            TryStartCheck(currentVersion, packageRootPath);
        }

        private static State ReadState()
        {
            int statusInt = SessionState.GetInt(SessionStatus, (int)UpdateStatus.Unknown);
            UpdateStatus status = Enum.IsDefined(typeof(UpdateStatus), statusInt)
                ? (UpdateStatus)statusInt
                : UpdateStatus.Unknown;

            string latest = SessionState.GetString(SessionLatestVersion, string.Empty);
            string url = SessionState.GetString(SessionUpdateUrl, string.Empty);
            string error = SessionState.GetString(SessionError, string.Empty);
            return new State(status, latest, url, error);
        }

        private static long GetLastCheckTicks()
        {
            string ticksStr = SessionState.GetString(SessionLastCheckTicks, string.Empty);
            if (long.TryParse(ticksStr, out long ticks))
            {
                return ticks;
            }

            return 0;
        }

        private static void SetLastCheckTicks(long ticks)
        {
            SessionState.SetString(SessionLastCheckTicks, ticks.ToString());
        }

        private static void TryStartCheck(string currentVersion, string packageRootPath)
        {
            try
            {
                string repoUrl = TryGetRepositoryUrlFromPackageJson(packageRootPath);

                // Фоллбек: если packageRootPath пуст, ищем package.json рядом со скриптом
                if (string.IsNullOrEmpty(repoUrl))
                {
                    repoUrl = TryFindRepositoryUrlFallback();
                }

                if (string.IsNullOrEmpty(repoUrl))
                {
                    SessionState.SetString(SessionError, "repository URL not found in package.json");
                    SessionState.SetInt(SessionStatus, (int)UpdateStatus.Unknown);
                    SetLastCheckTicks(DateTime.UtcNow.Ticks);
                    Debug.LogWarning("[NeoUpdateChecker] Не удалось найти URL репозитория в package.json. " +
                                     $"packageRootPath=\"{packageRootPath ?? "null"}\"");
                    return;
                }

                if (!TryParseGitHubOwnerRepo(repoUrl, out string owner, out string repo, out string repoWebUrl))
                {
                    SessionState.SetString(SessionError, $"Cannot parse GitHub URL: {repoUrl}");
                    SessionState.SetInt(SessionStatus, (int)UpdateStatus.Unknown);
                    SetLastCheckTicks(DateTime.UtcNow.Ticks);
                    Debug.LogWarning($"[NeoUpdateChecker] Не удалось разобрать GitHub URL: {repoUrl}");
                    return;
                }

                string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
                string updateUrl = repoWebUrl;

                _request = UnityWebRequest.Get(apiUrl);
                _request.SetRequestHeader("User-Agent", "NeoxiderTools-UnityEditor");
                _request.SetRequestHeader("Accept", "application/vnd.github+json");

                _isChecking = true;
                SetLastCheckTicks(DateTime.UtcNow.Ticks);
                Debug.Log($"[NeoUpdateChecker] Запрос обновления: {apiUrl}");

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
                Debug.LogWarning($"[NeoUpdateChecker] Исключение при проверке обновлений: {ex.Message}");
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

            // 403 — GitHub API rate limit. Не пробуем фоллбек tags, тоже будет 403.
            if (request.responseCode == 403)
            {
                string error = "GitHub API rate limit (403). Повторите позже.";
                ApplyResult(currentVersion, null, repoWebUrl, error);
                return;
            }

            // Если releases/latest не работает (например 404 — нет релизов), пробуем tags
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
                    }
                };
            }
            catch
            {
                ApplyResult(currentVersion, null, repoWebUrl, "Update check failed");
            }
        }

        private static void ApplyResult(string currentVersion, string latestVersion, string updateUrl, string error)
        {
            SessionState.SetString(SessionError, error ?? string.Empty);

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning($"[NeoUpdateChecker] Ошибка проверки обновлений: {error}");
            }

            if (string.IsNullOrEmpty(latestVersion))
            {
                SessionState.SetBool(SessionUpdateAvailable, false);
                SessionState.SetString(SessionLatestVersion, string.Empty);
                SessionState.SetString(SessionUpdateUrl, updateUrl ?? string.Empty);
                SessionState.SetInt(SessionStatus, (int)UpdateStatus.Unknown);
                return;
            }

            SessionState.SetString(SessionLatestVersion, latestVersion);
            SessionState.SetString(SessionUpdateUrl, updateUrl ?? string.Empty);

            UpdateStatus status = CompareToPublished(currentVersion, latestVersion);
            SessionState.SetInt(SessionStatus, (int)status);
            SessionState.SetBool(SessionUpdateAvailable, status == UpdateStatus.UpdateAvailable);

            Debug.Log(
                $"[NeoUpdateChecker] Проверка завершена: current={currentVersion}, latest={latestVersion}, status={status}");
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

            // Оставляем только цифры и точки
            v = Regex.Replace(v, "[^0-9.]", string.Empty);

            // Дополняем до формата x.y.z
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

            // Первый элемент массива: { "name": "vX.Y.Z", ... }
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

                // packageRootPath может быть Assets/... или Packages/...
                string projectRoot = Directory.GetCurrentDirectory();
                string packageJsonAbsolute = Path.GetFullPath(Path.Combine(projectRoot, packageJsonRelative));

                if (!File.Exists(packageJsonAbsolute))
                {
                    // Фоллбек через AssetDatabase
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
        ///     Фоллбек: ищем package.json через AssetDatabase по известному имени пакета.
        /// </summary>
        private static string TryFindRepositoryUrlFallback()
        {
            try
            {
                // Ищем все package.json в проекте
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
                    if (text.Contains("\"displayName\": \"Neoxider Tools\"") ||
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

            // Совсем последний фоллбек: прямой путь
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

            // https://github.com/Owner/Repo
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