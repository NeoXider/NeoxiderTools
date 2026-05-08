using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;
#if MIRROR
using Mirror;
using Neo.Network;
#endif

namespace Neo.Tools
{
    /// <summary>Output type for random generation: integer or float.</summary>
    public enum RandomRangeValueMode
    {
        Int = 0,
        Float = 1
    }

    /// <summary>
    ///     Generates random values in a configurable range. Exposes reactive value and events for NeoCondition and no-code
    ///     binding.
    ///     Use for random anomaly count (e.g. 0–5), spawn intervals, or any min–max random value.
    /// </summary>
    [NeoDoc("Tools/Components/RandomRange.md")]
    [CreateFromMenu("Neoxider/Tools/Components/RandomRange")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(RandomRange))]
#if MIRROR
    public class RandomRange : NetworkBehaviour
#else
    public class RandomRange : MonoBehaviour
#endif
    {
        [Header("Networking")]
        [Tooltip("If true, the generated random value will be synchronized to all clients so everyone gets the same number.")]
        public bool isNetworked = false;

#if MIRROR
        /// <summary>Server-authoritative last random value, synced to late-joining clients.</summary>
        [SyncVar]
        private float _syncValue;

        private float _lastCmdTime;
        private const float CmdRateLimit = 0.05f;
#endif

        [Header("Mode")] [Tooltip("Generate integer (inclusive min..max) or float (min..max).")] [SerializeField]
        private RandomRangeValueMode _valueMode = RandomRangeValueMode.Int;

        [Header("Range")] [Tooltip("Minimum value (inclusive for Int).")] [SerializeField]
        private float _min;

        [Tooltip("Maximum value (inclusive for Int).")] [SerializeField]
        private float _max = 10f;

        [Header("Output")] [Tooltip("Current value after last Generate(). Used by NeoCondition and bindings.")]
        public ReactivePropertyFloat Value = new();

        [Header("Events")] [Tooltip("Invoked after Generate() with new integer value (rounded in Float mode).")]
        public UnityEvent<int> OnGeneratedInt = new();

        [Tooltip("Invoked after Generate() with new float value.")]
        public UnityEvent<float> OnGeneratedFloat = new();

        /// <summary>Current value as int (for NeoCondition and comparisons).</summary>
        public int ValueInt => _valueMode == RandomRangeValueMode.Int
            ? Mathf.RoundToInt(Value.CurrentValue)
            : (int)Value.CurrentValue;

        /// <summary>Current value as float.</summary>
        public float ValueFloat => Value.CurrentValue;

        /// <summary>Min bound (get/set).</summary>
        public float Min
        {
            get => _min;
            set => _min = value;
        }

        /// <summary>Max bound (get/set).</summary>
        public float Max
        {
            get => _max;
            set => _max = value;
        }

        /// <summary>Generation mode (Int or Float).</summary>
        public RandomRangeValueMode ValueMode
        {
            get => _valueMode;
            set => _valueMode = value;
        }

        /// <summary>Generates a new random value in [Min, Max], updates Value and invokes events.</summary>
        [Button]
        public void Generate()
        {
#if MIRROR
            if (isNetworked && (NeoNetworkState.IsClient || NeoNetworkState.IsServer))
            {
                if (NeoNetworkState.IsClient && !NeoNetworkState.IsServer)
                {
                    CmdGenerate();
                    return;
                }
                
                // Server generates and multicasts
                float val = RollRandomValue();
                _syncValue = val;
                ApplyValueLocally(val);
                RpcReceiveValue(val);
                return;
            }
#endif
            float localVal = RollRandomValue();
            ApplyValueLocally(localVal);
        }

        private float RollRandomValue()
        {
            if (_valueMode == RandomRangeValueMode.Int)
            {
                int minI = Mathf.RoundToInt(_min);
                int maxI = Mathf.RoundToInt(_max);
                if (minI > maxI)
                {
                    int t = minI;
                    minI = maxI;
                    maxI = t;
                }
                return Random.Range(minI, maxI + 1);
            }
            return _min < _max ? Random.Range(_min, _max) : _min;
        }

        private void ApplyValueLocally(float newVal)
        {
            Value.Value = newVal;
            OnGeneratedInt?.Invoke(ValueInt);
            OnGeneratedFloat?.Invoke(newVal);
        }

#if MIRROR
        [Command(requiresAuthority = false)]
        private void CmdGenerate(NetworkConnectionToClient sender = null)
        {
            if (Time.time - _lastCmdTime < CmdRateLimit) return;
            _lastCmdTime = Time.time;

            float val = RollRandomValue();
            _syncValue = val;
            ApplyValueLocally(val);
            RpcReceiveValue(val);
        }

        [ClientRpc(includeOwner = true)]
        private void RpcReceiveValue(float val)
        {
            if (isServer) return; // Server already applied
            ApplyValueLocally(val);
        }

        protected override void OnValidate()
        {
            if (isNetworked)
            {
                base.OnValidate();
            }
        }

        /// <summary>Late-join: apply server-authoritative value to newly connected client.</summary>
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (isNetworked && !isServer)
            {
                ApplyValueLocally(_syncValue);
            }
        }
#endif

        /// <summary>Sets minimum bound (int).</summary>
        public void SetMin(int value)
        {
            _min = value;
        }

        /// <summary>Sets maximum bound (int).</summary>
        public void SetMax(int value)
        {
            _max = value;
        }

        /// <summary>Sets minimum bound (float).</summary>
        public void SetMin(float value)
        {
            _min = value;
        }

        /// <summary>Sets maximum bound (float).</summary>
        public void SetMax(float value)
        {
            _max = value;
        }
    }
}
