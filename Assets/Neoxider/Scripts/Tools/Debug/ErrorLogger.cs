using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Neo
{
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(ErrorLogger))]
    public class ErrorLogger : MonoBehaviour
    {
        [Header("Main Settings")]
        public TextMeshProUGUI textMesh;
        public LogType[] logTypesToDisplay = { LogType.Error, LogType.Exception };
        public bool addText = true;
        public bool checkExistingErrors = true;

        public string errorText;

        private List<string> errorList = new List<string>();

        void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
            textMesh.raycastTarget = false;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (logTypesToDisplay.Length == 0 || Array.Exists(logTypesToDisplay, t => t == type))
            {
                string errorText = GetColor(type) + "\n -- " + logString.AddColor(ColorHTML.orange) + "\n -- " + stackTrace + "\n\n";

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
        }

        private string GetColor(LogType type)
        {
            ColorHTML color = ColorHTML.white;

            switch (type)
            {
                case LogType.Exception:
                    color = ColorHTML.brown;
                    break;
                case LogType.Error:
                    color = ColorHTML.red;
                    break;
                case LogType.Assert:
                    color = ColorHTML.aqua;
                    break;
                case LogType.Warning:
                    color = ColorHTML.yellow;
                    break;
                case LogType.Log:
                    color = ColorHTML.white;
                    break;
            }

            return type.ToString().AddColor(color, true);
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
