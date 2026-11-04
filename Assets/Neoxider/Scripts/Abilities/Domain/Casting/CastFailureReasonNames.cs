using System;

namespace Neo.Abilities
{
    /// <summary>
    ///     Cached <see cref="CastFailureReason" /> names. <c>Enum.ToString</c> boxes and allocates a new
    ///     string on every call, which adds garbage when AI or auto-casters retry blocked casts each frame.
    /// </summary>
    internal static class CastFailureReasonNames
    {
        private static readonly string[] Names = BuildNames();

        /// <summary>Same text as <c>reason.ToString()</c>, without allocating for defined values.</summary>
        public static string Of(CastFailureReason reason)
        {
            int index = (int)reason;
            return index >= 0 && index < Names.Length && Names[index] != null ? Names[index] : reason.ToString();
        }

        private static string[] BuildNames()
        {
            Array values = Enum.GetValues(typeof(CastFailureReason));
            int max = 0;
            for (int i = 0; i < values.Length; i++)
            {
                int value = (int)values.GetValue(i);
                if (value > max)
                {
                    max = value;
                }
            }

            string[] names = new string[max + 1];
            for (int i = 0; i < values.Length; i++)
            {
                CastFailureReason reason = (CastFailureReason)values.GetValue(i);
                int value = (int)reason;
                if (value >= 0)
                {
                    names[value] = reason.ToString();
                }
            }

            return names;
        }
    }
}
