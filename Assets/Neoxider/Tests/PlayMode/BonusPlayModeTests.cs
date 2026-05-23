using System.Collections;
using System.Reflection;
using Neo.Bonus;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.PlayMode
{
    public class BonusPlayModeTests
    {
        [UnityTest]
        public IEnumerator Row_Spin_AppliesTargetIdsAtStop()
        {
            var root = new GameObject("SlotRoot");
            var row = root.AddComponent<Row>();
            row.countSlotElement = 3;
            row.spaceY = 100f;
            row.offsetY = 0f;
            row.speedControll = new SpeedControll { speed = 8000f, timeSpin = 0.05f };

            for (int i = 0; i < 6; i++)
            {
                var go = new GameObject($"El{i}");
                go.transform.SetParent(root.transform, false);
                go.AddComponent<RectTransform>();
                go.AddComponent<SlotElement>();
            }

            row.ApplyLayout();

            var so = ScriptableObject.CreateInstance<SpritesData>();
            var visuals = new[]
            {
                new SlotVisualData { id = 0 },
                new SlotVisualData { id = 1 },
                new SlotVisualData { id = 2 }
            };
            typeof(SpritesData).GetField("_visuals", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, visuals);

            int[] targets = { 2, 1, 0 };
            row.Spin(so, targets);

            float t = 0f;
            while (row.is_spinning && t < 15f)
            {
                t += Time.deltaTime;
                yield return null;
            }

            Assert.That(row.is_spinning, Is.False, "Row did not stop in time.");

            SlotElement[] bottomUp = row.GetVisibleBottomUp();
            Assert.That(bottomUp.Length, Is.EqualTo(3));
            Assert.That(bottomUp[0].id, Is.EqualTo(2));
            Assert.That(bottomUp[1].id, Is.EqualTo(1));
            Assert.That(bottomUp[2].id, Is.EqualTo(0));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(so);
        }

        [UnityTest]
        public IEnumerator WheelFortune_SpinStop_InvokesWinWithResolvedSector()
        {
            var root = new GameObject("WheelRoot");
            root.AddComponent<RectTransform>();

            var wheelGo = new GameObject("Wheel");
            wheelGo.transform.SetParent(root.transform, false);
            var wheelRt = wheelGo.AddComponent<RectTransform>();

            var arrowGo = new GameObject("Arrow");
            arrowGo.transform.SetParent(root.transform, false);
            arrowGo.AddComponent<RectTransform>();

            const int n = 4;
            var items = new GameObject[n];
            for (int i = 0; i < n; i++)
            {
                items[i] = new GameObject($"P{i}");
                items[i].transform.SetParent(wheelRt, false);
                items[i].AddComponent<RectTransform>();
            }

            var wf = root.AddComponent<WheelFortune>();

            SetPrivate(wf, "_wheelTransform", wheelRt);
            SetPrivate(wf, "_arrow", arrowGo.GetComponent<RectTransform>());
            SetPrivate(wf, "items", items);
            SetPrivate(wf, "_singleUse", false);
            SetPrivate(wf, "_autoStopTime", 0f);
            SetPrivate(wf, "_enableAlignment", false);
            SetPrivate(wf, "_initialAngularVelocity", 720f);
            SetPrivate(wf, "_angularDeceleration", 2000f);

            wheelRt.rotation = Quaternion.Euler(0, 0, 45f);
            arrowGo.transform.rotation = Quaternion.Euler(0, 0, 0f);

            int winId = -1;
            wf.OnWinIdVariant.AddListener(id => winId = id);

            wf.Spin();
            yield return null;
            wf.Stop();

            float wait = 0f;
            while (wf.State != WheelFortune.SpinState.Idle && wait < 10f)
            {
                wait += Time.deltaTime;
                yield return null;
            }

            int expected = WheelFortune.ResolveSectorIndex(
                wheelRt.rotation.eulerAngles.z,
                arrowGo.transform.eulerAngles.z,
                (float)(GetPrivate(wf, "_wheelOffsetZ") ?? 0f),
                n);

            Assert.That(winId, Is.EqualTo(expected));

            Object.DestroyImmediate(root);
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            FieldInfo f = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(f, Is.Not.Null, $"Missing field {fieldName}");
            f.SetValue(target, value);
        }

        private static object GetPrivate(object target, string fieldName)
        {
            FieldInfo f = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            return f?.GetValue(target);
        }
    }
}
