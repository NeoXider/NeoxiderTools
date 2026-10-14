using System;
using System.Collections.Generic;

namespace HtmlToUitk.Bake
{
    public enum GradientKind
    {
        Linear,
        Radial,
        Conic
    }

    public enum RadialShape
    {
        Ellipse,
        Circle
    }

    public enum RadialExtent
    {
        FarthestCorner,
        ClosestSide,
        ClosestCorner,
        FarthestSide,
        Explicit
    }

    public readonly struct GradientStop
    {
        public readonly double Position;
        public readonly Rgba8 Color;

        public GradientStop(double position, Rgba8 color)
        {
            Position = position;
            Color = color;
        }
    }

    public readonly struct UnresolvedStop
    {
        public readonly double? Position;
        public readonly Rgba8 Color;

        public UnresolvedStop(double? position, Rgba8 color)
        {
            Position = position;
            Color = color;
        }
    }

    public sealed class GradientSpec
    {
        public GradientKind Kind;
        public double AngleDegrees;
        public bool Repeating;
        public IReadOnlyList<GradientStop> Stops = new GradientStop[0];
        public double CenterX = 0.5;
        public double CenterY = 0.5;
        public RadialShape Shape = RadialShape.Ellipse;
        public RadialExtent Extent = RadialExtent.FarthestCorner;
        public double RadiusX;
        public double RadiusY;
    }

    public static class GradientBaker
    {
        public static IReadOnlyList<GradientStop> ResolveStops(IReadOnlyList<UnresolvedStop> stops)
        {
            if (stops == null || stops.Count < 2)
            {
                throw new ArgumentException("a gradient needs at least two colour stops", nameof(stops));
            }

            int n = stops.Count;
            var positions = new double?[n];
            for (int i = 0; i < n; i++)
            {
                positions[i] = stops[i].Position;
            }

            if (positions[0] == null)
            {
                positions[0] = 0.0;
            }

            if (positions[n - 1] == null)
            {
                positions[n - 1] = 1.0;
            }

            double highest = positions[0].Value;
            for (int i = 1; i < n; i++)
            {
                if (positions[i] != null)
                {
                    if (positions[i].Value < highest)
                    {
                        positions[i] = highest;
                    }

                    highest = positions[i].Value;
                }
            }

            int index = 1;
            while (index < n)
            {
                if (positions[index] != null)
                {
                    index++;
                    continue;
                }

                int start = index - 1;
                int end = index;
                while (positions[end] == null)
                {
                    end++;
                }

                double a = positions[start].Value;
                double b = positions[end].Value;
                for (int k = start + 1; k < end; k++)
                {
                    positions[k] = a + (b - a) * (k - start) / (end - start);
                }

                index = end + 1;
            }

            var result = new GradientStop[n];
            for (int i = 0; i < n; i++)
            {
                result[i] = new GradientStop(positions[i].Value, stops[i].Color);
            }

            return result;
        }

        public static double LinearLineLength(double angleDegrees, double width, double height)
        {
            double rad = angleDegrees * DMath.DegToRad;
            return Math.Abs(width * DMath.Sin(rad)) + Math.Abs(height * DMath.Cos(rad));
        }

        public static double CornerAngle(string horizontal, string vertical, double width, double height)
        {
            if (string.IsNullOrEmpty(horizontal) && string.IsNullOrEmpty(vertical))
            {
                return 180.0;
            }

            if (string.IsNullOrEmpty(horizontal))
            {
                return vertical == "top" ? 0.0 : 180.0;
            }

            if (string.IsNullOrEmpty(vertical))
            {
                return horizontal == "right" ? 90.0 : 270.0;
            }

            double toCorner = DMath.Atan2(width, height) / DMath.DegToRad;
            if (vertical == "top" && horizontal == "right")
            {
                return toCorner;
            }

            if (vertical == "bottom" && horizontal == "right")
            {
                return 180.0 - toCorner;
            }

            if (vertical == "bottom" && horizontal == "left")
            {
                return 180.0 + toCorner;
            }

            return 360.0 - toCorner;
        }

        public static RgbaImage Bake(GradientSpec spec, int width, int height)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            if (spec.Stops == null || spec.Stops.Count < 2)
            {
                throw new ArgumentException("a gradient needs at least two colour stops", nameof(spec));
            }

            var image = new RgbaImage(width, height);
            double cx = spec.CenterX * width;
            double cy = spec.CenterY * height;
            double sinA = 0;
            double cosA = 1;
            double lineLength = 1;
            double rx = 1;
            double ry = 1;
            double fromAngle = 0;
            if (spec.Kind == GradientKind.Linear)
            {
                double rad = spec.AngleDegrees * DMath.DegToRad;
                sinA = DMath.Sin(rad);
                cosA = DMath.Cos(rad);
                lineLength = Math.Abs(width * sinA) + Math.Abs(height * cosA);
                if (lineLength <= 0)
                {
                    lineLength = 1;
                }

                cx = width / 2.0;
                cy = height / 2.0;
            }
            else if (spec.Kind == GradientKind.Radial)
            {
                RadialRadii(spec, width, height, cx, cy, out rx, out ry);
            }
            else
            {
                fromAngle = spec.AngleDegrees;
            }

            double first = spec.Stops[0].Position;
            double last = spec.Stops[spec.Stops.Count - 1].Position;
            double span = last - first;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double px = x + 0.5;
                    double py = y + 0.5;
                    double t;
                    if (spec.Kind == GradientKind.Linear)
                    {
                        t = ((px - cx) * sinA - (py - cy) * cosA) / lineLength + 0.5;
                    }
                    else if (spec.Kind == GradientKind.Radial)
                    {
                        double dx = (px - cx) / rx;
                        double dy = (py - cy) / ry;
                        t = Math.Sqrt(dx * dx + dy * dy);
                    }
                    else
                    {
                        double degrees = DMath.Atan2(px - cx, -(py - cy)) / DMath.DegToRad - fromAngle;
                        degrees -= 360.0 * Math.Floor(degrees / 360.0);
                        t = degrees / 360.0;
                    }

                    if (spec.Repeating && span > 0)
                    {
                        t = first + (t - first - span * Math.Floor((t - first) / span));
                    }

                    image.Set(x, y, Sample(spec.Stops, t));
                }
            }

            return image;
        }

        private static void RadialRadii(GradientSpec spec, int width, int height, double cx, double cy, out double rx, out double ry)
        {
            double left = cx;
            double right = width - cx;
            double top = cy;
            double bottom = height - cy;
            if (spec.Extent == RadialExtent.Explicit)
            {
                rx = spec.RadiusX > 0 ? spec.RadiusX : 1e-9;
                ry = spec.Shape == RadialShape.Circle ? rx : (spec.RadiusY > 0 ? spec.RadiusY : 1e-9);
                return;
            }

            double nearX = Math.Min(left, right);
            double nearY = Math.Min(top, bottom);
            double farX = Math.Max(left, right);
            double farY = Math.Max(top, bottom);
            if (spec.Shape == RadialShape.Circle)
            {
                double r;
                switch (spec.Extent)
                {
                    case RadialExtent.ClosestSide:
                        r = Math.Min(nearX, nearY);
                        break;
                    case RadialExtent.FarthestSide:
                        r = Math.Max(farX, farY);
                        break;
                    case RadialExtent.ClosestCorner:
                        r = Math.Sqrt(nearX * nearX + nearY * nearY);
                        break;
                    default:
                        r = Math.Sqrt(farX * farX + farY * farY);
                        break;
                }

                rx = ry = r > 0 ? r : 1e-9;
                return;
            }

            const double Sqrt2 = 1.4142135623730951;
            switch (spec.Extent)
            {
                case RadialExtent.ClosestSide:
                    rx = nearX;
                    ry = nearY;
                    break;
                case RadialExtent.FarthestSide:
                    rx = farX;
                    ry = farY;
                    break;
                case RadialExtent.ClosestCorner:
                    rx = nearX * Sqrt2;
                    ry = nearY * Sqrt2;
                    break;
                default:
                    rx = farX * Sqrt2;
                    ry = farY * Sqrt2;
                    break;
            }

            if (rx <= 0)
            {
                rx = 1e-9;
            }

            if (ry <= 0)
            {
                ry = 1e-9;
            }
        }

        private static Rgba8 Sample(IReadOnlyList<GradientStop> stops, double t)
        {
            int count = stops.Count;
            if (t <= stops[0].Position)
            {
                return stops[0].Color;
            }

            if (t >= stops[count - 1].Position)
            {
                return stops[count - 1].Color;
            }

            int i = 1;
            while (i < count - 1 && stops[i].Position < t)
            {
                i++;
            }

            GradientStop a = stops[i - 1];
            GradientStop b = stops[i];
            double width = b.Position - a.Position;
            if (width <= 0)
            {
                return b.Color;
            }

            double f = (t - a.Position) / width;
            double aa = a.Color.A / 255.0;
            double ba = b.Color.A / 255.0;
            double alpha = aa + (ba - aa) * f;
            if (alpha <= 0)
            {
                return new Rgba8(0, 0, 0, 0);
            }

            double r = (a.Color.R * aa + (b.Color.R * ba - a.Color.R * aa) * f) / alpha;
            double g = (a.Color.G * aa + (b.Color.G * ba - a.Color.G * aa) * f) / alpha;
            double bl = (a.Color.B * aa + (b.Color.B * ba - a.Color.B * aa) * f) / alpha;
            return new Rgba8(ToByte(r), ToByte(g), ToByte(bl), ToByte(alpha * 255.0));
        }

        private static byte ToByte(double value)
        {
            int v = DMath.RoundToInt(value);
            return (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
        }
    }
}
