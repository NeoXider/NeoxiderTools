using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Neo.Level.Tests
{
    public class LevelManagerTests
    {
        [Test]
        public void SetMapId_RaisesSelectedMapId()
        {
            GameObject gameObject = new("LevelManager");
            LevelManager manager = gameObject.AddComponent<LevelManager>();
            manager.OnChangeMap = new UnityEvent<int>();

            try
            {
                SetPrivateField(manager, "_maps", new[] { new Map(), new Map() });
                SetPrivateField(manager, "_lvlBtns", Array.Empty<LevelButton>());

                int payload = -1;
                manager.OnChangeMap.AddListener(value => payload = value);

                manager.SetMapId(1);

                Assert.That(payload, Is.EqualTo(1));
                Assert.That(manager.MapId, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void GetLoopLevel_ReturnsZero_WhenCountIsNonPositive()
        {
            Assert.That(LevelManager.GetLoopLevel(5, 0), Is.EqualTo(0));
            Assert.That(LevelManager.GetLoopLevel(5, -3), Is.EqualTo(0));
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Field `{fieldName}` was not found.");
            fieldInfo.SetValue(target, value);
        }
    }
}
