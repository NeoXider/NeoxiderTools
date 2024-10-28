using TMPro;
using UnityEngine;

namespace Neoxider
{
    public enum TimeFormat
    {
        Milliseconds,
        Seconds,
        SecondsMilliseconds,
        Minutes,
        MinutesSeconds,
        Hours,
        HoursMinutes,
        HoursMinutesSeconds,
        Days,
        DaysHours,
        DaysHoursMinutes,
        DaysHoursMinutesSeconds,
    }

    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(TimeToText))]
    public class TimeToText : MonoBehaviour
    {
        public TimeFormat timeFormat = TimeFormat.MinutesSeconds;
        public string startAddText;
        public string endAddText;
        public string separator = ":";
        public TMP_Text text;

        public void SetText(float time = 0)
        {
            text.text = startAddText + FormatTime(time, timeFormat, separator) + endAddText;
        }

        public static string FormatTime(float time, TimeFormat format = TimeFormat.Seconds, string separator = ":")
        {
            int days = (int)(time / 86400);
            int hours = (int)((time % 86400) / 3600);
            int minutes = (int)((time % 3600) / 60);
            int seconds = (int)(time % 60);
            int milliseconds = (int)((time - (int)time) * 100);

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

        private void OnValidate()
        {
            if (text == null)
                text = GetComponent<TMP_Text>();
        }
    }
}
