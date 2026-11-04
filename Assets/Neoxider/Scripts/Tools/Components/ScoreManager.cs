using Neo.Reactive;
using Neo.Save;
using TMPro;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Scene score source: current score, persisted best score, target, normalized progress and stars, all
    ///     exposed as reactive properties, plus optional TMP / <see cref="SetText" /> outputs.
    /// </summary>
    [CreateFromMenu("Neoxider/Tools/Components/ScoreManager")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(ScoreManager))]
    [NeoDoc("Tools/Components/ScoreManager.md")]
    public class ScoreManager : Singleton<ScoreManager>
    {
        [SerializeField] [Tooltip("SaveProvider key of the best score. Use a unique key per score source.")]
        private string _keySave = "BestScore";

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

        // WHY: Cached strings to avoid allocations
        private string _cachedScoreString;
        private int _cachedScoreValue = int.MinValue;

        private int _countStars;
        private int _lastCountStars;

        /// <summary>Persisted best score. Raised through <see cref="SetBestScore" />, cleared by <see cref="ResetBestScore" />.</summary>
        public int BestScoreValue
        {
            get => _bestScore;
            private set
            {
                _bestScore = value;
                BestScore.Value = value;
            }
        }

        /// <summary>Current score. Change it through <see cref="Set(int, bool)" /> / <see cref="Add(int, bool)" />.</summary>
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

        /// <summary>Target score; setting it refreshes progress and stars immediately.</summary>
        public int TargetScoreValue
        {
            get => _targetScore;
            set
            {
                _targetScore = value;
                TargetScore.Value = value;
                Progress.Value = _targetScore > 0 ? Mathf.Clamp01((float)score / _targetScore) : 0f;
                CountStars = GetCountStars();
            }
        }

        /// <summary>SaveProvider key used for the best score.</summary>
        public string SaveKey => _keySave;

        /// <summary><see langword="true" /> when the current score reached the target score.</summary>
        public bool IsTarget => score >= _targetScore;

        /// <summary>Current score divided by the target, clamped to 0..1 (0 without a positive target).</summary>
        public float ProgressValue => Progress.CurrentValue;

        /// <summary>Star count (for NeoCondition and reflection).</summary>
        public int CountStarsValue => CountStarsReactive.CurrentValue;

        /// <summary>Current star count; recomputed on every score/target change, can be overridden explicitly.</summary>
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
            TargetScore.Value = _targetScore;
            // WHY: the ScoreValue setter already derives progress and stars for score 0; forcing CountStars to 0
            // afterwards disagreed with ResetScore() whenever a threshold is met at zero.
            ScoreValue = 0;
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

        private void SetBestScoreText()
        {
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

        private void SetScoreText()
        {
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
            SetBestScoreText();
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
            if (starScores == null)
            {
                return 0;
            }

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
