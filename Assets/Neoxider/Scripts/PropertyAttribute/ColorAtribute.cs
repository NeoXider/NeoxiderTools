using UnityEngine;

namespace Neo
{
    public enum ColorEnum
    {
        SoftRed,
        SoftGreen,
        SoftBlue,
        SoftYellow,
        SoftGray,
        SoftPurple,
        SoftCyan,
        SoftOrange
    }

    public class ColorAttribute : PropertyAttribute
    {
        public Color color { get; private set; }

        public ColorAttribute(double r, double g, double b, double a = 1f)
        {
            color = new Color((float)r, (float)g, (float)b, (float)a);
        }

        public ColorAttribute(ColorEnum colorEnum)
        {
            color = GetColor(colorEnum);
        }

        public static Color GetColor(ColorEnum colorEnum)
        {
            Color newColor = Color.white;

            switch (colorEnum)
            {
                case ColorEnum.SoftRed:
                    newColor = new Color(1f, 0.5f, 0.5f);
                    break;
                case ColorEnum.SoftGreen:
                    newColor = new Color(0.5f, 1f, 0.5f);
                    break;
                case ColorEnum.SoftBlue:
                    newColor = new Color(0.5f, 0.5f, 1f);
                    break;
                case ColorEnum.SoftYellow:
                    newColor = new Color(1f, 1f, 0.5f);
                    break;
                case ColorEnum.SoftGray:
                    newColor = new Color(0.5f, 0.5f, 0.5f);
                    break;
                case ColorEnum.SoftPurple:
                    newColor = new Color(0.5f, 0f, 0.5f);
                    break;
                case ColorEnum.SoftCyan:
                    newColor = new Color(0.5f, 1f, 1f);
                    break;
                case ColorEnum.SoftOrange:
                    newColor = new Color(1f, 0.5f, 0f);
                    break;
                default:
                    newColor = new Color(1f, 1f, 1f);
                    break;
            }

            return newColor;
        }
    }
}