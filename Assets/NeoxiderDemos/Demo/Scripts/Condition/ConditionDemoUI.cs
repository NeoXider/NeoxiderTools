using System;
using System.Text;
using Neo.Condition;
using Neo.Samples;
using Neo.Tools;
using TMPro;
using UnityEngine;

#pragma warning disable CS0618 // WHY: Condition demo keeps the legacy Health sample contract until the scene is migrated.

namespace Neo.Demo.Condition
{
    /// <summary>
    ///     UI controller for the NeoCondition demo scene.
    ///     Drives GameOver, Win, Warning panels and status text.
    ///     In the authored scene (NeoConditionDemoBuilder) the NeoCondition events toggle the panels directly through
    ///     persistent listeners; this component adds the C# path (<see cref="EvaluateInCode" />) and the pause switch
    ///     that shows conditions which keep checking while <see cref="Time.timeScale" /> is 0.
    /// </summary>
    [AddComponentMenu("Neoxider/Demo/Condition/ConditionDemoUI")]
    public class ConditionDemoUI : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Health _health;

        [SerializeField] private ScoreManager _scoreManager;

        [SerializeField] private int _damagePerHit = 25;
        [SerializeField] private int _healPerClick = 10;
        [SerializeField] private int _pointsPerClick = 25;

        [Header("UI Panels")] [SerializeField] private GameObject _gameOverPanel;

        [SerializeField] private GameObject _winPanel;
        [SerializeField] private GameObject _warningIcon;

        [Header("Status")] [SerializeField] private TMP_Text _statusText;

        [Header("Code path")]
        [Tooltip("NeoCondition components that Evaluate In Code reads with NeoCondition.Evaluate().")]
        [SerializeField]
        private NeoCondition[] _conditions = Array.Empty<NeoCondition>();

        [Tooltip("Label that shows the last EvaluateInCode result.")] [SerializeField]
        private TMP_Text _codeResultText;

        [Tooltip("Optional demo log feed.")] [SerializeField]
        private DemoLogFeed _log;

        private readonly StringBuilder _codeLine = new();
        private bool _pausedByDemo;

        /// <summary>NeoCondition components read by <see cref="EvaluateInCode" />.</summary>
        public NeoCondition[] Conditions => _conditions ?? Array.Empty<NeoCondition>();

        /// <summary>True while <see cref="SetPaused" /> holds <see cref="Time.timeScale" /> at 0.</summary>
        public bool IsPausedByDemo => _pausedByDemo;

        private void Start()
        {
            HideGameOver();
            HideWin();
            HideWarning();
        }

        private void OnDisable()
        {
            ReleasePause();
        }

        public void ShowGameOver()
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
            }

            // WHY: the centered panel covers the status/warning labels; hiding them avoids
            // duplicated text bleeding through the panel.
            HideWarning();
            SetStatusVisible(false);
            SetStatus("GAME OVER!");
        }

        public void HideGameOver()
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }

            SetStatusVisible(true);
        }

        public void ShowWin()
        {
            if (_winPanel != null)
            {
                _winPanel.SetActive(true);
            }

            HideWarning();
            SetStatusVisible(false);
            SetStatus("YOU WIN!");
        }

        public void HideWin()
        {
            if (_winPanel != null)
            {
                _winPanel.SetActive(false);
            }

            SetStatusVisible(true);
        }

        public void ShowWarning()
        {
            if (_warningIcon != null)
            {
                _warningIcon.SetActive(true);
            }
        }

        public void HideWarning()
        {
            if (_warningIcon != null)
            {
                _warningIcon.SetActive(false);
            }
        }

        public void SetStatus(string text)
        {
            if (_statusText != null)
            {
                _statusText.text = text;
            }
        }

        private void SetStatusVisible(bool visible)
        {
            if (_statusText != null)
            {
                _statusText.gameObject.SetActive(visible);
            }
        }

        public void ConfigureAmounts(int damagePerHit, int healPerClick, int pointsPerClick)
        {
            _damagePerHit = Mathf.Max(0, damagePerHit);
            _healPerClick = Mathf.Max(0, healPerClick);
            _pointsPerClick = Mathf.Max(0, pointsPerClick);
        }

        public void DealDamage()
        {
            if (_health != null)
            {
                _health.TakeDamage(_damagePerHit);
            }
        }

        public void HealPlayer()
        {
            if (_health != null)
            {
                _health.Heal(_healPerClick);
            }
        }

        public void AddScore()
        {
            if (_scoreManager != null)
            {
                _scoreManager.Add(_pointsPerClick);
            }
        }

        public void ResetAll()
        {
            if (_health != null)
            {
                _health.Restore();
            }

            if (_scoreManager != null)
            {
                _scoreManager.ResetScore();
            }

            HideGameOver();
            HideWin();
            HideWarning();
            SetStatus("Playing...");
        }

        /// <summary>
        ///     C# path of the scene rules: calls <see cref="NeoCondition.Evaluate" /> (no events) on every assigned
        ///     condition and writes the results to the result label and the log.
        /// </summary>
        public void EvaluateInCode()
        {
            _codeLine.Clear();
            NeoCondition[] conditions = _conditions ?? Array.Empty<NeoCondition>();
            for (int i = 0; i < conditions.Length; i++)
            {
                NeoCondition condition = conditions[i];
                if (condition == null)
                {
                    continue;
                }

                bool passed = condition.Evaluate();
                if (_codeLine.Length > 0)
                {
                    _codeLine.Append("   ");
                }

                _codeLine.Append(condition.name).Append(' ')
                    .Append(passed ? "<color=#43D9A3>true</color>" : "<color=#FF5C7A>false</color>");
            }

            string line = _codeLine.Length > 0 ? _codeLine.ToString() : "no NeoCondition assigned";
            if (_codeResultText != null)
            {
                _codeResultText.text = line;
            }

            Log("NeoCondition.Evaluate(): " + line);
        }

        /// <summary>
        ///     Toggle target: sets <see cref="Time.timeScale" /> to 0 (paused) or 1. Conditions with Use Unscaled Time
        ///     (and EveryFrame mode) keep checking while paused; the previous scale is restored when the demo is disabled.
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (paused)
            {
                Time.timeScale = 0f;
                _pausedByDemo = true;
                Log("Time.timeScale = 0: the conditions still check (Use Unscaled Time)");
                return;
            }

            ReleasePause();
            Log("Time.timeScale = 1");
        }

        private void ReleasePause()
        {
            if (!_pausedByDemo)
            {
                return;
            }

            Time.timeScale = 1f;
            _pausedByDemo = false;
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log.Log(message);
            }
        }
    }
}
