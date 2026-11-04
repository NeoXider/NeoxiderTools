using System.Collections.Generic;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Resolved Animator parameter: its name hash and whether the bound controller declares it with the expected type.
    /// </summary>
    internal readonly struct AnimatorParameterHandle
    {
        public readonly int Hash;
        public readonly bool Exists;

        public AnimatorParameterHandle(int hash)
        {
            Hash = hash;
            Exists = true;
        }
    }

    /// <summary>
    ///     Caches the parameters of an Animator's current controller so drivers can push values every frame by hash,
    ///     without re-hashing names and without Unity's per-call "parameter does not exist" warnings.
    /// </summary>
    /// <remarks>
    ///     The cache rebinds automatically when the Animator reference or its runtime controller changes; call
    ///     <see cref="Invalidate" /> after renaming the configured parameters.
    /// </remarks>
    internal sealed class AnimatorParameterCache
    {
        private readonly Dictionary<int, AnimatorControllerParameterType> _types = new();
        private Animator _animator;
        private RuntimeAnimatorController _controller;
        private bool _bound;

        /// <summary>Incremented on every rebind; drivers re-resolve their handles when it changes.</summary>
        public int Version { get; private set; }

        /// <summary>Forces the next <see cref="Refresh" /> to rebuild the cache.</summary>
        public void Invalidate()
        {
            _bound = false;
        }

        /// <summary>
        ///     Rebinds when needed. Returns false while the Animator has no controller or is not initialized yet
        ///     (for example on an inactive object), in which case no parameter should be written this frame.
        /// </summary>
        public bool Refresh(Animator animator)
        {
            if (animator == null || !animator.isInitialized)
            {
                return false;
            }

            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            if (controller == null)
            {
                return false;
            }

            if (_bound && ReferenceEquals(animator, _animator) && ReferenceEquals(controller, _controller))
            {
                return true;
            }

            _animator = animator;
            _controller = controller;
            _types.Clear();
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                _types[parameters[i].nameHash] = parameters[i].type;
            }

            _bound = true;
            Version++;
            return true;
        }

        /// <summary>
        ///     Resolves <paramref name="name" /> against the bound controller. Blank names and missing or differently
        ///     typed parameters resolve to a handle whose <see cref="AnimatorParameterHandle.Exists" /> is false.
        /// </summary>
        public AnimatorParameterHandle Resolve(string name, AnimatorControllerParameterType type)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return default;
            }

            int hash = Animator.StringToHash(name);
            return _types.TryGetValue(hash, out AnimatorControllerParameterType actual) && actual == type
                ? new AnimatorParameterHandle(hash)
                : default;
        }

        /// <summary>
        ///     Same as <see cref="Resolve(string, AnimatorControllerParameterType)" />, and when
        ///     <paramref name="reportMissing" /> is true a non-blank name that does not resolve logs one gated
        ///     <see cref="NeoDiagnostics" /> warning. Drivers resolve only on rebind, so the warning appears once per
        ///     name per controller binding instead of every frame.
        /// </summary>
        public AnimatorParameterHandle Resolve(string name, AnimatorControllerParameterType type, Object context,
            bool reportMissing)
        {
            AnimatorParameterHandle handle = Resolve(name, type);
            if (handle.Exists || !reportMissing || string.IsNullOrWhiteSpace(name)
                || !NeoDiagnostics.RuntimeWarningsEnabled)
            {
                return handle;
            }

            string owner = context != null ? context.GetType().Name : nameof(AnimatorParameterCache);
            string controllerName = _controller != null ? _controller.name : "null";
            string problem = _types.TryGetValue(Animator.StringToHash(name), out AnimatorControllerParameterType actual)
                ? $"declares '{name}' as {actual}, expected {type}"
                : $"has no {type} parameter '{name}'";
            NeoDiagnostics.LogWarning(
                $"[{owner}] Animator controller '{controllerName}' {problem}; the parameter is not driven.", context);
            return handle;
        }
    }
}
