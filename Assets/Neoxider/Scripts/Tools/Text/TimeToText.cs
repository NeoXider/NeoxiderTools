using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo
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
            [SerializeField] private bool _zeroText = true;
            public TimeFormat timeFormat = TimeFormat.MinutesSeconds;
            public string startAddText;
            public string endAddText;
            public string separator = ":";
            public TMP_Text text;

            public UnityEvent OnEnd;

            private float lastTime;

            public void SetText(float time = 0)
            {
                if ((time == 0 && _zeroText) || time > 0)
                    text.text = startAddText + FormatTime(time, timeFormat, separator) + endAddText;
                else
                    text.text = "";

                if (lastTime != time && time == 0)
                {
                    OnEnd?.Invoke();
                }

                lastTime = time;
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