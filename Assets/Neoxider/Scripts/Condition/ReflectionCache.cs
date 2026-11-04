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
        private static readonly Dictionary<(Type, string, BindingFlags), PropertyInfo> _properties = new();
        private static readonly Dictionary<(Type, string, BindingFlags), FieldInfo> _fields = new();
        private static readonly Dictionary<(Type, string, ArgumentKind, BindingFlags), MethodInfo> _methods = new();
        private static readonly HashSet<(Type, string, BindingFlags)> _ambiguousProperties = new();

        private static readonly object _lock = new();

        /// <summary>Cached <see cref="Type.GetProperty(string, BindingFlags)"/>; a missing property is cached as null.</summary>
        /// <exception cref="AmbiguousMatchException">Several properties share the name (for example overloaded indexers).</exception>
        public static PropertyInfo GetProperty(Type type, string propertyName, BindingFlags flags)
        {
            (Type type, string propertyName, BindingFlags flags) key = (type, propertyName, flags);
            lock (_lock)
            {
                if (_properties.TryGetValue(key, out PropertyInfo prop))
                {
                    return prop;
                }

                prop = type.GetProperty(propertyName, flags);
                _properties[key] = prop; // WHY: caching a null result is fine, it avoids repeated failed lookups
                return prop;
            }
        }

        /// <summary>
        ///     Exception-free variant of <see cref="GetProperty"/>: returns false when several properties share the
        ///     name (overloaded indexers). Ambiguity is cached, so a misconfigured binding that is retried on every
        ///     check does not throw and allocate an exception each time.
        /// </summary>
        /// <param name="property">The single matching property, or null when missing or ambiguous.</param>
        /// <returns>False only for an ambiguous name; a missing property returns true with a null result.</returns>
        public static bool TryGetProperty(Type type, string propertyName, BindingFlags flags, out PropertyInfo property)
        {
            (Type type, string propertyName, BindingFlags flags) key = (type, propertyName, flags);
            lock (_lock)
            {
                if (_ambiguousProperties.Contains(key))
                {
                    property = null;
                    return false;
                }
            }

            try
            {
                property = GetProperty(type, propertyName, flags);
                return true;
            }
            catch (AmbiguousMatchException)
            {
                lock (_lock)
                {
                    _ambiguousProperties.Add(key);
                }

                property = null;
                return false;
            }
        }

        /// <summary>Cached <see cref="Type.GetField(string, BindingFlags)"/>; a missing field is cached as null.</summary>
        public static FieldInfo GetField(Type type, string fieldName, BindingFlags flags)
        {
            (Type type, string fieldName, BindingFlags flags) key = (type, fieldName, flags);
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

        /// <summary>
        ///     Cached lookup of a non-generic single-argument method whose parameter matches <paramref name="argumentKind"/>
        ///     and whose return type is supported by conditions; null when none exists.
        /// </summary>
        public static MethodInfo GetMethod(Type type, string methodName, ArgumentKind argumentKind, BindingFlags flags)
        {
            (Type type, string methodName, ArgumentKind argumentKind, BindingFlags flags) key =
                (type, methodName, argumentKind, flags);
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
