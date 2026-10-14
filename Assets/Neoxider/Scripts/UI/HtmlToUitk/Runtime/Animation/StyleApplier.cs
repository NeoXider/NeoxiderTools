using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Animation
{
    public abstract class StyleChannel
    {
        public abstract void Apply(IStyle style, AnimValue value);

        public abstract object Capture(IStyle style);

        public abstract void Restore(IStyle style, object saved);

        public abstract AnimValue Underlying(VisualElement element);
    }

    public static class StyleApplier
    {
        private static readonly Dictionary<string, StyleChannel> Channels = Build();

        public static bool TryGet(string property, out StyleChannel channel)
        {
            return Channels.TryGetValue(property, out channel);
        }

        public static IEnumerable<string> SupportedProperties => Channels.Keys;

        public static Length ToLength(AnimValue value, int index)
        {
            int i = Math.Min(index, value.Numbers.Length - 1);
            string unit = value.Units[i];
            return unit == "%" ? Length.Percent(value.Numbers[i]) : new Length(value.Numbers[i], LengthUnit.Pixel);
        }

        private static AnimValue FromLength(Length length)
        {
            return AnimValue.FromNumber(length.value, length.unit == LengthUnit.Percent ? "%" : "px");
        }

        private static AnimValue FromColor(Color color)
        {
            return AnimValue.FromColor(color.r, color.g, color.b, color.a);
        }

        private static Color ToColor(AnimValue value)
        {
            return value.Kind == AnimValueKind.Color ? new Color(value.R, value.G, value.B, value.A) : Color.clear;
        }

        private static Dictionary<string, StyleChannel> Build()
        {
            Dictionary<string, StyleChannel> map = new Dictionary<string, StyleChannel>(StringComparer.Ordinal)
            {
                { "opacity", new FloatChannel(s => s.opacity, (s, v) => s.opacity = v, e => e.resolvedStyle.opacity) },
                { "flex-grow", new FloatChannel(s => s.flexGrow, (s, v) => s.flexGrow = v, e => e.resolvedStyle.flexGrow) },
                { "flex-shrink", new FloatChannel(s => s.flexShrink, (s, v) => s.flexShrink = v, e => e.resolvedStyle.flexShrink) },
                { "translate", new TranslateChannel() },
                { "rotate", new RotateChannel() },
                { "scale", new ScaleChannel() },
                { "color", new ColorChannel(s => s.color, (s, v) => s.color = v, e => e.resolvedStyle.color) },
                { "background-color", new ColorChannel(s => s.backgroundColor, (s, v) => s.backgroundColor = v, e => e.resolvedStyle.backgroundColor) },
                { "-unity-background-image-tint-color", new ColorChannel(s => s.unityBackgroundImageTintColor, (s, v) => s.unityBackgroundImageTintColor = v, e => e.resolvedStyle.unityBackgroundImageTintColor) },
                { "border-top-color", new ColorChannel(s => s.borderTopColor, (s, v) => s.borderTopColor = v, e => e.resolvedStyle.borderTopColor) },
                { "border-right-color", new ColorChannel(s => s.borderRightColor, (s, v) => s.borderRightColor = v, e => e.resolvedStyle.borderRightColor) },
                { "border-bottom-color", new ColorChannel(s => s.borderBottomColor, (s, v) => s.borderBottomColor = v, e => e.resolvedStyle.borderBottomColor) },
                { "border-left-color", new ColorChannel(s => s.borderLeftColor, (s, v) => s.borderLeftColor = v, e => e.resolvedStyle.borderLeftColor) },
                { "left", new LengthChannel(s => s.left, (s, v) => s.left = v, e => e.resolvedStyle.left) },
                { "top", new LengthChannel(s => s.top, (s, v) => s.top = v, e => e.resolvedStyle.top) },
                { "right", new LengthChannel(s => s.right, (s, v) => s.right = v, e => e.resolvedStyle.right) },
                { "bottom", new LengthChannel(s => s.bottom, (s, v) => s.bottom = v, e => e.resolvedStyle.bottom) },
                { "width", new LengthChannel(s => s.width, (s, v) => s.width = v, e => e.resolvedStyle.width) },
                { "height", new LengthChannel(s => s.height, (s, v) => s.height = v, e => e.resolvedStyle.height) },
                { "margin-top", new LengthChannel(s => s.marginTop, (s, v) => s.marginTop = v, e => e.resolvedStyle.marginTop) },
                { "margin-right", new LengthChannel(s => s.marginRight, (s, v) => s.marginRight = v, e => e.resolvedStyle.marginRight) },
                { "margin-bottom", new LengthChannel(s => s.marginBottom, (s, v) => s.marginBottom = v, e => e.resolvedStyle.marginBottom) },
                { "margin-left", new LengthChannel(s => s.marginLeft, (s, v) => s.marginLeft = v, e => e.resolvedStyle.marginLeft) },
                { "padding-top", new LengthChannel(s => s.paddingTop, (s, v) => s.paddingTop = v, e => e.resolvedStyle.paddingTop) },
                { "padding-right", new LengthChannel(s => s.paddingRight, (s, v) => s.paddingRight = v, e => e.resolvedStyle.paddingRight) },
                { "padding-bottom", new LengthChannel(s => s.paddingBottom, (s, v) => s.paddingBottom = v, e => e.resolvedStyle.paddingBottom) },
                { "padding-left", new LengthChannel(s => s.paddingLeft, (s, v) => s.paddingLeft = v, e => e.resolvedStyle.paddingLeft) },
                { "font-size", new LengthChannel(s => s.fontSize, (s, v) => s.fontSize = v, e => e.resolvedStyle.fontSize) },
                { "display", new DisplayChannel() },
                { "visibility", new VisibilityChannel() }
            };
            return map;
        }

        private sealed class FloatChannel : StyleChannel
        {
            private readonly Func<IStyle, StyleFloat> _get;
            private readonly Action<IStyle, StyleFloat> _set;
            private readonly Func<VisualElement, float> _resolved;

            public FloatChannel(Func<IStyle, StyleFloat> get, Action<IStyle, StyleFloat> set, Func<VisualElement, float> resolved)
            {
                _get = get;
                _set = set;
                _resolved = resolved;
            }

            public override void Apply(IStyle style, AnimValue value)
            {
                if (value.Kind == AnimValueKind.Numbers)
                    _set(style, value.Numbers[0]);
            }

            public override object Capture(IStyle style) => _get(style);

            public override void Restore(IStyle style, object saved) => _set(style, (StyleFloat)saved);

            public override AnimValue Underlying(VisualElement element) => AnimValue.FromNumber(_resolved(element));
        }

        private sealed class LengthChannel : StyleChannel
        {
            private readonly Func<IStyle, StyleLength> _get;
            private readonly Action<IStyle, StyleLength> _set;
            private readonly Func<VisualElement, float> _resolved;

            public LengthChannel(Func<IStyle, StyleLength> get, Action<IStyle, StyleLength> set, Func<VisualElement, float> resolved)
            {
                _get = get;
                _set = set;
                _resolved = resolved;
            }

            public override void Apply(IStyle style, AnimValue value)
            {
                if (value.Kind == AnimValueKind.Numbers)
                    _set(style, ToLength(value, 0));
            }

            public override object Capture(IStyle style) => _get(style);

            public override void Restore(IStyle style, object saved) => _set(style, (StyleLength)saved);

            public override AnimValue Underlying(VisualElement element) => AnimValue.FromNumber(_resolved(element), "px");
        }

        private sealed class ColorChannel : StyleChannel
        {
            private readonly Func<IStyle, StyleColor> _get;
            private readonly Action<IStyle, StyleColor> _set;
            private readonly Func<VisualElement, Color> _resolved;

            public ColorChannel(Func<IStyle, StyleColor> get, Action<IStyle, StyleColor> set, Func<VisualElement, Color> resolved)
            {
                _get = get;
                _set = set;
                _resolved = resolved;
            }

            public override void Apply(IStyle style, AnimValue value)
            {
                _set(style, ToColor(value));
            }

            public override object Capture(IStyle style) => _get(style);

            public override void Restore(IStyle style, object saved) => _set(style, (StyleColor)saved);

            public override AnimValue Underlying(VisualElement element) => FromColor(_resolved(element));
        }

        private sealed class TranslateChannel : StyleChannel
        {
            public override void Apply(IStyle style, AnimValue value)
            {
                if (value.Kind == AnimValueKind.Numbers)
                    style.translate = new Translate(ToLength(value, 0), value.Numbers.Length > 1 ? ToLength(value, 1) : new Length(0f, LengthUnit.Pixel));
            }

            public override object Capture(IStyle style) => style.translate;

            public override void Restore(IStyle style, object saved) => style.translate = (StyleTranslate)saved;

            public override AnimValue Underlying(VisualElement element)
            {
                Vector3 t = element.resolvedStyle.translate;
                return AnimValue.Parse(t.x.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "px " + t.y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "px");
            }
        }

        private sealed class RotateChannel : StyleChannel
        {
            public override void Apply(IStyle style, AnimValue value)
            {
                if (value.Kind != AnimValueKind.Numbers)
                    return;
                float amount = value.Numbers[0];
                switch (value.Units[0])
                {
                    case "rad":
                        style.rotate = new Rotate(new Angle(amount, AngleUnit.Radian));
                        break;
                    case "grad":
                        style.rotate = new Rotate(new Angle(amount, AngleUnit.Gradian));
                        break;
                    case "turn":
                        style.rotate = new Rotate(new Angle(amount, AngleUnit.Turn));
                        break;
                    default:
                        style.rotate = new Rotate(new Angle(amount, AngleUnit.Degree));
                        break;
                }
            }

            public override object Capture(IStyle style) => style.rotate;

            public override void Restore(IStyle style, object saved) => style.rotate = (StyleRotate)saved;

            public override AnimValue Underlying(VisualElement element)
            {
                return AnimValue.FromNumber(element.resolvedStyle.rotate.angle.ToDegrees(), "deg");
            }
        }

        private sealed class ScaleChannel : StyleChannel
        {
            public override void Apply(IStyle style, AnimValue value)
            {
                if (value.Kind != AnimValueKind.Numbers)
                    return;
                float x = value.Numbers[0];
                float y = value.Numbers.Length > 1 ? value.Numbers[1] : x;
                style.scale = new Scale(new Vector3(x, y, 1f));
            }

            public override object Capture(IStyle style) => style.scale;

            public override void Restore(IStyle style, object saved) => style.scale = (StyleScale)saved;

            public override AnimValue Underlying(VisualElement element)
            {
                Vector3 s = element.resolvedStyle.scale.value;
                return AnimValue.Parse(s.x.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + " " + s.y.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        private sealed class DisplayChannel : StyleChannel
        {
            public override void Apply(IStyle style, AnimValue value)
            {
                style.display = value.Kind == AnimValueKind.Discrete && string.Equals(value.Raw, "none", StringComparison.Ordinal) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            public override object Capture(IStyle style) => style.display;

            public override void Restore(IStyle style, object saved) => style.display = (StyleEnum<DisplayStyle>)saved;

            public override AnimValue Underlying(VisualElement element) => AnimValue.Discrete(element.resolvedStyle.display == DisplayStyle.None ? "none" : "flex");
        }

        private sealed class VisibilityChannel : StyleChannel
        {
            public override void Apply(IStyle style, AnimValue value)
            {
                style.visibility = value.Kind == AnimValueKind.Discrete && string.Equals(value.Raw, "hidden", StringComparison.Ordinal) ? Visibility.Hidden : Visibility.Visible;
            }

            public override object Capture(IStyle style) => style.visibility;

            public override void Restore(IStyle style, object saved) => style.visibility = (StyleEnum<Visibility>)saved;

            public override AnimValue Underlying(VisualElement element) => AnimValue.Discrete(element.resolvedStyle.visibility == Visibility.Hidden ? "hidden" : "visible");
        }
    }
}
