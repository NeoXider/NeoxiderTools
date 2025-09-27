using System;
using System.Collections.Generic;
using Neo.Extensions;
using TMPro;
using UnityEngine;

namespace Neo
{
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(ErrorLogger))]
    public class ErrorLogger : MonoBehaviour
    {
        public LogType[] logTypesToDisplay = { LogType.Error, LogType.Exception };
        public bool addText = true;
        public bool checkExistingErrors = true;

        public string errorText;

        private readonly List<string> errorList = new();
        [Header("Main Settings")] public TextMeshProUGUI textMesh;

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
            if (logTypesToDisplay.Length == 0 || Array.Exists(logTypesToDisplay, t => t == type))
            {
                var errorText = GetColor(type) + "\n -- " + logString.SetColor(Color.green) + "\n -- " + stackTrace +
                                "\n\n";

                if (checkExistingErrors && errorList.Contains(errorText)) return;

                errorList.Add(errorText);

                if (addText)
                    AppendText(errorText);
                else
                    UpdateText(errorText);
            }
        }

        private string GetColor(LogType type)
        {
            var color = Color.white;

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
            if (textMesh != null) textMesh.text = newText;
        }

        public void AppendText(string additionalText)
        {
            if (textMesh != null) textMesh.text += additionalText;
        }
    }
}