using DG.Tweening;
using Neo.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Bonus
{
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

        public float addProgress = 100;
        public float maxProgress = 300;
        [SerializeField] private float _progress;

        [Space] public UnityEvent OnTakePrize;
        public UnityEvent OnProgressReached;
        public UnityEvent OnProgressNotReached;
        public UnityEvent<bool> OnChangeProgress;

        public float progress
        {
            get => _progress;
            set
            {
                _progress = value;
                PlayerPrefs.SetFloat(_saveName + nameof(progress), value);
            }
        }

        public bool CheckProgress => progress >= maxProgress;

        private void Awake()
        {
            _progress = PlayerPrefs.GetFloat(_saveName + nameof(progress), 0);
        }

        private void OnEnable()
        {
            if (_animItem != null)
                _animItem.transform.localScale = Vector3.zero;

            Visual();
            Events();
        }

        public void AddProgress()
        {
            AddProgress(addProgress);
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
            var progressOpen = CheckProgress;

            OnChangeProgress?.Invoke(progressOpen);

            if (progressOpen)
                OnProgressReached?.Invoke();
            else
                OnProgressNotReached?.Invoke();
        }

[Button]
        public void TakePrize()
        {
            if (CheckProgress)
            {
                var itemData = Collection.I.GetPrize();

                if (itemData == null)
                    return;

                if (_itemPrize != null)
                    _itemPrize.sprite = itemData.sprite;

                if (_animItem != null)
                    _animItem.transform.DOScale(1, 2).SetEase(_ease);

                ChangeProgress(-maxProgress);

                Visual(true);
                
                OnTakePrize?.Invoke();
            }
        }

[Button]
        private void Visual(bool openBox = false)
        {
            if (_bar != null) _bar.fillAmount = progress / maxProgress;

            if (_boxImage != null)
            {
                _boxImage.sprite = _boxSpritesCloseOpen[openBox ? 1 : 0];
                _boxImage.SetNativeSize();
            }

            if (_textProgress != null) _textProgress.text = $"{progress.RoundToDecimal(0)}";

            if (_textMaxProgress != null) _textMaxProgress.text = $"{maxProgress.RoundToDecimal(0)}";

            if (_textProgressMaxProgress != null)
                _textProgressMaxProgress.text = $"{progress.RoundToDecimal(0)}/{maxProgress.RoundToDecimal(0)}";
        }
    }
}