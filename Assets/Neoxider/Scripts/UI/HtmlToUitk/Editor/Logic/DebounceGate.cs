using System;
using System.Collections.Generic;

namespace Neo.HtmlToUitk.Tooling
{
    public sealed class DebounceGate
    {
        private readonly HashSet<string> _pending = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private double _lastTouch;

        public DebounceGate(double quietSeconds)
        {
            QuietSeconds = quietSeconds;
        }

        public double QuietSeconds { get; set; }

        public bool HasPending => _pending.Count > 0;

        public void Touch(double now, string key)
        {
            _pending.Add(key ?? string.Empty);
            _lastTouch = now;
        }

        public bool TryFire(double now, List<string> keys)
        {
            if (_pending.Count == 0 || now - _lastTouch < QuietSeconds)
                return false;
            keys.Clear();
            keys.AddRange(_pending);
            keys.Sort(StringComparer.Ordinal);
            _pending.Clear();
            return true;
        }

        public void Clear()
        {
            _pending.Clear();
        }
    }
}
