using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Neo
{
    public static class ColorExtension
    {
        public static Color SetAlpha(this Color color, float alpha)
        {
            Color newColor = color;
            newColor.a = alpha;
            return newColor;
        }

        public static Color SetColor(this Color color, float r=1, float g=1, float b=1, float a = 1)
        {
            Color newColor = color;
            newColor.r = r;
            newColor.g = g;
            newColor.b = b;
            newColor.a = a;
            return newColor;
        }
    }
}
