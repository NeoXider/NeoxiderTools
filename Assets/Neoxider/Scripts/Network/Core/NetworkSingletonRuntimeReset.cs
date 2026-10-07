using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Network
{
    /// <summary>
    ///     Shared invalidation counter for <see cref="NetworkSingleton{T}.I"/>: every scene load or unload bumps
    ///     <see cref="Epoch"/>, which makes each singleton type search the scenes again instead of trusting an earlier miss.
    /// </summary>
    internal static class NetworkSingletonSearchState
    {
        internal static int Epoch;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Epoch = 0;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Epoch++;
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            Epoch++;
        }
    }

    internal static class NetworkSingletonRuntimeReset
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type[] types;
                try
                {
                    types = assemblies[i].GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }
                catch
                {
                    continue;
                }

                if (types == null)
                {
                    continue;
                }

                for (int j = 0; j < types.Length; j++)
                {
                    Type candidate = types[j];
                    if (candidate == null || candidate.IsAbstract || candidate.ContainsGenericParameters)
                    {
                        continue;
                    }

                    if (!IsSubclassOfRawGeneric(candidate, typeof(NetworkSingleton<>)))
                    {
                        continue;
                    }

                    Type closedGenericType = typeof(NetworkSingleton<>).MakeGenericType(candidate);
                    MethodInfo resetMethod = closedGenericType.GetMethod(
                        "ResetStaticStateForRuntime",
                        BindingFlags.Static | BindingFlags.NonPublic);

                    resetMethod?.Invoke(null, null);
                }
            }
        }

        private static bool IsSubclassOfRawGeneric(Type type, Type rawGeneric)
        {
            Type current = type;
            while (current != null && current != typeof(object))
            {
                Type candidate = current.IsGenericType ? current.GetGenericTypeDefinition() : current;
                if (candidate == rawGeneric)
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }
    }
}
