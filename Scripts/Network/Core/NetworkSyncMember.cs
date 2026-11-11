using System;
using System.Reflection;
using UnityEngine;

namespace Neo.Network
{
    /// <summary>Shared metadata validation for the two Inspector sync adapters.</summary>
    internal static class NetworkSyncMember
    {
        internal static bool TryResolve(Component target, string name, Type expectedType, bool requireWrite,
            out MemberInfo member, out string error)
        {
            member = null;
            error = null;
            if (target == null || string.IsNullOrEmpty(name) || expectedType == null)
            {
                error = "Missing target, member name or supported value type.";
                return false;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            try
            {
                Type type = target.GetType();
                FieldInfo field = type.GetField(name, flags);
                if (field != null)
                {
                    if (!expectedType.IsAssignableFrom(field.FieldType)
                        || (requireWrite && (field.IsInitOnly || field.IsLiteral)))
                    {
                        error = "Field type must match the configured payload and the field must be writable.";
                        return false;
                    }

                    member = field;
                    return true;
                }

                PropertyInfo property = type.GetProperty(name, flags);
                if (property == null || property.GetIndexParameters().Length != 0
                    || property.GetGetMethod(true) == null
                    || (requireWrite && property.GetSetMethod(true) == null)
                    || !expectedType.IsAssignableFrom(property.PropertyType))
                {
                    error = "Property must have the configured type and a non-indexed getter"
                            + (requireWrite ? " and setter." : ".");
                    return false;
                }

                member = property;
                return true;
            }
            catch (AmbiguousMatchException exception)
            {
                error = exception.Message;
                return false;
            }
        }

        internal static object Read(MemberInfo member, Component target)
        {
            return member is FieldInfo field ? field.GetValue(target) : ((PropertyInfo)member).GetValue(target);
        }
    }
}
