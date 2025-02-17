using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.UI
{
    [AddComponentMenu("Neoxider/UI/" + nameof(VariantView))]
    public class VariantView : MonoBehaviour
    {
        [System.Serializable]
        public class ImageVariant
        {
            public Image image;
            public Sprite[] sprites = new Sprite[0];
        }

        [System.Serializable]
        public class ImageColor
        {
            public Image image;
            public Color[] colors = new Color[0];
        }

        [System.Serializable]
        public class TmpColorTextVariant
        {
            public TMP_Text tmp;
            public bool use_text = false;
            public Color[] colors = new Color[0];
            public string[] texts = new string[0];
        }

        [System.Serializable]
        public class GameObjectVariant
        {
            public GameObject[][] objects = new GameObject[0][];
        }

        public ImageVariant[] _imageVariants = new ImageVariant[0];
        public ImageColor[] _imageColors = new ImageColor[0];
        public TmpColorTextVariant[] _textColorVariants = new TmpColorTextVariant[0];
        public GameObjectVariant _objectVariants = new GameObjectVariant();

        [SerializeField] private int _currentStateIndex;
        [SerializeField] private bool _isBuildPhase;

        // Добавляем поле для максимального количества вариантов
        [SerializeField]
        private int _maxStates;

        public int currentStateIndex => _currentStateIndex;

        public void NextState()
        {
            ChangeState(_currentStateIndex + 1);
        }

        public void PreviousState()
        {
            ChangeState(_currentStateIndex - 1);
        }

        private void ChangeState(int newIndex)
        {
            if (newIndex >= 0 && newIndex < _maxStates)
            {
                _currentStateIndex = newIndex;
                Visual();
            }
        }

        public void SetCurrentState(int index)
        {
            if (index >= 0 && index < _maxStates)
            {
                _currentStateIndex = index;
                Visual();
            }
        }

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
            {
                if (_currentStateIndex < v.sprites.Length && v.image != null)
                    v.image.sprite = v.sprites[_currentStateIndex];
            }
        }

        private void ImageColorVisual()
        {
            foreach (var c in _imageColors)
            {
                if (_currentStateIndex < c.colors.Length && c.image != null)
                    c.image.color = c.colors[_currentStateIndex];
            }
        }

        private void TextColorVisual()
        {
            foreach (var t in _textColorVariants)
            {
                if (t.tmp != null)
                {
                    if (_currentStateIndex < t.colors.Length)
                        t.tmp.color = t.colors[_currentStateIndex];
                    if (t.use_text && _currentStateIndex < t.texts.Length)
                        t.tmp.text = t.texts[_currentStateIndex];
                }
            }
        }

        private void VariantVisual()
        {
            foreach (var v in _objectVariants.objects)
            {
                for (int i = 0; i < v.Length; i++)
                {
                    if (v[i] != null)
                        v[i].SetActive(i == _currentStateIndex);
                }
            }
        }

        public void AddState(int newStateCount)
        {
            if (!_isBuildPhase) return;
            if (newStateCount <= _maxStates) return;

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

            foreach (var v in _objectVariants.objects)
            {
                var newObjects = new GameObject[v.Length];
                Array.Copy(v, newObjects, Math.Min(v.Length, newStateCount));
                Array.Resize(ref newObjects, newStateCount);
                if (_currentStateIndex < v.Length && v[_currentStateIndex] != null)
                    newObjects[newStateCount - 1] = v[_currentStateIndex];

                _objectVariants.objects[Array.IndexOf(_objectVariants.objects, v)] = newObjects;
            }

            _currentStateIndex = Math.Min(newStateCount - 1, _maxStates - 1);
        }

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

            _objectVariants.objects = new GameObject[0][];
        }

        private void OnValidate()
        {
            // Определяем максимальное количество состояний
            foreach (var v in _imageVariants) _maxStates = Math.Max(_maxStates, v.sprites.Length);
            foreach (var c in _imageColors) _maxStates = Math.Max(_maxStates, c.colors.Length);
            foreach (var t in _textColorVariants)
            {
                _maxStates = Math.Max(_maxStates, t.colors.Length);
                if (t.use_text)
                    _maxStates = Math.Max(_maxStates, t.texts.Length);
            }
            for (int i = 0; i < _objectVariants.objects.Length; i++)
                _maxStates = Math.Max(_maxStates, _objectVariants.objects[i].Length);

            // Resize all states to the maximum number of states
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
            for (int i = 0; i < _objectVariants.objects.Length; i++)
                ResizeArray(ref _objectVariants.objects[i], _maxStates);

            // Automatically set properties if they are not empty and _isBuildPhase is true
            if (_isBuildPhase)
            {
                foreach (var v in _imageVariants)
                {
                    if (v.sprites.Length > 0 && v.image != null)
                        v.sprites[v.sprites.Length - 1] = v.image.sprite;
                }

                foreach (var c in _imageColors)
                {
                    if (c.colors.Length > 0 && c.image != null)
                        c.colors[c.colors.Length - 1] = c.image.color;
                }

                foreach (var t in _textColorVariants)
                {
                    if (t.colors.Length > 0 && t.tmp != null)
                        t.colors[t.colors.Length - 1] = t.tmp.color;

                    if (t.use_text && t.texts.Length > 0 && t.tmp != null)
                        t.texts[t.texts.Length - 1] = t.tmp.text;
                }
            }

            // Ensure _currentStateIndex is within valid range
            _currentStateIndex = Math.Min(_currentStateIndex, _maxStates - 1);
        }

        private void ResizeArray<T>(ref T[] array, int newSize)
        {
            if (newSize < 0) return;

            var newArray = new T[newSize];
            for (int i = 0; i < Math.Min(array.Length, newSize); i++)
                newArray[i] = array[i];

            array = newArray;
        }

        // Метод для применения текущего состояния при изменении галочки
        private void ApplyCurrentState()
        {
            if (_isBuildPhase)
            {
                Visual();
            }
        }

        #if UNITY_EDITOR
        [UnityEditor.CustomPropertyDrawer(typeof(TmpColorTextVariant))]
        public class TmpColorTextVariantDrawer : UnityEditor.PropertyDrawer
        {
            public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
            {
                base.OnGUI(position, property, label);

                var useTextProp = property.FindPropertyRelative("use_text");
                if (UnityEditor.EditorGUI.EndChangeCheck() && useTextProp.boolValue)
                {
                    var variantView = (VariantView)property.serializedObject.targetObject;
                    variantView.ApplyCurrentState();
                }
            }
        }
        #endif
    }
}