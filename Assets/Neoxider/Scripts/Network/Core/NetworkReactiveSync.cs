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
    [CreateFromMenu("Neoxider/Network/Network Reactive Sync")]
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
        private MemberInfo _cachedMember;
        private bool _missingTargetLogged;
        private bool _typeMismatchLogged;
        private double _lastSyncTime;

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

            bool canWrite = (_direction == SyncPropertyDirection.ServerToClients && isServer)
                            || (_direction == SyncPropertyDirection.OwnerToServer && isOwned);
            if (!canWrite)
            {
                return;
            }

            double now = Time.unscaledTimeAsDouble;
            if (now - _lastSyncTime < _syncInterval)
            {
                return;
            }

            _lastSyncTime = now;
            ResolveReactive();
            if (_reactiveInstance == null)
            {
                return;
            }

            // WHY: the inspector enum can disagree with the resolved field type; a hard cast would
            // throw InvalidCastException every frame, so validate once and bail out instead.
            switch (_valueType)
            {
                case ReactiveSyncValueType.Float:
                    if (_reactiveInstance is not ReactivePropertyFloat floatProp)
                    {
                        WarnValueTypeMismatch();
                        return;
                    }

                    float f = floatProp.CurrentValue;
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

                    }

                    break;

                case ReactiveSyncValueType.Int:
                    if (_reactiveInstance is not ReactivePropertyInt intProp)
                    {
                        WarnValueTypeMismatch();
                        return;
                    }

                    int i = intProp.CurrentValue;
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

                    }

                    break;

                case ReactiveSyncValueType.Bool:
                    if (_reactiveInstance is not ReactivePropertyBool boolProp)
                    {
                        WarnValueTypeMismatch();
                        return;
                    }

                    bool b = boolProp.CurrentValue;
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

                    }

                    break;
            }
#endif
        }

#if MIRROR
        [Command(requiresAuthority = true)]
        private void CmdSyncFloat(float v)
        {
            if (!isNetworked || _direction != SyncPropertyDirection.OwnerToServer
                || _valueType != ReactiveSyncValueType.Float || !HasValidBinding() || RateLimitCheck())
            {
                return;
            }

            bool changed = !_syncFloat.Equals(v);
            _syncFloat = v;
            // Mirror runs SyncVar hooks on hosts and receiving clients, but not dedicated servers.
            if (changed && !NetworkServer.activeHost)
            {
                OnFloatSynced(default, v);
            }
        }

        [Command(requiresAuthority = true)]
        private void CmdSyncInt(int v)
        {
            if (!isNetworked || _direction != SyncPropertyDirection.OwnerToServer
                || _valueType != ReactiveSyncValueType.Int || !HasValidBinding() || RateLimitCheck())
            {
                return;
            }

            bool changed = !_syncInt.Equals(v);
            _syncInt = v;
            // Mirror runs SyncVar hooks on hosts and receiving clients, but not dedicated servers.
            if (changed && !NetworkServer.activeHost)
            {
                OnIntSynced(default, v);
            }
        }

        [Command(requiresAuthority = true)]
        private void CmdSyncBool(bool v)
        {
            if (!isNetworked || _direction != SyncPropertyDirection.OwnerToServer
                || _valueType != ReactiveSyncValueType.Bool || !HasValidBinding() || RateLimitCheck())
            {
                return;
            }

            bool changed = !_syncBool.Equals(v);
            _syncBool = v;
            // Mirror runs SyncVar hooks on hosts and receiving clients, but not dedicated servers.
            if (changed && !NetworkServer.activeHost)
            {
                OnBoolSynced(default, v);
            }
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

        private void WarnValueTypeMismatch()
        {
            if (_typeMismatchLogged)
            {
                return;
            }

            _typeMismatchLogged = true;
            NetworkDiagnostics.LogWarning(
                $"[NetworkReactiveSync] Value Type '{_valueType}' does not match the resolved type " +
                $"'{_reactiveInstance?.GetType().Name}' of '{_fieldName}' on '{name}'. Sync disabled.",
                this);
        }

        private void ResolveReactive()
        {
            if (_cacheResolved && (_cachedMember == null || _reactiveInstance != null))
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

            if (!_cacheResolved)
            {
                _cacheResolved = true;
                Type expectedType = _valueType switch
                {
                    ReactiveSyncValueType.Float => typeof(ReactivePropertyFloat),
                    ReactiveSyncValueType.Int => typeof(ReactivePropertyInt),
                    ReactiveSyncValueType.Bool => typeof(ReactivePropertyBool),
                    _ => null
                };
                if (!NetworkSyncMember.TryResolve(_targetComponent, _fieldName, expectedType, false,
                        out _cachedMember, out string error))
                {
                    NetworkDiagnostics.LogWarning(
                        $"[NetworkReactiveSync] Invalid binding '{_fieldName}' on '{name}': {error}", this);
                    return;
                }
            }

            try
            {
                // Metadata is stable; a valid but temporarily null instance remains retryable.
                _reactiveInstance = NetworkSyncMember.Read(_cachedMember, _targetComponent);
            }
            catch (Exception exception)
            {
                _cachedMember = null;
                NetworkDiagnostics.LogWarning(
                    $"[NetworkReactiveSync] Binding '{_fieldName}' on '{name}' disabled: {exception.GetBaseException().Message}", this);
            }
        }

        private bool HasValidBinding()
        {
            ResolveReactive();
            return _targetComponent != null && _cachedMember != null;
        }
    }
}
