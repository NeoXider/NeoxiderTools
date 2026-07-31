using System;
using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Open registry of effect operations. Built-ins are installed by <see cref="AbilitySystem" />;
    ///     games may register custom ops (or override built-ins) at startup.
    /// </summary>
    public sealed class EffectOpRegistry
    {
        private readonly Dictionary<string, IEffectOperation> _ops =
            new Dictionary<string, IEffectOperation>(StringComparer.OrdinalIgnoreCase);

        public void Register(IEffectOperation op)
        {
            if (op == null || string.IsNullOrEmpty(op.Id))
            {
                return;
            }

            _ops[op.Id] = op;
        }

        public bool TryGet(string opId, out IEffectOperation op)
        {
            op = null;
            return !string.IsNullOrEmpty(opId) && _ops.TryGetValue(opId, out op);
        }

        public IEnumerable<string> RegisteredIds => _ops.Keys;
    }
}
