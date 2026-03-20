using UnityEngine;

namespace Neo.Cards
{
    [CreateAssetMenu(fileName = "CardAnimationConfig", menuName = "Neoxider/Cards/Card Animation Config")]
    public class CardAnimationConfig : ScriptableObject
    {
        [Header("Deal")] [SerializeField] private float _dealMoveDuration = 0.3f;
        [SerializeField] private float _dealStepDelay = 0.12f;

        [Header("Shuffle / Shake")] [SerializeField]
        private float _shakeDuration = 1f;

        [SerializeField] private float _shakeIntensity = 0.12f;
        [SerializeField] private int _shakeFrames = 20;

        [Header("Shuffle / Cut")] [SerializeField]
        private float _cutDuration = 0.8f;

        [SerializeField] private float _cutLiftHeight = 0.5f;

        [Header("Shuffle / Riffle")] [SerializeField]
        private float _riffleDuration = 1.2f;

        [SerializeField] private float _riffleSpread = 0.3f;

        [Header("Stack")] [SerializeField] private Vector3 _stackPositionJitter = new(0.02f, 0f, 0.02f);
        [SerializeField] private Vector3 _stackRotationJitter = new(0f, 0f, 3f);
        [SerializeField] private float _stackStepY = 0.01f;

        [Header("Card View (Bounce, Pulse, Shake, etc.)")] [SerializeField]
        private float _cardBounceDuration = 0.25f;

        [SerializeField] private float _cardBouncePunch = 0.15f;
        [SerializeField] private float _cardPulseDuration = 0.4f;
        [SerializeField] private float _cardPulseScale = 0.08f;
        [SerializeField] private float _cardShakeDuration = 0.3f;
        [SerializeField] private float _cardShakeStrength = 8f;
        [SerializeField] private float _cardHighlightDuration = 0.2f;
        [SerializeField] private float _cardFlyInDuration = 0.35f;
        [SerializeField] private float _cardIdleDuration = 1.2f;
        [SerializeField] private float _cardIdleScale = 0.02f;

        public float DealMoveDuration => _dealMoveDuration;
        public float DealStepDelay => _dealStepDelay;
        public float ShakeDuration => _shakeDuration;
        public float ShakeIntensity => _shakeIntensity;
        public int ShakeFrames => Mathf.Max(1, _shakeFrames);
        public float CutDuration => _cutDuration;
        public float CutLiftHeight => _cutLiftHeight;
        public float RiffleDuration => _riffleDuration;
        public float RiffleSpread => _riffleSpread;
        public Vector3 StackPositionJitter => _stackPositionJitter;
        public Vector3 StackRotationJitter => _stackRotationJitter;
        public float StackStepY => _stackStepY;

        public float CardBounceDuration => _cardBounceDuration;
        public float CardBouncePunch => _cardBouncePunch;
        public float CardPulseDuration => _cardPulseDuration;
        public float CardPulseScale => _cardPulseScale;
        public float CardShakeDuration => _cardShakeDuration;
        public float CardShakeStrength => _cardShakeStrength;
        public float CardHighlightDuration => _cardHighlightDuration;
        public float CardFlyInDuration => _cardFlyInDuration;
        public float CardIdleDuration => _cardIdleDuration;
        public float CardIdleScale => _cardIdleScale;
    }
}
