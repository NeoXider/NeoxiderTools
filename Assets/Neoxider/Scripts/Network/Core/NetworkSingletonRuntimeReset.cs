using System;
using System.Reflection;
using UnityEngine;

namespace Neo.Network
{
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
