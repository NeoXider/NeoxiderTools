using System;
using System.Reflection;
using UnityEngine;

namespace Neo.Tools
{
    internal static class SingletonRuntimeReset
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            ResetForGenericBase(typeof(Singleton<>));
            ResetForGenericBase(typeof(SingletonById<>));
        }

        private static void ResetForGenericBase(Type genericBaseType)
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

                    // WHY: a two-level subclass (class MyGm : Gm, where Gm : Singleton<Gm>) violates the
                    // self-referencing constraint, so MakeGenericType(candidate) throws ArgumentException
                    // and aborts the whole sweep, leaving every later singleton with a stale instance.
                    // The closed base taken from the inheritance chain is both throw-free and the type
                    // that actually owns the statics such a subclass shares.
                    Type closedGenericType = FindClosedGenericBase(candidate, genericBaseType);
                    if (closedGenericType == null)
                    {
                        continue;
                    }

                    MethodInfo resetMethod = closedGenericType.GetMethod(
                        "ResetStaticStateForRuntime",
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                    if (resetMethod != null)
                    {
                        resetMethod.Invoke(null, null);
                    }
                }
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
