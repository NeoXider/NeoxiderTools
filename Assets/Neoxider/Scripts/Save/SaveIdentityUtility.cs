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

            return $"{monoBehaviour.GetType().FullName}:{GetStableIdentity(monoBehaviour)}";
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

            if (monoBehaviour is ISaveIdentityProvider identityProvider &&
                !string.IsNullOrWhiteSpace(identityProvider.SaveIdentity))
            {
                return identityProvider.SaveIdentity;
            }

            return BuildSceneIdentity(monoBehaviour);
        }

        private static string BuildSceneIdentity(MonoBehaviour monoBehaviour)
        {
            Scene scene = monoBehaviour.gameObject.scene;
            string sceneKey = scene.IsValid()
                ? string.IsNullOrEmpty(scene.path) ? scene.name : scene.path
                : "NoScene";

            string hierarchyPath = BuildHierarchyPath(monoBehaviour.transform);
            int componentIndex = GetComponentIndex(monoBehaviour);
            return $"{sceneKey}:{hierarchyPath}:{componentIndex}";
        }

        private static string BuildHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return "NoTransform";
            }

            StringBuilder builder = new();
            AppendTransformPath(builder, transform);
            return builder.ToString();
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
            MonoBehaviour[] components = monoBehaviour.GetComponents<MonoBehaviour>();
            int componentIndex = 0;
            for (int i = 0; i < components.Length; i++)
            {
                MonoBehaviour component = components[i];
                if (component == null)
                {
                    continue;
                }

                if (component == monoBehaviour)
                {
                    return componentIndex;
                }

                if (component.GetType() == monoBehaviour.GetType())
                {
                    componentIndex++;
                }
            }

            return componentIndex;
        }
    }
}
