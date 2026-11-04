using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Bonus;
using Neo.NPC;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using ShopBehaviour = Neo.Shop.Shop;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Every concrete component of the Bonus, Shop, Shop.Bridges and NPC assemblies, added from code, already has
    ///     each serialized UnityEvent its own module declares, so code-built scenes subscribe without a null check or
    ///     a SerializedObject pass. The cases are discovered by reflection, so a new component or a new event field
    ///     without an initializer fails here without a list to update.
    /// </summary>
    public sealed class CodeBuiltComponentEventsTests
    {
        private const string ShopBridgesAssemblyName = "Neo.Shop.Bridges";

        private static readonly Assembly[] ScannedAssemblies =
        {
            typeof(Collection).Assembly,
            typeof(ShopBehaviour).Assembly,
            FindLoadedAssembly(ShopBridgesAssemblyName),
            typeof(NpcNavigation).Assembly
        };

        private GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                Object.DestroyImmediate(_host);
            }

            _host = null;
        }

        public static IEnumerable<Type> ComponentTypes()
        {
            for (int a = 0; a < ScannedAssemblies.Length; a++)
            {
                if (ScannedAssemblies[a] == null)
                {
                    continue;
                }

                Type[] types = LoadableTypes(ScannedAssemblies[a]);
                for (int i = 0; i < types.Length; i++)
                {
                    Type type = types[i];
                    if (type != null && type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters &&
                        typeof(MonoBehaviour).IsAssignableFrom(type))
                    {
                        yield return type;
                    }
                }
            }
        }

        [Test]
        public void ScannedAssemblies_AreAllLoadedAndContainComponents()
        {
            for (int i = 0; i < ScannedAssemblies.Length; i++)
            {
                Assert.That(ScannedAssemblies[i], Is.Not.Null,
                    "Every scanned assembly must be loaded; a missing one would silently drop its components.");
            }

            List<Type> types = new(ComponentTypes());
            Assert.That(types, Has.Member(typeof(CollectionVisualManager)));
            Assert.That(types, Has.Member(typeof(ShopBehaviour)));
            Assert.That(types, Has.Member(typeof(NpcNavigation)));
            Assert.That(types.Exists(type => type.Assembly.GetName().Name == ShopBridgesAssemblyName), Is.True);
        }

        [TestCaseSource(nameof(ComponentTypes))]
        public void ComponentAddedFromCode_HasEveryUnityEventItsModuleDeclares(Type componentType)
        {
            _host = new GameObject("Code-built " + componentType.Name);
            // WHY: an inactive host keeps Awake and OnEnable from running, so only the field initializers are checked.
            _host.SetActive(false);

            Component component = _host.AddComponent(componentType);

            Assert.That(component, Is.Not.Null, componentType.FullName + " could not be added from code.");
            Assert.That(NullDeclaredUnityEvents(component), Is.Empty,
                "Give each serialized UnityEvent field an initializer (= new()); serialized scene data still overrides it.");
        }

        /// <summary>
        ///     Serialized UnityEvent fields that are null on <paramref name="component" />. Base types are walked while
        ///     they belong to a scanned assembly; events inherited from other modules are guarded by their own tests.
        /// </summary>
        private static List<string> NullDeclaredUnityEvents(Component component)
        {
            List<string> missing = new();
            for (Type type = component.GetType(); type != null && IsScanned(type.Assembly); type = type.BaseType)
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                                    BindingFlags.DeclaredOnly);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    bool serialized = (field.IsPublic || field.IsDefined(typeof(SerializeField), false)) &&
                                      !field.IsNotSerialized && !field.IsInitOnly;
                    if (serialized && typeof(UnityEventBase).IsAssignableFrom(field.FieldType) &&
                        field.GetValue(component) == null)
                    {
                        missing.Add(type.Name + "." + field.Name);
                    }
                }
            }

            return missing;
        }

        private static bool IsScanned(Assembly assembly)
        {
            return Array.IndexOf(ScannedAssemblies, assembly) >= 0;
        }

        private static Type[] LoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types;
            }
        }

        private static Assembly FindLoadedAssembly(string name)
        {
            Assembly[] loaded = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < loaded.Length; i++)
            {
                if (loaded[i].GetName().Name == name)
                {
                    return loaded[i];
                }
            }

            return null;
        }
    }
}
