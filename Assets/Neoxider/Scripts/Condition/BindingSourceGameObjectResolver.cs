using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Condition
{
    /// <summary>
    ///     Shared rules for resolving a binding root GameObject: <see cref="GameObject.Find(string)"/>, explicit
    ///     reference, or host fallback - used by <see cref="ConditionValueSource"/> and by Neo.NoCode float bindings.
    /// </summary>
    public static class BindingSourceGameObjectResolver
    {
        /// <summary>Default seconds between <see cref="GameObject.Find"/> attempts when the target is still missing.</summary>
        public const float DefaultFindRetryIntervalSeconds = 1f;

        /// <summary>
        ///     Cache for repeated <see cref="GameObject.Find"/> within one binding lifetime (cleared on invalidate).
        /// </summary>
        public struct ResolveCache
        {
            /// <summary>Last object returned by <see cref="GameObject.Find(string)"/>; reused while it is alive.</summary>
            public GameObject FoundByNameObject;

            /// <summary>
            ///     When <see cref="FoundByNameObject"/> is still null and retries are throttled: next time
            ///     <see cref="Time.realtimeSinceStartup"/> may run <see cref="GameObject.Find"/>.
            /// </summary>
            public float NextFindEligibleRealtime;

            /// <summary>Set after the one-time "object not found" warning; cleared when the object is found.</summary>
            public bool HasLoggedSearchNotFoundWarning;
        }

        /// <summary>
        ///     When <paramref name="useSceneSearch"/> and <paramref name="searchObjectName"/> are set, uses
        ///     <see cref="GameObject.Find(string)"/> (with cache and optional retry interval). Otherwise returns
        ///     <paramref name="sourceObject"/> or, when it is unassigned, <paramref name="hostFallback"/>; a destroyed
        ///     <paramref name="sourceObject"/> (see <see cref="IsDestroyedReference"/>) returns null instead.
        /// </summary>
        /// <param name="findRetryIntervalSeconds">
        ///     Seconds between Find attempts while the object is missing; does not block threads or spin. 0 = retry on
        ///     every <see cref="Resolve"/> call (no throttle).
        /// </param>
        public static GameObject Resolve(
            bool useSceneSearch,
            string searchObjectName,
            bool waitForObject,
            GameObject sourceObject,
            GameObject hostFallback,
            ref ResolveCache cache,
            string logPrefix,
            float findRetryIntervalSeconds = DefaultFindRetryIntervalSeconds)
        {
            float interval = Mathf.Max(0f, findRetryIntervalSeconds);

            if (useSceneSearch && !string.IsNullOrEmpty(searchObjectName))
            {
                GameObject cachedGo = cache.FoundByNameObject;
                if (cachedGo != null)
                {
                    return cachedGo;
                }

                cache.FoundByNameObject = null;

                float t = Time.realtimeSinceStartup;
                if (interval > 0f && t < cache.NextFindEligibleRealtime)
                {
                    return null;
                }

                GameObject found = GameObject.Find(searchObjectName);
                cache.FoundByNameObject = found;

                if (found != null)
                {
                    cache.HasLoggedSearchNotFoundWarning = false;
                    cache.NextFindEligibleRealtime = 0f;
                    return found;
                }

                if (!waitForObject && !cache.HasLoggedSearchNotFoundWarning)
                {
                    NeoDiagnostics.LogWarning(
                        $"{logPrefix} GameObject.Find(\"{searchObjectName}\") - object not found in scene.");
                    cache.HasLoggedSearchNotFoundWarning = true;
                }

                cache.NextFindEligibleRealtime = interval > 0f ? t + interval : 0f;
                return null;
            }

            if (IsDestroyedReference(sourceObject))
            {
                return null;
            }

            return sourceObject != null ? sourceObject : hostFallback;
        }

        /// <summary>
        ///     True when <paramref name="reference"/> pointed at an object that has since been destroyed (or whose
        ///     referenced object is missing). A field that was never assigned is not destroyed, also when the Unity
        ///     Editor deserialized it as an empty placeholder that compares equal to null, so it still falls back.
        /// </summary>
        /// <param name="reference">Serialized or cached reference; a real null returns false.</param>
        public static bool IsDestroyedReference(UnityEngine.Object reference)
        {
            // WHY: Unity's overloaded == treats a destroyed object as null while the managed reference survives, so
            // ReferenceEquals sees it. The Editor also fills unassigned serialized fields (in components,
            // ScriptableObjects and their nested classes) with a placeholder that never had a native object; its
            // GetHashCode (the instance id, read without the 6.5-obsolete GetInstanceID) is 0. Treating that
            // placeholder as destroyed stopped an empty source from falling back in Editor Play Mode.
            return !ReferenceEquals(reference, null) && reference == null && reference.GetHashCode() != 0;
        }

        /// <summary>Clears the cached Find result, the retry deadline and the missing-object warning flag.</summary>
        public static void InvalidateFindCache(ref ResolveCache cache)
        {
            cache.FoundByNameObject = null;
            cache.NextFindEligibleRealtime = 0f;
            cache.HasLoggedSearchNotFoundWarning = false;
        }

        /// <summary>
        ///     Returns the <paramref name="occurrence"/>-th component on <paramref name="root"/> whose type matches
        ///     <paramref name="componentTypeName"/> (see <see cref="MatchesComponentTypeName"/>), counting only matching
        ///     components in GetComponents order. Does not allocate, so bindings can retry a missing component on
        ///     every check.
        /// </summary>
        /// <returns>The component, or null when the root is missing/destroyed, the name is empty, the occurrence is
        ///     negative or there are not enough matching components.</returns>
        public static Component FindComponentOccurrence(GameObject root, string componentTypeName, int occurrence)
        {
            if (root == null || string.IsNullOrEmpty(componentTypeName) || occurrence < 0)
            {
                return null;
            }

            List<Component> components = ComponentScratch;
            try
            {
                root.GetComponents(components);
                int matchingOccurrence = 0;
                for (int i = 0; i < components.Count; i++)
                {
                    Component component = components[i];
                    if (component == null || !MatchesComponentTypeName(component.GetType(), componentTypeName))
                    {
                        continue;
                    }

                    if (matchingOccurrence == occurrence)
                    {
                        return component;
                    }

                    matchingOccurrence++;
                }

                return null;
            }
            finally
            {
                // WHY: the scratch list is shared; never keep scene components alive between lookups.
                components.Clear();
            }
        }

        /// <summary>
        ///     True when <paramref name="componentTypeName"/> equals the type's full name or its short name
        ///     (the same rule the Inspector pickers store). Allocation-free for non-generic types.
        /// </summary>
        public static bool MatchesComponentTypeName(Type type, string componentTypeName)
        {
            if (type == null || string.IsNullOrEmpty(componentTypeName))
            {
                return false;
            }

            string fullName = type.FullName;
            if (string.Equals(fullName, componentTypeName, StringComparison.Ordinal))
            {
                return true;
            }

            if (fullName == null || fullName.IndexOf('[') >= 0)
            {
                // WHY: closed generic full names carry assembly-qualified arguments, so only Type.Name is comparable.
                return string.Equals(type.Name, componentTypeName, StringComparison.Ordinal);
            }

            // WHY: Type.Name allocates on Mono; for non-generic types it is the segment after the last '.' or '+'.
            int nameLength = componentTypeName.Length;
            if (fullName.Length <= nameLength || componentTypeName.IndexOf('.') >= 0 ||
                componentTypeName.IndexOf('+') >= 0 ||
                !fullName.EndsWith(componentTypeName, StringComparison.Ordinal))
            {
                return false;
            }

            char separator = fullName[fullName.Length - nameLength - 1];
            return separator == '.' || separator == '+';
        }

        private static readonly List<Component> ComponentScratch = new();
    }
}
