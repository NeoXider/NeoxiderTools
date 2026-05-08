using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
#if MIRROR
using Mirror;
using Neo.Network;
#endif

namespace Neo.Network
{
    /// <summary>Type of value to synchronize.</summary>
    public enum SyncValueType
    {
        Float = 0,
        Int = 1,
        Bool = 2,
        String = 3,
        Vector3 = 4
    }

    /// <summary>Sync direction: who writes the authoritative value.</summary>
    public enum SyncPropertyDirection
    {
        /// <summary>Server owns the value, replicates to all clients.</summary>
        ServerToClients = 0,
        /// <summary>Owner client sends value to server, server replicates to others.</summary>
        OwnerToServer = 1
    }

    /// <summary>
    ///     Universal NoCode property synchronizer.
    ///     Reads a field/property from any Component via reflection and replicates it over the network.
    ///     <para>Usage: drag onto a NetworkIdentity object, pick target component + field in Inspector.</para>
    ///     <para>Without Mirror, this component does nothing (offline no-op).</para>
    /// </summary>
    [NeoDoc("Network/NetworkPropertySync.md")]
    [AddComponentMenu("Neoxider/Network/Network Property Sync")]
    public class NetworkPropertySync :
#if MIRROR
        NetworkBehaviour
#else
        MonoBehaviour
#endif
    {
        [Header("Target")]
        [Tooltip("The component whose field/property will be synchronized.")]
        [SerializeField] private Component _targetComponent;

        [Tooltip("Name of the field or property to synchronize.")]
        [SerializeField] private string _fieldName;

        [Header("Sync Settings")]
        [Tooltip("Value type to synchronize.")]
        [SerializeField] private SyncValueType _valueType = SyncValueType.Float;

        [Tooltip("Who writes the authoritative value.")]
        [SerializeField] private SyncPropertyDirection _direction = SyncPropertyDirection.ServerToClients;

        [Tooltip("How often to check for changes and synchronize (seconds).")]
        [SerializeField] private float _syncInterval = 0.1f;

        [Tooltip("Minimum change threshold before syncing (for Float/Int/Vector3).")]
        [SerializeField] private float _threshold = 0.01f;

        [Header("Events")]
        [Tooltip("Fired when the synced value changes on this client.")]
        public UnityEvent onValueChanged = new();

        // Reflection cache
        private MemberInfo _cachedMember;
        private bool _cacheResolved;
        private float _lastSyncTime;

        // Last known values for dirty-checking
        private float _lastFloat;
        private int _lastInt;
        private bool _lastBool;
        private string _lastString;
        private Vector3 _lastVector3;

#if MIRROR
        // SyncVars for each type — only one is used per instance based on _valueType.
        [SyncVar(hook = nameof(OnFloatSynced))]  private float _syncFloat;
        [SyncVar(hook = nameof(OnIntSynced))]    private int _syncInt;
        [SyncVar(hook = nameof(OnBoolSynced))]   private bool _syncBool;
        [SyncVar(hook = nameof(OnStringSynced))]  private string _syncString = "";
        [SyncVar(hook = nameof(OnVector3Synced))] private Vector3 _syncVector3;

        private float _lastCmdTime;
        private const float CmdRateLimit = 0.05f;
#endif

        // ────────────────────── Unity ──────────────────────

        private void Update()
        {
#if MIRROR
            if (_targetComponent == null || string.IsNullOrEmpty(_fieldName)) return;
            if (Time.time - _lastSyncTime < _syncInterval) return;

            bool canWrite = false;
            if (_direction == SyncPropertyDirection.ServerToClients && isServer) canWrite = true;
            if (_direction == SyncPropertyDirection.OwnerToServer && isOwned) canWrite = true;
            if (!canWrite) return;

            ResolveMember();
            if (_cachedMember == null) return;

            switch (_valueType)
            {
                case SyncValueType.Float:
                    float f = ReadFloat();
                    if (Mathf.Abs(f - _lastFloat) > _threshold)
                    {
                        _lastFloat = f;
                        if (_direction == SyncPropertyDirection.ServerToClients)
                            _syncFloat = f;
                        else
                            CmdSyncFloat(f);
                        _lastSyncTime = Time.time;
                    }
                    break;

                case SyncValueType.Int:
                    int i = ReadInt();
                    if (i != _lastInt)
                    {
                        _lastInt = i;
                        if (_direction == SyncPropertyDirection.ServerToClients)
                            _syncInt = i;
                        else
                            CmdSyncInt(i);
                        _lastSyncTime = Time.time;
                    }
                    break;

                case SyncValueType.Bool:
                    bool b = ReadBool();
                    if (b != _lastBool)
                    {
                        _lastBool = b;
                        if (_direction == SyncPropertyDirection.ServerToClients)
                            _syncBool = b;
                        else
                            CmdSyncBool(b);
                        _lastSyncTime = Time.time;
                    }
                    break;

                case SyncValueType.String:
                    string s = ReadString();
                    if (!string.Equals(s, _lastString, StringComparison.Ordinal))
                    {
                        _lastString = s;
                        if (_direction == SyncPropertyDirection.ServerToClients)
                            _syncString = s;
                        else
                            CmdSyncString(s);
                        _lastSyncTime = Time.time;
                    }
                    break;

                case SyncValueType.Vector3:
                    Vector3 v = ReadVector3();
                    if (Vector3.SqrMagnitude(v - _lastVector3) > _threshold * _threshold)
                    {
                        _lastVector3 = v;
                        if (_direction == SyncPropertyDirection.ServerToClients)
                            _syncVector3 = v;
                        else
                            CmdSyncVector3(v);
                        _lastSyncTime = Time.time;
                    }
                    break;
            }
#endif
        }

        // ────────────────────── Mirror Cmd / Rpc / Hooks ──────────────────────

#if MIRROR
        [Command(requiresAuthority = true)]
        private void CmdSyncFloat(float v)
        {
            if (Time.time - _lastCmdTime < CmdRateLimit) return;
            _lastCmdTime = Time.time;
            _syncFloat = v;
        }

        [Command(requiresAuthority = true)]
        private void CmdSyncInt(int v)
        {
            if (Time.time - _lastCmdTime < CmdRateLimit) return;
            _lastCmdTime = Time.time;
            _syncInt = v;
        }

        [Command(requiresAuthority = true)]
        private void CmdSyncBool(bool v)
        {
            if (Time.time - _lastCmdTime < CmdRateLimit) return;
            _lastCmdTime = Time.time;
            _syncBool = v;
        }

        [Command(requiresAuthority = true)]
        private void CmdSyncString(string v)
        {
            if (Time.time - _lastCmdTime < CmdRateLimit) return;
            _lastCmdTime = Time.time;
            _syncString = v;
        }

        [Command(requiresAuthority = true)]
        private void CmdSyncVector3(Vector3 v)
        {
            if (Time.time - _lastCmdTime < CmdRateLimit) return;
            _lastCmdTime = Time.time;
            _syncVector3 = v;
        }

        private void OnFloatSynced(float _, float newVal) { WriteFloat(newVal); onValueChanged?.Invoke(); }
        private void OnIntSynced(int _, int newVal) { WriteInt(newVal); onValueChanged?.Invoke(); }
        private void OnBoolSynced(bool _, bool newVal) { WriteBool(newVal); onValueChanged?.Invoke(); }
        private void OnStringSynced(string _, string newVal) { WriteString(newVal); onValueChanged?.Invoke(); }
        private void OnVector3Synced(Vector3 _, Vector3 newVal) { WriteVector3(newVal); onValueChanged?.Invoke(); }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!isServer)
            {
                ResolveMember();
                if (_cachedMember == null) return;
                switch (_valueType)
                {
                    case SyncValueType.Float:   WriteFloat(_syncFloat); break;
                    case SyncValueType.Int:      WriteInt(_syncInt); break;
                    case SyncValueType.Bool:     WriteBool(_syncBool); break;
                    case SyncValueType.String:   WriteString(_syncString); break;
                    case SyncValueType.Vector3:  WriteVector3(_syncVector3); break;
                }
            }
        }
#endif

        // ────────────────────── Reflection ──────────────────────

        private void ResolveMember()
        {
            if (_cacheResolved) return;
            _cacheResolved = true;

            if (_targetComponent == null || string.IsNullOrEmpty(_fieldName))
            {
                Debug.LogWarning($"[NetworkPropertySync] Missing target or field on '{name}'.", this);
                return;
            }

            Type type = _targetComponent.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            // Try field first, then property
            FieldInfo fi = type.GetField(_fieldName, flags);
            if (fi != null) { _cachedMember = fi; return; }

            PropertyInfo pi = type.GetProperty(_fieldName, flags);
            if (pi != null) { _cachedMember = pi; return; }

            Debug.LogWarning($"[NetworkPropertySync] Field/Property '{_fieldName}' not found on {type.Name}.", this);
        }

        private object ReadValue()
        {
            if (_cachedMember is FieldInfo fi) return fi.GetValue(_targetComponent);
            if (_cachedMember is PropertyInfo pi) return pi.GetValue(_targetComponent);
            return null;
        }

        private void WriteValue(object value)
        {
            if (_cachedMember is FieldInfo fi) fi.SetValue(_targetComponent, value);
            else if (_cachedMember is PropertyInfo pi && pi.CanWrite) pi.SetValue(_targetComponent, value);
        }

        private float ReadFloat()
        {
            object v = ReadValue();
            if (v is float f) return f;
            if (v is int i) return i;
            if (v is double d) return (float)d;
            return 0f;
        }

        private int ReadInt()
        {
            object v = ReadValue();
            if (v is int i) return i;
            if (v is float f) return Mathf.RoundToInt(f);
            return 0;
        }

        private bool ReadBool()
        {
            object v = ReadValue();
            if (v is bool b) return b;
            return false;
        }

        private string ReadString()
        {
            object v = ReadValue();
            return v?.ToString() ?? "";
        }

        private Vector3 ReadVector3()
        {
            object v = ReadValue();
            if (v is Vector3 vec) return vec;
            return Vector3.zero;
        }

        private void WriteFloat(float val) { WriteValue(val); _lastFloat = val; }
        private void WriteInt(int val) { WriteValue(val); _lastInt = val; }
        private void WriteBool(bool val) { WriteValue(val); _lastBool = val; }
        private void WriteString(string val) { WriteValue(val); _lastString = val; }
        private void WriteVector3(Vector3 val) { WriteValue(val); _lastVector3 = val; }
    }
}
