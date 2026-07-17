using System.Reflection;
using Neo.NoCode;
using Neo.Reactive;
using Neo.Samples.Survivor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Bright, self-contained demo for <b>Neo.NoCode</b> data-binding components, all created and wired from
    ///     code. One reactive float — <c>DemoHealth.Current</c> — is the single source of truth. Three binding
    ///     components resolve that member by (component type name + member name) through
    ///     <see cref="ComponentFloatBinding" /> and refresh automatically when the reactive value changes:
    ///     <see cref="NoCodeBindText" /> prints it, <see cref="SetProgress" /> drives a filled bar Image, and
    ///     <see cref="NoCodeFormattedText" /> formats current/max into one string. The +/- buttons only touch the
    ///     reactive value; text and bar update themselves through the binding layer. Robust in an empty scene.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/NoCode Binding Demo")]
    public sealed class NoCodeBindingDemoController : MonoBehaviour
    {
        private const string SourceType = nameof(DemoHealth);
        private const string CurrentMember = nameof(DemoHealth.Current);
        private const string MaxMember = nameof(DemoHealth.Max);

        private NeoDemoShell.Context _shell;
        private DemoHealth _health;

        private void Start()
        {
            _shell = NeoDemoShell.Build("Neo.NoCode", new Color(0.55f, 0.42f, 0.95f));

            NeoDemoShell.ShowInfoCardOnce(
                "Neo.NoCode binding (code-created)",
                "One reactive float feeds three binding components; +/- change only the value.",
                "NoCodeBindText → DemoHealth.Current (text)",
                "SetProgress → DemoHealth.Current (bar fill 0..Max)",
                "NoCodeFormattedText → '{Current} / {Max} HP'");

            // WHY: single source of truth — a reactive float on a plain component the bindings resolve by name.
            var host = new GameObject("DemoHealth");
            host.transform.SetParent(transform, false);
            _health = host.AddComponent<DemoHealth>();

            // WHY: NoCodeBindText prints Current onto its own TMP_Text (auto-found via GetComponent).
            TMP_Text bindTextValue = _shell.AddValueLabel("NoCodeBindText");
            var bindText = bindTextValue.gameObject.AddComponent<NoCodeBindText>();
            ConfigureBinding(bindText.Binding, host, CurrentMember);
            Rewake(bindText);

            // WHY: SetProgress maps Current through InverseLerp(0..Max) into a Filled Image.fillAmount.
            Image fill = AddFilledBarRow("SetProgress → fill", _shell.Accent);
            var progress = host.AddComponent<SetProgress>();
            ConfigureBinding(progress.Binding, host, CurrentMember);
            SetPrivateField(progress, "_image", fill);
            SetPrivateField(progress, "_minValue", 0f);
            SetPrivateField(progress, "_maxValue", _health.Max);
            Rewake(progress);

            // WHY: NoCodeFormattedText runs String.Format over two bindings (Current, Max).
            TMP_Text formattedValue = _shell.AddValueLabel("NoCodeFormattedText");
            var formatted = formattedValue.gameObject.AddComponent<NoCodeFormattedText>();
            SetPrivateField(formatted, "_values", new[]
            {
                MakeBinding(host, CurrentMember),
                MakeBinding(host, MaxMember)
            });
            SetPrivateField(formatted, "_format", "{0:0} / {1:0} HP");
            Rewake(formatted);

            _shell.AddButtonRow(
                ("-10 HP", () => AddHealth(-10f)),
                ("+10 HP", () => AddHealth(10f)),
                ("Reset", () => SetHealth(_health.Max)));

            _shell.Log($"NoCodeBindText → {SourceType}.{CurrentMember}");
            _shell.Log($"SetProgress → {SourceType}.{CurrentMember} (0..{_health.Max:0})");
            _shell.Log($"NoCodeFormattedText → {SourceType}.{CurrentMember}/{MaxMember}");
        }

        private void AddHealth(float delta)
        {
            SetHealth(_health.Current.Value + delta);
        }

        private void SetHealth(float value)
        {
            float clamped = Mathf.Clamp(value, 0f, _health.Max);
            // WHY: touch only the reactive value — the bindings react and repaint text + bar on their own.
            _health.Current.Value = clamped;
            _shell.Log($"DemoHealth.Current = {clamped:0} → bindings refreshed");
        }

        private static void ConfigureBinding(ComponentFloatBinding binding, GameObject source, string member)
        {
            binding.SourceRoot = source;
            binding.ComponentTypeName = SourceType;
            binding.MemberName = member;
        }

        private static ComponentFloatBinding MakeBinding(GameObject source, string member)
        {
            var binding = new ComponentFloatBinding();
            ConfigureBinding(binding, source, member);
            return binding;
        }

        // WHY: re-run OnEnable now that the binding is configured, so the component resolves the member,
        // subscribes to the reactive property, and paints its first value.
        private static void Rewake(Behaviour behaviour)
        {
            behaviour.enabled = false;
            behaviour.enabled = true;
        }

        private static void SetPrivateField(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            info?.SetValue(target, value);
        }

        private Image AddFilledBarRow(string caption, Color color)
        {
            RectTransform row = SurvivorUI.Rect("BarRow", _shell.Content);
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 46f;
            element.preferredHeight = 46f;
            element.flexibleWidth = 1f;

            TMP_Text cap = SurvivorUI.Label("Caption", row, caption, 15f, NeoDemoShell.Muted,
                TextAlignmentOptions.Left);
            SurvivorUI.Anchor(cap.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 18f));

            Image track = SurvivorUI.Image("Track", row, NeoDemoShell.Track);
            SurvivorUI.Anchor(track.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(0f, 18f));

            Image fill = SurvivorUI.Image("Fill", track.transform, color);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            RectTransform fillRt = fill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(2f, 2f);
            fillRt.offsetMax = new Vector2(-2f, -2f);
            return fill;
        }

        /// <summary>
        ///     Minimal "Health-like" source: a reactive current value plus a plain max. The NoCode bindings
        ///     resolve <see cref="Current" /> and <see cref="Max" /> purely by name via reflection.
        /// </summary>
        public sealed class DemoHealth : MonoBehaviour
        {
            public ReactivePropertyFloat Current = new(70f);
            public float Max = 100f;
        }
    }
}
