using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Neo.Bonus
{

    public class Row : MonoBehaviour
    {
        public int countSlotElement = 3;
        [Header("SlotElement x2")] public SlotElement[] SlotElements;
        public bool is_spinning { get => _isSpinning; }
        public SpeedControll speedControll = new();
        public float spaceY = 1;

        [SerializeField] private float _resetPosition;
        [SerializeField] private float[] yPositions;
        [SerializeField] private Sprite[] _sprites;
        [SerializeField] private Sprite[] _spritesEnd;

        [SerializeField] private float _timerSpin;
        [SerializeField] private float _positionTolerance = 0.5f;

        public bool _isSpinning = false;

        public UnityEvent OnStop = new UnityEvent();

        void Update() { }

        private IEnumerator SpinCoroutine()
        {
            _isSpinning = true;
            _timerSpin = 0;

            // Initial spin phase
            while (_timerSpin < speedControll.timeSpin)
            {
                _timerSpin += Time.deltaTime;
                MoveSlots(speedControll.speed);
                yield return null;
            }

            SetSpriteEnd();
            float newSpeed = speedControll.speed;

            // Deceleration phase
            while (((newSpeed > speedControll.minSpeed) || speedControll.minSpeed == 0) && speedControll.changeEndSpeed != 0)
            {
                MoveSlots(newSpeed, false);
                newSpeed = Mathf.Max(newSpeed - speedControll.changeEndSpeed * Time.deltaTime, speedControll.minSpeed);
                yield return null;
            }

            // Final adjustment phase
            while (Mathf.Abs(SlotElements[0].transform.position.y - yPositions[0]) > _positionTolerance)
            {
                MoveSlots(newSpeed, false);
                yield return null;
            }

            SetStartPositions();
            _isSpinning = false;
            OnStop?.Invoke();
        }

        private bool ShouldStopSpinning()
        {
            return _timerSpin >= speedControll.timeSpin
                && SlotElements[countSlotElement - 1].transform.position.y > yPositions[SlotElements.Length - 2];
        }

        private void MoveSlots(float speed, bool resetSprites = true)
        {
            foreach (var slot in SlotElements)
            {
                slot.transform.Translate(Vector3.down * speed * Time.deltaTime);

                if (slot.transform.position.y <= _resetPosition)
                {
                    float spawnY = GetLastY() + spaceY;
                    slot.transform.position = new Vector3(slot.transform.position.x, spawnY, slot.transform.position.z);

                    if (resetSprites)
                    {
                        if (_sprites != null)
                            slot.Sprite = GetRandomSprite();
                    }
                }
            }
        }

        private float GetLastY()
        {
            return SlotElements.Max(slotElement => slotElement.transform.position.y);
        }

        private void SetSpriteEnd()
        {
            for (int i = 0; i < countSlotElement; i++)
            {
                SlotElements[i].Sprite = _spritesEnd[i];
            }
        }

        private void SetStartPositions()
        {
            for (int j = 0; j < SlotElements.Length; j++)
            {
                SlotElements[j].transform.position = new Vector2(transform.position.x, yPositions[j]);
            }
        }

        private Sprite GetRandomSprite()
        {
            return _sprites[Random.Range(0, _sprites.Length)];
        }

        public void Spin(Sprite[] allSprites, Sprite[] spritesEnd)
        {
            SetStartPositions();
            _sprites = allSprites;
            _spritesEnd = spritesEnd;

            for (int i = SlotElements.Length - 1; i >= countSlotElement; i--)
            {
                if (_sprites != null)
                    SlotElements[i].Sprite = GetRandomSprite();
            }

            StartCoroutine(SpinCoroutine());
        }

        public void Stop()
        {
            _isSpinning = false;
        }

        public void SetSprites(Sprite sprite)
        {
            foreach (var s in SlotElements)
            {
                s.Sprite = sprite;
            }
        }

        public void OnValidate()
        {
            yPositions = new float[countSlotElement * 2];
            _resetPosition = transform.position.y - spaceY;

            if (_spritesEnd.Length != countSlotElement)
                _spritesEnd = new Sprite[countSlotElement];

            SlotElements = GetComponentsInChildren<SlotElement>(true);

            for (int i = 0; i < SlotElements.Length; i++)
            {
                yPositions[i] = transform.position.y + spaceY * i;
            }

            SetStartPositions();
        }
    }
}