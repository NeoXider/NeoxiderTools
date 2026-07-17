namespace Neo.Abilities
{
    /// <summary>
    ///     Outcome of a cast request.
    /// </summary>
    public readonly struct CastResult
    {
        public readonly bool Success;
        public readonly CastFailureReason Failure;

        /// <summary>Monotonic id of the accepted cast (0 when rejected). Correlates receipts and projectiles.</summary>
        public readonly uint CastId;

        private CastResult(bool success, CastFailureReason failure, uint castId)
        {
            Success = success;
            Failure = failure;
            CastId = castId;
        }

        public static CastResult Ok(uint castId)
        {
            return new CastResult(true, CastFailureReason.None, castId);
        }

        public static CastResult Fail(CastFailureReason reason)
        {
            return new CastResult(false, reason, 0);
        }
    }
}
