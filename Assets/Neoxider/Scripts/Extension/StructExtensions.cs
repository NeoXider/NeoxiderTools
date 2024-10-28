using System;

namespace Neoxider
{
    public static class StructExtensions
    {
        public static int ToInt(this bool value)
        {
            return value ? 1 : 0;
        }

        public static bool ToBool(this int value)
        {
            return value > 0;
        }

        public static string FormatTime(this float sec, TimeFormat format = TimeFormat.Seconds)
        {
            return TimeToText.FormatTime(sec, format);
        }

        public static int CountEmptyElements<T>(this T[] array)
        {
            int emptyCount = 0;

            foreach (T element in array)
            {
                if (element == null)
                {
                    emptyCount++;
                }
            }

            return emptyCount;
        }

        public static string FormatWithSeparator(this int number, string separator)
        {
            string numberString = number.ToString();

            char[] numberArray = numberString.ToCharArray();
            Array.Reverse(numberArray);
            string reversedNumber = new string(numberArray);

            string formattedNumber = "";
            for (int i = 0; i < reversedNumber.Length; i++)
            {
                if (i > 0 && i % 3 == 0)
                {
                    formattedNumber += separator;
                }
                formattedNumber += reversedNumber[i];
            }

            char[] formattedArray = formattedNumber.ToCharArray();
            Array.Reverse(formattedArray);

            return new string(formattedArray);
        }

        public static string FormatWithSeparator(this float number, string separator = "", int decimalPlaces = 2)
        {
            number = (float)Math.Round(number, decimalPlaces);

            int integerPart = (int)number;
            float fractionPart = number - integerPart;

            string formattedInteger = integerPart.FormatWithSeparator(separator);

            string formattedFraction = fractionPart.ToString($"F{decimalPlaces}").Substring(1);

            return formattedInteger + formattedFraction;
        }
    }
}
