using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Save
{
    /// <summary>
    ///     Builds stable save identities for scene components.
    /// </summary>
    public static class SaveIdentityUtility
    {
        // WHY: keys are rebuilt on every register, save and load; reuse one builder and component buffer. Neither is
        // held across a user callback (ISaveIdentityProvider.SaveIdentity runs before they are touched).
        private static readonly StringBuilder s_keyBuilder = new(128);
        private static readonly List<MonoBehaviour> s_componentBuffer = new();

        /// <summary>
        ///     Builds a full save key for the specified component, including the component type.
        /// </summary>
        /// <param name="monoBehaviour">Target component.</param>
        /// <returns>A stable component key, or an empty string when the component is null.</returns>
        public static string GetComponentKey(MonoBehaviour monoBehaviour)
        {
            if (monoBehaviour == null)
            {
                return string.Empty;
            }

            string typeName = monoBehaviour.GetType().FullName;
            if (TryGetCustomIdentity(monoBehaviour, out string customIdentity))
            {
                return string.Concat(typeName, ":", customIdentity);
            }

            s_keyBuilder.Clear();
            s_keyBuilder.Append(typeName).Append(':');
            AppendSceneIdentity(s_keyBuilder, monoBehaviour);
            string key = s_keyBuilder.ToString();
            s_keyBuilder.Clear();
            return key;
        }

        /// <summary>
        ///     Resolves the stable identity for the specified component.
        /// </summary>
        /// <param name="monoBehaviour">Target component.</param>
        /// <returns>
        ///     A custom identity from <see cref="ISaveIdentityProvider" /> when available; otherwise a scene-based identity.
        /// </returns>
        public static string GetStableIdentity(MonoBehaviour monoBehaviour)
        {
            if (monoBehaviour == null)
            {
                return string.Empty;
            }

            if (TryGetCustomIdentity(monoBehaviour, out string customIdentity))
            {
                return customIdentity;
            }

            s_keyBuilder.Clear();
            AppendSceneIdentity(s_keyBuilder, monoBehaviour);
            string identity = s_keyBuilder.ToString();
            s_keyBuilder.Clear();
            return identity;
        }

        private static bool TryGetCustomIdentity(MonoBehaviour monoBehaviour, out string identity)
        {
            if (monoBehaviour is ISaveIdentityProvider identityProvider)
            {
                identity = identityProvider.SaveIdentity;
                if (!string.IsNullOrWhiteSpace(identity))
                {
                    return true;
                }
            }

            identity = null;
            return false;
        }

        /// <summary>Appends <c>scene:hierarchy/path#sibling:componentIndex</c>.</summary>
        private static void AppendSceneIdentity(StringBuilder builder, MonoBehaviour monoBehaviour)
        {
            Scene scene = monoBehaviour.gameObject.scene;
            string sceneKey = scene.IsValid()
                ? string.IsNullOrEmpty(scene.path) ? scene.name : scene.path
                : "NoScene";

            builder.Append(sceneKey).Append(':');
            Transform transform = monoBehaviour.transform;
            if (transform == null)
            {
                builder.Append("NoTransform");
            }
            else
            {
                AppendTransformPath(builder, transform);
            }

            builder.Append(':').Append(GetComponentIndex(monoBehaviour));
        }

        private static void AppendTransformPath(StringBuilder builder, Transform transform)
        {
            if (transform.parent != null)
            {
                AppendTransformPath(builder, transform.parent);
                builder.Append('/');
            }

            builder.Append(transform.name);
            builder.Append('#');
            builder.Append(transform.GetSiblingIndex());
        }

        private static int GetComponentIndex(MonoBehaviour monoBehaviour)
        {
            s_componentBuffer.Clear();
            monoBehaviour.GetComponents(s_componentBuffer);
            Type type = monoBehaviour.GetType();
            int componentIndex = 0;
            try
            {
                for (int i = 0; i < s_componentBuffer.Count; i++)
                {
                    MonoBehaviour component = s_componentBuffer[i];
                    if (component == null)
                    {
                        continue;
                    }

                    if (component == monoBehaviour)
                    {
                        return componentIndex;
                    }

                    if (component.GetType() == type)
                    {
                        componentIndex++;
                    }
                }

                return componentIndex;
            }
            finally
            {
                s_componentBuffer.Clear();
            }
        }
    }
}
