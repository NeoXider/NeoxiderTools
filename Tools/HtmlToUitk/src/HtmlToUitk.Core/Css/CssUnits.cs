using System;
using System.Collections.Generic;

namespace HtmlToUitk.Core.Css
{
    public enum UnitKind { None, Length, Percent, Angle, Time, Frequency, Resolution, Flex, Unknown }

    public static class CssUnits
    {
        private static readonly HashSet<string> AbsoluteLengths = new HashSet<string>(StringComparer.Ordinal) { "px", "pt", "pc", "in", "cm", "mm", "q" };
        private static readonly HashSet<string> RelativeLengths = new HashSet<string>(StringComparer.Ordinal) { "em", "rem", "ex", "ch", "cap", "ic", "lh", "rlh", "vw", "vh", "vmin", "vmax", "vi", "vb", "svw", "svh", "lvw", "lvh", "dvw", "dvh" };
        private static readonly HashSet<string> ViewportLengths = new HashSet<string>(StringComparer.Ordinal) { "vw", "vh", "vmin", "vmax", "vi", "vb", "svw", "svh", "lvw", "lvh", "dvw", "dvh" };

        public static UnitKind Classify(string unit)
        {
            if (string.IsNullOrEmpty(unit)) return UnitKind.None;
            string lower = unit.ToLowerInvariant();
            if (lower == "%") return UnitKind.Percent;
            if (AbsoluteLengths.Contains(lower) || RelativeLengths.Contains(lower)) return UnitKind.Length;
            switch (lower)
            {
                case "deg":
                case "rad":
                case "grad":
                case "turn":
                    return UnitKind.Angle;
                case "s":
                case "ms":
                    return UnitKind.Time;
                case "hz":
                case "khz":
                    return UnitKind.Frequency;
                case "dpi":
                case "dpcm":
                case "dppx":
                case "x":
                    return UnitKind.Resolution;
                case "fr":
                    return UnitKind.Flex;
                default:
                    return UnitKind.Unknown;
            }
        }

        public static bool IsAbsoluteLength(string unit) => unit != null && AbsoluteLengths.Contains(unit.ToLowerInvariant());

        public static bool IsRelativeLength(string unit) => unit != null && RelativeLengths.Contains(unit.ToLowerInvariant());

        public static bool IsViewportUnit(string unit) => unit != null && ViewportLengths.Contains(unit.ToLowerInvariant());

        public static bool TryToPixels(CssValue value, out double pixels)
        {
            pixels = 0;
            var number = value as CssNumber;
            if (number != null && number.Value == 0) return true;
            var dimension = value as CssDimension;
            if (dimension == null) return false;
            switch (dimension.Unit.ToLowerInvariant())
            {
                case "px":
                    pixels = dimension.Value;
                    return true;
                case "pt":
                    pixels = dimension.Value * 96.0 / 72.0;
                    return true;
                case "pc":
                    pixels = dimension.Value * 16.0;
                    return true;
                case "in":
                    pixels = dimension.Value * 96.0;
                    return true;
                case "cm":
                    pixels = dimension.Value * 96.0 / 2.54;
                    return true;
                case "mm":
                    pixels = dimension.Value * 96.0 / 25.4;
                    return true;
                case "q":
                    pixels = dimension.Value * 96.0 / 101.6;
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryToDegrees(CssValue value, out double degrees)
        {
            degrees = 0;
            var number = value as CssNumber;
            if (number != null && number.Value == 0) return true;
            var dimension = value as CssDimension;
            if (dimension == null) return false;
            switch (dimension.Unit.ToLowerInvariant())
            {
                case "deg":
                    degrees = dimension.Value;
                    return true;
                case "grad":
                    degrees = dimension.Value * 0.9;
                    return true;
                case "rad":
                    degrees = dimension.Value * 180.0 / Math.PI;
                    return true;
                case "turn":
                    degrees = dimension.Value * 360.0;
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryToSeconds(CssValue value, out double seconds)
        {
            seconds = 0;
            var dimension = value as CssDimension;
            if (dimension == null) return false;
            switch (dimension.Unit.ToLowerInvariant())
            {
                case "s":
                    seconds = dimension.Value;
                    return true;
                case "ms":
                    seconds = dimension.Value / 1000.0;
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryToMilliseconds(CssValue value, out double milliseconds)
        {
            double seconds;
            if (TryToSeconds(value, out seconds))
            {
                milliseconds = seconds * 1000.0;
                return true;
            }
            milliseconds = 0;
            return false;
        }

        public static bool IsZero(CssValue value)
        {
            var number = value as CssNumber;
            if (number != null) return number.Value == 0;
            var dimension = value as CssDimension;
            if (dimension != null) return dimension.Value == 0;
            var percent = value as CssPercent;
            return percent != null && percent.Value == 0;
        }
    }
}
