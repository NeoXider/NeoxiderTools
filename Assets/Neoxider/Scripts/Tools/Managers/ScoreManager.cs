using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    public class ScoreManager : Singleton<ScoreManager>
    {
        [SerializeField] private string _keySave = "BestScore";

        public TMP_Text[] textScores;
        public TMP_Text[] textBestScores;
        public SetText[] setTextScore;
        public SetText[] setTextBestScores;

        public UnityEvent<int> OnValueChange = new();
        public UnityEvent<int> OnBestValueChanged = new();

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
        [Button]
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
            if (textBestScores != null)
                foreach (var text in textBestScores)
                {
                    text.text = _bestScore.ToString();
                }

            if (setTextBestScores != null)
                foreach (var text in setTextBestScores)
                {
                    text.Set(_bestScore);
                }
        }


        /// <summary>
        /// ���������� ����� �����
        /// </summary>
        private void SetScoreText()
        {
            if (textScores != null)
                foreach (var text in textScores)
                {
                    text.text = _currentScore.ToString();
                }

            if (setTextScore != null)
                foreach (var text in setTextScore)
                {
                    text.Set(_currentScore);
                }
        }

        /// <summary>
        /// ����� ����� (��������, ��� �������� �� ����� ������� ��� ��������)
        /// </summary>
        public void ResetScore()
        {
            _currentScore = 0;
            OnValueChange?.Invoke(_currentScore);
            SetScoreText();
        }

        /// <summary>
        /// Получение количества звезд по количеству очков
        /// параметры:
        /// пример {1, 2500, 5000}, 3500 => 2
        /// возвращает количество звезд
        /// </summary>
public int GetCountStars(int[] starScores, int? score = null)
{
    if (score == null)
        score = _currentScore;

    int stars = 0;
    for (int i = 0; i < starScores.Length; i++)
    {
        if (score >= starScores[i])
            stars++;
        else
            break;
    }
    return stars;
}
    }
}