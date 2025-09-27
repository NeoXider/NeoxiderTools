using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.UI
{
    [AddComponentMenu("Neoxider/UI/" + nameof(VariantView))]
    public class VariantView : MonoBehaviour
    {
        #region NESTED_CLASSES

        [Serializable]
        public class ImageVariant
        {
            public Image image;
            public Sprite[] sprites = new Sprite[0];
        }

        [Serializable]
        public class ImageColor
        {
            public Image image;
            public Color[] colors = new Color[0];
        }

        [Serializable]
        public class TmpColorTextVariant
        {
            public TMP_Text tmp;
            public bool use_text;
            public Color[] colors = new Color[0];
            public string[] texts = new string[0];
        }

        /// <summary>
        ///     Каждый GameObjectVariant имеет один массив objects,
        ///     длина которого = количеству состояний (_maxStates).
        ///     На индекс i включается objects[i], все остальные выключены.
        /// </summary>
        [Serializable]
        public class GameObjectVariant
        {
            public GameObject[] objects = new GameObject[0];
        }

        #endregion

        #region FIELDS

        [Header("Image / Sprite Variants")] [SerializeField]
        private ImageVariant[] _imageVariants = new ImageVariant[0];

        [Header("Image / Color Variants")] [SerializeField]
        private ImageColor[] _imageColors = new ImageColor[0];

        [Header("TMP_Text / Color / Text Variants")] [SerializeField]
        private TmpColorTextVariant[] _textColorVariants = new TmpColorTextVariant[0];

        [Space] [Header("GameObject Variants (Array)")] [SerializeField]
        private GameObjectVariant[] _objectVariants = new GameObjectVariant[0];

        [Space] [Header("State Index / Build Settings")] [SerializeField]
        private int _currentStateIndex;

        [SerializeField] private bool _isBuildPhase;

        [SerializeField] private int _maxStates;

        #endregion

        #region PROPERTIES

        /// <summary>
        ///     Текущий индекс состояния.
        /// </summary>
        public int currentStateIndex => _currentStateIndex;

        /// <summary>
        ///     Общее максимальное число состояний (автоматически вычисляется в OnValidate).
        /// </summary>
        public int MaxStates => _maxStates;

        #endregion

        #region PUBLIC_METHODS

        /// <summary>
        ///     Переход к следующему состоянию (currentStateIndex + 1).
        /// </summary>
        public void NextState()
        {
            ChangeState(_currentStateIndex + 1);
        }

        /// <summary>
        ///     Переход к предыдущему состоянию (currentStateIndex - 1).
        /// </summary>
        public void PreviousState()
        {
            ChangeState(_currentStateIndex - 1);
        }

        /// <summary>
        ///     Установить текущее состояние по индексу.
        /// </summary>
        public void SetCurrentState(int index)
        {
            if (index >= 0 && index < _maxStates)
            {
                _currentStateIndex = index;
                Visual();
            }
        }

        /// <summary>
        ///     Добавить (расширить) количество состояний до newStateCount (если оно больше current).
        /// </summary>
        public void AddState(int newStateCount)
        {
            if (!_isBuildPhase) return;
            if (newStateCount <= _maxStates) return;

            // Расширяем массивы до newStateCount
            foreach (var v in _imageVariants)
            {
                Array.Resize(ref v.sprites, newStateCount);
                if (_currentStateIndex < v.sprites.Length && v.image != null)
                    v.sprites[newStateCount - 1] = v.sprites[_currentStateIndex];
            }

            foreach (var c in _imageColors)
            {
                Array.Resize(ref c.colors, newStateCount);
                if (_currentStateIndex < c.colors.Length && c.image != null)
                    c.colors[newStateCount - 1] = c.colors[_currentStateIndex];
            }

            foreach (var t in _textColorVariants)
            {
                Array.Resize(ref t.colors, newStateCount);
                if (_currentStateIndex < t.colors.Length && t.tmp != null)
                    t.colors[newStateCount - 1] = t.colors[_currentStateIndex];

                if (t.use_text)
                {
                    Array.Resize(ref t.texts, newStateCount);
                    if (_currentStateIndex < t.texts.Length && t.tmp != null)
                        t.texts[newStateCount - 1] = t.texts[_currentStateIndex];
                }
            }

            // Для каждого GameObjectVariant
            foreach (var gv in _objectVariants)
            {
                Array.Resize(ref gv.objects, newStateCount);
                if (_currentStateIndex < gv.objects.Length && gv.objects[_currentStateIndex] != null)
                    gv.objects[newStateCount - 1] = gv.objects[_currentStateIndex];
            }

            _maxStates = newStateCount;
            _currentStateIndex = Math.Min(newStateCount - 1, _maxStates - 1);
        }

        /// <summary>
        ///     Полностью очистить все варианты (сделать массивы длины 0), index=0.
        /// </summary>
        public void ClearAllStates()
        {
            _currentStateIndex = 0;

            foreach (var v in _imageVariants)
                v.sprites = new Sprite[0];

            foreach (var c in _imageColors)
                c.colors = new Color[0];

            foreach (var t in _textColorVariants)
            {
                t.colors = new Color[0];
                if (t.use_text)
                    t.texts = new string[0];
            }

            foreach (var gv in _objectVariants) gv.objects = new GameObject[0];

            _maxStates = 0;
        }

        #endregion

        #region PRIVATE_METHODS

        private void ChangeState(int newIndex)
        {
            if (newIndex >= 0 && newIndex < _maxStates)
            {
                _currentStateIndex = newIndex;
                Visual();
            }
        }

        /// <summary>
        ///     Применяет визуальное состояние ко всем компонентам (Image, Color, TMP, GameObject).
        /// </summary>
        private void Visual()
        {
            ImageVisual();
            ImageColorVisual();
            TextColorVisual();
            VariantVisual();
        }

        private void ImageVisual()
        {
            foreach (var v in _imageVariants)
                if (_currentStateIndex < v.sprites.Length && v.image != null && v.sprites[_currentStateIndex] != null)
                    v.image.sprite = v.sprites[_currentStateIndex];
        }

        private void ImageColorVisual()
        {
            foreach (var c in _imageColors)
                if (_currentStateIndex < c.colors.Length && c.image != null)
                    c.image.color = c.colors[_currentStateIndex];
        }

        private void TextColorVisual()
        {
            foreach (var t in _textColorVariants)
                if (t.tmp != null)
                {
                    if (_currentStateIndex < t.colors.Length)
                        t.tmp.color = t.colors[_currentStateIndex];

                    if (t.use_text && _currentStateIndex < t.texts.Length)
                        t.tmp.text = t.texts[_currentStateIndex];
                }
        }

        private void VariantVisual()
        {
            // Для каждого GameObjectVariant, 
            // у нас массив objects длиной _maxStates.
            // На _currentStateIndex включаем, остальные отключаем.
            foreach (var gv in _objectVariants)
                for (var i = 0; i < gv.objects.Length; i++)
                    if (gv.objects[i] != null)
                        gv.objects[i].SetActive(i == _currentStateIndex);
        }

        private void OnValidate()
        {
            // Сначала вычисляем _maxStates заново
            _maxStates = 0;

            // 1) ImageVariant (спрайты)
            foreach (var v in _imageVariants)
                _maxStates = Math.Max(_maxStates, v.sprites.Length);

            // 2) ImageColor (цвета)
            foreach (var c in _imageColors)
                _maxStates = Math.Max(_maxStates, c.colors.Length);

            // 3) TMP (цвет, текст)
            foreach (var t in _textColorVariants)
            {
                _maxStates = Math.Max(_maxStates, t.colors.Length);
                if (t.use_text)
                    _maxStates = Math.Max(_maxStates, t.texts.Length);
            }

            // 4) GameObjectVariant[] – 
            // у каждого gv один массив gv.objects
            foreach (var gv in _objectVariants) _maxStates = Math.Max(_maxStates, gv.objects.Length);

            // Ресайзим массивы до _maxStates
            foreach (var v in _imageVariants)
                ResizeArray(ref v.sprites, _maxStates);

            foreach (var c in _imageColors)
                ResizeArray(ref c.colors, _maxStates);

            foreach (var t in _textColorVariants)
            {
                ResizeArray(ref t.colors, _maxStates);
                if (t.use_text)
                    ResizeArray(ref t.texts, _maxStates);
            }

            foreach (var gv in _objectVariants) ResizeArray(ref gv.objects, _maxStates);

            // Если BuildPhase включён, сохраняем текущее значение в последний слот
            if (_isBuildPhase && _maxStates > 0)
            {
                var lastIndex = _maxStates - 1;

                foreach (var v in _imageVariants)
                    if (v.sprites.Length > lastIndex && v.image != null)
                        v.sprites[lastIndex] = v.image.sprite;

                foreach (var c in _imageColors)
                    if (c.colors.Length > lastIndex && c.image != null)
                        c.colors[lastIndex] = c.image.color;

                foreach (var t in _textColorVariants)
                {
                    if (t.colors.Length > lastIndex && t.tmp != null)
                        t.colors[lastIndex] = t.tmp.color;

                    if (t.use_text && t.texts.Length > lastIndex && t.tmp != null)
                        t.texts[lastIndex] = t.tmp.text;
                }
            }

            _currentStateIndex = Mathf.Clamp(_currentStateIndex, 0, _maxStates - 1);

            ChangeState(_currentStateIndex);
        }

        private void ResizeArray<T>(ref T[] array, int newSize)
        {
            if (newSize < 0) return;

            var newArray = new T[newSize];
            for (var i = 0; i < Math.Min(array.Length, newSize); i++) newArray[i] = array[i];
            array = newArray;
        }
    }

    #endregion
}