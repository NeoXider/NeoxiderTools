using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    public class ScoreManager : Singleton<ScoreManager>
    {
        [SerializeField]
        private string _keySave = "BestScore";

        public TMP_Text[] textScores;
        public TMP_Text[] textBestScores;

        public UnityEvent<int> OnValueChange;
        public UnityEvent<int> OnBestValueChanged;

        private int _currentScore;
        private int _bestScore;

        public int bestScore => _bestScore;
        public int currentScore => _currentScore;

        protected override void Init()
        {
            base.Init();
            _bestScore = PlayerPrefs.GetInt(_keySave, 0);
            _currentScore = 0;
            OnValueChange?.Invoke(_currentScore);
            OnBestValueChanged?.Invoke(_bestScore);
            SetBestScoreText();
            SetScoreText();
        }

        private void Start()
        {

        }

        /// <summary>
        /// ���������� ���� � ��������� ������ ������
        /// </summary>
        public void AddScore(int amount, bool updateBestScore = false)
        {
            _currentScore += amount;
            SetScoreText();
            OnValueChange?.Invoke(_currentScore);

            if (updateBestScore)
            {
                SetBestScore();
            }
        }

        public void SetBestScore(int? score = 0)
        {
            if (score != null)
                score = _currentScore;
            
            if (score > _bestScore)
            {
                _bestScore = _currentScore;
                PlayerPrefs.SetInt(_keySave, _bestScore);
                SetBestScoreText();
                OnBestValueChanged?.Invoke(_bestScore);
            }
        }

        /// <summary>
        /// ���������� ����� ������� �����
        /// </summary>
        private void SetBestScoreText()
        {
            foreach (var text in textBestScores)
            {
                text.text = _bestScore.ToString();
            }
        }


        /// <summary>
        /// ���������� ����� �����
        /// </summary>
        private void SetScoreText()
        {
            foreach (var text in textScores)
            {
                text.text = _currentScore.ToString();
            }
        }

        /// <summary>
        /// ����� ����� (��������, ��� �������� �� ����� ������� ��� ��������)
        /// </summary>
        public void ResetScore()
        {
            _currentScore = 0;
            SetScoreText();
        }
    }
}
