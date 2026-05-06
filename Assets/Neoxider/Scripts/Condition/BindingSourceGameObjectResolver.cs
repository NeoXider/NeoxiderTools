using UnityEngine;

namespace Neo.Condition
{
    /// <summary>
    ///     Shared rules for resolving a binding root GameObject: <see cref="GameObject.Find(string)"/>, explicit
    ///     reference, or host fallback — used by <see cref="ConditionValueSource"/> and by Neo.NoCode float bindings.
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
            public GameObject FoundByNameObject;

            /// <summary>
            ///     When <see cref="FoundByNameObject"/> is still null and retries are throttled: next time
            ///     <see cref="Time.realtimeSinceStartup"/> may run <see cref="GameObject.Find"/>.
            /// </summary>
            public float NextFindEligibleRealtime;

            public bool HasLoggedSearchNotFoundWarning;
        }

        /// <summary>
        ///     When <paramref name="useSceneSearch"/> and <paramref name="searchObjectName"/> are set, uses
        ///     <see cref="GameObject.Find(string)"/> (with cache and optional retry interval). Otherwise returns
        ///     <paramref name="sourceObject"/> or <paramref name="hostFallback"/>.
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
                    Debug.LogWarning(
                        $"{logPrefix} GameObject.Find(\"{searchObjectName}\") — object not found in scene.");
                    cache.HasLoggedSearchNotFoundWarning = true;
                }

                cache.NextFindEligibleRealtime = interval > 0f ? t + interval : 0f;
                return null;
            }

            if (IsDestroyedSentinel(sourceObject))
            {
                return null;
            }

            return sourceObject != null ? sourceObject : hostFallback;
        }

        public static void InvalidateFindCache(ref ResolveCache cache)
        {
            cache.FoundByNameObject = null;
            cache.NextFindEligibleRealtime = 0f;
            cache.HasLoggedSearchNotFoundWarning = false;
        }

        private static bool IsDestroyedSentinel(GameObject obj)
        {
            return obj != null && !ReferenceEquals(obj, null) && obj == null;
        }
    }
}
