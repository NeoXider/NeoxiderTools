using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Clears <see cref="Singleton{T}" /> and <see cref="SingletonById{T}" /> statics before each Editor play session
    ///     so Enter Play Mode Options with domain reload disabled never resolve a previous session's instance.
    /// </summary>
    internal static class SingletonRuntimeReset
    {
        // WHY: a player process starts with fresh statics, so the sweep (a reflection pass over every
        // loaded type) only runs where a domain can survive between play sessions: the Editor.
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        private static void ResetStaticState()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            bool[] relevant = FindDependentAssemblies(assemblies, typeof(SingletonRuntimeReset).Assembly);
            for (int i = 0; i < assemblies.Length; i++)
            {
                if (relevant[i])
                {
                    ResetAssembly(assemblies[i]);
                }
            }
        }

        /// <summary>
        ///     Marks <paramref name="owner" /> and every assembly that directly or transitively references it.
        /// </summary>
        /// <remarks>
        ///     A subclass can only derive from a type of an assembly it references, so every closed
        ///     <c>Singleton&lt;T&gt;</c> (even behind an abstract intermediate base in another assembly) lives in
        ///     this set. Skipping the rest avoids <c>GetTypes()</c> on engine, editor and BCL assemblies.
        /// </remarks>
        internal static bool[] FindDependentAssemblies(Assembly[] assemblies, Assembly owner)
        {
            bool[] relevant = new bool[assemblies.Length];
            AssemblyName[][] references = new AssemblyName[assemblies.Length][];
            HashSet<string> relevantNames = new(StringComparer.Ordinal) { owner.GetName().Name };
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < assemblies.Length; i++)
                {
                    if (relevant[i])
                    {
                        continue;
                    }

                    Assembly assembly = assemblies[i];
                    if (assembly != owner && !ReferencesAny(assembly, ref references[i], relevantNames))
                    {
                        continue;
                    }

                    relevant[i] = true;
                    relevantNames.Add(assembly.GetName().Name);
                    changed = true;
                }
            }

            return relevant;
        }

        private static bool ReferencesAny(Assembly assembly, ref AssemblyName[] cachedReferences,
            HashSet<string> names)
        {
            if (cachedReferences == null)
            {
                try
                {
                    cachedReferences = assembly.GetReferencedAssemblies();
                }
                catch
                {
                    // WHY: when references cannot be read, scan the assembly rather than miss a singleton.
                    return true;
                }
            }

            for (int i = 0; i < cachedReferences.Length; i++)
            {
                if (names.Contains(cachedReferences[i].Name))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ResetAssembly(Assembly assembly)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }
            catch
            {
                return;
            }

            if (types == null)
            {
                return;
            }

            for (int j = 0; j < types.Length; j++)
            {
                Type candidate = types[j];
                if (candidate == null || candidate.IsAbstract || candidate.ContainsGenericParameters)
                {
                    continue;
                }

                // WHY: a two-level subclass (class MyGm : Gm, where Gm : Singleton<Gm>) violates the
                // self-referencing constraint, so MakeGenericType(candidate) throws ArgumentException
                // and aborts the whole sweep, leaving every later singleton with a stale instance.
                // The closed base taken from the inheritance chain is both throw-free and the type
                // that actually owns the statics such a subclass shares.
                InvokeReset(FindClosedGenericBase(candidate, typeof(Singleton<>)));
                InvokeReset(FindClosedGenericBase(candidate, typeof(SingletonById<>)));
            }
        }

        private static void InvokeReset(Type closedGenericType)
        {
            if (closedGenericType == null)
            {
                return;
            }

            MethodInfo resetMethod = closedGenericType.GetMethod(
                "ResetStaticStateForRuntime",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            if (resetMethod != null)
            {
                resetMethod.Invoke(null, null);
            }
        }

        /// <summary>
        ///     Returns the closed generic base built from <paramref name="rawGeneric" /> in the inheritance
        ///     chain of <paramref name="type" /> (e.g. <c>Singleton&lt;Gm&gt;</c>), or <see langword="null" />.
        /// </summary>
        private static Type FindClosedGenericBase(Type type, Type rawGeneric)
        {
            Type current = type;
            while (current != null && current != typeof(object))
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == rawGeneric)
                {
                    return current;
                }

                current = current.BaseType;
            }

            return null;
        }
    }
}
