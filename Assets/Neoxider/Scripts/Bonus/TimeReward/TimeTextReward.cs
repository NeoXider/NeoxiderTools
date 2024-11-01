using TMPro;
using UnityEngine;
using Neoxider.Tools;

namespace Neoxider
{
    [AddComponentMenu("Neoxider/" + "Bonus/" + nameof(TimeTextReward))]
    public class TimeTextReward : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _text;

        [SerializeField]
        private TimeFormat _timeFormat = TimeFormat.HoursMinutesSeconds;

        [SerializeField]
        private string _textGet = "Get";

        public void SetText(float time)
        {
            _text.text = time == 0 ? _textGet : time.FormatTime(_timeFormat);
        }
    }
}
