using System;
using System.Collections;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor
{
    /// <summary>
    ///     Adds PERSISTENT UnityEvent listeners (saved in the scene, visible in the Inspector) through
    ///     <see cref="UnityEventTools" />. Targets must be public instance methods on a component or asset; pass a
    ///     method group (<c>controller.Save</c>), never a lambda. Adding is idempotent: an identical call
    ///     (same target, method, mode and argument) already on the event is not added twice. Events of components
    ///     ensured by <see cref="DemoSceneBuildContext" /> on generated objects start empty in every build; events of
    ///     hand-authored components are re-owned with <see cref="PruneOwned(UnityEventBase, Object[])" /> (through
    ///     DemoSceneBuildContext.OwnListeners) before the builder adds its calls again.
    /// </summary>
    public static class DemoWiring
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly FieldInfo PersistentCallsField = typeof(UnityEventBase).GetField("m_PersistentCalls", Private);

        /// <summary>Calls a parameterless method (works on any event, its arguments are ignored).</summary>
        public static void AddVoid(UnityEventBase unityEvent, UnityAction call)
        {
            Validate(unityEvent, call);
            if (!Exists(unityEvent, call.Target, call.Method.Name, PersistentListenerMode.Void, null))
            {
                UnityEventTools.AddVoidPersistentListener(unityEvent, call);
            }
        }

        /// <summary>Forwards the event's own value (Slider float, Toggle bool, Dropdown int, InputField string).</summary>
        public static void AddDynamic<T>(UnityEvent<T> unityEvent, UnityAction<T> call)
        {
            Validate(unityEvent, call);
            if (!Exists(unityEvent, call.Target, call.Method.Name, PersistentListenerMode.EventDefined, null))
            {
                UnityEventTools.AddPersistentListener(unityEvent, call);
            }
        }

        /// <summary>Forwards both event values (e.g. StateMachineBehaviourBase.OnStateChanged(from, to)).</summary>
        public static void AddDynamic<T0, T1>(UnityEvent<T0, T1> unityEvent, UnityAction<T0, T1> call)
        {
            Validate(unityEvent, call);
            if (!Exists(unityEvent, call.Target, call.Method.Name, PersistentListenerMode.EventDefined, null))
            {
                UnityEventTools.AddPersistentListener(unityEvent, call);
            }
        }

        /// <summary>Forwards all three event values (e.g. QuestManager.OnObjectiveProgress(id, index, count)).</summary>
        public static void AddDynamic<T0, T1, T2>(UnityEvent<T0, T1, T2> unityEvent, UnityAction<T0, T1, T2> call)
        {
            Validate(unityEvent, call);
            if (!Exists(unityEvent, call.Target, call.Method.Name, PersistentListenerMode.EventDefined, null))
            {
                UnityEventTools.AddPersistentListener(unityEvent, call);
            }
        }

        /// <summary>
        ///     Forwards the event value into a public property setter, the Inspector's "float fillAmount" /
        ///     "bool enabled" entries (Image.fillAmount, Behaviour.enabled, DeckComponent.UseFixedShuffleSeed).
        /// </summary>
        public static void AddSetter<T>(UnityEvent<T> unityEvent, Object target, string propertyName)
        {
            AddDynamic(unityEvent, Setter<T>(target, propertyName));
        }

        /// <summary>
        ///     Delegate of a public instance property setter, for persistent listeners with a fixed argument:
        ///     <c>DemoWiring.AddBool(evt, DemoWiring.Setter&lt;bool&gt;(behaviour, "enabled"), false)</c>.
        /// </summary>
        public static UnityAction<T> Setter<T>(Object target, string propertyName)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "DemoWiring.Setter: the target of '" + propertyName + "' is null.");
            }

            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            MethodInfo setter = property != null ? property.GetSetMethod() : null;
            if (setter == null || property.PropertyType != typeof(T))
            {
                throw new ArgumentException(target.GetType().Name + " has no public " + typeof(T).Name + " property '" +
                                            propertyName + "' with a public setter.");
            }

            return (UnityAction<T>)Delegate.CreateDelegate(typeof(UnityAction<T>), target, setter);
        }

        public static void AddInt(UnityEventBase unityEvent, UnityAction<int> call, int argument)
        {
            Validate(unityEvent, call);
            if (!Exists(unityEvent, call.Target, call.Method.Name, PersistentListenerMode.Int, argument))
            {
                UnityEventTools.AddIntPersistentListener(unityEvent, call, argument);
            }
        }

        public static void AddFloat(UnityEventBase unityEvent, UnityAction<float> call, float argument)
        {
            Validate(unityEvent, call);
            if (!Exists(unityEvent, call.Target, call.Method.Name, PersistentListenerMode.Float, argument))
            {
                UnityEventTools.AddFloatPersistentListener(unityEvent, call, argument);
            }
        }

        public static void AddString(UnityEventBase unityEvent, UnityAction<string> call, string argument)
        {
            Validate(unityEvent, call);
            string value = argument ?? string.Empty;
            if (!Exists(unityEvent, call.Target, call.Method.Name, PersistentListenerMode.String, value))
            {
                UnityEventTools.AddStringPersistentListener(unityEvent, call, value);
            }
        }

        public static void AddBool(UnityEventBase unityEvent, UnityAction<bool> call, bool argument)
        {
            Validate(unityEvent, call);
            if (!Exists(unityEvent, call.Target, call.Method.Name, PersistentListenerMode.Bool, argument))
            {
                UnityEventTools.AddBoolPersistentListener(unityEvent, call, argument);
            }
        }

        /// <summary>Passes a fixed Unity object (asset or scene object) as the argument.</summary>
        public static void AddObject<T>(UnityEventBase unityEvent, UnityAction<T> call, T argument) where T : Object
        {
            Validate(unityEvent, call);
            if (!Exists(unityEvent, call.Target, call.Method.Name, PersistentListenerMode.Object, argument))
            {
                UnityEventTools.AddObjectPersistentListener(unityEvent, call, argument);
            }
        }

        /// <summary>Removes every persistent listener (use on events of hand-authored objects you fully own).</summary>
        public static void Clear(UnityEventBase unityEvent)
        {
            if (unityEvent == null)
            {
                return;
            }

            for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(unityEvent, i);
            }
        }

        /// <summary>
        ///     Removes the listeners a builder owns on an event of a hand-authored component: calls whose target is
        ///     missing (deleted, e.g. by a retired layout), lives under a generated root, or is one of
        ///     <paramref name="owners" /> (for example the demo's own hand-authored controller). Calls the author wired
        ///     to other hand-authored objects stay, so the builder can add its wiring again without duplicating or
        ///     dropping them. Prefer <see cref="DemoSceneBuildContext.OwnListeners(UnityEngine.Component, UnityEventBase, Object[])" />,
        ///     which also validates the event after the build.
        /// </summary>
        /// <returns>Number of removed calls (0 for a null event).</returns>
        public static int PruneOwned(UnityEventBase unityEvent, params Object[] owners)
        {
            return PruneWhere(unityEvent, (target, method) =>
                target == null || DemoSceneBuildContext.IsGeneratedTarget(target) ||
                (owners != null && Array.IndexOf(owners, target) >= 0));
        }

        /// <summary><see cref="PruneOwned(UnityEventBase, Object[])" /> with a predicate for the extra owned targets.</summary>
        public static int PruneOwned(UnityEventBase unityEvent, Func<Object, bool> alsoOwned)
        {
            return PruneWhere(unityEvent, (target, method) =>
                target == null || DemoSceneBuildContext.IsGeneratedTarget(target) ||
                (alsoOwned != null && alsoOwned(target)));
        }

        /// <summary>
        ///     Removes every persistent call for which <paramref name="remove" />(target, methodName) is true. The
        ///     target is null for a missing or destroyed object.
        /// </summary>
        public static int PruneWhere(UnityEventBase unityEvent, Func<Object, string, bool> remove)
        {
            if (unityEvent == null || remove == null)
            {
                return 0;
            }

            int removed = 0;
            for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                Object target = unityEvent.GetPersistentTarget(i);
                if (remove(target == null ? null : target, unityEvent.GetPersistentMethodName(i)))
                {
                    UnityEventTools.RemovePersistentListener(unityEvent, i);
                    removed++;
                }
            }

            return removed;
        }

        public static int Count(UnityEventBase unityEvent)
        {
            return unityEvent == null ? 0 : unityEvent.GetPersistentEventCount();
        }

        /// <summary>"Target.Method; Target.Method" description of the persistent calls (logs, tests).</summary>
        public static string Describe(UnityEventBase unityEvent)
        {
            if (unityEvent == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new();
            for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
            {
                if (i > 0)
                {
                    builder.Append("; ");
                }

                Object target = unityEvent.GetPersistentTarget(i);
                builder.Append(target != null ? target.GetType().Name : "<missing>").Append('.')
                    .Append(unityEvent.GetPersistentMethodName(i));
            }

            return builder.ToString();
        }

        private static void Validate(UnityEventBase unityEvent, Delegate call)
        {
            if (unityEvent == null)
            {
                throw new ArgumentNullException(nameof(unityEvent), "DemoWiring: the UnityEvent is null.");
            }

            if (call == null)
            {
                throw new ArgumentNullException(nameof(call), "DemoWiring: the listener is null.");
            }

            MethodInfo method = call.Method;
            if (!(call.Target is Object target) || target == null)
            {
                throw new ArgumentException("Persistent listeners need a method on a component or asset; '" +
                                            method.Name + "' targets " + (call.Target == null ? "nothing (static or lambda)" :
                                                call.Target.GetType().Name) + ". Pass a method group such as controller.Save.");
            }

            if (method.IsStatic || !method.IsPublic)
            {
                throw new ArgumentException("Persistent listener '" + target.GetType().Name + "." + method.Name +
                                            "' must be a public instance method so it shows in the Inspector.");
            }

            if (!EditorUtility.IsPersistent(target) && !(target is UnityEngine.Component || target is UnityEngine.GameObject))
            {
                throw new ArgumentException("Persistent listener target '" + target.name +
                                            "' is a runtime-only object; it would be lost when the scene is saved.");
            }
        }

        private static bool Exists(UnityEventBase unityEvent, object target, string method, PersistentListenerMode mode,
            object argument)
        {
            IList calls = ReadCalls(unityEvent);
            for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
            {
                if (!ReferenceEquals(unityEvent.GetPersistentTarget(i), target) ||
                    unityEvent.GetPersistentMethodName(i) != method)
                {
                    continue;
                }

                if (calls == null || i >= calls.Count)
                {
                    return true;
                }

                if (SameCall(calls[i], mode, argument))
                {
                    return true;
                }
            }

            return false;
        }

        private static IList ReadCalls(UnityEventBase unityEvent)
        {
            object group = PersistentCallsField?.GetValue(unityEvent);
            FieldInfo callsField = group?.GetType().GetField("m_Calls", Private);
            return callsField?.GetValue(group) as IList;
        }

        private static bool SameCall(object call, PersistentListenerMode mode, object argument)
        {
            if (call == null)
            {
                return false;
            }

            Type callType = call.GetType();
            object storedMode = callType.GetField("m_Mode", Private)?.GetValue(call);
            if (storedMode is PersistentListenerMode actualMode && actualMode != mode)
            {
                return false;
            }

            if (mode == PersistentListenerMode.Void || mode == PersistentListenerMode.EventDefined)
            {
                return true;
            }

            object cache = callType.GetField("m_Arguments", Private)?.GetValue(call);
            if (cache == null)
            {
                return true;
            }

            string field = mode switch
            {
                PersistentListenerMode.Int => "m_IntArgument",
                PersistentListenerMode.Float => "m_FloatArgument",
                PersistentListenerMode.String => "m_StringArgument",
                PersistentListenerMode.Bool => "m_BoolArgument",
                _ => "m_ObjectArgument"
            };
            object stored = cache.GetType().GetField(field, Private)?.GetValue(cache);
            if (mode == PersistentListenerMode.Object)
            {
                return ReferenceEquals(stored, argument) || Equals(stored as Object, argument as Object);
            }

            return Equals(stored, argument);
        }
    }
}
