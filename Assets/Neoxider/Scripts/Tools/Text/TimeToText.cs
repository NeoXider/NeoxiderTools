using TMPro;
using UnityEngine;

namespace Neoxider
{
    namespace Tools
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
                return time.FormatTime(format, separator);
            }

            private void OnValidate()
            {
                if (text == null)
                    text = GetComponent<TMP_Text>();
            }
        }
    }
}