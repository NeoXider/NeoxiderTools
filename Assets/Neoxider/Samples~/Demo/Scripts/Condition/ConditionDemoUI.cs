using Neo.Tools;
using TMPro;
using UnityEngine;

#pragma warning disable CS0618 // WHY: Condition demo keeps the legacy Health sample contract until the scene is migrated.

namespace Neo.Demo.Condition
{
    /// <summary>
    ///     UI controller for the NeoCondition demo scene.
    ///     Drives GameOver, Win, Warning panels and status text.
    ///     HP/Score text updates use Health.OnChange and ScoreManager.textScores.
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

        private void Start()
        {
            HideGameOver();
            HideWin();
            HideWarning();
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
    }
}
