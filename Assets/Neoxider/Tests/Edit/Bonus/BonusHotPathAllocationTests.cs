using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Bonus;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Allocation and parity guards for Bonus slot paths that run per frame or per payline: reel window
    ///     classification, spin settle polling, win-line travel animation, payline counting and weighted picks.
    /// </summary>
    public sealed class BonusHotPathAllocationTests
    {
        private const int Iterations = 200;
        private readonly List<Object> _owned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object owned in _owned)
            {
                if (owned != null)
                {
                    Object.DestroyImmediate(owned);
                }
            }

            _owned.Clear();
        }

        [Test]
        public void Row_GetVisibleElement_MatchesWindowRowsAndOutOfRangeIsNull()
        {
            Row row = NewRow(out SlotElement[] elements);

            for (int windowRow = 0; windowRow < row.countSlotElement; windowRow++)
            {
                SlotElement visible = row.GetVisibleElement(windowRow);
                Assert.That(visible, Is.Not.Null);
                Assert.That(row.TryGetWindowRowFromBottom(visible, out int resolved), Is.True);
                Assert.That(resolved, Is.EqualTo(windowRow));
            }

            Assert.That(row.GetVisibleElement(-1), Is.Null);
            Assert.That(row.GetVisibleElement(row.countSlotElement), Is.Null);
            Assert.That(row.GetVisibleTopDown(), Is.Not.SameAs(row.GetVisibleTopDown()),
                "The public accessor returns a copy callers may keep.");
            Assert.That(elements.Length, Is.EqualTo(4));
        }

        [Test]
        public void Row_WindowQueries_DoNotAllocateOnceWarm()
        {
            Row row = NewRow(out SlotElement[] elements);
            SlotElement probe = elements[0];
            row.TryGetWindowRowFromBottom(probe, out _);
            row.GetVisibleElement(1);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                row.TryGetWindowRowFromBottom(probe, out _);
                row.GetVisibleElement(i % row.countSlotElement);
            }

            long allocated = allocationCounter.End();
            Assert.That(allocated, Is.EqualTo(0L));
        }

        [Test]
        public void SpinController_IsStop_DoesNotAllocateWhilePolled()
        {
            GameObject root = Own(new GameObject("SpinControllerIsStop"));
            SpinController controller = root.AddComponent<SpinController>();
            Row[] rows = { NewRow(out _), NewRow(out _), NewRow(out _) };
            SetField(controller, "_rows", rows);
            Assert.That(controller.IsStop(), Is.True);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                controller.IsStop();
            }

            long allocated = allocationCounter.End();
            Assert.That(allocated, Is.EqualTo(0L));
        }

        [TestCase(WinLineRendererPlayback.WinLineColorStyle.AccentGlow)]
        [TestCase(WinLineRendererPlayback.WinLineColorStyle.LinearGradient)]
        [TestCase(WinLineRendererPlayback.WinLineColorStyle.CustomGradient)]
        public void WinLinePlayback_TravelFrame_ReusesOneGradientInsteadOfAllocatingPerFrame(
            WinLineRendererPlayback.WinLineColorStyle style)
        {
            GameObject root = Own(new GameObject("WinLineTravel"));
            LineRenderer line = root.AddComponent<LineRenderer>();
            WinLineRendererPlayback playback = new()
            {
                enabled = true,
                renderers = new[] { line },
                travelSpeed = 1.5f,
                colorStyle = style
            };
            playback.customLineGradient.SetKeys(
                new[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.blue, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

            Action<LineRenderer, Vector3[]> configure = Bind<Action<LineRenderer, Vector3[]>>(playback, "ConfigurePolyline");
            Action<LineRenderer, float> frame = Bind<Action<LineRenderer, float>>(playback, "ApplyVisualFrame");
            configure(line, new[] { Vector3.zero, Vector3.right, Vector3.right * 2f });
            frame(line, 0.1f);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                frame(line, i * 0.016f);
            }

            long allocated = allocationCounter.End();
            Assert.That(allocated, Is.EqualTo(0L));
            Assert.That(line.colorGradient.colorKeys.Length, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void CheckSpin_PaylineDefinitionCount_MatchesEffectiveLinesWithoutAllocating()
        {
            CheckSpin spin = new();
            Assert.That(spin.GetPaylineDefinitionCount(3, 3), Is.EqualTo(spin.GetEffectiveLines(3, 3).Length));
            Assert.That(spin.GetPaylineDefinitionCount(0, 3), Is.EqualTo(spin.GetEffectiveLines(0, 3).Length));

            LinesData lines = Own(ScriptableObject.CreateInstance<LinesData>());
            SetField(lines, "_lines", new[]
            {
                new LinesData.InnerArray { corY = new[] { 0, 1, 2 } },
                new LinesData.InnerArray { corY = new[] { 0, 3, 0 } },
                new LinesData.InnerArray { corY = new[] { 1, 1 } },
                null,
                new LinesData.InnerArray { corY = new[] { 2, 2, 2 } }
            });
            spin.LinesDataAsset = lines;
            Assert.That(spin.GetPaylineDefinitionCount(3, 3), Is.EqualTo(2));
            Assert.That(spin.GetPaylineDefinitionCount(3, 3), Is.EqualTo(spin.GetEffectiveLines(3, 3).Length));
            Assert.That(spin.GetPaylineDefinitionCount(4, 3), Is.EqualTo(spin.GetEffectiveLines(4, 3).Length),
                "No valid Lines Data falls back to horizontal lines in both paths.");

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                spin.GetPaylineDefinitionCount(3, 3);
            }

            long allocated = allocationCounter.End();
            Assert.That(allocated, Is.EqualTo(0L));
        }

        [Test]
        public void CheckSpin_WinningLines_StillRequireOneContiguousNonNegativeRun()
        {
            CheckSpin spin = new();
            spin.SetSequenceLength(3);

            Assert.That(spin.GetWinningLines(Grid(5, 7, 7, 1, 7, 7), 1), Is.Empty, "[A,A,X,A,A] is not three in a row.");
            Assert.That(spin.GetWinningLines(Grid(5, 7, 7, 7, 1, 2), 1), Is.EqualTo(new[] { 0 }));
            Assert.That(spin.GetWinningLines(Grid(3, -1, -1, -1), 1), Is.Empty, "Unknown symbols never pay.");
            Assert.That(spin.GetMultiplayers(Grid(5, 4, 4, 4, 4, 2), 1), Is.EqualTo(new[] { 1f }));
        }

        [Test]
        public void SymbolWeightOverrides_DeterministicPick_DoesNotAllocateADelegatePerCall()
        {
            SlotEconomyDefinition economy = Own(ScriptableObject.CreateInstance<SlotEconomyDefinition>());
            SetField(economy, "_symbols", new[]
            {
                new SlotEconomyDefinition.Symbol { Name = "A", Id = 1, Weight = 1f },
                new SlotEconomyDefinition.Symbol { Name = "B", Id = 2, Weight = 3f }
            });
            SlotSymbolWeightOverrides overrides = new();
            overrides.SyncWith(economy);
            overrides.Enabled = true;
            overrides.SetWeight(1, 3f);
            overrides.SetWeight(2, 1f);
            Assert.That(overrides.PickWeightedId(economy, 0.5f), Is.EqualTo(1));

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                overrides.PickWeightedId(economy, (i % 10) / 10f);
            }

            long allocated = allocationCounter.End();
            Assert.That(allocated, Is.EqualTo(0L));
        }

        private Row NewRow(out SlotElement[] elements)
        {
            GameObject root = Own(new GameObject("Row"));
            Row row = root.AddComponent<Row>();
            row.countSlotElement = 3;
            row.spaceY = 1f;
            row.offsetY = 0f;
            elements = new SlotElement[4];
            for (int i = 0; i < elements.Length; i++)
            {
                GameObject child = new("Element" + i);
                child.transform.SetParent(root.transform, false);
                elements[i] = child.AddComponent<SlotElement>();
            }

            row.ApplyLayout();
            return row;
        }

        private static int[,] Grid(int columns, params int[] bottomRowIds)
        {
            int[,] grid = new int[columns, 1];
            for (int x = 0; x < columns; x++)
            {
                grid[x, 0] = bottomRowIds[x];
            }

            return grid;
        }

        private static TDelegate Bind<TDelegate>(object target, string methodName) where TDelegate : Delegate
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), target, method);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private T Own<T>(T owned) where T : Object
        {
            _owned.Add(owned);
            return owned;
        }
    }
}
