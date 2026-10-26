using System;
using System.Collections.Generic;
using System.Globalization;

namespace Neo.HtmlToUitk.Animation
{
    public static class Easing
    {
        private const double Pi = Math.PI;

        private static readonly Dictionary<string, double[]> Beziers = new Dictionary<string, double[]>(StringComparer.Ordinal)
        {
            { "ease", new[] { 0.25, 0.1, 0.25, 1.0 } },
            { "ease-in", new[] { 0.42, 0.0, 1.0, 1.0 } },
            { "ease-out", new[] { 0.0, 0.0, 0.58, 1.0 } },
            { "ease-in-out", new[] { 0.42, 0.0, 0.58, 1.0 } }
        };

        public static bool IsKnown(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            if (name == "linear" || Beziers.ContainsKey(name))
                return true;
            if (TryParseCubicBezier(name, out _))
                return true;
            return TryFamily(name, out _, out _);
        }

        public static double Evaluate(string name, double t)
        {
            if (t <= 0.0)
                return 0.0;
            if (t >= 1.0)
                return 1.0;
            if (string.IsNullOrEmpty(name) || name == "linear")
                return t;
            if (Beziers.TryGetValue(name, out double[] bezier))
                return CubicBezier(bezier[0], bezier[1], bezier[2], bezier[3], t);
            if (TryParseCubicBezier(name, out double[] custom))
                return CubicBezier(custom[0], custom[1], custom[2], custom[3], t);
            if (TryFamily(name, out string family, out string mode))
                return Family(family, mode, t);
            return t;
        }

        public static double CubicBezier(double x1, double y1, double x2, double y2, double x)
        {
            double t = x;
            for (int i = 0; i < 8; i++)
            {
                double error = BezierAxis(x1, x2, t) - x;
                if (Math.Abs(error) < 1e-7)
                    return BezierAxis(y1, y2, t);
                double slope = BezierSlope(x1, x2, t);
                if (Math.Abs(slope) < 1e-6)
                    break;
                t -= error / slope;
            }

            double low = 0.0;
            double high = 1.0;
            t = x;
            for (int i = 0; i < 40; i++)
            {
                double value = BezierAxis(x1, x2, t);
                if (Math.Abs(value - x) < 1e-7)
                    break;
                if (value < x)
                    low = t;
                else
                    high = t;
                t = (low + high) * 0.5;
            }

            return BezierAxis(y1, y2, t);
        }

        private static double BezierAxis(double a, double b, double t)
        {
            double u = 1.0 - t;
            return 3.0 * u * u * t * a + 3.0 * u * t * t * b + t * t * t;
        }

        private static double BezierSlope(double a, double b, double t)
        {
            double u = 1.0 - t;
            return 3.0 * u * u * a + 6.0 * u * t * (b - a) + 3.0 * t * t * (1.0 - b);
        }

        private static bool TryParseCubicBezier(string text, out double[] values)
        {
            values = null;
            const string prefix = "cubic-bezier(";
            if (text == null || !text.StartsWith(prefix, StringComparison.Ordinal) || !text.EndsWith(")", StringComparison.Ordinal))
                return false;
            string[] parts = text.Substring(prefix.Length, text.Length - prefix.Length - 1).Split(',');
            if (parts.Length != 4)
                return false;
            double[] result = new double[4];
            for (int i = 0; i < 4; i++)
            {
                if (!double.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result[i])
                    || double.IsNaN(result[i]) || double.IsInfinity(result[i]))
                    return false;
            }

            if (result[0] < 0 || result[0] > 1 || result[2] < 0 || result[2] > 1)
                return false;
            values = result;
            return true;
        }

        private static bool TryFamily(string name, out string family, out string mode)
        {
            family = null;
            mode = null;
            string[] modes = { "in-out", "in", "out" };
            for (int m = 0; m < modes.Length; m++)
            {
                string prefix = "ease-" + modes[m] + "-";
                if (!name.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                string candidate = name.Substring(prefix.Length);
                if (candidate == "sine" || candidate == "cubic" || candidate == "circ" || candidate == "elastic" || candidate == "back" || candidate == "bounce")
                {
                    family = candidate;
                    mode = modes[m];
                    return true;
                }
            }

            return false;
        }

        private static double Family(string family, string mode, double t)
        {
            switch (mode)
            {
                case "in":
                    return EaseIn(family, t);
                case "out":
                    return 1.0 - EaseIn(family, 1.0 - t);
                default:
                    return t < 0.5 ? EaseIn(family, t * 2.0) * 0.5 : 1.0 - EaseIn(family, (1.0 - t) * 2.0) * 0.5;
            }
        }

        private static double EaseIn(string family, double t)
        {
            switch (family)
            {
                case "sine":
                    return 1.0 - Math.Cos(t * Pi * 0.5);
                case "cubic":
                    return t * t * t;
                case "circ":
                    return 1.0 - Math.Sqrt(1.0 - t * t);
                case "back":
                    {
                        const double c1 = 1.70158;
                        return (c1 + 1.0) * t * t * t - c1 * t * t;
                    }
                case "elastic":
                    {
                        if (t <= 0.0)
                            return 0.0;
                        if (t >= 1.0)
                            return 1.0;
                        double c4 = 2.0 * Pi / 3.0;
                        return -Math.Pow(2.0, 10.0 * t - 10.0) * Math.Sin((t * 10.0 - 10.75) * c4);
                    }
                case "bounce":
                    return 1.0 - BounceOut(1.0 - t);
                default:
                    return t;
            }
        }

        private static double BounceOut(double t)
        {
            const double n1 = 7.5625;
            const double d1 = 2.75;
            if (t < 1.0 / d1)
                return n1 * t * t;
            if (t < 2.0 / d1)
            {
                t -= 1.5 / d1;
                return n1 * t * t + 0.75;
            }

            if (t < 2.5 / d1)
            {
                t -= 2.25 / d1;
                return n1 * t * t + 0.9375;
            }

            t -= 2.625 / d1;
            return n1 * t * t + 0.984375;
        }
    }
}
