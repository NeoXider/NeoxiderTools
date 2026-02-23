using Neo.Reactive;
using Neo.Save;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [CreateFromMenu("Neoxider/Tools/Components/ScoreManager")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(ScoreManager))]
    [NeoDoc("Tools/Components/ScoreManager.md")]
    public class ScoreManager : Singleton<ScoreManager>
    {
        [SerializeField] private string _keySave = "BestScore";

        [Space(20)] [SerializeField] [GUIColor(1, 0, 1)]
        private int score;

        [GUIColor(0, 1, 1)] [SerializeField] private int _bestScore;
        [SerializeField] private int _targetScore;

        [Space] [Header("Stars")] public bool useProgress = true;
        public float[] starScores = { 0.25f, 0.5f, 0.75f };

        [Tooltip("Reactive score; subscribe via Score.OnChanged")]
        public ReactivePropertyInt Score = new();
        [Tooltip("Reactive best score; subscribe via BestScore.OnChanged")]
        public ReactivePropertyInt BestScore = new();
        [Tooltip("Reactive target score; subscribe via TargetScore.OnChanged")]
        public ReactivePropertyInt TargetScore = new();
        [Tooltip("Reactive progress (0-1); subscribe via Progress.OnChanged")]
        public ReactivePropertyFloat Progress = new();
        [Tooltip("Reactive star count; subscribe via CountStarsReactive.OnChanged")]
        public ReactivePropertyInt CountStarsReactive = new();

        [Space] [Header("Text")] public SetText[] setTextBestScores;
        public SetText[] setTextScore;

        [Space] public TMP_Text[] textBestScores;
        public TMP_Text[] textScores;
        private string _cachedBestScoreString;
        private int _cachedBestScoreValue = int.MinValue;

        // Кэшированные строки для избежания аллокаций
        private string _cachedScoreString;
        private int _cachedScoreValue = int.MinValue;

        private int _countStars;
        private int _lastCountStars;

        public int BestScoreValue
        {
            get => _bestScore;
            private set
            {
                _bestScore = value;
                BestScore.Value = value;
            }
        }

        public int ScoreValue
        {
            get => score;
            private set
            {
                score = value;
                Score.Value = value;
                Progress.Value = _targetScore > 0 ? Mathf.Clamp01((float)score / _targetScore) : 0f;
                CountStars = GetCountStars();
            }
        }

        public int TargetScoreValue
        {
            get => _targetScore;
            set
            {
                _targetScore = value;
                TargetScore.Value = value;
                Progress.Value = _targetScore > 0 ? Mathf.Clamp01((float)score / _targetScore) : 0f;
            }
        }

        public bool IsTarget => score >= _targetScore;
        public float ProgressValue => Progress.CurrentValue;
        /// <summary>Количество звёзд (для NeoCondition и рефлексии).</summary>
        public int CountStarsValue => (int)CountStarsReactive.CurrentValue;

        public int CountStars
        {
            get => _countStars;
            set
            {
                _lastCountStars = _countStars;
                _countStars = value;
                CountStarsReactive.Value = value;
            }
        }

        protected override void Init()
        {
            base.Init();
            BestScoreValue = SaveProvider.GetInt(_keySave);
            ScoreValue = 0;
            CountStars = 0;
            SetBestScoreText();
            SetScoreText();
        }

        /// <summary>
        ///     Добавляет очки к текущему счету и опционально обновляет лучший результат.
        /// </summary>
        [Button]
        public void Add(int amount, bool updateBestScore = true)
        {
            Set(score + amount, updateBestScore);
        }

        [Button]
        public void Add(int amount)
        {
            Set(score + amount);
        }

        /// <summary>
        ///     Устанавливает точное количество очков и опционально обновляет лучший результат.
        /// </summary>
        [Button]
        public void Set(int amount, bool updateBestScore = true)
        {
            ScoreValue = amount;
            SetScoreText();
            if (updateBestScore)
            {
                SetBestScore();
            }
        }

        /// <summary>
        ///     Обновляет лучший результат: без аргумента — из текущего счёта; с аргументом — заданное значение (если больше текущего рекорда).
        /// </summary>
        [Button]
        public void SetBestScore(int? candidate = null)
        {
            int value = candidate ?? score;
            if (value <= _bestScore)
            {
                return;
            }

            BestScoreValue = value;
            SaveProvider.SetInt(_keySave, _bestScore);
            SetBestScoreText();
        }

        /// <summary>
        ///     Обновляет текстовые поля лучшего результата с кэшированием строки.
        /// </summary>
        private void SetBestScoreText()
        {
            // Кэшируем строку только при изменении значения
            if (_cachedBestScoreValue != _bestScore)
            {
                _cachedBestScoreValue = _bestScore;
                _cachedBestScoreString = _bestScore.ToString();
            }

            if (textBestScores != null)
            {
                foreach (TMP_Text text in textBestScores)
                {
                    if (text != null)
                    {
                        text.text = _cachedBestScoreString;
                    }
                }
            }

            if (setTextBestScores != null)
            {
                foreach (SetText text in setTextBestScores)
                {
                    if (text != null)
                    {
                        text.Set(_bestScore);
                    }
                }
            }
        }

        /// <summary>
        ///     Обновляет текстовые поля текущего счета с кэшированием строки.
        /// </summary>
        private void SetScoreText()
        {
            // Кэшируем строку только при изменении значения
            if (_cachedScoreValue != score)
            {
                _cachedScoreValue = score;
                _cachedScoreString = score.ToString();
            }

            if (textScores != null)
            {
                foreach (TMP_Text text in textScores)
                {
                    if (text != null)
                    {
                        text.text = _cachedScoreString;
                    }
                }
            }

            if (setTextScore != null)
            {
                foreach (SetText text in setTextScore)
                {
                    if (text != null)
                    {
                        text.Set(score);
                    }
                }
            }
        }

        /// <summary>
        ///     Сбрасывает текущий счет до нуля.
        /// </summary>
        [Button]
        public void ResetScore()
        {
            ScoreValue = 0;
            SetScoreText();
        }

        [Button]
        public void ResetBestScore()
        {
            BestScoreValue = 0;
            SaveProvider.DeleteKey(_keySave);
        }

        public int GetCountStars()
        {
            return GetCountStars(starScores, useProgress);
        }

        /// <summary>
        ///     Получение количества звезд по количеству очков.
        /// </summary>
        /// <param name="starScores">Массив пороговых значений для получения звезд.</param>
        /// <param name="useProgress">Использовать прогресс (0-1) или абсолютные значения очков.</param>
        /// <param name="score">Счет для расчета (по умолчанию используется текущий).</param>
        /// <returns>Количество полученных звезд.</returns>
        public int GetCountStars(float[] starScores, bool useProgress = true, int? score = null)
        {
            if (score == null)
            {
                score = this.score;
            }

            int stars = 0;
            for (int i = 0; i < starScores.Length; i++)
            {
                if (IsStar(starScores[i], useProgress, score))
                {
                    stars++;
                }
                else
                {
                    break;
                }
            }

            return stars;
        }

        public bool IsStar(float target, bool useProgress = true, int? score = null)
        {
            if (score == null)
            {
                score = this.score;
            }

            if (useProgress)
            {
                return Mathf.Clamp01((float)score / _targetScore) >= target;
            }

            return score >= target;
        }
    }
}