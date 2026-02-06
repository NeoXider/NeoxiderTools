using Neo.Tools;
using TMPro;
using UnityEngine;

namespace Neo.Demo.Condition
{
    /// <summary>
    ///     UI контроллер для демо-сцены NeoCondition.
    ///     Управляет панелями (GameOver, Win, Warning) и статусом.
    ///     Обновление текста HP/Score — через встроенные поля Health.OnChange и ScoreManager.textScores.
    /// </summary>
    [AddComponentMenu("Neo/Demo/Condition/ConditionDemoUI")]
    public class ConditionDemoUI : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Health _health;

        [SerializeField] private ScoreManager _scoreManager;

        [Header("UI Panels")] [SerializeField] private GameObject _gameOverPanel;

        [SerializeField] private GameObject _winPanel;
        [SerializeField] private GameObject _warningIcon;

        [Header("Status")] [SerializeField] private TMP_Text _statusText;

        private void Start()
        {
            HideGameOver();
            HideWin();
            HideWarning();
        }

        // --- Panel control (void methods for persistent listeners) ---

        public void ShowGameOver()
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
            }

            SetStatus("GAME OVER!");
        }

        public void HideGameOver()
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }
        }

        public void ShowWin()
        {
            if (_winPanel != null)
            {
                _winPanel.SetActive(true);
            }

            SetStatus("YOU WIN!");
        }

        public void HideWin()
        {
            if (_winPanel != null)
            {
                _winPanel.SetActive(false);
            }
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

        // --- Actions (void methods for button persistent listeners) ---

        public void DealDamage()
        {
            if (_health != null)
            {
                _health.TakeDamage(25);
            }
        }

        public void HealPlayer()
        {
            if (_health != null)
            {
                _health.Heal(10);
            }
        }

        public void AddScore()
        {
            if (_scoreManager != null)
            {
                _scoreManager.Add(25);
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
    }
}