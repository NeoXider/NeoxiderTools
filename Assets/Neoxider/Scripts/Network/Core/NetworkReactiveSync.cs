using System;
using System.Reflection;
using Neo.Reactive;
using UnityEngine;
#if MIRROR
using Mirror;
#endif

namespace Neo.Network
{
    /// <summary>Reactive value kind handled by <see cref="NetworkReactiveSync"/>.</summary>
    public enum ReactiveSyncValueType
    {
        Float = 0,
        Int = 1,
        Bool = 2
    }

    /// <summary>
    ///     NoCode replication for <see cref="ReactivePropertyFloat"/>/<see cref="ReactivePropertyInt"/>/
    ///     <see cref="ReactivePropertyBool"/> fields — the inspector counterpart of
    ///     <see cref="NetworkReactivePropertyBridge"/>, which requires hand-written SyncVar code.
    ///     <para>
    ///         Drag onto a NetworkIdentity object, reference the component holding the reactive field
    ///         (e.g. <c>Money</c> → <c>CurrentMoney</c>), pick the field name and direction — the value
    ///         replicates and every local binding (TextMoney, UI, UnityEvents) fires on all clients.
    ///     </para>
    ///     <para>Without Mirror the component is a no-op; the reactive property works locally as usual.</para>
    /// </summary>
    [NeoDoc("Network/NetworkReactiveSync.md")]
    [AddComponentMenu("Neoxider/Network/Network Reactive Sync")]
    public class NetworkReactiveSync : NeoNetworkComponent
    {
        [Header("Target")]
        [Tooltip("Component holding the reactive property field (e.g. Money, ScoreManager).")]
        [SerializeField]
        private Component _targetComponent;

        [Tooltip("Name of the ReactivePropertyFloat/Int/Bool field or property (e.g. CurrentMoney).")]
        [SerializeField]
        private string _fieldName;

        [Header("Sync Settings")]
        [SerializeField] private ReactiveSyncValueType _valueType = ReactiveSyncValueType.Float;

        [Tooltip("Who writes the authoritative value.")]
        [SerializeField]
        private SyncPropertyDirection _direction = SyncPropertyDirection.ServerToClients;

        // WHY: Floor must stay above NeoNetworkComponent.NetworkRateLimit (0.05s) — see NetworkPropertySync.
        [Tooltip("How often to check the reactive value for changes (seconds).")]
        [SerializeField] [Min(0.1f)]
        private float _syncInterval = 0.1f;

        [Tooltip("Minimum change before a float is synced.")]
        [SerializeField]
        private float _floatThreshold = 0.01f;

        private object _reactiveInstance;
        private bool _cacheResolved;
        private bool _missingTargetLogged;
        private float _lastSyncTime;

        private float _lastFloat;
        private int _lastInt;
        private bool _lastBool;

#if MIRROR
        [SyncVar(hook = nameof(OnFloatSynced))] private float _syncFloat;
        [SyncVar(hook = nameof(OnIntSynced))] private int _syncInt;
        [SyncVar(hook = nameof(OnBoolSynced))] private bool _syncBool;
#endif

        private void Update()
        {
#if MIRROR
            if (!isNetworked || _targetComponent == null || string.IsNullOrEmpty(_fieldName))
            {
                return;
            }

            if (Time.time - _lastSyncTime < _syncInterval)
            {
                return;
            }

            bool canWrite = (_direction == SyncPropertyDirection.ServerToClients && isServer)
                            || (_direction == SyncPropertyDirection.OwnerToServer && isOwned);
            if (!canWrite)
            {
                return;
            }

            ResolveReactive();
            if (_reactiveInstance == null)
            {
                return;
            }

            switch (_valueType)
            {
                case ReactiveSyncValueType.Float:
                    float f = ((ReactivePropertyFloat)_reactiveInstance).CurrentValue;
                    if (Mathf.Abs(f - _lastFloat) > _floatThreshold)
                    {
                        _lastFloat = f;
                        if (_direction == SyncPropertyDirection.ServerToClients)
                        {
                            _syncFloat = f;
                        }
                        else
                        {
                            CmdSyncFloat(f);
                        }

                        _lastSyncTime = Time.time;
                    }

                    break;

                case ReactiveSyncValueType.Int:
                    int i = ((ReactivePropertyInt)_reactiveInstance).CurrentValue;
                    if (i != _lastInt)
                    {
                        _lastInt = i;
                        if (_direction == SyncPropertyDirection.ServerToClients)
                        {
                            _syncInt = i;
                        }
                        else
                        {
                            CmdSyncInt(i);
                        }

                        _lastSyncTime = Time.time;
                    }

                    break;

                case ReactiveSyncValueType.Bool:
                    bool b = ((ReactivePropertyBool)_reactiveInstance).CurrentValue;
                    if (b != _lastBool)
                    {
                        _lastBool = b;
                        if (_direction == SyncPropertyDirection.ServerToClients)
                        {
                            _syncBool = b;
                        }
                        else
                        {
                            CmdSyncBool(b);
                        }

                        _lastSyncTime = Time.time;
                    }

                    break;
            }
#endif
        }

#if MIRROR
        [Command(requiresAuthority = true)]
        private void CmdSyncFloat(float v)
        {
            if (RateLimitCheck())
            {
                return;
            }

            _syncFloat = v;
        }

        [Command(requiresAuthority = true)]
        private void CmdSyncInt(int v)
        {
            if (RateLimitCheck())
            {
                return;
            }

            _syncInt = v;
        }

        [Command(requiresAuthority = true)]
        private void CmdSyncBool(bool v)
        {
            if (RateLimitCheck())
            {
                return;
            }

            _syncBool = v;
        }

        private void OnFloatSynced(float _, float newVal)
        {
            _lastFloat = newVal;
            ResolveReactive();
            if (_reactiveInstance is ReactivePropertyFloat prop)
            {
                NetworkReactivePropertyBridge.SetFromNetwork(prop, newVal);
            }
        }

        private void OnIntSynced(int _, int newVal)
        {
            _lastInt = newVal;
            ResolveReactive();
            if (_reactiveInstance is ReactivePropertyInt prop)
            {
                NetworkReactivePropertyBridge.SetFromNetwork(prop, newVal);
            }
        }

        private void OnBoolSynced(bool _, bool newVal)
        {
            _lastBool = newVal;
            ResolveReactive();
            if (_reactiveInstance is ReactivePropertyBool prop)
            {
                NetworkReactivePropertyBridge.SetFromNetwork(prop, newVal);
            }
        }

        protected override void ApplyNetworkState()
        {
            switch (_valueType)
            {
                case ReactiveSyncValueType.Float: OnFloatSynced(0f, _syncFloat); break;
                case ReactiveSyncValueType.Int: OnIntSynced(0, _syncInt); break;
                case ReactiveSyncValueType.Bool: OnBoolSynced(false, _syncBool); break;
            }
        }
#endif

        private void ResolveReactive()
        {
            if (_cacheResolved)
            {
                return;
            }

            if (_targetComponent == null || string.IsNullOrEmpty(_fieldName))
            {
                if (!_missingTargetLogged)
                {
                    _missingTargetLogged = true;
                    NetworkDiagnostics.LogWarning($"[NetworkReactiveSync] Missing target or field on '{name}'.",
                        this);
                }

                return;
            }

            Type type = _targetComponent.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            FieldInfo fi = type.GetField(_fieldName, flags);
            object value = fi != null ? fi.GetValue(_targetComponent) : null;
            if (value == null)
            {
                PropertyInfo pi = type.GetProperty(_fieldName, flags);
                value = pi != null ? pi.GetValue(_targetComponent) : null;
            }

            if (value is ReactivePropertyFloat || value is ReactivePropertyInt || value is ReactivePropertyBool)
            {
                _reactiveInstance = value;
                _cacheResolved = true;
                return;
            }

            _cacheResolved = true;
            NetworkDiagnostics.LogWarning(
                $"[NetworkReactiveSync] '{_fieldName}' on {type.Name} is not a ReactivePropertyFloat/Int/Bool.",
                this);
        }
    }
}
