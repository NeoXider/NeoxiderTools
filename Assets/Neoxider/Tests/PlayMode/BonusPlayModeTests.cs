using System.Collections;
using System.Reflection;
using Neo.Bonus;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.PlayMode
{
    public class BonusPlayModeTests
    {
        [UnityTest]
        public IEnumerator CooldownReward_InheritedUpdate_AdvancesCountdown()
        {
            const string saveSuffix = "InheritedUpdatePlayModeTest";
            var go = new GameObject(nameof(CooldownReward_InheritedUpdate_AdvancesCountdown));
            CooldownReward reward = go.AddComponent<CooldownReward>();
            reward.SetAdditionalKey(saveSuffix, false);
            reward.CooldownSeconds = 1f;
            reward.AutoClaim = false;
            DeleteCooldownSave(reward);

            yield return null;
            float initial = reward.CurrentTime;
            yield return new WaitForSecondsRealtime(0.25f);

            Assert.That(initial, Is.GreaterThan(0.8f));
            Assert.That(reward.CurrentTime, Is.LessThan(initial - 0.1f),
                "A CooldownReward must inherit TimerObject.Update and advance without a project-side driver.");
            Assert.That(reward.RemainingTimeValue, Is.EqualTo(reward.CurrentTime).Within(0.05f));

            DeleteCooldownSave(reward);
            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator Row_Spin_AppliesTargetIdsAtStop()
        {
            var root = new GameObject("SlotRoot");
            Row row = root.AddComponent<Row>();
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

            SpritesData so = ScriptableObject.CreateInstance<SpritesData>();
            SlotVisualData[] visuals = new[]
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
        public IEnumerator SpinController_ForcedOutcome_PreservesBottomUpRowsForPaylines()
        {
            var root = new GameObject("SlotControllerRoot");
            SpinController controller = root.AddComponent<SpinController>();
            SpritesData sprites = CreateSpritesData(0, 1, 2, 3);
            var rows = new Row[3];

            for (int x = 0; x < rows.Length; x++)
            {
                var rowObject = new GameObject($"Row{x}");
                rowObject.transform.SetParent(root.transform, false);
                Row row = rowObject.AddComponent<Row>();
                row.countSlotElement = 3;
                row.spaceY = 100f;
                row.offsetY = 0f;
                row.extraStepsAtDecel = 3;
                row.speedControll = new SpeedControll { speed = 8000f, timeSpin = 0.05f };

                for (int i = 0; i < 6; i++)
                {
                    var elementObject = new GameObject($"Row{x}_El{i}");
                    elementObject.transform.SetParent(rowObject.transform, false);
                    elementObject.AddComponent<RectTransform>();
                    elementObject.AddComponent<SlotElement>();
                }

                row.ApplyLayout();
                rows[x] = row;
            }

            controller.allSpritesData = sprites;
            controller.Rows = rows;
            controller.checkSpin = new CheckSpin();
            controller.OnEnd = new UnityEngine.Events.UnityEvent<bool>();
            controller.checkSpin.SetFallbackPaylineWindowRows(2, 2);
            controller.checkSpin.SetSequenceLength(3);
            SetPrivate(controller, "_countLine", 1);
            SetPrivate(controller, "chanceWin", 0f);
            SetPrivate(controller, "_delaySpinRoll", 0f);
            SetPrivate(controller, "_priceOnLine", false);

            yield return null;

            int[,] outcome =
            {
                { 0, 3, 1 },
                { 2, 3, 1 },
                { 0, 3, 1 }
            };
            controller.ForceNextOutcome(outcome);

            bool ended = false;
            bool won = false;
            controller.OnEnd.AddListener(value =>
            {
                ended = true;
                won = value;
            });

            controller.StartSpin();

            float t = 0f;
            while (!ended && t < 15f)
            {
                t += Time.deltaTime;
                yield return null;
            }

            Assert.That(ended, Is.True, "SpinController did not finish in time.");
            Assert.That(won, Is.True,
                "Top fallback payline should win when forced outcome sets top row to the same id.");

            int[,] finalIds = controller.GetElementIDsMatrix(false);
            Assert.That(finalIds.GetLength(0), Is.EqualTo(3));
            Assert.That(finalIds.GetLength(1), Is.EqualTo(3));
            for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
            {
                Assert.That(finalIds[x, y], Is.EqualTo(outcome[x, y]),
                    $"Final visible matrix must keep y=0 bottom orientation at [{x},{y}].");
            }

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(sprites);
        }

        [UnityTest]
        public IEnumerator WheelFortune_SpinStop_InvokesWinWithResolvedSector()
        {
            var root = new GameObject("WheelRoot");
            root.AddComponent<RectTransform>();

            var wheelGo = new GameObject("Wheel");
            wheelGo.transform.SetParent(root.transform, false);
            RectTransform wheelRt = wheelGo.AddComponent<RectTransform>();

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

            WheelFortune wf = root.AddComponent<WheelFortune>();

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

        private static SpritesData CreateSpritesData(params int[] ids)
        {
            SpritesData so = ScriptableObject.CreateInstance<SpritesData>();
            var visuals = new SlotVisualData[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                visuals[i] = new SlotVisualData { id = ids[i] };
            }

            typeof(SpritesData).GetField("_visuals", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, visuals);
            return so;
        }

        private static object GetPrivate(object target, string fieldName)
        {
            FieldInfo f = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            return f?.GetValue(target);
        }

        private static void DeleteCooldownSave(CooldownReward reward)
        {
            SaveProvider.DeleteKey(reward.RewardTimeKey);
            SaveProvider.DeleteKey(reward.RewardTimeKey + "_rt");
            SaveProvider.DeleteKey(reward.RewardTimeKey + "_a");
        }
    }
}
