using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    public class CreateMenuObjectLegacyTests
    {
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
            CollectionAssert.DoesNotContain(menuPaths, "Neoxider/UI/UIReady");
            CollectionAssert.DoesNotContain(menuPaths, "Neoxider/Tools/Other/AiNavigation");
        }
    }
}
