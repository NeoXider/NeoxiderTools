using System;

namespace HtmlToUitk.Bake
{
    public static class DMath
    {
        public const double Pi = 3.14159265358979323846;
        public const double TwoPi = 6.28318530717958647692;
        public const double HalfPi = 1.57079632679489661923;
        public const double DegToRad = Pi / 180.0;

        public static double Sin(double x)
        {
            x -= TwoPi * Math.Floor(x / TwoPi + 0.5);
            if (x > HalfPi)
            {
                x = Pi - x;
            }
            else if (x < -HalfPi)
            {
                x = -Pi - x;
            }

            double x2 = x * x;
            double term = x;
            double sum = x;
            for (int k = 1; k <= 9; k++)
            {
                term = -term * x2 / ((2 * k) * (2 * k + 1));
                sum += term;
            }

            return sum;
        }

        public static double Cos(double x)
        {
            return Sin(x + HalfPi);
        }

        public static double Atan(double x)
        {
            bool negative = x < 0;
            if (negative)
            {
                x = -x;
            }

            bool inverted = x > 1;
            if (inverted)
            {
                x = 1.0 / x;
            }

            double y = x / (1.0 + Math.Sqrt(1.0 + x * x));
            y = y / (1.0 + Math.Sqrt(1.0 + y * y));
            double y2 = y * y;
            double term = y;
            double sum = y;
            for (int k = 1; k <= 8; k++)
            {
                term = -term * y2;
                sum += term / (2 * k + 1);
            }

            double result = 4.0 * sum;
            if (inverted)
            {
                result = HalfPi - result;
            }

            return negative ? -result : result;
        }

        public static double Atan2(double y, double x)
        {
            if (x > 0)
            {
                return Atan(y / x);
            }

            if (x < 0)
            {
                return y >= 0 ? Atan(y / x) + Pi : Atan(y / x) - Pi;
            }

            if (y > 0)
            {
                return HalfPi;
            }

            return y < 0 ? -HalfPi : 0.0;
        }

        public static double Round(double value)
        {
            return Math.Floor(value + 0.5);
        }

        public static int RoundToInt(double value)
        {
            return (int)Math.Floor(value + 0.5);
        }

        public static double Clamp(double value, double min, double max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
