using DG.Tweening;
using Neo.Extensions;
using Neo.Reactive;
using Neo.Save;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Bonus
{
    [NeoDoc("Bonus/Collection/Box.md")]
    [CreateFromMenu("Neoxider/Bonus/Box")]
    [AddComponentMenu("Neoxider/" + "Bonus/" + nameof(Box))]
    public class Box : MonoBehaviour
    {
        [SerializeField] private string _saveName = "BoxPrize";

        [Space] [Header("View")] [SerializeField]
        private Image _boxImage;

        [SerializeField] private Sprite[] _boxSpritesCloseOpen = new Sprite[2];
        [SerializeField] private Image _bar;
        [SerializeField] private TMP_Text _textProgress;
        [SerializeField] private TMP_Text _textMaxProgress;
        [SerializeField] private TMP_Text _textProgressMaxProgress;

        [Space] [Header("Animation and prize")] [SerializeField]
        private GameObject _animItem;

        [SerializeField] private Ease _ease = Ease.InCubic;
        [SerializeField] private Image _itemPrize;

        [SerializeField] private float _addProgress = 100f;
        [SerializeField] private float _maxProgress = 300f;
        [SerializeField] private float _progress;

        [Space] public UnityEvent OnTakePrize;
        public UnityEvent OnProgressReached;
        public UnityEvent OnProgressNotReached;
        public UnityEvent<bool> OnChangeProgress;

        [Tooltip("Reactive progress value; subscribe via Progress.OnChanged")]
        public ReactivePropertyFloat Progress = new();

        public float AddProgressAmount => _addProgress;
        public float MaxProgress => _maxProgress;
        /// <summary>Текущий прогресс (для NeoCondition и рефлексии).</summary>
        public float ProgressValue => Progress.CurrentValue;

        public float progress
        {
            get => _progress;
            set
            {
                _progress = value;
                SaveProvider.SetFloat(_saveName + nameof(progress), value);
                Progress.Value = value;
            }
        }

        public bool CheckProgress => progress >= _maxProgress;

        private void Awake()
        {
            _progress = SaveProvider.GetFloat(_saveName + nameof(progress));
            Progress.Value = _progress;
        }

        private void OnEnable()
        {
            if (_animItem != null)
            {
                _animItem.transform.localScale = Vector3.zero;
            }

            Visual();
            Events();
        }

        public void AddProgress()
        {
            AddProgress(_addProgress);
        }

        [Button]
        public void AddProgress(float amount)
        {
            ChangeProgress(amount);
        }

        [Button]
        public void ChangeProgress(float amount)
        {
            progress += amount;
            Events();
        }

        private void Events()
        {
            bool progressOpen = CheckProgress;

            OnChangeProgress?.Invoke(progressOpen);

            if (progressOpen)
            {
                OnProgressReached?.Invoke();
            }
            else
            {
                OnProgressNotReached?.Invoke();
            }
        }

        [Button]
        public void TakePrize()
        {
            if (CheckProgress)
            {
                ItemCollectionData itemData = Collection.I.GetPrize();

                if (itemData == null)
                {
                    return;
                }

                if (_itemPrize != null)
                {
                    _itemPrize.sprite = itemData.Sprite;
                }

                if (_animItem != null)
                {
                    _animItem.transform.DOScale(1, 2).SetEase(_ease);
                }

                ChangeProgress(-_maxProgress);
                progress = Mathf.Max(0, progress);

                Visual(true);

                OnTakePrize?.Invoke();
            }
        }

        [Button]
        private void Visual(bool openBox = false)
        {
            if (_bar != null)
            {
                _bar.fillAmount = progress / _maxProgress;
            }

            if (_boxImage != null)
            {
                _boxImage.sprite = _boxSpritesCloseOpen[openBox ? 1 : 0];
                _boxImage.SetNativeSize();
            }

            if (_textProgress != null)
            {
                _textProgress.text = $"{progress.RoundToDecimal(0)}";
            }

            if (_textMaxProgress != null)
            {
                _textMaxProgress.text = $"{_maxProgress.RoundToDecimal(0)}";
            }

            if (_textProgressMaxProgress != null)
            {
                _textProgressMaxProgress.text = $"{progress.RoundToDecimal(0)}/{_maxProgress.RoundToDecimal(0)}";
            }
        }
    }
}