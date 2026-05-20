using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Rpg.Runtime
{
    /// <summary>
    ///     Single source of truth for active buffs / status effects on a runtime character.
    ///     <para>Replaces the duplicated <c>_activeBuffs</c> / <c>_activeStatuses</c> lists that used to
    ///     live in multiple components; <c>RpgCharacter</c> holds ONE shelf and the
    ///     lifecycle code (apply / refresh / expire / tick) exists exactly once.</para>
    ///     Pure C# - not a MonoBehaviour, so it never adds an extra hierarchy entry.
    /// </summary>
    public sealed class RpgEffectShelf
    {
        private readonly Dictionary<string, BuffDefinition> _buffLibrary = new();
        private readonly Dictionary<string, StatusEffectDefinition> _statusLibrary = new();
        private readonly Dictionary<string, InlineBuffEntry> _inlineLibrary = new();

        private readonly List<ActiveBuffEntry> _activeBuffs = new();
        private readonly List<ActiveStatusEntry> _activeStatuses = new();
        private readonly Dictionary<string, float> _statusTickAccumulators = new();

        public IReadOnlyList<ActiveBuffEntry> ActiveBuffs => _activeBuffs;
        public IReadOnlyList<ActiveStatusEntry> ActiveStatuses => _activeStatuses;

        // ── Library registration ──

        /// <summary>Populates the buff library from a definition array (usually template + character extras).</summary>
        public void RegisterBuffLibrary(IEnumerable<BuffDefinition> defs)
        {
            _buffLibrary.Clear();
            if (defs == null) return;
            foreach (BuffDefinition def in defs)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.Id)) continue;
                _buffLibrary[def.Id] = def;
            }
        }

        public void RegisterStatusLibrary(IEnumerable<StatusEffectDefinition> defs)
        {
            _statusLibrary.Clear();
            if (defs == null) return;
            foreach (StatusEffectDefinition def in defs)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.Id)) continue;
                _statusLibrary[def.Id] = def;
            }
        }

        /// <summary>Adds inline buff entries to the lookup table so ApplyBuffById / projection works.</summary>
        public void RegisterInlineBuffs(IEnumerable<InlineBuffEntry> entries)
        {
            _inlineLibrary.Clear();
            if (entries == null) return;
            foreach (InlineBuffEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id)) continue;
                _inlineLibrary[entry.Id] = entry;
            }
        }

        public bool TryGetBuff(string id, out BuffDefinition def) =>
            _buffLibrary.TryGetValue(id ?? string.Empty, out def);

        public bool TryGetStatus(string id, out StatusEffectDefinition def) =>
            _statusLibrary.TryGetValue(id ?? string.Empty, out def);

        public bool TryGetInlineBuff(string id, out InlineBuffEntry entry) =>
            _inlineLibrary.TryGetValue(id ?? string.Empty, out entry);

        // ── Apply / remove ──

        /// <summary>Apply a buff. Returns the active entry (new or stacked) and whether it was newly added.</summary>
        public ApplyResult<ActiveBuffEntry> ApplyBuff(BuffDefinition def)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.Id))
                return ApplyResult<ActiveBuffEntry>.Failed("Definition is null or has empty Id.");

            _buffLibrary[def.Id] = def; // ensure library lookup works for buffs applied at runtime
            double expiresAt = RpgTimeUtility.GetCurrentUnixTimestamp() + Mathf.Max(0.01f, def.Duration);

            ActiveBuffEntry existing = FindBuff(def.Id);
            if (existing != null)
            {
                // Refresh — push the timer forward, that's the usual expectation.
                existing.ExpiresAtUtc = expiresAt;
                return ApplyResult<ActiveBuffEntry>.Ok(existing, isNew: false);
            }

            ActiveBuffEntry entry = new() { BuffId = def.Id, ExpiresAtUtc = expiresAt };
            _activeBuffs.Add(entry);
            return ApplyResult<ActiveBuffEntry>.Ok(entry, isNew: true);
        }

        /// <summary>Applies an inline (non-SO) buff. Same lifecycle as the SO variant.</summary>
        public ApplyResult<ActiveBuffEntry> ApplyInlineBuff(InlineBuffEntry inline)
        {
            if (inline == null || string.IsNullOrWhiteSpace(inline.Id))
                return ApplyResult<ActiveBuffEntry>.Failed("InlineBuffEntry is null or has empty Id.");

            _inlineLibrary[inline.Id] = inline;
            double expiresAt = RpgTimeUtility.GetCurrentUnixTimestamp() + Mathf.Max(0.01f, inline.Duration);

            ActiveBuffEntry existing = FindBuff(inline.Id);
            if (existing != null)
            {
                existing.ExpiresAtUtc = expiresAt;
                return ApplyResult<ActiveBuffEntry>.Ok(existing, isNew: false);
            }

            ActiveBuffEntry entry = new() { BuffId = inline.Id, ExpiresAtUtc = expiresAt };
            _activeBuffs.Add(entry);
            return ApplyResult<ActiveBuffEntry>.Ok(entry, isNew: true);
        }

        public bool RemoveBuff(string id)
        {
            ActiveBuffEntry entry = FindBuff(id);
            if (entry == null) return false;
            _activeBuffs.Remove(entry);
            return true;
        }

        public void ClearAllBuffs() => _activeBuffs.Clear();

        public ApplyResult<ActiveStatusEntry> ApplyStatus(StatusEffectDefinition def)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.Id))
                return ApplyResult<ActiveStatusEntry>.Failed("Definition is null or has empty Id.");

            _statusLibrary[def.Id] = def;
            double expiresAt = RpgTimeUtility.GetCurrentUnixTimestamp() + Mathf.Max(0.01f, def.Duration);

            ActiveStatusEntry existing = FindStatus(def.Id);
            if (existing != null)
            {
                if (def.Stackable && existing.Stacks < Mathf.Max(1, def.MaxStacks))
                {
                    existing.Stacks++;
                }
                existing.ExpiresAtUtc = expiresAt;
                return ApplyResult<ActiveStatusEntry>.Ok(existing, isNew: false);
            }

            ActiveStatusEntry entry = new() { StatusId = def.Id, ExpiresAtUtc = expiresAt, Stacks = 1 };
            _activeStatuses.Add(entry);
            return ApplyResult<ActiveStatusEntry>.Ok(entry, isNew: true);
        }

        public bool RemoveStatus(string id)
        {
            ActiveStatusEntry entry = FindStatus(id);
            if (entry == null) return false;
            _activeStatuses.Remove(entry);
            _statusTickAccumulators.Remove(entry.StatusId);
            return true;
        }

        public void ClearAllStatuses()
        {
            _activeStatuses.Clear();
            _statusTickAccumulators.Clear();
        }

        public bool HasBuff(string id) => FindBuff(id) != null;
        public bool HasStatus(string id) => FindStatus(id) != null;

        // ── Tick ──

        /// <summary>
        ///     Advance time. Removes expired entries (calls expire callbacks), accumulates status tick damage
        ///     and invokes <paramref name="statusDamageCallback"/> for each tick of damage.
        /// </summary>
        public void Tick(float deltaTime, Action<string> onBuffExpired, Action<string> onStatusExpired,
            Action<StatusEffectDefinition, float> statusDamageCallback)
        {
            double now = RpgTimeUtility.GetCurrentUnixTimestamp();

            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                if (_activeBuffs[i].ExpiresAtUtc > now) continue;
                string id = _activeBuffs[i].BuffId;
                _activeBuffs.RemoveAt(i);
                onBuffExpired?.Invoke(id);
            }

            for (int i = _activeStatuses.Count - 1; i >= 0; i--)
            {
                ActiveStatusEntry entry = _activeStatuses[i];

                if (statusDamageCallback != null &&
                    TryGetStatus(entry.StatusId, out StatusEffectDefinition def) &&
                    def != null && def.TickDamagePerSecond > 0f && def.Duration > 0f)
                {
                    if (!_statusTickAccumulators.TryGetValue(entry.StatusId, out float acc)) acc = 0f;
                    acc += deltaTime;
                    float interval = def.TickInterval;
                    while (acc >= interval)
                    {
                        statusDamageCallback(def, def.TickDamagePerSecond * interval * Mathf.Max(1, entry.Stacks));
                        acc -= interval;
                    }
                    _statusTickAccumulators[entry.StatusId] = acc;
                }

                if (entry.ExpiresAtUtc <= now)
                {
                    string id = entry.StatusId;
                    _activeStatuses.RemoveAt(i);
                    _statusTickAccumulators.Remove(id);
                    onStatusExpired?.Invoke(id);
                }
            }
        }

        // ── Modifier projection ──

        /// <summary>
        ///     Projects each active buff's <c>BuffStatModifier[]</c> into a flat list of
        ///     <see cref="BuffStatModifierApplication"/> for <see cref="RpgStatResolver"/>.
        ///     Caller is expected to reuse the list (we Clear and fill).
        /// </summary>
        public void BuildModifierApplications(List<BuffStatModifierApplication> buffer)
        {
            buffer.Clear();
            for (int i = 0; i < _activeBuffs.Count; i++)
            {
                string id = _activeBuffs[i].BuffId;

                // SO buff
                if (TryGetBuff(id, out BuffDefinition def) && def != null && def.Modifiers != null)
                {
                    AppendModifiers(buffer, def.Modifiers);
                    continue;
                }

                // Inline buff (rare but matters for one-off scene effects)
                if (TryGetInlineBuff(id, out InlineBuffEntry inline) && inline != null && inline.Modifiers != null)
                {
                    AppendModifiers(buffer, inline.Modifiers);
                }
            }
        }

        private static void AppendModifiers(List<BuffStatModifierApplication> buffer, BuffStatModifier[] mods)
        {
            for (int j = 0; j < mods.Length; j++)
            {
                BuffStatModifier m = mods[j];
                if (m == null) continue;
                buffer.Add(new BuffStatModifierApplication(
                    m.StatType,
                    m.TargetIdValue,
                    m.SpecificDamageType,
                    m.Value,
                    stacks: 1));
            }
        }

        // ── Persistence helpers ──

        /// <summary>Copies the active entries into save/snapshot collections.</summary>
        public void CopyActiveEffectsTo(ICollection<ActiveBuffEntry> buffs, ICollection<ActiveStatusEntry> statuses)
        {
            buffs?.Clear();
            statuses?.Clear();

            foreach (ActiveBuffEntry e in _activeBuffs)
                buffs?.Add(new ActiveBuffEntry { BuffId = e.BuffId, ExpiresAtUtc = e.ExpiresAtUtc });

            foreach (ActiveStatusEntry e in _activeStatuses)
                statuses?.Add(new ActiveStatusEntry
                {
                    StatusId = e.StatusId, ExpiresAtUtc = e.ExpiresAtUtc, Stacks = e.Stacks
                });
        }

        /// <summary>Restores active entries from save/snapshot collections.</summary>
        public void RestoreActiveEffects(IEnumerable<ActiveBuffEntry> buffs, IEnumerable<ActiveStatusEntry> statuses)
        {
            _activeBuffs.Clear();
            _activeStatuses.Clear();
            _statusTickAccumulators.Clear();
            if (buffs != null) _activeBuffs.AddRange(buffs);
            if (statuses != null) _activeStatuses.AddRange(statuses);
        }

        // ── Internals ──

        private ActiveBuffEntry FindBuff(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < _activeBuffs.Count; i++)
                if (_activeBuffs[i].BuffId == id) return _activeBuffs[i];
            return null;
        }

        private ActiveStatusEntry FindStatus(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < _activeStatuses.Count; i++)
                if (_activeStatuses[i].StatusId == id) return _activeStatuses[i];
            return null;
        }

        public readonly struct ApplyResult<T> where T : class
        {
            public readonly T Entry;
            public readonly bool Success;
            public readonly bool IsNew;
            public readonly string FailureReason;

            private ApplyResult(T entry, bool success, bool isNew, string failureReason)
            {
                Entry = entry; Success = success; IsNew = isNew; FailureReason = failureReason;
            }

            public static ApplyResult<T> Ok(T entry, bool isNew) => new(entry, true, isNew, null);
            public static ApplyResult<T> Failed(string reason) => new(null, false, false, reason);
        }
    }
}
