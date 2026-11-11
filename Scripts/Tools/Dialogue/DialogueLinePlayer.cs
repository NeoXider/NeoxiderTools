using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Scene wrapper over <see cref="DialogueLinePool" />: plays pooled barks into an optional TMP label, schedules
    ///     idle lines, hides lines after a duration and lets contextual requests interrupt idle chatter.
    ///     Context time is the number of seconds since the component was enabled or <see cref="ResetSession" /> ran.
    /// </summary>
    [NeoDoc("Tools/Dialogue/DialogueLinePool.md")]
    [CreateFromMenu("Neoxider/Tools/Dialogue/DialogueLinePlayer")]
    [AddComponentMenu("Neoxider/" + "Tools/Dialogue/" + nameof(DialogueLinePlayer))]
    public class DialogueLinePlayer : MonoBehaviour
    {
        [Header("Lines")] [SerializeField] private List<PooledDialogueLine> lines = new();

        [Tooltip("Never pick the previous line of a tag again while another line of that tag is available.")]
        [SerializeField]
        private bool avoidBackToBackRepeat = true;

        [Header("Idle")] [Tooltip("Tag requested by the idle scheduler and by RequestIdle().")] [SerializeField]
        private string idleTag = "idle";

        [Tooltip("Seconds between idle lines while nothing is playing. 0 disables automatic idle lines.")]
        [SerializeField]
        [Min(0f)]
        private float idleInterval;

        [Tooltip("Priority of contextual requests made through RequestContext(tag).")] [SerializeField]
        private int contextPriority = DialogueLinePool.ContextPriority;

        [Header("Playback")]
        [Tooltip("Seconds a line stays before it finishes. 0 keeps it until FinishLine() is called.")]
        [SerializeField]
        [Min(0f)]
        private float lineDuration = 3f;

        [Tooltip("Count context time, idle and line duration in unscaled time.")] [SerializeField]
        private bool useUnscaledTime;

        [Header("Output (optional)")] [SerializeField]
        private TMP_Text lineText;

        [SerializeField] private TMP_Text speakerText;

        [Tooltip("Clear the labels when a line finishes.")] [SerializeField]
        private bool clearTextOnFinish = true;

        [Header("Events")] public UnityEvent<string> OnLineStarted = new();
        public UnityEvent<string> OnSpeakerChanged = new();
        public UnityEvent OnLineFinished = new();

        private DialogueLinePool _pool;
        private float _contextTime;
        private float _lineElapsed;
        private float _idleElapsed;

        /// <summary>Plain C# pool driven by this component (created on first access).</summary>
        public DialogueLinePool Pool
        {
            get
            {
                EnsurePool();
                return _pool;
            }
        }

        /// <summary>Seconds since enable or the last <see cref="ResetSession" />; passed to the pool as context time.</summary>
        public float ContextTime => _contextTime;

        /// <summary>Authoring list; call <see cref="RebuildPool" /> after changing it at runtime.</summary>
        public List<PooledDialogueLine> Lines => lines;

        /// <summary>Tag used by idle requests.</summary>
        public string IdleTag
        {
            get => idleTag;
            set => idleTag = value;
        }

        /// <summary>Seconds between automatic idle lines; 0 disables them.</summary>
        public float IdleInterval
        {
            get => idleInterval;
            set => idleInterval = Mathf.Max(0f, value);
        }

        /// <summary>Seconds a line stays before it finishes; 0 keeps it until <see cref="FinishLine" />.</summary>
        public float LineDuration
        {
            get => lineDuration;
            set => lineDuration = Mathf.Max(0f, value);
        }

        /// <summary>When true, the built-in <c>Update</c> advances time with unscaled frame deltas.</summary>
        public bool UseUnscaledTime
        {
            get => useUnscaledTime;
            set => useUnscaledTime = value;
        }

        /// <summary>Optional label that receives the text of each started line; <c>null</c> disables it.</summary>
        public TMP_Text LineText
        {
            get => lineText;
            set => lineText = value;
        }

        /// <summary>Optional label that receives the speaker of each started line; <c>null</c> disables it.</summary>
        public TMP_Text SpeakerText
        {
            get => speakerText;
            set => speakerText = value;
        }

        /// <summary>When true, both labels are cleared when a line finishes.</summary>
        public bool ClearTextOnFinish
        {
            get => clearTextOnFinish;
            set => clearTextOnFinish = value;
        }

        private void OnEnable()
        {
            EnsurePool();
            _contextTime = 0f;
            _idleElapsed = 0f;
        }

        private void OnDisable()
        {
            _pool?.Finish();
        }

        private void OnDestroy()
        {
            if (_pool == null)
            {
                return;
            }

            _pool.LineStarted -= HandleLineStarted;
            _pool.LineFinished -= HandleLineFinished;
        }

        private void OnValidate()
        {
            if (_pool != null)
            {
                RebuildPool();
            }
        }

        // WHY: Update is only the Unity-time adapter; the scheduling rule lives in Tick so tests, replays and custom
        // loops can drive it with their own deltas (see Tools/Time Clocks.md).
        private void Update()
        {
            Tick(useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        /// <summary>
        ///     Advances context time, the playing line's duration and the idle scheduler by
        ///     <paramref name="deltaTime" /> seconds. While the component is enabled its <c>Update</c> calls it with
        ///     Unity's frame delta, so call it directly only from tests, replays or a loop that owns time instead (for
        ///     example on a disabled component). Non-positive, NaN and infinite deltas are ignored.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!(deltaTime > 0f) || float.IsInfinity(deltaTime))
            {
                return;
            }

            EnsurePool();
            _contextTime += deltaTime;
            if (_pool.IsPlaying)
            {
                if (lineDuration > 0f)
                {
                    _lineElapsed += deltaTime;
                    if (_lineElapsed >= lineDuration)
                    {
                        _pool.Finish();
                    }
                }

                return;
            }

            if (idleInterval <= 0f)
            {
                return;
            }

            _idleElapsed += deltaTime;
            if (_idleElapsed >= idleInterval)
            {
                _idleElapsed = 0f;
                PlayIdle();
            }
        }

        /// <summary>Plays an idle line unless any line is playing. Returns true when a line started.</summary>
        public bool PlayIdle()
        {
            return Play(idleTag, DialogueLinePool.IdlePriority);
        }

        /// <summary>Plays a contextual line of <paramref name="tag" />, interrupting lower-priority (idle) lines.</summary>
        public bool PlayContext(string tag)
        {
            return Play(tag, contextPriority);
        }

        /// <summary>Plays a line of <paramref name="tag" /> at <paramref name="priority" />.</summary>
        public bool Play(string tag, int priority)
        {
            return Pool.TryPlay(tag, _contextTime, priority, out PooledDialogueLine _);
        }

        /// <summary>UnityEvent-friendly <see cref="PlayIdle" />.</summary>
        public void RequestIdle()
        {
            PlayIdle();
        }

        /// <summary>UnityEvent-friendly <see cref="PlayContext" />.</summary>
        public void RequestContext(string tag)
        {
            PlayContext(tag);
        }

        /// <summary>Ends the current line now.</summary>
        public void FinishLine()
        {
            Pool.Finish();
        }

        /// <summary>Restarts context time and pick history (first-pick slots become available again).</summary>
        public void ResetSession()
        {
            Pool.Finish();
            Pool.ResetHistory();
            _contextTime = 0f;
            _idleElapsed = 0f;
        }

        /// <summary>Copies the authoring list into the pool and applies the repeat setting.</summary>
        public void RebuildPool()
        {
            EnsurePool();
            _pool.SetLines(lines);
            _pool.AvoidBackToBackRepeat = avoidBackToBackRepeat;
        }

        private void EnsurePool()
        {
            if (_pool != null)
            {
                return;
            }

            _pool = new DialogueLinePool(lines) { AvoidBackToBackRepeat = avoidBackToBackRepeat };
            _pool.LineStarted += HandleLineStarted;
            _pool.LineFinished += HandleLineFinished;
        }

        private void HandleLineStarted(PooledDialogueLine line)
        {
            _lineElapsed = 0f;
            _idleElapsed = 0f;
            if (lineText != null)
            {
                lineText.text = line.Text;
            }

            if (speakerText != null)
            {
                speakerText.text = line.Speaker;
            }

            OnSpeakerChanged?.Invoke(line.Speaker);
            OnLineStarted?.Invoke(line.Text);
        }

        private void HandleLineFinished(PooledDialogueLine line)
        {
            _idleElapsed = 0f;
            if (clearTextOnFinish)
            {
                if (lineText != null)
                {
                    lineText.text = string.Empty;
                }

                if (speakerText != null)
                {
                    speakerText.text = string.Empty;
                }
            }

            OnLineFinished?.Invoke();
        }
    }
}
