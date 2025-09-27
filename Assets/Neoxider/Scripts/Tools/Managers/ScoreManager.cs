using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Neo.Tools
{
    public class ScoreManager : Singleton<ScoreManager>
    {
        [SerializeField] private string _keySave = "BestScore";

        [Space] [Header("Stars")] public bool useProgress = true;
        public float[] starScores = { 0.25f, 0.5f, 0.75f };

        [Space] public UnityEvent<int> OnValueChange = new();
        public UnityEvent<int> OnBestValueChange = new();
        public UnityEvent<int> OnTargetChange = new();

        [Space] public UnityEvent<float> OnProgressChange = new();
        public UnityEvent<int> OnStarChange = new();

        [FormerlySerializedAs("_currentScore")] [Space(20)] [Header("Debug")] [SerializeField]
        private int score;

        [SerializeField] private int _bestScore;
        [SerializeField] private int _targetScore;
        private int _countStars;

        private int _lastCountStars;
        public SetText[] setTextBestScores;
        public SetText[] setTextScore;

        [Space] [Header("Best Score")] public TMP_Text[] textBestScores;

        public TMP_Text[] textScores;

        public int BestScore
        {
            get => _bestScore;
            private set
            {
                _bestScore = value;
                OnBestValueChange?.Invoke(value);
            }
        }

        public int Score
        {
            get => score;
            private set
            {
                score = value;
                OnValueChange?.Invoke(value);
                OnProgressChange.Invoke(Progress);

                CountStars = GetCountStars();
            }
        }

        public int TargetScore
        {
            get => _targetScore;
            set
            {
                _targetScore = value;
                OnTargetChange?.Invoke(_targetScore);
            }
        }

        public bool IsTarget => score >= _targetScore;
        public float Progress => Mathf.Clamp01((float)score / _targetScore);

        public int CountStars
        {
            get => _countStars;
            set
            {
                if (_lastCountStars != value)
                {
                    _lastCountStars = _countStars;
                    OnStarChange?.Invoke(value);
                }

                _countStars = value;
            }
        }

        private void Start()
        {
        }

        protected override void Init()
        {
            base.Init();
            BestScore = PlayerPrefs.GetInt(_keySave, 0);
            Score = 0;
            CountStars = 0;

            SetBestScoreText();
            SetScoreText();
        }

        /// <summary>
        ///     ���������� ���� � ��������� ������ ������
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#else
        [Button]
#endif
        public void Add(int amount, bool updateBestScore = true)
        {
            Set(score + amount, updateBestScore);
        }

#if ODIN_INSPECTOR
        [Button]
#else
        [Button]
#endif
        public void Add(int amount)
        {
            Set(score + amount);
        }

        /// <summary>
        ///     ���������� ���� � ��������� ������ ������
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#else
        [Button]
#endif
        public void Set(int amount, bool updateBestScore = true)
        {
            Score = amount;
            SetScoreText();

            if (updateBestScore) SetBestScore();
        }

#if ODIN_INSPECTOR
        [Button]
#else
        [Button]
#endif
        public void SetBestScore(int? score = 0)
        {
            if (score != null)
                score = this.score;

            if (score > _bestScore)
            {
                BestScore = this.score;
                PlayerPrefs.SetInt(_keySave, _bestScore);
                SetBestScoreText();
            }
        }

        /// <summary>
        ///     ���������� ����� ������� �����
        /// </summary>
        private void SetBestScoreText()
        {
            if (textBestScores != null)
                foreach (var text in textBestScores)
                    text.text = _bestScore.ToString();

            if (setTextBestScores != null)
                foreach (var text in setTextBestScores)
                    text.Set(_bestScore);
        }


        /// <summary>
        ///     ���������� ����� �����
        /// </summary>
        private void SetScoreText()
        {
            if (textScores != null)
                foreach (var text in textScores)
                    text.text = score.ToString();

            if (setTextScore != null)
                foreach (var text in setTextScore)
                    text.Set(score);
        }

        /// <summary>
        ///     ����� ����� (��������, ��� �������� �� ����� ������� ��� ��������)
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#else
        [Button]
#endif
        public void ResetScore()
        {
            Score = 0;
            OnValueChange?.Invoke(score);
            SetScoreText();
        }

#if ODIN_INSPECTOR
        [Button]
#else
        [Button]
#endif
        public void ResetBestScore()
        {
            BestScore = 0;
            PlayerPrefs.DeleteKey(_keySave);
        }

        public int GetCountStars()
        {
            return GetCountStars(starScores, useProgress);
        }

        /// <summary>
        ///     Получение количества звезд по количеству очков
        ///     параметры:
        ///     пример {500, 2500, 5000}, 3500 => 2, 6000 => 3
        ///     возвращает количество звезд
        /// </summary>
        public int GetCountStars(float[] starScores, bool useProgress = true, int? score = null)
        {
            if (score == null)
                score = this.score;

            var stars = 0;
            for (var i = 0; i < starScores.Length; i++)
                if (IsStar(starScores[i], useProgress, score))
                    stars++;
                else
                    break;

            return stars;
        }

        public bool IsStar(float target, bool useProgress = true, int? score = null)
        {
            if (score == null)
                score = this.score;

            if (useProgress)
                return Mathf.Clamp01((float)score / _targetScore) >= target;

            return score >= target;
        }
    }
}