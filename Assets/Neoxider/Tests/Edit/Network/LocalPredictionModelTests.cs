using NUnit.Framework;
using Neo.Network.Realtime;

namespace Neo.Editor.Tests
{
    /// <summary>
    /// Client-side prediction of the local player. The authority's side is simulated here too (same integration, same
    /// tick) so the reconcile can be checked against a "server" that applies each input after a one-way delay.
    /// </summary>
    [TestFixture]
    public sealed class LocalPredictionModelTests
    {
        private const float Tick = 0.02f;
        private const float Speed = 6f;

        private static LocalPredictionModel NewModel(float smoothing = 0.12f, float snap = 3f)
        {
            LocalPredictionSettings settings = new LocalPredictionSettings(Tick, smoothing, snap, 0.01f, 128);
            return new LocalPredictionModel(settings)
            {
                Bounds = new PredictionBounds(-100f, 100f, -100f, 100f),
            };
        }

        private sealed class Wall : IPredictionMovementConstraint
        {
            public float X;

            public void Constrain(float fromX, float fromY, ref float toX, ref float toY)
            {
                if (toX > X) toX = X;
            }
        }

        /// <summary>A toy authority: applies the inputs it has "received" (after a delay) with the same integration.</summary>
        private sealed class ToyServer
        {
            public float X;
            public float Y;
            public uint Ack;

            private readonly System.Collections.Generic.Queue<(uint tick, float dx, float dy, int arriveAtStep)> inbox = new();
            private int step;

            public void Send(uint tick, float dx, float dy, int delaySteps) => inbox.Enqueue((tick, dx, dy, step + delaySteps));

            public (float dx, float dy) held;

            public void Step()
            {
                while (inbox.Count > 0 && inbox.Peek().arriveAtStep <= step)
                {
                    (uint tick, float dx, float dy, int arriveAtStep) input = inbox.Dequeue();
                    held = (input.dx, input.dy);
                    Ack = input.tick;
                }

                X += held.dx * Speed * Tick;
                Y += held.dy * Speed * Tick;
                step++;
            }
        }

        [Test]
        public void FirstReconcile_AdoptsTheServerPosition()
        {
            LocalPredictionModel model = NewModel();
            PredictionReport report = model.Reconcile(10f, -4f, 0u, Speed);
            Assert.That(report.Outcome, Is.EqualTo(PredictionOutcome.Initialised));
            Assert.That(model.HasState, Is.True);
            Assert.That(model.X, Is.EqualTo(10f));
            Assert.That(model.Y, Is.EqualTo(-4f));
        }

        [Test]
        public void StepsBeforeAnyState_AreIgnored()
        {
            LocalPredictionModel model = NewModel();
            model.Step(1, 1f, 0f, Speed);
            Assert.That(model.HasState, Is.False);
            Assert.That(model.PendingInputCount, Is.EqualTo(0));
        }

        [Test]
        public void InputMovesTheBodyImmediately_WithoutWaitingForTheServer()
        {
            LocalPredictionModel model = NewModel();
            model.Teleport(0f, 0f);
            for (uint t = 1; t <= 10; t++) model.Step(t, 1f, 0f, Speed);

            Assert.That(model.X, Is.EqualTo(10 * Speed * Tick).Within(0.0001f), "10 ticks at 6 u/s, no round trip involved");
            Assert.That(model.PendingInputCount, Is.EqualTo(10));
        }

        [Test]
        public void MovementIsClampedToTheBounds()
        {
            LocalPredictionModel model = NewModel();
            model.Teleport(99.9f, 0f);
            for (uint t = 1; t <= 50; t++) model.Step(t, 1f, 0f, Speed);
            Assert.That(model.X, Is.EqualTo(100f));
        }

        [Test]
        public void ZeroSpeed_StandsStill_ButStillRemembersTheInput()
        {
            LocalPredictionModel model = NewModel();
            model.Teleport(0f, 0f);
            model.Step(1, 1f, 0f, 0f);
            Assert.That(model.X, Is.EqualTo(0f));
            Assert.That(model.PendingInputCount, Is.EqualTo(1));
        }

        [Test]
        public void ConstraintShapesBothTheStepAndTheReplay()
        {
            LocalPredictionModel model = NewModel();
            model.Constraint = new Wall { X = 0.1f };
            model.Teleport(0f, 0f);
            for (uint t = 1; t <= 20; t++) model.Step(t, 1f, 0f, Speed);
            Assert.That(model.X, Is.EqualTo(0.1f).Within(0.0001f), "the predicted body stops at the same wall the server body does");

            PredictionReport report = model.Reconcile(0.1f, 0f, 0u, Speed, fallbackReplayTicks: 20);
            Assert.That(report.ErrorDistance, Is.LessThan(0.001f), "replay hits the wall too, so there is nothing to correct");
        }

        [Test]
        public void AcknowledgedInputs_AreDiscarded_TheRestReplayedOnTopOfTheServer()
        {
            LocalPredictionModel model = NewModel();
            model.Teleport(0f, 0f);
            for (uint t = 1; t <= 10; t++) model.Step(t, 1f, 0f, Speed);

            // the server has applied ticks 1..6 only: it stands at 6 ticks of travel
            float serverX = 6 * Speed * Tick;
            PredictionReport report = model.Reconcile(serverX, 0f, 6u, Speed);

            Assert.That(report.ReplayedTicks, Is.EqualTo(4));
            Assert.That(model.PendingInputCount, Is.EqualTo(4), "ticks 7..10 are still unacknowledged");
            Assert.That(report.Outcome, Is.EqualTo(PredictionOutcome.Corrected));
            Assert.That(report.ErrorDistance, Is.LessThan(0.0001f), "a perfectly matching server is a zero error");
            Assert.That(model.X, Is.EqualTo(10 * Speed * Tick).Within(0.0001f));
        }

        [Test]
        public void WithLatencyAndSteadyInput_ThePredictionNeverSnapsOrDrifts()
        {
            LocalPredictionModel model = NewModel();
            ToyServer server = new ToyServer();
            model.Teleport(0f, 0f);

            const int delaySteps = 6; // 120 ms one way
            uint tick = 0;
            float maxError = 0f;
            int snaps = 0;

            for (int step = 0; step < 400; step++)
            {
                tick++;
                float dx = step < 200 ? 1f : 0f;
                float dy = step >= 100 && step < 300 ? 1f : 0f;
                model.Step(tick, dx, dy, Speed);
                server.Send(tick, dx, dy, delaySteps);
                server.Step();

                if (step % 3 == 0)
                {
                    PredictionReport report = model.Reconcile(server.X, server.Y, server.Ack, Speed);
                    if (report.Outcome == PredictionOutcome.Snapped) snaps++;
                    if (step > 20 && report.ErrorDistance > maxError) maxError = report.ErrorDistance;
                }
            }

            Assert.That(snaps, Is.EqualTo(0));
            Assert.That(maxError, Is.LessThan(0.2f), "ack-based replay removes the latency from the error, leaving only one tick of slack");
        }

        [Test]
        public void ASmallDisagreement_IsAbsorbedByTheRenderOffset_NotAJump()
        {
            LocalPredictionModel model = NewModel();
            model.Teleport(0f, 0f);
            float before = model.RenderX;

            PredictionReport report = model.Reconcile(0.5f, 0f, 0u, Speed);
            Assert.That(report.Outcome, Is.EqualTo(PredictionOutcome.Corrected));
            Assert.That(model.X, Is.EqualTo(0.5f), "the simulation moves to the server's answer");
            Assert.That(model.RenderX, Is.EqualTo(before).Within(0.0001f), "what is drawn does not jump");
            Assert.That(model.OffsetX, Is.EqualTo(-0.5f).Within(0.0001f));

            for (int i = 0; i < 30; i++) model.Relax(1f / 60f);
            Assert.That(System.Math.Abs(model.OffsetX), Is.LessThan(0.5f * 0.5f), "half a second later most of it is gone");
            for (int i = 0; i < 600; i++) model.Relax(1f / 60f);
            Assert.That(model.OffsetX, Is.EqualTo(0f), "and it settles to exactly zero");
            Assert.That(model.RenderX, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void ALargeDisagreement_Snaps()
        {
            LocalPredictionModel model = NewModel(snap: 3f);
            model.Teleport(0f, 0f);
            PredictionReport report = model.Reconcile(8f, 0f, 0u, Speed);
            Assert.That(report.Outcome, Is.EqualTo(PredictionOutcome.Snapped));
            Assert.That(model.RenderX, Is.EqualTo(8f));
            Assert.That(model.OffsetX, Is.EqualTo(0f));
        }

        [Test]
        public void NoAck_FallsBackToReplayingTheLastFewInputs()
        {
            LocalPredictionModel model = NewModel();
            model.Teleport(0f, 0f);
            for (uint t = 1; t <= 30; t++) model.Step(t, 1f, 0f, Speed);

            PredictionReport report = model.Reconcile(0f, 0f, 0u, Speed, fallbackReplayTicks: 5);
            Assert.That(report.ReplayedTicks, Is.EqualTo(5));
            Assert.That(model.PendingInputCount, Is.EqualTo(5));
        }

        [Test]
        public void RenderBetweenFixedSteps_InterpolatesFromThePreviousStep()
        {
            LocalPredictionModel model = NewModel();
            model.Teleport(0f, 0f);
            model.Step(1, 1f, 0f, Speed);
            float step = Speed * Tick;
            Assert.That(model.RenderXAt(0f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(model.RenderXAt(0.5f), Is.EqualTo(step * 0.5f).Within(0.0001f));
            Assert.That(model.RenderXAt(1f), Is.EqualTo(step).Within(0.0001f));
            Assert.That(model.RenderXAt(9f), Is.EqualTo(step).Within(0.0001f), "alpha is clamped");
        }

        [Test]
        public void TheHistoryIsABoundedRing()
        {
            LocalPredictionModel model = new LocalPredictionModel(new LocalPredictionSettings(Tick, 0.12f, 3f, 0.01f, 16));
            model.Teleport(0f, 0f);
            for (uint t = 1; t <= 100; t++) model.Step(t, 0f, 0f, 0f);
            Assert.That(model.PendingInputCount, Is.EqualTo(16));
        }

        [Test]
        public void Teleport_DropsPendingInputsAndOffset()
        {
            LocalPredictionModel model = NewModel();
            model.Teleport(0f, 0f);
            model.Step(1, 1f, 0f, Speed);
            model.Reconcile(1f, 0f, 0u, Speed);
            model.Teleport(50f, 50f);
            Assert.That(model.PendingInputCount, Is.EqualTo(0));
            Assert.That(model.OffsetX, Is.EqualTo(0f));
            Assert.That(model.RenderX, Is.EqualTo(50f));
        }

        [Test]
        public void Reset_ForgetsEverything()
        {
            LocalPredictionModel model = NewModel();
            model.Teleport(5f, 5f);
            model.Step(1, 1f, 1f, Speed);
            model.Reset();
            Assert.That(model.HasState, Is.False);
            Assert.That(model.PendingInputCount, Is.EqualTo(0));
            Assert.That(model.X, Is.EqualTo(0f));
        }

        [Test]
        public void ANonFiniteServerPosition_IsIgnored()
        {
            LocalPredictionModel model = NewModel();
            model.Teleport(1f, 2f);
            PredictionReport report = model.Reconcile(float.NaN, 0f, 0u, Speed);
            Assert.That(report.ReplayedTicks, Is.EqualTo(0));
            Assert.That(model.X, Is.EqualTo(1f));
            Assert.That(model.Y, Is.EqualTo(2f));
        }

        [Test]
        public void ForTickRate_SizesTheStepAndTheHistory()
        {
            LocalPredictionSettings settings = LocalPredictionSettings.ForTickRate(30f);
            Assert.That(settings.TickSeconds, Is.EqualTo(1f / 30f).Within(1e-6f));
            Assert.That(settings.HistoryCapacity, Is.EqualTo(75));
        }
    }
}
