using Neo.Reactive;
using Neo.Save;
using TMPro;
using UnityEngine;

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

        // Cached strings to avoid allocations
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

        /// <summary>Star count (for NeoCondition and reflection).</summary>
        public int CountStarsValue => CountStarsReactive.CurrentValue;

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
        ///     Adds points to the current score and optionally updates best score.
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
        ///     Sets the exact score and optionally updates best score.
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
        ///     Updates best score: no argument uses current score; with argument uses that value if higher than record.
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
        ///     Updates best-score text fields with string caching.
        /// </summary>
        private void SetBestScoreText()
        {
            // Cache string only when value changes
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
        ///     Updates current score text fields with string caching.
        /// </summary>
        private void SetScoreText()
        {
            // Cache string only when value changes
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
        ///     Resets current score to zero.
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
        ///     Computes star count from score thresholds.
        /// </summary>
        /// <param name="starScores">Threshold values for earning stars.</param>
        /// <param name="useProgress">Use progress (0–1) or absolute score values.</param>
        /// <param name="score">Score to evaluate (defaults to current score).</param>
        /// <returns>Number of stars earned.</returns>
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
                if (_targetScore <= 0)
                {
                    return false;
                }

                return Mathf.Clamp01((float)score / _targetScore) >= target;
            }

            return score >= target;
        }
    }
}
