using System.Collections;
using System.Reflection;
using Neo.Bonus;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
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
        public IEnumerator SpinController_TimeScaleZero_CompletesExactlyOnce_AndAcceptsNextSpin()
        {
            GameObject root = new GameObject("UnscaledSlotControllerRoot");
            SpinController controller = CreateSpinController(root, 1, 2, 0.02f);
            int completed = 0;
            controller.OnEnd.AddListener(_ => completed++);

            float previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            try
            {
                controller.ForceNextOutcome(new[,] { { 1, 2 } });
                controller.StartSpin();

                float deadline = Time.realtimeSinceStartup + 2f;
                while (completed < 1 && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(completed, Is.EqualTo(1));
                Assert.That(controller.IsSpinInProgress, Is.False);
                Assert.That(controller.IsStop(), Is.True);

                controller.ForceNextOutcome(new[,] { { 2, 1 } });
                controller.StartSpin();
                deadline = Time.realtimeSinceStartup + 2f;
                while (completed < 2 && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(completed, Is.EqualTo(2), "A completed unscaled spin must accept the next spin.");
                Assert.That(controller.IsSpinInProgress, Is.False);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Object.DestroyImmediate(controller.allSpritesData);
                Object.DestroyImmediate(root);
            }
        }

        [UnityTest]
        public IEnumerator SpinController_DisableDuringSpin_ResumesPlannedOutcomeExactlyOnceWhenEnabled()
        {
            GameObject root = new GameObject("DisabledSlotControllerRoot");
            SpinController controller = CreateSpinController(root, 2, 2, 1f);
            controller.DelayBetweenColumnSpins = 1f;
            int completed = 0;
            controller.OnEnd.AddListener(_ => completed++);

            int[,] outcome =
            {
                { 2, 1 },
                { 1, 2 }
            };
            controller.ForceNextOutcome(outcome);
            controller.StartSpin();
            yield return null;

            root.SetActive(false);

            Assert.That(completed, Is.Zero,
                "Callbacks must not target a disabled object graph.");
            Assert.That(controller.IsSpinInProgress, Is.True);
            Assert.That(controller.IsStop(), Is.True);

            root.SetActive(true);
            yield return null;
            Assert.That(completed, Is.EqualTo(1), "Re-enabling must not emit a duplicate completion.");
            Assert.That(controller.IsSpinInProgress, Is.False);
            CollectionAssert.AreEqual(outcome, controller.FinalElementIDs);

            controller.ForceNextOutcome(outcome);
            controller.StartSpin();
            Assert.That(controller.CompleteActiveSpinImmediately(), Is.True);
            Assert.That(completed, Is.EqualTo(2), "The next spin must be accepted after lifecycle recovery.");
            Assert.That(controller.CompleteActiveSpinImmediately(), Is.False);
            Assert.That(completed, Is.EqualTo(2), "Repeated recovery calls must be idempotent.");

            Object.DestroyImmediate(controller.allSpritesData);
            Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator SpinController_NonUpdatingRow_UsesUnscaledDeadlineAndRecovers()
        {
            GameObject root = new GameObject("TimedOutSlotControllerRoot");
            SpinController controller = CreateSpinController(root, 1, 2, 10f);
            controller.SpinTimeoutSeconds = 0.1f;
            controller.Rows[0].gameObject.SetActive(false);
            int completed = 0;
            controller.OnEnd.AddListener(_ => completed++);

            controller.ForceNextOutcome(new[,] { { 1, 2 } });
            controller.StartSpin();

            float deadline = Time.realtimeSinceStartup + 2f;
            while (completed < 1 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(completed, Is.EqualTo(1));
            Assert.That(controller.IsSpinInProgress, Is.False);
            Assert.That(controller.IsStop(), Is.True);

            Object.DestroyImmediate(controller.allSpritesData);
            Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator SpinController_ExplicitCancel_EmitsNoResult_AndAcceptsNextSpin()
        {
            GameObject root = new("CancelledSlotControllerRoot");
            SpinController controller = CreateSpinController(root, 1, 2, 10f);
            int completed = 0;
            controller.OnEnd.AddListener(_ => completed++);

            controller.ForceNextOutcome(new[,] { { 1, 2 } });
            controller.StartSpin();
            yield return null;

            Assert.That(controller.CancelActiveSpin(), Is.True);
            Assert.That(controller.CancelActiveSpin(), Is.False);
            Assert.That(completed, Is.Zero);
            Assert.That(controller.IsSpinInProgress, Is.False);
            Assert.That(controller.IsStop(), Is.True);

            controller.ForceNextOutcome(new[,] { { 2, 1 } });
            controller.StartSpin();
            Assert.That(controller.CompleteActiveSpinImmediately(), Is.True);
            Assert.That(completed, Is.EqualTo(1));

            Object.DestroyImmediate(controller.allSpritesData);
            Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator SpinController_ExternallyStoppedCoroutine_StillCompletesAndAcceptsNextSpin()
        {
            GameObject root = new GameObject("StoppedCoroutineSlotControllerRoot");
            SpinController controller = CreateSpinController(root, 1, 2, 10f);
            controller.SpinTimeoutSeconds = 0.1f;
            int completed = 0;
            controller.OnEnd.AddListener(_ => completed++);

            controller.ForceNextOutcome(new[,] { { 1, 2 } });
            controller.StartSpin();
            controller.StopAllCoroutines();

            float deadline = Time.realtimeSinceStartup + 2f;
            while (completed < 1 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(completed, Is.EqualTo(1));
            Assert.That(controller.IsSpinInProgress, Is.False);
            Assert.That(controller.IsStop(), Is.True);

            controller.ForceNextOutcome(new[,] { { 2, 1 } });
            controller.StartSpin();
            Assert.That(controller.CompleteActiveSpinImmediately(), Is.True);
            Assert.That(completed, Is.EqualTo(2));

            Object.DestroyImmediate(controller.allSpritesData);
            Object.DestroyImmediate(root);
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

        private static SpinController CreateSpinController(GameObject root, int columnCount, int windowRows,
            float spinSeconds)
        {
            SpinController controller = root.AddComponent<SpinController>();
            Row[] rows = new Row[columnCount];
            for (int x = 0; x < columnCount; x++)
            {
                GameObject rowObject = new GameObject($"Row{x}");
                rowObject.transform.SetParent(root.transform, false);
                Row row = rowObject.AddComponent<Row>();
                row.countSlotElement = windowRows;
                row.spaceY = 100f;
                row.offsetY = 0f;
                row.extraStepsAtDecel = windowRows;
                row.speedControll = new SpeedControll { speed = 8000f, timeSpin = spinSeconds };

                for (int i = 0; i < windowRows * 2; i++)
                {
                    GameObject elementObject = new GameObject($"Row{x}_El{i}");
                    elementObject.transform.SetParent(rowObject.transform, false);
                    elementObject.AddComponent<RectTransform>();
                    elementObject.AddComponent<SlotElement>();
                }

                row.ApplyLayout();
                rows[x] = row;
            }

            controller.allSpritesData = CreateSpritesData(0, 1, 2, 3);
            controller.Rows = rows;
            controller.checkSpin = new CheckSpin { isActive = false };
            controller.OnEnd = new UnityEngine.Events.UnityEvent<bool>();
            controller.DelayBetweenColumnSpins = 0f;
            SetPrivate(controller, "_priceOnLine", false);
            controller.VisibleWindowRows = windowRows;
            return controller;
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
