using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Neo.Extensions;
using TMPro;
using UnityEngine;

namespace Neo
{
    [System.Flags]
    public enum LogTypeFilter
    {
        None = 0,
        Error = 1 << 0,
        Exception = 1 << 1,
        Warning = 1 << 2,
        Log = 1 << 3,
        Assert = 1 << 4
    }

    [AddComponentMenu("Neo/" + "Tools/" + nameof(ErrorLogger))]
    public class ErrorLogger : MonoBehaviour
    {
        [Header("Log Type Filters")]
        [Tooltip("Выберите типы логов для отображения")]
        public LogTypeFilter logTypeFilter = LogTypeFilter.Error | LogTypeFilter.Exception;

        [Header("Display Settings")]
        [Tooltip("Добавлять текст в конец (true) или заменять (false)")]
        public bool addText = true;
        
        [Tooltip("Проверять на дубликаты ошибок")]
        public bool checkExistingErrors = true;
        
        [Tooltip("Добавлять путь к скрипту откуда был вызван лог")]
        public bool showScriptPath = false;

        public string errorText;
        [Header("Main Settings")] public TextMeshProUGUI textMesh;

        private readonly List<string> errorList = new();

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
            textMesh.raycastTarget = false;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            // Проверяем, нужно ли отображать этот тип лога
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

            if (checkExistingErrors && errorList.Contains(errorText))
            {
                return;
            }

            errorList.Add(errorText);

            if (addText)
            {
                AppendText(errorText);
            }
            else
            {
                UpdateText(errorText);
            }
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

            // Ищем паттерн: (at Assets/Path/To/Script.cs:line)
            // Или: (at Packages/com.package/Script.cs:line)
            Match match = Regex.Match(stackTrace, @"\(at\s+([^)]+\.cs):(\d+)\)");
            
            if (match.Success)
            {
                string filePath = match.Groups[1].Value;
                string lineNumber = match.Groups[2].Value;
                return $"{filePath}:{lineNumber}";
            }

            // Альтернативный паттерн без скобок
            match = Regex.Match(stackTrace, @"at\s+[^\s]+\s+\(([^)]+\.cs):(\d+)\)");
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