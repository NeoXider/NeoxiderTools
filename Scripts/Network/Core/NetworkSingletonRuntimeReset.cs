using System;
using System.Collections.Generic;
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

    /// <summary>
    ///     Clears every closed <see cref="NetworkSingleton{T}"/>'s static instance at play start for editors that keep
    ///     the domain between play sessions.
    /// </summary>
    internal static class NetworkSingletonRuntimeReset
    {
        // WHY: a player starts with fresh statics, so the reflection sweep would only cost start-up time there.
        // In the editor only assemblies that can declare a singleton (Neo.Network and its dependents) are walked
        // instead of every type of every loaded assembly.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
#if UNITY_EDITOR
            List<Assembly> assemblies = CollectDependentAssemblies(typeof(NetworkSingleton<>).Assembly);
            for (int i = 0; i < assemblies.Count; i++)
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

                    Type closedGenericType = FindSingletonBase(candidate);
                    if (closedGenericType == null)
                    {
                        continue;
                    }

                    MethodInfo resetMethod = closedGenericType.GetMethod(
                        "ResetStaticStateForRuntime",
                        BindingFlags.Static | BindingFlags.NonPublic);

                    resetMethod?.Invoke(null, null);
                }
            }
#endif
        }

        /// <summary>
        ///     <paramref name="root"/> plus every loaded assembly that references it directly or through another
        ///     assembly of the set: the only places a type deriving from <see cref="NetworkSingleton{T}"/> can live.
        /// </summary>
        internal static List<Assembly> CollectDependentAssemblies(Assembly root)
        {
            Assembly[] loaded = AppDomain.CurrentDomain.GetAssemblies();
            HashSet<string> included = new(StringComparer.Ordinal) { root.GetName().Name };
            List<Assembly> result = new() { root };
            List<Assembly> pending = new(loaded.Length);
            List<AssemblyName[]> pendingReferences = new(loaded.Length);
            for (int i = 0; i < loaded.Length; i++)
            {
                Assembly assembly = loaded[i];
                if (assembly == root || assembly.IsDynamic)
                {
                    continue;
                }

                AssemblyName[] references;
                try
                {
                    references = assembly.GetReferencedAssemblies();
                }
                catch (Exception)
                {
                    continue;
                }

                pending.Add(assembly);
                pendingReferences.Add(references);
            }

            bool grew = true;
            while (grew)
            {
                grew = false;
                for (int i = pending.Count - 1; i >= 0; i--)
                {
                    if (!ReferencesAny(pendingReferences[i], included))
                    {
                        continue;
                    }

                    included.Add(pending[i].GetName().Name);
                    result.Add(pending[i]);
                    pending.RemoveAt(i);
                    pendingReferences.RemoveAt(i);
                    grew = true;
                }
            }

            return result;
        }

        private static bool ReferencesAny(AssemblyName[] references, HashSet<string> names)
        {
            for (int i = 0; i < references.Length; i++)
            {
                if (names.Contains(references[i].Name))
                {
                    return true;
                }
            }

            return false;
        }

        private static Type FindSingletonBase(Type type)
        {
            Type current = type;
            while (current != null && current != typeof(object))
            {
                Type candidate = current.IsGenericType ? current.GetGenericTypeDefinition() : current;
                if (candidate == typeof(NetworkSingleton<>))
                {
                    return current;
                }

                current = current.BaseType;
            }

            return null;
        }
    }
}
