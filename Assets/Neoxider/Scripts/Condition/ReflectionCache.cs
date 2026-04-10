using System;
using System.Collections.Generic;
using System.Reflection;

namespace Neo.Condition
{
    /// <summary>
    ///     Caches Reflection data for NeoCondition to optimize runtime performance
    ///     and prevent garbage allocations during member resolution.
    /// </summary>
    public static class ReflectionCache
    {
        private static readonly Dictionary<(Type, string), PropertyInfo> _properties = new();
        private static readonly Dictionary<(Type, string), FieldInfo> _fields = new();
        private static readonly Dictionary<(Type, string, ArgumentKind), MethodInfo> _methods = new();

        private static readonly object _lock = new();

        public static PropertyInfo GetProperty(Type type, string propertyName, BindingFlags flags)
        {
            (Type type, string propertyName) key = (type, propertyName);
            lock (_lock)
            {
                if (_properties.TryGetValue(key, out PropertyInfo prop))
                {
                    return prop;
                }

                prop = type.GetProperty(propertyName, flags);
                _properties[key] = prop; // Can be null, that's fine to cache not found
                return prop;
            }
        }

        public static FieldInfo GetField(Type type, string fieldName, BindingFlags flags)
        {
            (Type type, string fieldName) key = (type, fieldName);
            lock (_lock)
            {
                if (_fields.TryGetValue(key, out FieldInfo field))
                {
                    return field;
                }

                field = type.GetField(fieldName, flags);
                _fields[key] = field;
                return field;
            }
        }

        public static MethodInfo GetMethod(Type type, string methodName, ArgumentKind argumentKind, BindingFlags flags)
        {
            (Type type, string methodName, ArgumentKind argumentKind) key = (type, methodName, argumentKind);
            lock (_lock)
            {
                if (_methods.TryGetValue(key, out MethodInfo method))
                {
                    return method;
                }

                method = ConditionEntry.FindMethodWithOneArgument(type, methodName, argumentKind, flags);
                _methods[key] = method;
                return method;
            }
        }
    }
}
