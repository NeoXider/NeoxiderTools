using System;
using Neoxider.Tools;
using UnityEngine;

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

        public static string FormatTime(this float timeSeconds, TimeFormat format = TimeFormat.Seconds, string separator = ":")
        {
            int days = (int)(timeSeconds / 86400);
            int hours = (int)((timeSeconds % 86400) / 3600);
            int minutes = (int)((timeSeconds % 3600) / 60);
            int seconds = (int)(timeSeconds % 60);
            int milliseconds = (int)((timeSeconds - (int)timeSeconds) * 100);
            string formattedTime = "";

            switch (format)
            {
                case TimeFormat.Milliseconds:
                    formattedTime = $"{milliseconds:D2}";
                    break;
                case TimeFormat.SecondsMilliseconds:
                    formattedTime = $"{seconds:D2}{separator}{milliseconds:D2}";
                    break;
                case TimeFormat.Seconds:
                    formattedTime = $"{seconds:D2}";
                    break;
                case TimeFormat.Minutes:
                    formattedTime = $"{minutes:D2}";
                    break;
                case TimeFormat.MinutesSeconds:
                    formattedTime = $"{minutes:D2}{separator}{seconds:D2}";
                    break;
                case TimeFormat.Hours:
                    formattedTime = $"{hours:D2}";
                    break;
                case TimeFormat.HoursMinutes:
                    formattedTime = $"{hours:D2}{separator}{minutes:D2}";
                    break;
                case TimeFormat.HoursMinutesSeconds:
                    formattedTime = $"{hours:D2}{separator}{minutes:D2}{separator}{seconds:D2}";
                    break;
                case TimeFormat.Days:
                    formattedTime = $"{days:D2}";
                    break;
                case TimeFormat.DaysHours:
                    formattedTime = $"{days:D2}{separator}{hours:D2}";
                    break;
                case TimeFormat.DaysHoursMinutes:
                    formattedTime = $"{days:D2}{separator}{hours:D2}{separator}{minutes:D2}";
                    break;
                case TimeFormat.DaysHoursMinutesSeconds:
                    formattedTime = $"{days:D2}{separator}{hours:D2}{separator}{minutes:D2}{separator}{seconds:D2}";
                    break;
                default:
                    formattedTime = "00";
                    break;
            }

            return formattedTime;
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
