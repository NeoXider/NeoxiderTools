using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Neo.Extensions;
using TMPro;
using UnityEngine;

namespace Neo.Tools
{
    [Flags]
    public enum LogTypeFilter
    {
        None = 0,
        Error = 1 << 0,
        Exception = 1 << 1,
        Warning = 1 << 2,
        Log = 1 << 3,
        Assert = 1 << 4
    }

    /// <summary>
    ///     Shows filtered Unity log messages (severity, message, optional script path and stack trace) in a
    ///     <see cref="TextMeshProUGUI" /> label. History is bounded by <see cref="MaxEntries" /> and optionally by
    ///     <see cref="MaxCharacters" />.
    /// </summary>
    [NeoDoc("Tools/Debug/ErrorLogger.md")]
    [CreateFromMenu("Neoxider/Tools/Debug/ErrorLogger", "Prefabs/Tools/ErrorLogger.prefab")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(ErrorLogger))]
    public class ErrorLogger : MonoBehaviour
    {
        // WHY: not RegexOptions.Compiled - IL2CPP has no Reflection.Emit, so it only adds startup cost.
        private static readonly Regex AtPathPattern = new(@"\(at\s+(.+?\.cs):(\d+)\)");
        private static readonly Regex MethodPathPattern = new(@"at\s+[^\s]+\s+\((.+?\.cs):(\d+)\)");

        [Header("Log Type Filters")] [Tooltip("Select log types to display")]
        public LogTypeFilter logTypeFilter = LogTypeFilter.Error | LogTypeFilter.Exception;

        [Header("Display Settings")] [Tooltip("Append text (true) or replace (false)")]
        public bool addText = true;

        [Tooltip("Check for duplicate errors")]
        public bool checkExistingErrors = true;

        [Tooltip("Include script path where log was called")]
        public bool showScriptPath;

        [Tooltip(
            "Maximum captured entries kept in history and on screen; the oldest are dropped first. 0 = unlimited " +
            "(memory and label size then grow with every new message).")]
        [Min(0)]
        [SerializeField]
        private int _maxEntries = 50;

        [Tooltip(
            "Maximum characters (stack traces and color tags included) kept in history and on screen; the oldest " +
            "entries are dropped first, the newest entry always stays. 0 = unlimited. A huge TMP label is slow to " +
            "rebuild and can exceed the mesh vertex limit.")]
        [Min(0)]
        [SerializeField]
        private int _maxCharacters;

        public string errorText;
        [Header("Main Settings")] public TextMeshProUGUI textMesh;

        private readonly List<string> errorList = new();
        private readonly Dictionary<string, int> _entryCounts = new(StringComparer.Ordinal);
        private readonly StringBuilder _textBuilder = new();
        private int _historyCharacters;

        /// <summary>Captured entries, oldest first (formatted exactly as displayed).</summary>
        public IReadOnlyList<string> Entries => errorList;

        /// <summary>Total characters of <see cref="Entries" />.</summary>
        public int HistoryCharacterCount => _historyCharacters;

        /// <summary>
        ///     Maximum entries kept in history and on screen (0 = unlimited). Lowering it drops the oldest entries now.
        /// </summary>
        public int MaxEntries
        {
            get => _maxEntries;
            set
            {
                _maxEntries = Mathf.Max(0, value);
                if (TrimHistory() && addText)
                {
                    RebuildText();
                }
            }
        }

        /// <summary>
        ///     Character budget of the history and label (0 = unlimited): the oldest entries are dropped until the
        ///     rest fits, but the newest entry always stays. Lowering it trims now.
        /// </summary>
        public int MaxCharacters
        {
            get => _maxCharacters;
            set
            {
                _maxCharacters = Mathf.Max(0, value);
                if (TrimHistory() && addText)
                {
                    RebuildText();
                }
            }
        }

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
            if (textMesh != null)
            {
                textMesh.raycastTarget = false;
            }
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void OnValidate()
        {
            _maxEntries = Mathf.Max(0, _maxEntries);
            _maxCharacters = Mathf.Max(0, _maxCharacters);
            // WHY: lowering a limit in the Inspector during Play applies at once, like the property setters.
            if (TrimHistory() && addText)
            {
                RebuildText();
            }
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (!ShouldDisplayLog(type))
            {
                return;
            }

            string scriptPath = "";
            if (showScriptPath)
            {
                scriptPath = ExtractScriptPath(stackTrace);
                if (!string.IsNullOrEmpty(scriptPath))
                {
                    scriptPath = "\n     " + scriptPath.SetColor(Color.cyan);
                }
            }

            string errorText = GetColor(type) + "\n -- " + logString.SetColor(Color.green) + scriptPath +
                               "\n -- " + stackTrace + "\n\n";

            if (checkExistingErrors && _entryCounts.ContainsKey(errorText))
            {
                return;
            }

            AddEntry(errorText);
            bool trimmed = TrimHistory();

            if (addText)
            {
                if (trimmed)
                {
                    RebuildText();
                }
                else
                {
                    AppendText(errorText);
                }
            }
            else
            {
                UpdateText(errorText);
            }
        }

        /// <summary>
        ///     Clears the captured history (including the duplicate filter) and the label.
        /// </summary>
        public void Clear()
        {
            errorList.Clear();
            _entryCounts.Clear();
            _historyCharacters = 0;
            UpdateText(string.Empty);
        }

        private void AddEntry(string entry)
        {
            errorList.Add(entry);
            _historyCharacters += entry.Length;
            _entryCounts.TryGetValue(entry, out int count);
            _entryCounts[entry] = count + 1;
        }

        private bool TrimHistory()
        {
            int removeCount = _maxEntries > 0 && errorList.Count > _maxEntries ? errorList.Count - _maxEntries : 0;
            int keptCharacters = _historyCharacters;
            for (int i = 0; i < removeCount; i++)
            {
                keptCharacters -= errorList[i].Length;
            }

            if (_maxCharacters > 0)
            {
                // WHY: the newest entry always stays, even when it alone is over the budget.
                while (keptCharacters > _maxCharacters && removeCount < errorList.Count - 1)
                {
                    keptCharacters -= errorList[removeCount].Length;
                    removeCount++;
                }
            }

            if (removeCount == 0)
            {
                return false;
            }

            for (int i = 0; i < removeCount; i++)
            {
                string removed = errorList[i];
                if (_entryCounts.TryGetValue(removed, out int count) && count > 1)
                {
                    _entryCounts[removed] = count - 1;
                }
                else
                {
                    _entryCounts.Remove(removed);
                }
            }

            errorList.RemoveRange(0, removeCount);
            _historyCharacters = keptCharacters;
            return true;
        }

        private void RebuildText()
        {
            _textBuilder.Clear();
            for (int i = 0; i < errorList.Count; i++)
            {
                _textBuilder.Append(errorList[i]);
            }

            UpdateText(_textBuilder.ToString());
        }

        private bool ShouldDisplayLog(LogType type)
        {
            LogTypeFilter filter = type switch
            {
                LogType.Error => LogTypeFilter.Error,
                LogType.Exception => LogTypeFilter.Exception,
                LogType.Warning => LogTypeFilter.Warning,
                LogType.Log => LogTypeFilter.Log,
                LogType.Assert => LogTypeFilter.Assert,
                _ => LogTypeFilter.None
            };

            return (logTypeFilter & filter) != 0;
        }

        private string ExtractScriptPath(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
            {
                return "";
            }

            // WHY: Pattern: (at Assets/Path/To/Script.cs:line)
            // Or: (at Packages/com.package/Script.cs:line)
            Match match = AtPathPattern.Match(stackTrace);

            if (match.Success)
            {
                string filePath = match.Groups[1].Value;
                string lineNumber = match.Groups[2].Value;
                return $"{filePath}:{lineNumber}";
            }

            // WHY: Alternate pattern without outer parens
            match = MethodPathPattern.Match(stackTrace);
            if (match.Success)
            {
                string filePath = match.Groups[1].Value;
                string lineNumber = match.Groups[2].Value;
                return $"{filePath}:{lineNumber}";
            }

            return "";
        }

        private string GetColor(LogType type)
        {
            Color color = Color.white;

            switch (type)
            {
                case LogType.Exception:
                    color = Color.magenta;
                    break;
                case LogType.Error:
                    color = Color.red;
                    break;
                case LogType.Assert:
                    color = Color.cyan;
                    break;
                case LogType.Warning:
                    color = Color.yellow;
                    break;
                case LogType.Log:
                    color = Color.white;
                    break;
            }

            return type.ToString().SetColor(color);
        }

        public void UpdateText(string newText)
        {
            if (textMesh != null)
            {
                textMesh.text = newText;
            }
        }

        public void AppendText(string additionalText)
        {
            if (textMesh != null)
            {
                textMesh.text += additionalText;
            }
        }
    }
}
