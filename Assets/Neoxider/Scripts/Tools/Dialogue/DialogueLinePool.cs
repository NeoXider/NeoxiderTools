using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>Which picks of a tag a <see cref="PooledDialogueLine" /> may take part in.</summary>
    public enum PooledLineSlot
    {
        /// <summary>Eligible for every pick of its tag.</summary>
        Any,

        /// <summary>Eligible only for the first pick of its tag since the last history reset.</summary>
        FirstOnly,

        /// <summary>Eligible only after the tag has been picked at least once.</summary>
        AfterFirst
    }

    /// <summary>
    ///     One line in a <see cref="DialogueLinePool" />: free-standing text picked by tag (barks, idle chatter,
    ///     announcer calls), not a node of a linear <see cref="Dialogue" /> tree.
    /// </summary>
    [Serializable]
    public sealed class PooledDialogueLine
    {
        [Tooltip("Group this line belongs to, e.g. \"idle\", \"hit\", \"win\". Requests pick by tag.")]
        [SerializeField]
        private string tag = "idle";

        [Tooltip("Optional speaker name shown with the line.")] [SerializeField]
        private string speaker;

        [TextArea(2, 5)] [SerializeField] private string text;

        [Tooltip("Relative pick weight inside its tag. 0 disables the line.")] [Min(0f)] [SerializeField]
        private float weight = 1f;

        [Tooltip("Earliest context time (seconds) at which the line may be picked.")] [Min(0f)] [SerializeField]
        private float minTime;

        [Tooltip("Latest context time (seconds) at which the line may be picked. 0 or less = no upper limit.")]
        [SerializeField]
        private float maxTime;

        [Tooltip("Restrict the line to the first pick of its tag, or to every pick after the first.")]
        [SerializeField]
        private PooledLineSlot slot = PooledLineSlot.Any;

        /// <summary>Creates an empty line (used by serialization).</summary>
        public PooledDialogueLine()
        {
        }

        /// <summary>Creates a line for <paramref name="lineTag" />.</summary>
        public PooledDialogueLine(string lineTag, string lineText, float lineWeight = 1f)
        {
            tag = lineTag;
            text = lineText;
            weight = lineWeight;
        }

        /// <summary>Group this line belongs to; requests pick by tag (ordinal comparison).</summary>
        public string Tag
        {
            get => tag;
            set => tag = value;
        }

        /// <summary>Optional speaker name.</summary>
        public string Speaker
        {
            get => speaker;
            set => speaker = value;
        }

        /// <summary>Line text.</summary>
        public string Text
        {
            get => text;
            set => text = value;
        }

        /// <summary>Relative pick weight inside its tag; 0 disables the line.</summary>
        public float Weight
        {
            get => weight;
            set => weight = value;
        }

        /// <summary>Earliest context time (seconds) at which the line may be picked.</summary>
        public float MinTime
        {
            get => minTime;
            set => minTime = value;
        }

        /// <summary>Latest context time (seconds); 0 or less means no upper limit.</summary>
        public float MaxTime
        {
            get => maxTime;
            set => maxTime = value;
        }

        /// <summary>Restricts the line to the first pick of its tag or to later picks.</summary>
        public PooledLineSlot Slot
        {
            get => slot;
            set => slot = value;
        }

        /// <summary>True when <paramref name="contextTime" /> lies in [minTime, maxTime]; NaN ignores the window.</summary>
        public bool IsInWindow(float contextTime)
        {
            if (float.IsNaN(contextTime))
            {
                return true;
            }

            return contextTime >= minTime && (maxTime <= 0f || contextTime <= maxTime);
        }
    }

    /// <summary>
    ///     Plain C# picker for pooled dialogue lines: picks a weighted random line by tag, never repeats the previous
    ///     line of a tag back to back (when another candidate exists), honours optional time windows and first-pick
    ///     slots, and tracks one playing line so a higher-priority contextual line pre-empts a lower-priority idle one.
    ///     Usable without <see cref="DialogueController" />; <see cref="DialogueLinePlayer" /> is the scene wrapper.
    /// </summary>
    /// <example>
    ///     <code>
    /// DialogueLinePool pool = new DialogueLinePool(lines);
    /// pool.LineStarted += line => label.text = line.Text;
    /// pool.TryPlay("idle", elapsedSeconds, DialogueLinePool.IdlePriority, out _);
    /// pool.TryPlay("hit", elapsedSeconds, DialogueLinePool.ContextPriority, out _); // interrupts the idle line
    /// pool.Finish();
    /// </code>
    /// </example>
    public sealed class DialogueLinePool
    {
        /// <summary>Suggested priority for ambient idle lines.</summary>
        public const int IdlePriority = 0;

        /// <summary>Suggested priority for contextual lines that should interrupt idle chatter.</summary>
        public const int ContextPriority = 10;

        private readonly List<PooledDialogueLine> _lines = new();
        private readonly Dictionary<string, PooledDialogueLine> _lastByTag = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _pickCountByTag = new(StringComparer.Ordinal);

        /// <summary>Creates a pool over <paramref name="lines" /> (null entries are skipped).</summary>
        public DialogueLinePool(IEnumerable<PooledDialogueLine> lines = null)
        {
            SetLines(lines);
        }

        /// <summary>Raised when a line starts playing through <see cref="TryPlay" />.</summary>
        public event Action<PooledDialogueLine> LineStarted;

        /// <summary>Raised with the old line when a higher-priority line pre-empts it.</summary>
        public event Action<PooledDialogueLine> LineInterrupted;

        /// <summary>Raised when <see cref="Finish" /> ends the playing line.</summary>
        public event Action<PooledDialogueLine> LineFinished;

        /// <summary>Lines in the pool.</summary>
        public IReadOnlyList<PooledDialogueLine> Lines => _lines;

        /// <summary>Source of uniform values in [0..1); <c>null</c> uses <see cref="UnityEngine.Random.value" />.</summary>
        public Func<float> RandomProvider { get; set; }

        /// <summary>When true (default), the previous line of a tag is skipped while another candidate exists.</summary>
        public bool AvoidBackToBackRepeat { get; set; } = true;

        /// <summary>Line currently playing, or null.</summary>
        public PooledDialogueLine Current { get; private set; }

        /// <summary>Priority of <see cref="Current" />; meaningless while nothing plays.</summary>
        public int CurrentPriority { get; private set; }

        /// <summary>True while a line is playing.</summary>
        public bool IsPlaying => Current != null;

        /// <summary>Replaces the pool content. History and the playing line are kept.</summary>
        public void SetLines(IEnumerable<PooledDialogueLine> lines)
        {
            _lines.Clear();
            if (lines == null)
            {
                return;
            }

            foreach (PooledDialogueLine line in lines)
            {
                if (line != null)
                {
                    _lines.Add(line);
                }
            }
        }

        /// <summary>Adds one line.</summary>
        public void Add(PooledDialogueLine line)
        {
            if (line != null)
            {
                _lines.Add(line);
            }
        }

        /// <summary>Forgets the last line and the pick count of every tag (start of a new round or scene).</summary>
        public void ResetHistory()
        {
            _lastByTag.Clear();
            _pickCountByTag.Clear();
        }

        /// <summary>How many times <paramref name="tag" /> was picked since the last history reset.</summary>
        public int GetPickCount(string tag)
        {
            return _pickCountByTag.TryGetValue(tag ?? string.Empty, out int count) ? count : 0;
        }

        /// <summary>Picks a line by tag ignoring time windows. Does not change <see cref="Current" />.</summary>
        public bool TryPick(string tag, out PooledDialogueLine line)
        {
            return TryPick(tag, float.NaN, out line);
        }

        /// <summary>
        ///     Picks a weighted random line of <paramref name="tag" /> whose window contains
        ///     <paramref name="contextTime" /> (NaN ignores windows). Records it as the tag's last line. Does not
        ///     change <see cref="Current" />. Returns false when no line is eligible.
        /// </summary>
        public bool TryPick(string tag, float contextTime, out PooledDialogueLine line)
        {
            string key = tag ?? string.Empty;
            int pickCount = GetPickCount(key);
            _lastByTag.TryGetValue(key, out PooledDialogueLine previous);

            float total = SumWeights(key, contextTime, pickCount, null, out int candidates);
            PooledDialogueLine excluded = null;
            if (AvoidBackToBackRepeat && previous != null && candidates > 1 &&
                IsCandidate(previous, key, contextTime, pickCount))
            {
                excluded = previous;
                total = SumWeights(key, contextTime, pickCount, excluded, out candidates);
            }

            if (candidates == 0 || total <= 0f)
            {
                line = null;
                return false;
            }

            float roll = Mathf.Clamp01(RandomProvider != null ? RandomProvider() : UnityEngine.Random.value) * total;
            line = null;
            float cumulative = 0f;
            for (int i = 0; i < _lines.Count; i++)
            {
                PooledDialogueLine candidate = _lines[i];
                if (ReferenceEquals(candidate, excluded) || !IsCandidate(candidate, key, contextTime, pickCount))
                {
                    continue;
                }

                line = candidate;
                cumulative += candidate.Weight;
                if (roll < cumulative)
                {
                    break;
                }
            }

            _lastByTag[key] = line;
            _pickCountByTag[key] = pickCount + 1;
            return line != null;
        }

        /// <summary>
        ///     Picks and starts a line unless a line of equal or higher priority is playing. A strictly higher
        ///     <paramref name="priority" /> interrupts the playing line (raising <see cref="LineInterrupted" />).
        ///     When nothing is eligible the playing line continues and false is returned.
        /// </summary>
        public bool TryPlay(string tag, float contextTime, int priority, out PooledDialogueLine line)
        {
            if (Current != null && priority <= CurrentPriority)
            {
                line = null;
                return false;
            }

            if (!TryPick(tag, contextTime, out line))
            {
                return false;
            }

            PooledDialogueLine interrupted = Current;
            Current = line;
            CurrentPriority = priority;
            if (interrupted != null)
            {
                LineInterrupted?.Invoke(interrupted);
            }

            if (ReferenceEquals(Current, line))
            {
                LineStarted?.Invoke(line);
            }

            return true;
        }

        /// <summary>Ends the playing line and raises <see cref="LineFinished" />; no-op while nothing plays.</summary>
        public void Finish()
        {
            PooledDialogueLine finished = Current;
            if (finished == null)
            {
                return;
            }

            Current = null;
            CurrentPriority = 0;
            LineFinished?.Invoke(finished);
        }

        private float SumWeights(string tag, float contextTime, int pickCount, PooledDialogueLine excluded,
            out int candidates)
        {
            float total = 0f;
            candidates = 0;
            for (int i = 0; i < _lines.Count; i++)
            {
                PooledDialogueLine line = _lines[i];
                if (ReferenceEquals(line, excluded) || !IsCandidate(line, tag, contextTime, pickCount))
                {
                    continue;
                }

                total += line.Weight;
                candidates++;
            }

            return total;
        }

        private static bool IsCandidate(PooledDialogueLine line, string tag, float contextTime, int pickCount)
        {
            if (line == null || !(line.Weight > 0f) || float.IsInfinity(line.Weight) ||
                !string.Equals(line.Tag ?? string.Empty, tag, StringComparison.Ordinal) || !line.IsInWindow(contextTime))
            {
                return false;
            }

            return line.Slot switch
            {
                PooledLineSlot.FirstOnly => pickCount == 0,
                PooledLineSlot.AfterFirst => pickCount > 0,
                _ => true
            };
        }
    }
}
