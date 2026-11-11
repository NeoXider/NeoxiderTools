using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor
{
    /// <summary>
    ///     One field that carries an editor injection attribute, with every attribute resolved once.
    /// </summary>
    internal sealed class InjectedField
    {
        public readonly FieldInfo Field;
        public readonly FindInSceneAttribute FindInScene;
        public readonly FindAllInSceneAttribute FindAllInScene;
        public readonly GetComponentAttribute GetComponent;
        public readonly GetComponentsAttribute GetComponents;
        public readonly LoadFromResourcesAttribute LoadFromResources;
        public readonly LoadAllFromResourcesAttribute LoadAllFromResources;

        public InjectedField(FieldInfo field)
        {
            Field = field;
            FindInScene = field.GetCustomAttribute<FindInSceneAttribute>(false);
            FindAllInScene = field.GetCustomAttribute<FindAllInSceneAttribute>(false);
            GetComponent = field.GetCustomAttribute<GetComponentAttribute>(false);
            GetComponents = field.GetCustomAttribute<GetComponentsAttribute>(false);
            LoadFromResources = field.GetCustomAttribute<LoadFromResourcesAttribute>(false);
            LoadAllFromResources = field.GetCustomAttribute<LoadAllFromResourcesAttribute>(false);
        }

        public bool HasComponentLookup =>
            FindInScene != null || FindAllInScene != null || GetComponent != null || GetComponents != null;

        public bool HasResourceLookup => LoadFromResources != null || LoadAllFromResources != null;
    }

    /// <summary>
    ///     Per-type list of the fields <see cref="ComponentDrawer" /> and <see cref="ResourceDrawer" /> act on.
    /// </summary>
    /// <remarks>
    ///     WHY: both processors run on every IMGUI event of every MonoBehaviour inspector (the fallback editor covers
    ///     third-party scripts too). Walking all fields, boxing value-type fields through
    ///     <see cref="FieldInfo.GetValue" /> and allocating attribute arrays per field cost reflection and garbage on
    ///     each event although the answer only changes with a domain reload. Types without attributed fields now
    ///     cost one dictionary lookup.
    /// </remarks>
    internal static class InjectedFieldCache
    {
        private static readonly Dictionary<Type, InjectedField[]> s_componentFields = new();
        private static readonly Dictionary<Type, InjectedField[]> s_resourceFields = new();
        private static readonly Dictionary<FieldInfo, RequireInterface[]> s_requirements = new();

        /// <summary>Fields of <paramref name="type" /> with a component or scene lookup attribute.</summary>
        public static InjectedField[] GetComponentFields(Type type)
        {
            if (!s_componentFields.TryGetValue(type, out InjectedField[] fields))
            {
                Build(type);
                fields = s_componentFields[type];
            }

            return fields;
        }

        /// <summary>Fields of <paramref name="type" /> with a Resources lookup attribute.</summary>
        public static InjectedField[] GetResourceFields(Type type)
        {
            if (!s_resourceFields.TryGetValue(type, out InjectedField[] fields))
            {
                Build(type);
                fields = s_resourceFields[type];
            }

            return fields;
        }

        /// <summary>Every <see cref="RequireInterface" /> stacked on <paramref name="field" />, resolved once.</summary>
        public static RequireInterface[] GetRequirements(FieldInfo field)
        {
            if (!s_requirements.TryGetValue(field, out RequireInterface[] requirements))
            {
                object[] attributes = field.GetCustomAttributes(typeof(RequireInterface), true);
                requirements = new RequireInterface[attributes.Length];
                for (int i = 0; i < attributes.Length; i++)
                {
                    requirements[i] = (RequireInterface)attributes[i];
                }

                s_requirements[field] = requirements;
            }

            return requirements;
        }

        private static void Build(Type type)
        {
            List<InjectedField> component = null;
            List<InjectedField> resource = null;
            FieldInfo[] fields = type.GetFields(CustomEditorBase.FIELD_FLAGS);
            for (int i = 0; i < fields.Length; i++)
            {
                if (!fields[i].IsDefined(typeof(UnityEngine.PropertyAttribute), false))
                {
                    continue;
                }

                InjectedField injected = new(fields[i]);
                if (injected.HasComponentLookup)
                {
                    (component ??= new List<InjectedField>()).Add(injected);
                }

                if (injected.HasResourceLookup)
                {
                    (resource ??= new List<InjectedField>()).Add(injected);
                }
            }

            s_componentFields[type] = component != null ? component.ToArray() : Array.Empty<InjectedField>();
            s_resourceFields[type] = resource != null ? resource.ToArray() : Array.Empty<InjectedField>();
        }
    }

    /// <summary>
    ///     Remembers which (object, field) pairs already reported a diagnostic so the inspector, which reprocesses on
    ///     every IMGUI event, reports each problem once until it is resolved.
    /// </summary>
    internal sealed class InjectionDiagnostics
    {
        private readonly ConditionalWeakTable<Object, HashSet<FieldInfo>> _reported = new();

        /// <summary>True the first time a problem is reported for this pair; false while it is still known.</summary>
        public bool ShouldReport(Object target, FieldInfo field)
        {
            return _reported.GetOrCreateValue(target).Add(field);
        }

        /// <summary>Forgets a pair once its problem is resolved, so a later regression is reported again.</summary>
        public void Resolve(Object target, FieldInfo field)
        {
            if (_reported.TryGetValue(target, out HashSet<FieldInfo> fields))
            {
                fields.Remove(field);
            }
        }
    }

    /// <summary>
    ///     Inspector-side negative cache for injection lookups that found nothing: a field stays unresolved until the
    ///     hierarchy or the project changes, or for at most <see cref="RetryInterval" /> seconds, instead of running
    ///     a scene or Resources search on every IMGUI event.
    /// </summary>
    /// <remarks>
    ///     WHY: a [FindInScene] field with no candidate or a [LoadFromResources] field with no asset ran
    ///     FindObjectsByType / Resources.FindObjectsOfTypeAll several times per frame while its inspector was open.
    ///     A newly added object raises hierarchyChanged and a new asset projectChanged, so it is still found on the
    ///     next draw; the time limit covers changes that raise neither. The public ProcessComponentAttributes /
    ///     ProcessResourceAttributes entry points keep their unthrottled contract.
    /// </remarks>
    internal sealed class InjectionLookupThrottle
    {
        internal const double RetryInterval = 0.5;

        // WHY: one throttle per processor. Both key by (object, field), so a field carrying a component and a
        // Resources attribute (e.g. [FindInScene][LoadFromResources("X")]) was deferred by the component pass and
        // the Resources fallback after it never ran on the inspector path.

        /// <summary>The throttle the Neo inspector uses for <see cref="ComponentDrawer" /> lookups.</summary>
        public static readonly InjectionLookupThrottle InspectorComponents = new();

        /// <summary>The throttle the Neo inspector uses for <see cref="ResourceDrawer" /> lookups.</summary>
        public static readonly InjectionLookupThrottle InspectorResources = new();

        private static int s_epoch;
        private static bool s_hooked;

        private struct Entry
        {
            public double RetryAt;
            public int Epoch;
        }

        private readonly ConditionalWeakTable<Object, Dictionary<FieldInfo, Entry>> _deferred = new();

        /// <summary>True when the lookup for this pair may run now.</summary>
        public bool ShouldTry(Object target, FieldInfo field, double now)
        {
            Hook();
            if (target == null || !_deferred.TryGetValue(target, out Dictionary<FieldInfo, Entry> fields) ||
                !fields.TryGetValue(field, out Entry entry))
            {
                return true;
            }

            return entry.Epoch != s_epoch || now >= entry.RetryAt;
        }

        /// <summary>Records that the lookup for this pair found nothing at <paramref name="now" />.</summary>
        public void Defer(Object target, FieldInfo field, double now)
        {
            if (target == null)
            {
                return;
            }

            _deferred.GetOrCreateValue(target)[field] = new Entry { RetryAt = now + RetryInterval, Epoch = s_epoch };
        }

        /// <summary>Forgets every deferred pair (the next draw searches again).</summary>
        internal static void InvalidateAll()
        {
            s_epoch++;
        }

        private static void Hook()
        {
            if (s_hooked)
            {
                return;
            }

            s_hooked = true;
            EditorApplication.hierarchyChanged += InvalidateAll;
            EditorApplication.projectChanged += InvalidateAll;
        }
    }
}
