using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Neo.Editor.Tests
{
    public sealed class DialogueLinePoolTests
    {
        [Test]
        public void TryPick_ReturnsOnlyLinesOfTheRequestedTag()
        {
            PooledDialogueLine idle = new("idle", "Hm.");
            PooledDialogueLine hit = new("hit", "Ouch!");
            DialogueLinePool pool = new(new[] { idle, hit }) { RandomProvider = () => 0f };

            Assert.That(pool.TryPick("hit", out PooledDialogueLine line), Is.True);
            Assert.That(line, Is.SameAs(hit));
            Assert.That(pool.TryPick("win", out line), Is.False);
            Assert.That(line, Is.Null);
        }

        [Test]
        public void TryPick_NeverRepeatsTheSameLineBackToBack()
        {
            PooledDialogueLine first = new("idle", "A");
            PooledDialogueLine second = new("idle", "B");
            DialogueLinePool pool = new(new[] { first, second }) { RandomProvider = () => 0f };
            List<PooledDialogueLine> picked = new();

            for (int i = 0; i < 6; i++)
            {
                Assert.That(pool.TryPick("idle", out PooledDialogueLine line), Is.True);
                picked.Add(line);
            }

            for (int i = 1; i < picked.Count; i++)
            {
                Assert.That(picked[i], Is.Not.SameAs(picked[i - 1]));
            }
        }

        [Test]
        public void TryPick_SingleCandidateMayRepeatAndRepeatGuardCanBeDisabled()
        {
            PooledDialogueLine only = new("idle", "A");
            DialogueLinePool pool = new(new[] { only });

            Assert.That(pool.TryPick("idle", out PooledDialogueLine line), Is.True);
            Assert.That(pool.TryPick("idle", out line), Is.True);
            Assert.That(line, Is.SameAs(only));

            PooledDialogueLine other = new("idle", "B");
            pool.Add(other);
            pool.AvoidBackToBackRepeat = false;
            pool.RandomProvider = () => 0f;
            Assert.That(pool.TryPick("idle", out line), Is.True);
            Assert.That(line, Is.SameAs(only));
        }

        [Test]
        public void TryPick_HonoursTimeWindowsAndFirstPickSlots()
        {
            PooledDialogueLine opening = new("round", "Fight!") { Slot = PooledLineSlot.FirstOnly, MaxTime = 5f };
            PooledDialogueLine early = new("round", "Warming up") { Slot = PooledLineSlot.AfterFirst, MaxTime = 30f };
            PooledDialogueLine late = new("round", "Finish it") { Slot = PooledLineSlot.AfterFirst, MinTime = 30f };
            DialogueLinePool pool = new(new[] { opening, early, late }) { RandomProvider = () => 0f };

            Assert.That(pool.TryPick("round", 1f, out PooledDialogueLine line), Is.True);
            Assert.That(line, Is.SameAs(opening));
            Assert.That(pool.TryPick("round", 10f, out line), Is.True);
            Assert.That(line, Is.SameAs(early));
            Assert.That(pool.TryPick("round", 45f, out line), Is.True);
            Assert.That(line, Is.SameAs(late));
            Assert.That(pool.GetPickCount("round"), Is.EqualTo(3));

            pool.ResetHistory();
            Assert.That(pool.TryPick("round", 45f, out line), Is.False,
                "the opening line is out of its window and later lines wait for the first pick");
        }

        [Test]
        public void ContextualLine_PreemptsIdleAndBlocksIdleUntilFinished()
        {
            PooledDialogueLine idle = new("idle", "...");
            PooledDialogueLine hit = new("hit", "Ouch!");
            DialogueLinePool pool = new(new[] { idle, hit });
            List<string> log = new();
            pool.LineStarted += line => log.Add("start " + line.Text);
            pool.LineInterrupted += line => log.Add("interrupt " + line.Text);
            pool.LineFinished += line => log.Add("finish " + line.Text);

            Assert.That(pool.TryPlay("idle", 0f, DialogueLinePool.IdlePriority, out _), Is.True);
            Assert.That(pool.TryPlay("idle", 0f, DialogueLinePool.IdlePriority, out _), Is.False);
            Assert.That(pool.TryPlay("hit", 0f, DialogueLinePool.ContextPriority, out PooledDialogueLine played), Is.True);
            Assert.That(played, Is.SameAs(hit));
            Assert.That(pool.TryPlay("idle", 0f, DialogueLinePool.IdlePriority, out _), Is.False);
            pool.Finish();
            Assert.That(pool.IsPlaying, Is.False);
            Assert.That(pool.TryPlay("idle", 0f, DialogueLinePool.IdlePriority, out _), Is.True);

            Assert.That(log, Is.EqualTo(new[]
            {
                "start ...", "interrupt ...", "start Ouch!", "finish Ouch!", "start ..."
            }));
        }

        [Test]
        public void TryPlay_WithoutEligibleLine_KeepsThePlayingLine()
        {
            PooledDialogueLine idle = new("idle", "...");
            DialogueLinePool pool = new(new[] { idle });
            pool.TryPlay("idle", 0f, DialogueLinePool.IdlePriority, out _);

            Assert.That(pool.TryPlay("missing", 0f, DialogueLinePool.ContextPriority, out _), Is.False);
            Assert.That(pool.Current, Is.SameAs(idle));
        }

        [Test]
        public void ZeroWeightAndNullLines_AreIgnored()
        {
            PooledDialogueLine muted = new("idle", "muted", 0f);
            DialogueLinePool pool = new(new[] { null, muted });

            Assert.That(pool.Lines, Has.Count.EqualTo(1));
            Assert.That(pool.TryPick("idle", out _), Is.False);
        }

        [Test]
        public void Player_WritesLabelsAndClearsThemOnFinish()
        {
            GameObject host = new(nameof(Player_WritesLabelsAndClearsThemOnFinish));
            GameObject labelHost = new("Line", typeof(RectTransform));
            labelHost.SetActive(false);
            try
            {
                TextMeshProUGUI label = labelHost.AddComponent<TextMeshProUGUI>();
                DialogueLinePlayer player = host.AddComponent<DialogueLinePlayer>();
                player.Lines.Add(new PooledDialogueLine("hit", "Ouch!") { Speaker = "Knight" });
                player.LineText = label;
                player.RebuildPool();
                string started = null;
                player.OnLineStarted.AddListener(text => started = text);

                Assert.That(player.PlayContext("hit"), Is.True);
                Assert.That(started, Is.EqualTo("Ouch!"));
                Assert.That(label.text, Is.EqualTo("Ouch!"));

                player.FinishLine();
                Assert.That(label.text, Is.Empty);
                Assert.That(player.Pool.IsPlaying, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(labelHost);
            }
        }

        [Test]
        public void PlayerTick_SchedulesIdleLinesAndFinishesThemAfterTheLineDuration()
        {
            GameObject host = new(nameof(PlayerTick_SchedulesIdleLinesAndFinishesThemAfterTheLineDuration));
            try
            {
                DialogueLinePlayer player = host.AddComponent<DialogueLinePlayer>();
                player.Lines.Add(new PooledDialogueLine("idle", "..."));
                player.RebuildPool();
                player.IdleInterval = 2f;
                player.LineDuration = 1f;
                int finished = 0;
                player.OnLineFinished.AddListener(() => finished++);

                player.Tick(1.5f);
                Assert.That(player.Pool.IsPlaying, Is.False, "the idle interval has not elapsed yet");
                player.Tick(0.5f);
                Assert.That(player.Pool.IsPlaying, Is.True);
                player.Tick(1f);

                Assert.That(player.Pool.IsPlaying, Is.False);
                Assert.That(finished, Is.EqualTo(1));
                Assert.That(player.ContextTime, Is.EqualTo(3f).Within(0.0001f));

                player.Tick(float.NaN);
                player.Tick(-1f);
                player.Tick(float.PositiveInfinity);
                Assert.That(player.ContextTime, Is.EqualTo(3f).Within(0.0001f), "invalid deltas are ignored");
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Player_OutputLabelsHaveACSharpPath()
        {
            GameObject host = new(nameof(Player_OutputLabelsHaveACSharpPath));
            GameObject labelHost = new("Labels", typeof(RectTransform));
            labelHost.SetActive(false);
            try
            {
                TextMeshProUGUI speaker = labelHost.AddComponent<TextMeshProUGUI>();
                DialogueLinePlayer player = host.AddComponent<DialogueLinePlayer>();
                player.SpeakerText = speaker;
                player.ClearTextOnFinish = false;
                player.Lines.Add(new PooledDialogueLine("hit", "Ouch!") { Speaker = "Knight" });
                player.RebuildPool();

                Assert.That(player.PlayContext("hit"), Is.True);
                player.FinishLine();

                Assert.That(player.SpeakerText, Is.SameAs(speaker));
                Assert.That(speaker.text, Is.EqualTo("Knight"), "ClearTextOnFinish = false keeps the last speaker");
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(labelHost);
            }
        }
    }
}
