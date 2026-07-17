using UnityEngine;

namespace Neo.Samples.Survivor
{
    /// <summary>
    ///     Tiny runtime art helper so the demo needs zero imported assets: procedural soft-circle and
    ///     ring sprites plus a rounded UI sprite, all cached and shared. Keeps the sample self-contained.
    /// </summary>
    public static class SurvivorArt
    {
        private static Sprite _disc;
        private static Sprite _ring;
        private static Sprite _rounded;
        private static Sprite _soft;

        /// <summary>A filled circle with a soft anti-aliased edge.</summary>
        public static Sprite Disc => _disc != null ? _disc : _disc = BuildDisc(128, 0f);

        /// <summary>A hollow ring (for the arena border / telegraphs).</summary>
        public static Sprite Ring => _ring != null ? _ring : _ring = BuildRing(128, 0.12f);

        /// <summary>A radial glow disc (bright center fading to transparent).</summary>
        public static Sprite Glow => _soft != null ? _soft : _soft = BuildGlow(128);

        /// <summary>A 9-sliced rounded rectangle for UI panels/bars.</summary>
        public static Sprite RoundedRect => _rounded != null ? _rounded : _rounded = BuildRounded(48, 14);

        private static Sprite BuildDisc(int size, float _)
        {
            var tex = NewTex(size);
            float r = size * 0.5f;
            float rc = r - 1.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - r + 0.5f) * (x - r + 0.5f) + (y - r + 0.5f) * (y - r + 0.5f));
                    float a = Mathf.Clamp01(rc - d + 1f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            return Finish(tex, size);
        }

        private static Sprite BuildRing(int size, float thickness)
        {
            var tex = NewTex(size);
            float r = size * 0.5f;
            float outer = r - 1.5f;
            float inner = outer * (1f - thickness * 2f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - r + 0.5f) * (x - r + 0.5f) + (y - r + 0.5f) * (y - r + 0.5f));
                    float a = Mathf.Clamp01(outer - d + 1f) * Mathf.Clamp01(d - inner + 1f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            return Finish(tex, size);
        }

        private static Sprite BuildGlow(int size)
        {
            var tex = NewTex(size);
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - r + 0.5f) * (x - r + 0.5f) + (y - r + 0.5f) * (y - r + 0.5f)) / r;
                    float a = Mathf.Clamp01(1f - d);
                    a *= a;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            return Finish(tex, size);
        }

        private static Sprite BuildRounded(int size, int radius)
        {
            var tex = NewTex(size);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float a = RoundedAlpha(x, y, size, radius);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply();
            var border = new Vector4(radius, radius, radius, radius);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
        }

        private static float RoundedAlpha(int x, int y, int size, int radius)
        {
            float cx = Mathf.Clamp(x, radius, size - radius);
            float cy = Mathf.Clamp(y, radius, size - radius);
            float dx = x - cx;
            float dy = y - cy;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(radius - d + 1f);
        }

        private static Texture2D NewTex(int size)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private static Sprite Finish(Texture2D tex, int size)
        {
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
