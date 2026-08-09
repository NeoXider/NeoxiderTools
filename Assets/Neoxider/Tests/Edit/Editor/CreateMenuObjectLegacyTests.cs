using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Tests
{
    public class CreateMenuObjectLegacyTests
    {
        [Test]
        public void UiCreateMenuObjectFacade_PreservesLegacyPublicApiWithoutMenuItems()
        {
#pragma warning disable CS0618
            Type facadeType = typeof(Neo.UI.CreateMenuObject);
#pragma warning restore CS0618
            BindingFlags publicStatic = BindingFlags.Public | BindingFlags.Static;

            FieldInfo createPatchField = facadeType.GetField("createPatch", publicStatic);
            PropertyInfo startPathProperty = facadeType.GetProperty("startPath", publicStatic);
            MethodInfo[] createMethods = facadeType.GetMethods(publicStatic)
                .Where(method => method.Name == "Create")
                .ToArray();
            MethodInfo getResourcesMethod = facadeType.GetMethods(publicStatic)
                .Single(method => method.Name == "GetResources");

            Assert.That(facadeType.GetCustomAttribute<ObsoleteAttribute>(), Is.Not.Null);
            Assert.That(createPatchField, Is.Not.Null);
            Assert.That(createPatchField.IsLiteral, Is.True);
            Assert.That(createPatchField.GetRawConstantValue(), Is.EqualTo("GameObject/UI/Neoxider/"));
            Assert.That(startPathProperty, Is.Not.Null);
            Assert.That(startPathProperty.CanRead, Is.True);
            Assert.That(createMethods.Count(method => method.IsGenericMethodDefinition), Is.EqualTo(2));
            Assert.That(createMethods.Count(method => !method.IsGenericMethod), Is.EqualTo(1));
            Assert.That(createMethods.Single(method => !method.IsGenericMethod).ReturnType,
                Is.EqualTo(typeof(GameObject)));
            Assert.That(getResourcesMethod.IsGenericMethodDefinition, Is.True);
            Assert.That(facadeType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .SelectMany(method => method.GetCustomAttributes<MenuItem>()).ToArray(), Is.Empty);
        }

        [Test]
        public void BuildCreateMenuEntriesForWindow_ExcludesLegacyComponents()
        {
            Type createMenuObjectType = typeof(CreateMenuObject);
            Assert.That(createMenuObjectType, Is.Not.Null);

            MethodInfo buildEntriesMethod = createMenuObjectType.GetMethod("BuildCreateMenuEntriesForWindow",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(buildEntriesMethod, Is.Not.Null);

            var entries = buildEntriesMethod.Invoke(null, null) as IEnumerable;
            Assert.That(entries, Is.Not.Null);

            string[] menuPaths = entries.Cast<object>()
                .Select(entry => entry.GetType().GetField("MenuPath").GetValue(entry) as string)
                .ToArray();

            CollectionAssert.DoesNotContain(menuPaths, "Neoxider/Bonus/TimeReward");
            CollectionAssert.DoesNotContain(menuPaths, "Neoxider/Tools/Other/AiNavigation");
        }
    }
}
