using System;
using UnityEngine;

namespace Neo.Rpg
{
    public static class RpgTimeUtility
    {
        public static double GetCurrentUnixTimestamp()
        {
            return DateTime.UtcNow
                .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .TotalSeconds;
        }
    }

    /// <summary>
    ///     Serializable active buff entry used by runtime effects, network snapshots, and save payloads.
    /// </summary>
    [Serializable]
    public sealed class ActiveBuffEntry
    {
        [SerializeField] private string _buffId = string.Empty;
        [SerializeField] private double _expiresAtUtc;
        [SerializeField] private int _stacks = 1;

        public string BuffId
        {
            get => _buffId;
            set => _buffId = value ?? string.Empty;
        }

        public double ExpiresAtUtc
        {
            get => _expiresAtUtc;
            set => _expiresAtUtc = value;
        }

        public int Stacks
        {
            get => _stacks;
            set => _stacks = Mathf.Max(1, value);
        }

        public double RemainingSeconds => Math.Max(0, _expiresAtUtc - RpgTimeUtility.GetCurrentUnixTimestamp());
    }

    /// <summary>
    ///     Serializable active status effect entry used by runtime effects, network snapshots, and save payloads.
    /// </summary>
    [Serializable]
    public sealed class ActiveStatusEntry
    {
        [SerializeField] private string _statusId = string.Empty;
        [SerializeField] private double _expiresAtUtc;
        [SerializeField] private int _stacks = 1;

        public string StatusId
        {
            get => _statusId;
            set => _statusId = value ?? string.Empty;
        }

        public double ExpiresAtUtc
        {
            get => _expiresAtUtc;
            set => _expiresAtUtc = value;
        }

        public int Stacks
        {
            get => _stacks;
            set => _stacks = Mathf.Max(1, value);
        }

        public double RemainingSeconds => Math.Max(0, _expiresAtUtc - RpgTimeUtility.GetCurrentUnixTimestamp());
    }
}
