using TMPro;
using UnityEngine;

namespace Neo
{
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(FPS))] 
    public class FPS : MonoBehaviour
    {
        public TMP_Text text;
        public float updateTime = 0.2f;

        private void Awake()
        {
            Application.targetFrameRate = 0;
        }

        void Start()
        {
            InvokeRepeating(nameof(SetText), 0, updateTime);
        }

        private void SetText()
        {
            float fps = 1f / Time.deltaTime;
            text.text = Mathf.Round(fps).ToString();
        }
    }
}
