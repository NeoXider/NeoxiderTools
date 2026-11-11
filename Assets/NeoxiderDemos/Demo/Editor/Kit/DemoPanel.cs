using System;
using System.Collections.Generic;
using Neo.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor
{
    /// <summary>Parts of a slider row created by <see cref="DemoPanel.SliderRow" />.</summary>
    public readonly struct DemoSliderRow
    {
        public DemoSliderRow(Slider slider, TMP_Text caption, TMP_Text value, DemoValueReadout readout)
        {
            Slider = slider;
            Caption = caption;
            Value = value;
            Readout = readout;
        }

        public Slider Slider { get; }
        public TMP_Text Caption { get; }

        /// <summary>Live value label (null when the row was created without one).</summary>
        public TMP_Text Value { get; }

        /// <summary>Readout wired persistently to Slider.onValueChanged (null without a value label).</summary>
        public DemoValueReadout Readout { get; }
    }

    /// <summary>Parts of a "caption ....... value" row created by <see cref="DemoPanel.ValueRow" />.</summary>
    public readonly struct DemoValueRow
    {
        public DemoValueRow(RectTransform row, TMP_Text caption, TMP_Text value)
        {
            Row = row;
            Caption = caption;
            Value = value;
        }

        public RectTransform Row { get; }
        public TMP_Text Caption { get; }

        /// <summary>Value label to update at runtime (or to put a <see cref="DemoValueReadout" /> on).</summary>
        public TMP_Text Value { get; }
    }

    /// <summary>Parts of a progress bar row created by <see cref="DemoPanel.BarRow" />.</summary>
    public readonly struct DemoBarRow
    {
        public DemoBarRow(RectTransform row, TMP_Text caption, Image track, Image fill)
        {
            Row = row;
            Caption = caption;
            Track = track;
            Fill = fill;
        }

        public RectTransform Row { get; }
        public TMP_Text Caption { get; }
        public Image Track { get; }

        /// <summary>
        ///     Fill image: anchor-driven (SurvivorUI.SetFill) or, for a filled bar, a Filled image whose fillAmount a
        ///     persistent listener drives (<c>DemoWiring.AddSetter(evt, bar.Fill, "fillAmount")</c>, NoCode SetProgress).
        /// </summary>
        public Image Fill { get; }
    }

    /// <summary>
    ///     A card with a vertical layout that rows are appended to, in call order. Every row is a named child
    ///     ("Slider · Mouse sensitivity", "Buttons · Low | High"), so a rebuild finds and restyles the same
    ///     objects. Button objects keep the NeoDemoShell name "Btn_&lt;label&gt;" that play-mode tests click by.
    ///     Controls come back without listeners; wire them with <see cref="DemoWiring" />.
    /// </summary>
    public sealed class DemoPanel
    {
        private const string Separator = " · ";

        public DemoPanel(DemoSceneBuildContext context, RectTransform rect, Color accent)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Rect = rect != null ? rect : throw new ArgumentNullException(nameof(rect));
            Accent = accent;
        }

        public DemoSceneBuildContext Context { get; }
        public RectTransform Rect { get; }
        public Color Accent { get; }

        /// <summary>Empty full-width row of a fixed height for custom content.</summary>
        public RectTransform Row(string name, float height)
        {
            RectTransform row = Context.UiChild(Rect, name);
            LayoutElement element = Context.Ensure<LayoutElement>(row.gameObject);
            element.ignoreLayout = false;
            element.minWidth = -1f;
            element.preferredWidth = -1f;
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleWidth = 1f;
            element.flexibleHeight = -1f;
            return row;
        }

        /// <summary>Small uppercase accent caption that groups the rows below it.</summary>
        public TMP_Text Section(string caption)
        {
            RectTransform row = Row("Section" + Separator + caption, 28f);
            TMP_Text text = DemoSceneKit.Text(Context, row, "Caption", (caption ?? string.Empty).ToUpperInvariant(), 14f,
                Accent, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
            text.characterSpacing = 6f;
            DemoSceneKit.Stretch(text.rectTransform);
            return text;
        }

        /// <summary>Single-line label row (status lines, short explanations).</summary>
        public TMP_Text Label(string text, float fontSize = 22f, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            RectTransform row = Row("Label" + Separator + Shorten(text), Mathf.Max(24f, fontSize + 8f));
            TMP_Text label = DemoSceneKit.Text(Context, row, "Label", text, fontSize, DemoPalette.Text, alignment);
            DemoSceneKit.Stretch(label.rectTransform);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        /// <summary>Wrapping instructions / status text (24 pt).</summary>
        public TMP_Text Paragraph(string text, float height = 64f)
        {
            RectTransform row = Row("Text" + Separator + Shorten(text), height);
            TMP_Text paragraph = DemoSceneKit.Text(Context, row, "Instructions", text, 24f, DemoPalette.Text);
            DemoSceneKit.Stretch(paragraph.rectTransform);
            paragraph.textWrappingMode = TextWrappingModes.Normal;
            return paragraph;
        }

        /// <summary>Small muted single line (hints under a control).</summary>
        public TMP_Text Note(string text, float height = 24f)
        {
            RectTransform row = Row("Note" + Separator + Shorten(text), height);
            TMP_Text note = DemoSceneKit.Text(Context, row, "Note", text, 16f, DemoPalette.Muted);
            DemoSceneKit.Stretch(note.rectTransform);
            return note;
        }

        /// <summary>"caption ....... value" row; returns the value label to update at runtime.</summary>
        public TMP_Text ValueLabel(string caption, string initial = "-")
        {
            return ValueRow(caption, initial).Value;
        }

        /// <summary>
        ///     "caption ....... value" row with all its parts. <paramref name="fitOneLine" /> makes caption and value
        ///     shrink to one line (auto-size, caption ellipsis) instead of wrapping into the next row: use it in
        ///     half-width <see cref="Columns" />.
        /// </summary>
        public DemoValueRow ValueRow(string caption, string initial = "-", bool fitOneLine = false)
        {
            RectTransform row = Row("Value" + Separator + caption, 30f);
            TMP_Text label = DemoSceneKit.Text(Context, row, "Caption", caption, 22f, DemoPalette.Muted);
            DemoSceneKit.Anchor(label.rectTransform, new Vector2(0f, 0f), new Vector2(0.55f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, Vector2.zero);
            TMP_Text value = DemoSceneKit.Text(Context, row, "Value", initial, 24f, DemoPalette.Text,
                TextAlignmentOptions.Right, FontStyles.Bold);
            DemoSceneKit.Anchor(value.rectTransform, new Vector2(0.45f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                Vector2.zero, Vector2.zero);
            if (fitOneLine)
            {
                FitOneLine(label, 11f, 17f);
                label.overflowMode = TextOverflowModes.Ellipsis;
                FitOneLine(value, 12f, 19f);
            }

            return new DemoValueRow(row, label, value);
        }

        /// <summary>
        ///     Value row for long live values at fixed smaller sizes: the caption takes <paramref name="split" /> of the
        ///     width, the value the rest, both single-line with an ellipsis. Returns the value label.
        /// </summary>
        /// <param name="caption">Plain caption; also names the row object, so keep it free of rich text.</param>
        /// <param name="displayCaption">Optional rich-text caption shown instead of <paramref name="caption" />.</param>
        /// <param name="split">Fraction of the row width given to the caption (0.2 - 0.8).</param>
        public TMP_Text CompactValue(string caption, string initial = "-", float split = 0.46f, float captionSize = 15f,
            float valueSize = 16f, string displayCaption = null)
        {
            split = Mathf.Clamp(split, 0.2f, 0.8f);
            DemoValueRow row = ValueRow(caption, initial);
            TMP_Text captionText = row.Caption;
            if (displayCaption != null)
            {
                captionText.text = displayCaption;
            }

            captionText.fontSize = captionSize;
            captionText.textWrappingMode = TextWrappingModes.NoWrap;
            captionText.overflowMode = TextOverflowModes.Ellipsis;
            DemoSceneKit.Anchor(captionText.rectTransform, new Vector2(0f, 0f), new Vector2(split, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

            TMP_Text value = row.Value;
            value.fontSize = valueSize;
            value.textWrappingMode = TextWrappingModes.NoWrap;
            value.overflowMode = TextOverflowModes.Ellipsis;
            DemoSceneKit.Anchor(value.rectTransform, new Vector2(split - 0.02f, 0f), new Vector2(1f, 1f),
                new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
            return value;
        }

        /// <summary>
        ///     Value row whose label is driven by a <see cref="DemoValueReadout" /> (format, optional bool texts): wire
        ///     reactive events straight to it, e.g. <c>DemoWiring.AddDynamic(property.OnChanged, readout.SetFloat)</c>.
        /// </summary>
        public DemoValueReadout ValueReadout(string caption, string format, string initial = "-", string onText = null,
            string offText = null)
        {
            DemoValueRow row = ValueRow(caption, initial);
            return DemoSceneKit.Readout(Context, row.Value, format, string.Empty, string.Empty, onText, offText);
        }

        /// <summary>
        ///     Text row with a stable object name (not derived from the initial text) for runtime-updated, possibly
        ///     multi-line text: "Text · <paramref name="name" />".
        /// </summary>
        public TMP_Text TextRow(string name, string initial, float fontSize = 18f, float height = 30f, bool wrap = true,
            TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft, Color? color = null)
        {
            RectTransform row = Row("Text" + Separator + name, height);
            TMP_Text text = DemoSceneKit.Text(Context, row, "Text", initial, fontSize, color ?? DemoPalette.Text, alignment);
            DemoSceneKit.Stretch(text.rectTransform);
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = wrap ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
            return text;
        }

        /// <summary>
        ///     "Log feed" section plus an inset <see cref="DemoLogFeed" /> row (use it as the last rows of a card when the
        ///     bottom of the screen belongs to the world and the frame's own log feed is off).
        /// </summary>
        public DemoLogFeed LogRow(int lines, string readyMessage = "ready", string section = "Log feed")
        {
            const float lineHeight = 21f;
            int clamped = Mathf.Clamp(lines, 1, DemoLogFeed.MaxCapacity);
            if (!string.IsNullOrEmpty(section))
            {
                Section(section);
            }

            RectTransform row = Row("Log", clamped * lineHeight + 14f);
            Image inset = Context.Ensure<Image>(row.gameObject);
            DemoSceneKit.StyleImage(inset, DemoPalette.WithAlpha(DemoPalette.Ink, 0.55f), true, false);

            TMP_Text text = DemoSceneKit.Text(Context, row, "LogText", "› " + readyMessage, 15f, DemoPalette.Text,
                TextAlignmentOptions.TopLeft);
            DemoSceneKit.Stretch(text.rectTransform, 8f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;

            DemoLogFeed log = Context.Ensure<DemoLogFeed>(row.gameObject);
            Context.Set(log, "_text", text);
            Context.Set(log, "_maxLines", clamped);
            Context.Set(log, "_lineColor", DemoPalette.Text);
            Context.Set(log, "_readyMessage", readyMessage);
            return log;
        }

        /// <summary>Big centered hero label (a level number, a score).</summary>
        public TMP_Text BigLabel(string initial)
        {
            RectTransform row = Row("Big" + Separator + Shorten(initial), 58f);
            TMP_Text big = DemoSceneKit.Text(Context, row, "Big", initial, 44f, DemoPalette.Text,
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.Stretch(big.rectTransform);
            return big;
        }

        /// <summary>One full-width accent button.</summary>
        public Button Button(string label)
        {
            return ButtonRow(label)[0];
        }

        /// <summary>Row of accent buttons sharing the width evenly (objects named "Btn_&lt;label&gt;").</summary>
        public Button[] ButtonRow(params string[] labels)
        {
            if (labels == null || labels.Length == 0)
            {
                throw new ArgumentException("ButtonRow needs at least one label.", nameof(labels));
            }

            RectTransform row = Row("Buttons" + Separator + string.Join(" | ", labels), 52f);
            HorizontalLayoutGroup group = Context.Ensure<HorizontalLayoutGroup>(row.gameObject);
            group.padding = new RectOffset(0, 0, 0, 0);
            group.spacing = 8f;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;

            Button[] buttons = new Button[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                buttons[i] = DemoSceneKit.AccentButton(Context, row, labels[i], Accent);
            }

            return buttons;
        }

        /// <summary>Labeled on/off toggle (box on the right).</summary>
        public Toggle Toggle(string label, bool isOn)
        {
            RectTransform row = Row("Toggle" + Separator + label, 40f);
            TMP_Text caption = DemoSceneKit.Text(Context, row, "Caption", label, 18f, DemoPalette.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);
            DemoSceneKit.Anchor(caption.rectTransform, new Vector2(0f, 0f), new Vector2(0.75f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, Vector2.zero);

            Image box = DemoSceneKit.Image(Context, row, "Box", DemoPalette.Track, true, true);
            DemoSceneKit.Anchor(box.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                Vector2.zero, new Vector2(34f, 34f));
            Image check = DemoSceneKit.Image(Context, box.transform, "Check", Accent);
            DemoSceneKit.Stretch(check.rectTransform, 6f);

            Toggle toggle = Context.Ensure<Toggle>(box.gameObject);
            toggle.interactable = true;
            toggle.transition = Selectable.Transition.ColorTint;
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.group = null;
            toggle.toggleTransition = UnityEngine.UI.Toggle.ToggleTransition.Fade;
            toggle.SetIsOnWithoutNotify(isOn);
            return toggle;
        }

        /// <summary>Labeled slider; its value label follows the slider through a persistent listener.</summary>
        public Slider Slider(string label, float min, float max, float value, bool wholeNumbers = false,
            string format = "0.##")
        {
            return SliderRow(label, min, max, value, wholeNumbers, format).Slider;
        }

        /// <summary>
        ///     Slider row with all its parts. Pass <paramref name="format" /> = null to omit the value label (when
        ///     a controller syncs the slider with SetValueWithoutNotify and shows the value elsewhere).
        /// </summary>
        public DemoSliderRow SliderRow(string label, float min, float max, float value, bool wholeNumbers = false,
            string format = "0.##")
        {
            if (max < min)
            {
                throw new ArgumentException("Slider '" + label + "' has max " + max + " below min " + min + ".");
            }

            RectTransform row = Row("Slider" + Separator + label, 62f);
            TMP_Text caption = DemoSceneKit.Text(Context, row, "Caption", label, 18f, DemoPalette.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);
            DemoSceneKit.Anchor(caption.rectTransform, new Vector2(0f, 1f), new Vector2(0.7f, 1f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(0f, 24f));

            TMP_Text valueText = null;
            DemoValueReadout readout = null;
            if (format != null)
            {
                valueText = DemoSceneKit.Text(Context, row, "Value",
                    value.ToString(format, System.Globalization.CultureInfo.InvariantCulture), 18f, Accent,
                    TextAlignmentOptions.Right, FontStyles.Bold);
                DemoSceneKit.Anchor(valueText.rectTransform, new Vector2(0.7f, 1f), new Vector2(1f, 1f),
                    new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 24f));
                readout = Context.Ensure<DemoValueReadout>(valueText.gameObject);
                readout.Configure(valueText, format);
            }

            RectTransform sliderRect = Context.UiChild(row, "Slider");
            DemoSceneKit.Anchor(sliderRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 8f), new Vector2(0f, 20f));

            Image background = DemoSceneKit.Image(Context, sliderRect, "Background", DemoPalette.Track, true, true);
            DemoSceneKit.SetAnchors(background.rectTransform, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f),
                Vector2.zero, Vector2.zero);

            RectTransform fillArea = Context.UiChild(sliderRect, "Fill Area");
            DemoSceneKit.SetAnchors(fillArea, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), new Vector2(6f, 0f),
                new Vector2(-6f, 0f));
            Image fill = DemoSceneKit.Image(Context, fillArea, "Fill", Accent);
            fill.rectTransform.sizeDelta = Vector2.zero;

            RectTransform handleArea = Context.UiChild(sliderRect, "Handle Slide Area");
            DemoSceneKit.SetAnchors(handleArea, Vector2.zero, Vector2.one, new Vector2(6f, 0f), new Vector2(-6f, 0f));
            Image handle = DemoSceneKit.Image(Context, handleArea, "Handle", DemoPalette.Text, true, true);
            handle.rectTransform.sizeDelta = new Vector2(16f, 0f);

            Slider slider = Context.Ensure<Slider>(sliderRect.gameObject);
            slider.interactable = true;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.wholeNumbers = wholeNumbers;
            slider.maxValue = max;
            slider.minValue = min;
            slider.maxValue = max;
            slider.SetValueWithoutNotify(value);
            ColorBlock colors = slider.colors;
            colors.highlightedColor = Color.white;
            slider.colors = colors;

            if (readout != null)
            {
                DemoWiring.AddDynamic(slider.onValueChanged, readout.SetFloat);
            }

            return new DemoSliderRow(slider, caption, valueText, readout);
        }

        /// <summary>Labeled TextMeshPro input field (built from TMP_DefaultControls, restyled dark).</summary>
        public TMP_InputField InputField(string label, string text = "", string placeholder = "Type here...")
        {
            RectTransform row = Row("Input" + Separator + label, 52f);
            TMP_Text caption = DemoSceneKit.Text(Context, row, "Caption", label, 18f, DemoPalette.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);
            DemoSceneKit.Anchor(caption.rectTransform, new Vector2(0f, 0f), new Vector2(0.4f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, Vector2.zero);

            GameObject host = Context.Reuse(row, "InputField") ??
                              Context.Attach(TMP_DefaultControls.CreateInputField(DemoSceneAssets.TmpResources()), row,
                                  "InputField");
            DemoSceneKit.SetAnchors((RectTransform)host.transform, new Vector2(0.4f, 0f), Vector2.one, new Vector2(8f, 4f),
                new Vector2(0f, -4f));

            Image background = Context.Ensure<Image>(host);
            DemoSceneKit.StyleImage(background, new Color(1f, 1f, 1f, 0.12f), true, true);
            TMP_InputField field = Context.Ensure<TMP_InputField>(host);
            field.interactable = true;
            field.targetGraphic = background;
            TMP_FontAsset font = DemoSceneAssets.Font;
            if (font != null)
            {
                field.fontAsset = font;
            }

            if (field.textComponent != null)
            {
                DemoSceneKit.StyleText(field.textComponent, field.textComponent.text, 18f, DemoPalette.Text,
                    TextAlignmentOptions.Left, FontStyles.Normal);
                field.textComponent.textWrappingMode = TextWrappingModes.NoWrap;
                Context.MarkModified(field.textComponent);
            }

            if (field.placeholder is TMP_Text placeholderText)
            {
                DemoSceneKit.StyleText(placeholderText, placeholder, 18f, DemoPalette.WithAlpha(DemoPalette.Muted, 0.7f),
                    TextAlignmentOptions.Left, FontStyles.Italic);
                placeholderText.textWrappingMode = TextWrappingModes.NoWrap;
                Context.MarkModified(placeholderText);
            }

            field.customCaretColor = true;
            field.caretColor = Accent;
            field.selectionColor = DemoPalette.WithAlpha(Accent, 0.35f);
            field.SetTextWithoutNotify(text ?? string.Empty);
            return field;
        }

        /// <summary>Labeled TextMeshPro dropdown with the given options (built from TMP_DefaultControls, restyled dark).</summary>
        public TMP_Dropdown Dropdown(string label, IReadOnlyList<string> options, int value = 0)
        {
            if (options == null || options.Count == 0)
            {
                throw new ArgumentException("Dropdown '" + label + "' needs at least one option.", nameof(options));
            }

            RectTransform row = Row("Dropdown" + Separator + label, 52f);
            TMP_Text caption = DemoSceneKit.Text(Context, row, "Caption", label, 18f, DemoPalette.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);
            DemoSceneKit.Anchor(caption.rectTransform, new Vector2(0f, 0f), new Vector2(0.4f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, Vector2.zero);

            GameObject host = Context.Reuse(row, "Dropdown") ??
                              Context.Attach(TMP_DefaultControls.CreateDropdown(DemoSceneAssets.TmpResources()), row,
                                  "Dropdown");
            DemoSceneKit.SetAnchors((RectTransform)host.transform, new Vector2(0.4f, 0f), Vector2.one, new Vector2(8f, 4f),
                new Vector2(0f, -4f));

            Image background = Context.Ensure<Image>(host);
            DemoSceneKit.StyleImage(background, new Color(1f, 1f, 1f, 0.12f), true, true);
            TMP_Dropdown dropdown = Context.Ensure<TMP_Dropdown>(host);
            dropdown.interactable = true;
            dropdown.targetGraphic = background;
            StyleDropdownText(dropdown.captionText, 18f);
            StyleDropdownText(dropdown.itemText, 18f);
            StyleChildImage(host.transform, "Arrow", DemoPalette.Text, false);

            RectTransform template = dropdown.template;
            if (template != null)
            {
                template.sizeDelta = new Vector2(template.sizeDelta.x, 180f);
                StyleChildImage(host.transform, "Template", new Color(DemoPalette.Card.r, DemoPalette.Card.g,
                    DemoPalette.Card.b, 1f), true);
                StyleChildImage(template, "Viewport/Content/Item/Item Background", new Color(0.20f, 0.21f, 0.28f, 1f),
                    false);
                StyleChildImage(template, "Viewport/Content/Item/Item Checkmark", Accent, false);
                StyleChildImage(template, "Scrollbar", DemoPalette.Track, false);
                StyleChildImage(template, "Scrollbar/Sliding Area/Handle", DemoPalette.Muted, false);
                SetChildHeight(template, "Viewport/Content/Item", 32f);
                SetChildHeight(template, "Viewport/Content", 32f);
                template.gameObject.SetActive(false);
            }

            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
            dropdown.SetValueWithoutNotify(Mathf.Clamp(value, 0, options.Count - 1));
            dropdown.RefreshShownValue();
            return dropdown;
        }

        /// <summary>Labeled progress bar; returns the fill image (drive it with SurvivorUI.SetFill at runtime).</summary>
        public Image Bar(string label, Color color, float fill = 0f)
        {
            return BarRow(label, color, fill).Fill;
        }

        /// <summary>
        ///     Labeled progress bar with all its parts. By default the fill is sized by its anchors (code drives it);
        ///     with <paramref name="filled" /> it is a Filled image on the plain white kit sprite, so a persistent
        ///     listener can drive Image.fillAmount without code (NoCode SetProgress, a binding's OnPercent).
        /// </summary>
        public DemoBarRow BarRow(string label, Color color, float fill = 0f, bool filled = false)
        {
            RectTransform row = Row("Bar" + Separator + label, 44f);
            TMP_Text caption = DemoSceneKit.Text(Context, row, "Caption", label, 15f, DemoPalette.Muted);
            DemoSceneKit.Anchor(caption.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(0f, 18f));

            Image track = DemoSceneKit.Image(Context, row, "Bar", DemoPalette.Track);
            DemoSceneKit.Anchor(track.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 4f), new Vector2(0f, 16f));
            Image fillImage = DemoSceneKit.Image(Context, track.transform, "Fill", color);
            RectTransform fillRect = fillImage.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(filled ? 1f : Mathf.Clamp01(fill), 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            if (filled)
            {
                // WHY: fillAmount only shows on a Filled image; the 9-sliced rounded sprite would stretch its corners.
                fillImage.sprite = DemoSceneAssets.WhiteSquare;
                fillImage.type = UnityEngine.UI.Image.Type.Filled;
                fillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
                fillImage.fillAmount = Mathf.Clamp01(fill);
            }

            return new DemoBarRow(row, caption, track, fillImage);
        }

        /// <summary>Side-by-side columns, each a panel with its own vertical layout.</summary>
        public DemoPanel[] Columns(string name, int count, float spacing = 12f)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Columns needs at least one column.");
            }

            RectTransform row = Context.UiChild(Rect, "Columns" + Separator + name);
            LayoutElement element = Context.Ensure<LayoutElement>(row.gameObject);
            element.minHeight = -1f;
            element.preferredHeight = -1f;
            element.flexibleWidth = 1f;
            HorizontalLayoutGroup group = Context.Ensure<HorizontalLayoutGroup>(row.gameObject);
            group.spacing = spacing;
            group.childAlignment = TextAnchor.UpperLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            DemoPanel[] columns = new DemoPanel[count];
            for (int i = 0; i < count; i++)
            {
                RectTransform column = Context.UiChild(row, "Column " + (i + 1));
                DemoSceneKit.VerticalLayout(Context, column.gameObject, new RectOffset(0, 0, 0, 0), 8f);
                columns[i] = new DemoPanel(Context, column, Accent);
            }

            return columns;
        }

        /// <summary>Empty vertical gap.</summary>
        public RectTransform Spacer(float height)
        {
            return Row("Spacer", height);
        }

        /// <summary>Single line that auto-sizes between <paramref name="min" /> and <paramref name="max" /> points.</summary>
        public static void FitOneLine(TMP_Text text, float min, float max)
        {
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
            text.fontSize = max;
        }

        private void StyleDropdownText(TMP_Text text, float size)
        {
            if (text == null)
            {
                return;
            }

            DemoSceneKit.StyleText(text, text.text, size, DemoPalette.Text, TextAlignmentOptions.Left, FontStyles.Normal);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            Context.MarkModified(text);
        }

        private void StyleChildImage(Transform parent, string path, Color color, bool rounded)
        {
            Transform child = parent.Find(path);
            Image image = child != null ? child.GetComponent<Image>() : null;
            if (image == null)
            {
                Context.Warn("Dropdown part '" + path + "' not found under '" + DemoSceneBuildContext.PathOf(parent) +
                             "'; left unstyled.");
                return;
            }

            image.color = color;
            if (rounded)
            {
                image.sprite = DemoSceneAssets.RoundedRect;
                image.type = UnityEngine.UI.Image.Type.Sliced;
            }

            Context.MarkModified(image);
        }

        private void SetChildHeight(Transform parent, string path, float height)
        {
            RectTransform child = parent.Find(path) as RectTransform;
            if (child != null)
            {
                child.sizeDelta = new Vector2(child.sizeDelta.x, height);
                Context.MarkModified(child);
            }
        }

        private static string Shorten(string text)
        {
            const int max = 32;
            if (string.IsNullOrEmpty(text))
            {
                return "(empty)";
            }

            string line = text.Replace('\n', ' ').Replace('/', '-');
            return line.Length <= max ? line : line.Substring(0, max);
        }
    }
}
