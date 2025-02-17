using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace Neo
{
    namespace UI
    {
        [AddComponentMenu("Neoxider/" + "UI/" + nameof(Points))]
        public class Points : MonoBehaviour
        {
            [SerializeField] private Button[] _points;
            [SerializeField] private bool _findChildPoints = true;

            [Header("Setting points")]
            [SerializeField] private bool _interactablePoints = true;
            [SerializeField] private bool _imageSetNativeSize = true;

            [Space, Header("Visual")]
            [SerializeField] private Sprite _spritesOff;
            [SerializeField] private Sprite[] _spritesPoints;
            [SerializeField] private bool useOneSprite = true;
            [SerializeField] private Color[] _colors_off_on = { Color.white, Color.white };

            [Space, Header("Main settings")]
            [SerializeField] private bool _fill;
            [SerializeField] private bool _flip;
            [SerializeField] private bool _zeroPoints;

            [Space, SerializeField] private bool _visualClick = true;

            [Space, Header("point")]
            [SerializeField, Min(-1)] private int _id;
            [SerializeField] private bool _looping = false;
            [SerializeField] private bool useMaxInt = false;
            [SerializeField] private int _maxCount = 3;

            public int id
            {
                get { return _id; }
                set
                {
                    _id = value;
                    OnChangeId?.Invoke(id);
                }
            }

            [Space]
            public UnityEvent<int> OnChangeId;
            public UnityEvent<int> OnClickPoint;

            private void Awake()
            {
                for (int i = 0; i < _points.Length; i++)
                {
                    int index = i;
                    _points[i].onClick.AddListener(() => Click(index));
                }
            }

            private void Start()
            {
                SetPoint(_id);
            }

            private void OnDestroy()
            {
                for (int i = 0; i < _points.Length; i++)
                {
                    int index = i;
                    _points[i].onClick.RemoveAllListeners();
                }
            }

            public void SetPoint(int value)
            {
                this.id = SafeId(value);

                int id = _id;

                if (_zeroPoints)
                    id -= 1;

                if (_flip)
                    id = _points.Length - 1 - id;

                if (_spritesOff != null && _spritesPoints.Length > 0)
                {
                    for (int j = 0; j < _points.Length; j++)
                    {
                        int i = j;
                        bool activ = _fill ? (_flip ? id <= i : i <= id) : i == id;

                        int currentId = activ ? 1 : 0;//.ToInt();
                        Sprite sprite = null;

                        if (useOneSprite)
                            sprite = activ ? _spritesPoints[0] : _spritesOff;
                        else
                            sprite = activ ? _spritesPoints[j] : _spritesOff;


                        Color color = _colors_off_on[currentId];
                        _points[i].image.color = color;

                        if (sprite != null)
                        {
                            _points[i].image.sprite = sprite;

                            if (_imageSetNativeSize)
                                _points[i].image.SetNativeSize();
                        }
                    }
                }
            }

            public void SetPoint(float floatValue)
            {
                int maxIndex = _points.Length;
                int intValue = Mathf.RoundToInt(floatValue * maxIndex);
                if (floatValue == 1f)
                    intValue = maxIndex;

                SetPoint(intValue);
            }

            public void IncreaseId()
            {
                int count = GetCount();

                if (_looping)
                {
                    _id = (_id + 1) % count;
                }
                else
                {
                    _id = Mathf.Min(_id + 1, count);
                }
                SetPoint(_id);
            }

            public void DecreaseId()
            {
                int count = GetCount();

                if (_looping)
                {
                    _id = (_id - 1 + count) % count;
                }
                else
                {
                    _id = Mathf.Max(_id - 1, 0);
                }
                SetPoint(_id);
            }

            private int SafeId(int id)
            {
                id = Mathf.Clamp(id, -1, GetCount());

                return id;
            }

            private int GetCount()
            {
                int count = 0;

                if (useMaxInt)
                    count = _zeroPoints ? _maxCount : _maxCount - 1;
                else
                    count = _zeroPoints ? _points.Length : _points.Length - 1;

                return count;
            }

            public void SetInteractablePoints(bool interactable)
            {
                _interactablePoints = interactable;

                if (_points != null)
                    for (int i = 0; i < _points.Length; i++)
                    {
                        _points[i].enabled = interactable;
                    }
            }

            public void Click(int id)
            {
                if (_interactablePoints)
                {
                    int currentId = id;

                    if (_flip)
                    {
                        currentId = _points.Length - 1 - currentId;
                    }

                    if (_zeroPoints)
                    {
                        currentId += 1;
                    }

                    OnClickPoint?.Invoke(currentId);

                    if (_visualClick)
                        SetPoint(currentId);
                }
            }

            public void UpdatePoints()
            {
                if (useOneSprite)
                {
                    if (_spritesPoints != null && _spritesPoints.Length > 0 && _spritesPoints[0] != null)
                    {
                        Sprite sprite = _spritesPoints[0];
                        _spritesPoints = new Sprite[] { sprite };
                    }
                    else
                    {
                        _spritesPoints = new Sprite[1];
                    }
                }

                if (_findChildPoints)
                    CheckAndSetButtons();

                SetInteractablePoints(_interactablePoints);
                SetVisual();
            }

            private void CheckAndSetButtons()
            {
                List<Button> list = new List<Button>();
                int pointId = 0;


                for (int i = 0; i < transform.childCount; i++)
                {
                    if (transform.GetChild(i).TryGetComponent(out Button button))
                    {
                        if (pointId < _points.Length)
                        {
                            list.Add(button);
                            pointId++;
                        }
                        else
                        {
                        }
                    }
                }

                _points = list.ToArray();
            }

            private void SetVisual()
            {
                _id = SafeId(_id);
                SetPoint(_id);
            }

            private void OnTransformChildrenChanged()
            {
                UpdatePoints();
            }

            private void OnValidate()
            {
                UpdatePoints();
            }
        }
    }
}