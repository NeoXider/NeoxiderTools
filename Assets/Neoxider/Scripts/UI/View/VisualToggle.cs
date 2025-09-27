using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo
{
    namespace UI
    {
        [AddComponentMenu("Neoxider/" + "UI/" + nameof(VisualToggle))]
        public class VisualToggle : MonoBehaviour
        {
            public ImageVariant[] imageV = new ImageVariant[0];
            public ImageColor[] imageC = new ImageColor[0];
            public TmpColorTextVariant[] textColor = new TmpColorTextVariant[0];
            public GameObjectVariant variants;

            public bool end;

            private void Awake()
            {
                if (TryGetComponent(out Toggle toggle))
                    Visual(toggle.isOn);
            }

            private void OnValidate()
            {
                if (!end)
                {
                    foreach (var v in imageV)
                        if (v.image != null)
                            if (v.start == null)
                                v.start = v.image.sprite;

                    foreach (var t in textColor)
                        if (t.tmp != null)
                        {
                            if (t.start == null)
                                t.start = t.tmp.color;

                            if (!t.use_text)
                                t.start_t = t.tmp.text;
                        }
                }

                Visual();
            }

            public void Press()
            {
                end = true;
                Visual();
            }

            public void EndPress()
            {
                end = false;
                Visual();
            }

            public void Visual()
            {
                if (imageV != null)
                    ImageVisual();

                if (imageC != null)
                    ImageColorVisual();

                if (textColor != null)
                    TextColorVisual();

                if (variants != null)
                    VariantVisual();
            }

            private void ImageColorVisual()
            {
                foreach (var c in imageC) c.image.color = end ? c.end : c.start;
            }

            public void Visual(bool activ)
            {
                end = activ;
                Visual();
            }

            private void ImageVisual()
            {
                foreach (var v in imageV)
                {
                    v.image.sprite = end ? v.end : v.start;

                    if (v.setNativeSize) v.image.SetNativeSize();
                }
            }

            private void TextColorVisual()
            {
                foreach (var t in textColor)
                    if (t.tmp != null)
                    {
                        t.tmp.color = end ? t.end : t.start;

                        if (t.use_text) t.tmp.text = end ? t.end_t : t.start_t;
                    }
            }

            private void VariantVisual()
            {
                for (var i = 0; i < variants.starts.Length; i++) variants.starts[i].SetActive(!end);

                for (var i = 0; i < variants.ends.Length; i++) variants.ends[i].SetActive(end);
            }

            [Serializable]
            public class ImageVariant
            {
                public bool setNativeSize;
                public Image image;
                public Sprite start;
                public Sprite end;
            }

            [Serializable]
            public class ImageColor
            {
                public Image image;
                public Color start = Color.white;
                public Color end = Color.white;
            }

            [Serializable]
            public class TmpColorTextVariant
            {
                public TextMeshProUGUI tmp;
                public Color start = Color.white;
                public Color end = Color.white;
                public bool use_text;
                public string start_t;
                public string end_t;
            }

            [Serializable]
            public class GameObjectVariant
            {
                public GameObject[] starts;
                public GameObject[] ends;
            }
        }
    }
}