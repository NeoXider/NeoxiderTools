#if MIRROR
using System;
using System.Text;
using Mirror;
using Neo.Network.Realtime;
#endif
using UnityEngine;

namespace Neo.Network
{
#if MIRROR
    /// <summary>
    ///     Static read access to Mirror's link measurements, in milliseconds.
    /// </summary>
    public static class NetTelemetry
    {
        /// <summary>Smoothed client round-trip time to the server, ms (0 until the first ping answer).</summary>
        public static float RttMilliseconds => (float)(NetworkTime.rtt * 1000d);

        /// <summary>Round-trip jitter (standard deviation of the RTT), ms.</summary>
        public static float RttJitterMilliseconds => (float)(Math.Sqrt(Math.Max(0d, NetworkTime.rttVariance)) * 1000d);

        /// <summary>
        ///     Server: the highest round-trip time among connected clients (excluding the host's loopback), ms.
        ///     0 when no remote client is connected or no server is active.
        /// </summary>
        public static float WorstClientRttMilliseconds
        {
            get
            {
                if (!NetworkServer.active)
                {
                    return 0f;
                }

                double worst = 0d;
                foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
                {
                    if (connection is LocalConnectionToClient)
                    {
                        continue;
                    }

                    if (connection.rtt > worst)
                    {
                        worst = connection.rtt;
                    }
                }

                return (float)(worst * 1000d);
            }
        }
    }

    /// <summary>
    ///     Optional telemetry for a Mirror session: round-trip time, bytes and messages per second in and out, and the
    ///     busiest message kinds, with an optional on-screen overlay.
    ///     <para>
    ///         It subscribes to Mirror's <c>NetworkDiagnostics</c> events only while enabled; Mirror boxes each message
    ///         for those events, so leave the component off (or <see cref="CaptureTraffic"/> unticked) in a shipped
    ///         build unless you want the numbers. RTT is read from <c>NetworkTime</c> and is always free.
    ///     </para>
    /// </summary>
    [NeoDoc("Network/NeoNetworkTelemetry.md")]
    [CreateFromMenu("Neoxider/Network/NeoNetworkTelemetry")]
    [AddComponentMenu("Neoxider/Network/" + nameof(NeoNetworkTelemetry))]
    [DisallowMultipleComponent]
    public sealed class NeoNetworkTelemetry : MonoBehaviour
    {
        private const int TopKinds = 5;

        [Header("Capture")]
        [Tooltip("Count bytes and messages per message kind from Mirror's NetworkDiagnostics events. " +
                 "Mirror boxes every message while subscribed, so it is a debugging cost.")]
        [SerializeField]
        private bool _captureTraffic = true;

        [Tooltip("Seconds the per-second rates are averaged over.")] [SerializeField] [Min(0.1f)]
        private float _windowSeconds = 1f;

        [Header("Overlay")]
        [Tooltip("Draw a small on-screen readout (ping, in/out rates, busiest message kinds).")]
        [SerializeField]
        private bool _showOverlay;

        [Tooltip("Top-left corner of the overlay, in screen pixels.")] [SerializeField]
        private Vector2 _overlayPosition = new(10f, 10f);

        private readonly StringBuilder _text = new(256);
        private readonly NetTrafficMeter.KindStat[] _topIn = new NetTrafficMeter.KindStat[TopKinds];
        private readonly NetTrafficMeter.KindStat[] _topOut = new NetTrafficMeter.KindStat[TopKinds];
        private NetTrafficMeter _in;
        private NetTrafficMeter _out;
        private SnapshotTimeline _timeline;
        private float _nextTextAt;
        private float _nextRttAt;
        private string _overlayText = string.Empty;
        private bool _subscribed;
        private GUIStyle _style;

        /// <summary>Incoming traffic (what this peer received).</summary>
        public NetTrafficMeter Incoming => _in ??= new NetTrafficMeter(_windowSeconds);

        /// <summary>Outgoing traffic (what this peer sent; a message sent to N connections counts N times).</summary>
        public NetTrafficMeter Outgoing => _out ??= new NetTrafficMeter(_windowSeconds);

        /// <summary>Count per-kind traffic from Mirror's diagnostics events.</summary>
        public bool CaptureTraffic
        {
            get => _captureTraffic;
            set
            {
                _captureTraffic = value;
                RefreshSubscription();
            }
        }

        /// <summary>Draw the on-screen readout.</summary>
        public bool ShowOverlay
        {
            get => _showOverlay;
            set => _showOverlay = value;
        }

        /// <summary>Client round-trip time, ms.</summary>
        public float RttMs { get; private set; }

        /// <summary>Round-trip jitter, ms.</summary>
        public float RttJitterMs { get; private set; }

        /// <summary>Server: worst remote client round-trip time, ms.</summary>
        public float WorstClientRttMs { get; private set; }

        /// <summary>Bytes per second received over the last window.</summary>
        public float BytesInPerSecond => Incoming.BytesPerSecond;

        /// <summary>Bytes per second sent over the last window.</summary>
        public float BytesOutPerSecond => Outgoing.BytesPerSecond;

        /// <summary>
        ///     Optional: show the jitter a <see cref="SnapshotTimeline"/> measured next to the ping, the best single
        ///     "how rough is this link" number for a snapshot-interpolated game.
        /// </summary>
        public void SetTimeline(SnapshotTimeline timeline)
        {
            _timeline = timeline;
        }

        /// <summary>Toggles the overlay (wire it to a debug button).</summary>
        public void ToggleOverlay()
        {
            _showOverlay = !_showOverlay;
        }

        /// <summary>One-line summary: <c>ping 34 ms (+-5) | in 3.1 KB/s | out 12.4 KB/s</c>.</summary>
        public string BuildSummary()
        {
            _text.Length = 0;
            AppendSummary(_text);
            return _text.ToString();
        }

        private void OnEnable()
        {
            RefreshSubscription();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            _in?.Tick(now);
            _out?.Tick(now);

            float clock = Time.unscaledTime;
            if (clock >= _nextRttAt)
            {
                _nextRttAt = clock + 0.5f;
                RttMs = NetTelemetry.RttMilliseconds;
                RttJitterMs = NetTelemetry.RttJitterMilliseconds;
                WorstClientRttMs = NetTelemetry.WorstClientRttMilliseconds;
            }

            if (_showOverlay && clock >= _nextTextAt)
            {
                _nextTextAt = clock + 0.25f;
                RebuildOverlayText();
            }
        }

        private void OnGUI()
        {
            if (!_showOverlay || _overlayText.Length == 0)
            {
                return;
            }

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 12,
                    richText = false,
                    padding = new RectOffset(6, 6, 4, 4)
                };
                _style.normal.textColor = Color.white;
            }

            Vector2 size = _style.CalcSize(new GUIContent(_overlayText));
            GUI.Box(new Rect(_overlayPosition.x, _overlayPosition.y, size.x + 4f, size.y + 4f), _overlayText, _style);
        }

        private void RefreshSubscription()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_captureTraffic)
            {
                Subscribe();
            }
            else
            {
                Unsubscribe();
            }
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            _subscribed = true;
            Mirror.NetworkDiagnostics.InMessageEvent += OnIn;
            Mirror.NetworkDiagnostics.OutMessageEvent += OnOut;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _subscribed = false;
            Mirror.NetworkDiagnostics.InMessageEvent -= OnIn;
            Mirror.NetworkDiagnostics.OutMessageEvent -= OnOut;
        }

        private void OnIn(Mirror.NetworkDiagnostics.MessageInfo info)
        {
            Incoming.Record(info.message?.GetType(), info.bytes, 1);
        }

        private void OnOut(Mirror.NetworkDiagnostics.MessageInfo info)
        {
            Outgoing.Record(info.message?.GetType(), info.bytes, info.count < 1 ? 1 : info.count);
        }

        private void RebuildOverlayText()
        {
            _text.Length = 0;
            AppendSummary(_text);
            if (_captureTraffic)
            {
                AppendTop(_text, "in", Incoming, _topIn);
                AppendTop(_text, "out", Outgoing, _topOut);
            }

            _overlayText = _text.ToString();
        }

        private void AppendSummary(StringBuilder text)
        {
            text.Append("ping ").Append(Mathf.RoundToInt(RttMs)).Append(" ms (+-")
                .Append(Mathf.RoundToInt(RttJitterMs)).Append(')');
            if (NetworkServer.active && WorstClientRttMs > 0f)
            {
                text.Append(" | worst client ").Append(Mathf.RoundToInt(WorstClientRttMs)).Append(" ms");
            }

            if (_timeline != null && _timeline.HasTimeline)
            {
                text.Append(" | snap jitter ").Append(Mathf.RoundToInt(_timeline.JitterSeconds * 1000f)).Append(" ms");
            }

            text.Append(" | in ");
            AppendRate(text, BytesInPerSecond);
            text.Append(" | out ");
            AppendRate(text, BytesOutPerSecond);
        }

        private static void AppendRate(StringBuilder text, float bytesPerSecond)
        {
            if (bytesPerSecond >= 1024f * 1024f)
            {
                text.Append((bytesPerSecond / (1024f * 1024f)).ToString("0.0")).Append(" MB/s");
            }
            else if (bytesPerSecond >= 1024f)
            {
                text.Append((bytesPerSecond / 1024f).ToString("0.0")).Append(" KB/s");
            }
            else
            {
                text.Append(Mathf.RoundToInt(bytesPerSecond)).Append(" B/s");
            }
        }

        private static void AppendTop(StringBuilder text, string label, NetTrafficMeter meter,
            NetTrafficMeter.KindStat[] buffer)
        {
            int count = meter.GetTopKinds(buffer);
            for (int i = 0; i < count; i++)
            {
                if (buffer[i].BytesPerSecond <= 0f)
                {
                    break;
                }

                text.Append('\n').Append(label).Append(' ').Append(buffer[i].Kind.Name).Append("  ");
                AppendRate(text, buffer[i].BytesPerSecond);
                text.Append("  ").Append(Mathf.RoundToInt(buffer[i].MessagesPerSecond)).Append(" msg/s");
            }
        }
    }
#else
    /// <summary>Telemetry needs Mirror; without it the component is an empty stub.</summary>
    [NeoDoc("Network/NeoNetworkTelemetry.md")]
    [AddComponentMenu("Neoxider/Network/" + nameof(NeoNetworkTelemetry))]
    public sealed class NeoNetworkTelemetry : MonoBehaviour
    {
    }
#endif
}
