using System;

namespace Neo.Network.Realtime
{
    /// <summary>
    /// Numbers of the local-player prediction. Plain data so the model is testable without a config asset.
    /// </summary>
    public readonly struct LocalPredictionSettings
    {
        /// <summary>Fixed step both peers integrate movement with (the physics step, 0.02 s by default).</summary>
        public readonly float TickSeconds;

        /// <summary>
        /// Time constant of the correction: a reconciled error is absorbed by a render offset that decays with this
        /// constant, so the body glides to where the server says it is instead of snapping there.
        /// </summary>
        public readonly float SmoothingSeconds;

        /// <summary>An error above this many world units snaps instantly (a teleport, a dash, a respawn).</summary>
        public readonly float SnapDistance;

        /// <summary>An offset below this is zeroed outright: no endless sub-pixel drift toward the server.</summary>
        public readonly float SettleDistance;

        /// <summary>How many ticks of input are kept for replay (2.5 s at 50 Hz is far beyond any real round trip).</summary>
        public readonly int HistoryCapacity;

        public LocalPredictionSettings(
            float tickSeconds,
            float smoothingSeconds,
            float snapDistance,
            float settleDistance,
            int historyCapacity)
        {
            TickSeconds = tickSeconds <= 0f ? 0.02f : tickSeconds;
            SmoothingSeconds = smoothingSeconds <= 0f ? 0.001f : smoothingSeconds;
            SnapDistance = snapDistance <= 0f ? 3f : snapDistance;
            SettleDistance = settleDistance < 0f ? 0f : settleDistance;
            HistoryCapacity = historyCapacity < 8 ? 8 : historyCapacity;
        }

        public static LocalPredictionSettings Default => new LocalPredictionSettings(
            tickSeconds: 0.02f,
            smoothingSeconds: 0.12f,
            snapDistance: 3f,
            settleDistance: 0.01f,
            historyCapacity: 128);

        /// <summary>
        /// <see cref="Default"/> numbers for a simulation that steps <paramref name="ticksPerSecond"/> times per second
        /// (history sized to about 2.5 seconds of input).
        /// </summary>
        /// <param name="ticksPerSecond">Fixed simulation rate shared by client and authority, in hertz.</param>
        public static LocalPredictionSettings ForTickRate(float ticksPerSecond)
        {
            float rate = ticksPerSecond <= 0f ? 50f : ticksPerSecond;
            return new LocalPredictionSettings(
                tickSeconds: 1f / rate,
                smoothingSeconds: 0.12f,
                snapDistance: 3f,
                settleDistance: 0.01f,
                historyCapacity: (int)(rate * 2.5f));
        }
    }

    /// <summary>
    /// Lets the host environment veto part of a predicted step (walls, props). The model only knows the bounds
    /// rectangle; anything physical that the server body would collide with is the constraint's job, so predicted and
    /// authoritative movement slide along the same obstacles instead of fighting at every one.
    /// </summary>
    public interface IPredictionMovementConstraint
    {
        /// <summary>Adjusts the wanted destination so the body does not pass through anything solid.</summary>
        void Constrain(float fromX, float fromY, ref float toX, ref float toY);
    }

    /// <summary>The playable rectangle predicted steps are clamped to. Must match the authority's.</summary>
    public readonly struct PredictionBounds
    {
        public readonly float MinX;
        public readonly float MaxX;
        public readonly float MinY;
        public readonly float MaxY;

        public PredictionBounds(float minX, float maxX, float minY, float maxY)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
        }

        /// <summary>Effectively unbounded: only an obstacle constraint limits movement.</summary>
        public static PredictionBounds Unbounded => new PredictionBounds(float.MinValue, float.MaxValue, float.MinValue, float.MaxValue);
    }

    /// <summary>What a reconcile call decided.</summary>
    public enum PredictionOutcome
    {
        /// <summary>First server state: the model adopted it.</summary>
        Initialised = 0,

        /// <summary>The error was small; the render offset will absorb it smoothly.</summary>
        Corrected = 1,

        /// <summary>The error was too large to hide; the model jumped to the replayed server state.</summary>
        Snapped = 2,
    }

    /// <summary>Result of one <see cref="LocalPredictionModel.Reconcile"/>.</summary>
    public readonly struct PredictionReport
    {
        public readonly PredictionOutcome Outcome;

        /// <summary>Distance between where the model was and where the replayed server state says it should be.</summary>
        public readonly float ErrorDistance;

        /// <summary>How many unacknowledged input ticks were replayed on top of the server position.</summary>
        public readonly int ReplayedTicks;

        public PredictionReport(PredictionOutcome outcome, float errorDistance, int replayedTicks)
        {
            Outcome = outcome;
            ErrorDistance = errorDistance;
            ReplayedTicks = replayedTicks;
        }
    }

    /// <summary>
    /// Client-side prediction for the local player, so its own body answers input on the very next frame
    /// instead of one round trip plus the interpolation buffer later.
    /// <para>
    /// <b>How it works.</b> Every fixed tick the model integrates the local input into a predicted position exactly as
    /// the authority integrates the same input (<c>position += direction * speed * tick</c>, clamped to the bounds) and
    /// remembers that input under its tick number. When a snapshot arrives it carries the authoritative position and the
    /// tick of the last input the authority had applied: everything up to that tick is already in the server position,
    /// everything after it is not. The model throws the acknowledged inputs away, replays the rest on top of the
    /// server position, and that is where the body should be <i>now</i>. If it is somewhere else the difference is
    /// not applied as a jump: the visible position is <c>predicted + renderOffset</c>, and the reconcile moves the
    /// error into the offset, which then decays with <see cref="LocalPredictionSettings.SmoothingSeconds"/>. A large
    /// error (teleport, dash, respawn) snaps.
    /// </para>
    /// <para>
    /// <b>The server stays authoritative.</b> Nothing the model computes is ever sent anywhere; it only chooses where
    /// to draw the local body. A cheating client that predicts garbage is corrected on the next snapshot, and the
    /// authority never reads a position from a client.
    /// </para>
    /// <para>Pure C#: no Unity, no Mirror, no allocation after construction.</para>
    /// </summary>
    public sealed class LocalPredictionModel
    {
        private struct InputSample
        {
            public uint Tick;
            public float DirX;
            public float DirY;
        }

        private readonly LocalPredictionSettings settings;
        private readonly InputSample[] history;
        private int head;
        private int count;
        private float previousX;
        private float previousY;

        public LocalPredictionModel(LocalPredictionSettings settings)
        {
            this.settings = settings;
            history = new InputSample[settings.HistoryCapacity];
            Bounds = PredictionBounds.Unbounded;
        }

        /// <summary>The corrected simulation position, before the render offset.</summary>
        public float X { get; private set; }

        public float Y { get; private set; }

        /// <summary>Remaining correction the render still has to absorb.</summary>
        public float OffsetX { get; private set; }

        public float OffsetY { get; private set; }

        /// <summary>True once a server position has been adopted.</summary>
        public bool HasState { get; private set; }

        /// <summary>Position to draw at the end of the last step.</summary>
        public float RenderX => X + OffsetX;

        public float RenderY => Y + OffsetY;

        /// <summary>Inputs still waiting for the authority to acknowledge them.</summary>
        public int PendingInputCount => count;

        /// <summary>The rectangle predicted steps are clamped to. Must match the authority's.</summary>
        public PredictionBounds Bounds { get; set; }

        /// <summary>Optional obstacle model. Null means "only the bounds rectangle".</summary>
        public IPredictionMovementConstraint Constraint { get; set; }

        /// <summary>Position to draw <paramref name="alpha"/> of the way through the current fixed step (0 = previous step).</summary>
        public float RenderXAt(float alpha) => previousX + (X - previousX) * Clamp01(alpha) + OffsetX;

        public float RenderYAt(float alpha) => previousY + (Y - previousY) * Clamp01(alpha) + OffsetY;

        /// <summary>Forgets everything, including the history. Call on disconnect and on a restarted match.</summary>
        public void Reset()
        {
            HasState = false;
            X = Y = previousX = previousY = 0f;
            OffsetX = OffsetY = 0f;
            head = 0;
            count = 0;
        }

        /// <summary>Jumps to a position with no smoothing and drops the pending inputs (respawn, dash, forced move).</summary>
        public void Teleport(float x, float y)
        {
            X = previousX = x;
            Y = previousY = y;
            OffsetX = OffsetY = 0f;
            HasState = true;
            head = 0;
            count = 0;
        }

        /// <summary>
        /// Integrates one fixed tick of local input and remembers it for replay. <paramref name="speed"/> is the
        /// authority's last reported effective speed; 0 stands still (dead, frozen, between rounds).
        /// </summary>
        public void Step(uint tick, float dirX, float dirY, float speed)
        {
            if (!HasState) return;

            previousX = X;
            previousY = Y;
            Record(tick, dirX, dirY);
            Advance(dirX, dirY, speed);
        }

        /// <summary>Decays the render offset by one frame's real time. Call once per rendered frame.</summary>
        public void Relax(float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return;
            float keep = (float)Math.Exp(-deltaSeconds / settings.SmoothingSeconds);
            OffsetX *= keep;
            OffsetY *= keep;
            if (OffsetX * OffsetX + OffsetY * OffsetY <= settings.SettleDistance * settings.SettleDistance)
            {
                OffsetX = 0f;
                OffsetY = 0f;
            }
        }

        /// <summary>
        /// Applies an authoritative sample for the local participant. <paramref name="ackTick"/> is the last input tick
        /// the authority had applied when it wrote the sample; 0 means "no acknowledgement" (an older server, or no
        /// input reached it yet), in which case the last <paramref name="fallbackReplayTicks"/> inputs are replayed
        /// instead, an estimate of the round trip in ticks.
        /// </summary>
        public PredictionReport Reconcile(float serverX, float serverY, uint ackTick, float speed, int fallbackReplayTicks = 0)
        {
            if (float.IsNaN(serverX) || float.IsNaN(serverY) || float.IsInfinity(serverX) || float.IsInfinity(serverY))
            {
                return new PredictionReport(PredictionOutcome.Corrected, 0f, 0);
            }

            if (!HasState)
            {
                Teleport(serverX, serverY);
                return new PredictionReport(PredictionOutcome.Initialised, 0f, 0);
            }

            if (ackTick != 0u) DiscardThrough(ackTick);
            else TrimToNewest(fallbackReplayTicks);

            float targetX = serverX;
            float targetY = serverY;
            int replayed = 0;
            for (int i = 0; i < count; i++)
            {
                InputSample sample = history[(head + i) % history.Length];
                float dirX = sample.DirX;
                float dirY = sample.DirY;
                MoveFrom(ref targetX, ref targetY, dirX, dirY, speed);
                replayed++;
            }

            float errorX = targetX - X;
            float errorY = targetY - Y;
            float error = (float)Math.Sqrt(errorX * errorX + errorY * errorY);

            if (error > settings.SnapDistance)
            {
                X = previousX = targetX;
                Y = previousY = targetY;
                OffsetX = OffsetY = 0f;
                return new PredictionReport(PredictionOutcome.Snapped, error, replayed);
            }

            // Keep what is drawn continuous: whatever the corrected state moves by, the offset takes the other way.
            float drawnX = X + OffsetX;
            float drawnY = Y + OffsetY;
            previousX += targetX - X;
            previousY += targetY - Y;
            X = targetX;
            Y = targetY;
            OffsetX = drawnX - X;
            OffsetY = drawnY - Y;
            return new PredictionReport(PredictionOutcome.Corrected, error, replayed);
        }

        // ---- internals -----------------------------------------------------------------------

        private void Advance(float dirX, float dirY, float speed)
        {
            float x = X;
            float y = Y;
            MoveFrom(ref x, ref y, dirX, dirY, speed);
            X = x;
            Y = y;
        }

        private void MoveFrom(ref float x, ref float y, float dirX, float dirY, float speed)
        {
            if (speed <= 0f || (dirX == 0f && dirY == 0f)) return;

            float step = speed * settings.TickSeconds;
            float toX = x + dirX * step;
            float toY = y + dirY * step;
            toX = Clamp(toX, Bounds.MinX, Bounds.MaxX);
            toY = Clamp(toY, Bounds.MinY, Bounds.MaxY);
            Constraint?.Constrain(x, y, ref toX, ref toY);
            x = toX;
            y = toY;
        }

        private void Record(uint tick, float dirX, float dirY)
        {
            if (count == history.Length)
            {
                head = (head + 1) % history.Length;
                count--;
            }

            int slot = (head + count) % history.Length;
            history[slot] = new InputSample { Tick = tick, DirX = dirX, DirY = dirY };
            count++;
        }

        private void DiscardThrough(uint ackTick)
        {
            while (count > 0 && history[head].Tick <= ackTick)
            {
                head = (head + 1) % history.Length;
                count--;
            }
        }

        private void TrimToNewest(int keep)
        {
            if (keep < 0) keep = 0;
            while (count > keep)
            {
                head = (head + 1) % history.Length;
                count--;
            }
        }

        private static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
