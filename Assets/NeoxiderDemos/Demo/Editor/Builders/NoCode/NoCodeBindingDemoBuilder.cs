using System;
using Neo.NoCode;
using Neo.Samples;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the NoCode binding demo into NoCodeBindingDemo.unity. One source - the hand-authored 'Counter'
    ///     (Counter.Value, a ReactivePropertyFloat) - drives every binding the module offers: NoCodeBindText into a
    ///     tweening SetText, NoCodeFormattedText, SetProgress into a Filled image and into a slider, plus the update
    ///     modes side by side (Reactive, Poll 0.5 s, Once + RefreshBinding). Buttons and the input slider call Counter
    ///     directly; nothing is built at runtime and there is no controller. The pre-kit Canvas is retired.
    /// </summary>
    internal static class NoCodeBindingDemoBuilder
    {
        internal const string ScenePath = "Scenes/NoCode/NoCodeBindingDemo.unity";
        internal const string MinusLabel = "-10";
        internal const string PlusLabel = "+10";
        internal const string ResetLabel = "Reset to 50";
        internal const string RefreshLabel = "RefreshBinding()";
        internal const string CounterName = "Counter";
        internal const float PollSeconds = 0.5f;

        private const string SceneFile = "NoCodeBindingDemo.unity";
        private const float StartValue = 50f;
        private const float RangeMax = 100f;

        private static readonly Color Accent = new(0.49f, 0.36f, 1f);

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            Counter counter = Step("ensure the Counter source", () => EnsureCounter(ctx));
            ModuleDemoSceneInfo info = Step("ensure the info service",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            Step("retire the pre-kit layout", () => RetireLegacyLayout(ctx));
            Step("upgrade the EventSystem input module", () => DemoSceneKit.UpgradeLegacyInputModules(ctx));

            DemoFrame frame = Step("author the frame", () => DemoSceneKit.Frame(ctx,
                new DemoFrameOptions("Neo.NoCode · Bindings", Accent) { ContentWidth = 920f, LogLines = 5 }));
            DemoPanel[] columns = Step("author the columns", () => frame.Content.Columns("Source | Bindings", 2, 20f));
            Step("author the source column", () => BuildSourceColumn(ctx, columns[0], counter, frame.Log));
            Step("author the bindings column", () => BuildBindingsColumn(ctx, columns[1], counter, frame.Log));
            Step("author the info card", () => frame.InfoCard(info, InfoContent()));
        }

        private static Counter EnsureCounter(DemoSceneBuildContext ctx)
        {
            Counter counter = LogicDemoBuildUtility.FindHandAuthored<Counter>(ctx, CounterName);
            if (counter != null)
            {
                return counter;
            }

            counter = ctx.Ensure<Counter>(ctx.Child(ctx.Root("Services").transform, CounterName));
            ctx.Set(counter, "_valueMode", (int)CounterValueMode.Int);
            ctx.Set(counter, "Value._value", StartValue);
            ctx.Set(counter, "_saveEnabled", false);
            ctx.Set(counter, "_saveKey", "Demo.NoCode.Counter");
            ctx.Note("No hand-authored 'Counter'; authored '[Demo] Services/" + CounterName + "'.");
            return counter;
        }

        private static void RetireLegacyLayout(DemoSceneBuildContext ctx)
        {
            GameObject canvas = ctx.FindHandAuthoredRoot("Canvas");
            if (canvas == null)
            {
                return;
            }

            bool isLegacyLayout = canvas.GetComponent<Canvas>() != null &&
                                  canvas.GetComponentInChildren<Counter>(true) == null &&
                                  (canvas.GetComponentInChildren<NoCodeFloatBindingBehaviour>(true) != null ||
                                   canvas.GetComponentInChildren<NoCodeFormattedText>(true) != null) &&
                                  LogicDemoBuildUtility.HasButtonCalling<Counter>(canvas);
            if (isLegacyLayout)
            {
                ctx.Retire(canvas,
                    "pre-kit labels, progress bar and +/- buttons; '[Demo] Frame' re-authors the same bindings");
            }
            else
            {
                ctx.Warn("Kept hand-authored root 'Canvas': it is not the pre-kit binding layout. Delete it by hand if " +
                         "it duplicates '[Demo] Frame'.");
            }
        }

        private static void BuildSourceColumn(DemoSceneBuildContext ctx, DemoPanel column, Counter counter, DemoLogFeed log)
        {
            string counterType = typeof(Counter).FullName;

            column.Section("Source · object '" + counter.name + "'");
            column.Note("Counter.Value (ReactivePropertyFloat)");
            TMP_Text big = column.BigLabel(StartValue.ToString("0"));
            SetText tween = ctx.Ensure<SetText>(big.gameObject);
            ctx.Set(tween, "text", big);
            ctx.Set(tween, "separator", string.Empty);
            ctx.Set(tween, "decimal", 0);
            ctx.Set(tween, "startAdd", string.Empty);
            ctx.Set(tween, "endAdd", string.Empty);
            ctx.Set(tween, "_timeAnim", 0.3f);
            NoCodeBindText bindText = ctx.Ensure<NoCodeBindText>(big.gameObject);
            LogicDemoBuildUtility.Bind(ctx, bindText, counter.gameObject, counterType, nameof(Counter.Value),
                NoCodeFloatUpdateMode.Reactive);
            ctx.Set(bindText, "_setText", tween);
            ctx.Set(bindText, "_fallbackText", big);
            column.Note("Above: NoCodeBindText -> SetText (counts up)");

            Button[] step = column.ButtonRow(MinusLabel, PlusLabel);
            DemoWiring.AddInt(step[0].onClick, counter.Add, -10);
            DemoWiring.AddString(step[0].onClick, log.Log, "Counter.Add(-10)");
            DemoWiring.AddInt(step[1].onClick, counter.Add, 10);
            DemoWiring.AddString(step[1].onClick, log.Log, "Counter.Add(10)");

            Slider input = column.Slider("Drag -> Counter.Set(float)", 0f, RangeMax, StartValue, true, "0");
            DemoWiring.AddDynamic(input.onValueChanged, counter.Set);

            Button reset = column.Button(ResetLabel);
            DemoWiring.AddInt(reset.onClick, counter.Set, (int)StartValue);
            DemoWiring.AddString(reset.onClick, log.Log, "Counter.Set(50)");
            column.Note("C#: counter.Add(10); counter.Set(50f);");
        }

        private static void BuildBindingsColumn(DemoSceneBuildContext ctx, DemoPanel column, Counter counter, DemoLogFeed log)
        {
            string counterType = typeof(Counter).FullName;
            LogicBindingSource value = new(counter.gameObject, counterType, nameof(Counter.Value));

            column.Section("Bindings · Reactive");
            TMP_Text formatted = column.ValueLabel("NoCodeFormattedText", "50 / 100");
            LogicDemoBuildUtility.FormattedText(ctx, formatted, "{0:0} / 100", NoCodeFloatUpdateMode.Reactive, 0.16f,
                value);

            Image fill = LogicDemoBuildUtility.FilledBar(ctx, column, "SetProgress -> Image.fillAmount", Accent,
                StartValue / RangeMax, out TMP_Text percent);
            LogicDemoBuildUtility.FormattedText(ctx, percent, "{0:0} %", NoCodeFloatUpdateMode.Reactive, 0.16f, value);
            SetProgress imageProgress = ctx.Ensure<SetProgress>(fill.transform.parent.parent.gameObject);
            LogicDemoBuildUtility.Bind(ctx, imageProgress, counter.gameObject, counterType, nameof(Counter.Value),
                NoCodeFloatUpdateMode.Reactive);
            ctx.Set(imageProgress, "_image", fill);
            ctx.Set(imageProgress, "_slider", null);
            ctx.Set(imageProgress, "_minValue", 0f);
            ctx.Set(imageProgress, "_maxValue", RangeMax);

            DemoSliderRow display = column.SliderRow("SetProgress -> Slider.normalizedValue", 0f, 1f,
                StartValue / RangeMax, false, null);
            // WHY: output only; an interactive slider here would write back into nothing and confuse the demo.
            display.Slider.interactable = false;
            SetProgress sliderProgress = ctx.Ensure<SetProgress>(display.Slider.transform.parent.gameObject);
            LogicDemoBuildUtility.Bind(ctx, sliderProgress, counter.gameObject, counterType, nameof(Counter.Value),
                NoCodeFloatUpdateMode.Reactive);
            ctx.Set(sliderProgress, "_image", null);
            ctx.Set(sliderProgress, "_slider", display.Slider);
            ctx.Set(sliderProgress, "_minValue", 0f);
            ctx.Set(sliderProgress, "_maxValue", RangeMax);

            column.Section("Update modes");
            TMP_Text polled = column.ValueLabel("Poll · every 0.5 s", "50 · polled");
            NoCodeFormattedText poll = LogicDemoBuildUtility.FormattedText(ctx, polled, "{0:0} · polled",
                NoCodeFloatUpdateMode.Poll, PollSeconds, value);
            DemoWiring.AddDynamic(poll.OnTextChanged, log.Log);

            TMP_Text once = column.ValueLabel("Once · on enable", "50");
            NoCodeBindText onceBinding = ctx.Ensure<NoCodeBindText>(once.gameObject);
            LogicDemoBuildUtility.Bind(ctx, onceBinding, counter.gameObject, counterType, nameof(Counter.Value),
                NoCodeFloatUpdateMode.Once);
            ctx.Set(onceBinding, "_setText", null);
            ctx.Set(onceBinding, "_fallbackText", once);
            Button refresh = column.Button(RefreshLabel);
            DemoWiring.AddVoid(refresh.onClick, onceBinding.RefreshBinding);
            DemoWiring.AddString(refresh.onClick, log.Log, "NoCodeBindText.RefreshBinding() on the Once binding");
            column.Note("Poll writes to the log through OnTextChanged");
        }

        private static DemoInfoContent InfoContent()
        {
            return new DemoInfoContent
            {
                Title = "Neo.NoCode · Bindings",
                Purpose = "Show any numeric value without code: each binding reads 'Counter' / Counter.Value and pushes " +
                          "it to text, a formatted string, an image fill or a slider.",
                RuntimeApi = "binding.Binding.SourceRoot / MemberName, RefreshBinding(), UpdateMode; " +
                             "NoCodeFormattedText.SetValues(sources, format); SetProgress.SetRange(min, max).",
                SceneWorkflow = "Select any label or bar on the right: its NoCode component names the source object, " +
                                "component and member. Buttons call Counter.Add / Set through persistent UnityEvents.",
                Verification = "Play: +10 / -10 or drag the slider - the Reactive bindings follow at once, Poll catches " +
                               "up within 0.5 s, Once changes only on RefreshBinding().",
                Hints = new[]
                {
                    "Buttons / slider -> Counter.Add / Counter.Set (NoCode)",
                    "Counter.Value -> NoCodeBindText, NoCodeFormattedText, SetProgress",
                    "Poll binding -> OnTextChanged -> log"
                },
                Corner = ModuleDemoSceneInfo.Corner.TopLeft,
                PanelWidth = 440f,
                StartCollapsed = false
            };
        }

        private static void Step(string step, Action action)
        {
            DemoSceneKit.Step(SceneFile, step, action);
        }

        private static T Step<T>(string step, Func<T> action)
        {
            return DemoSceneKit.Step(SceneFile, step, action);
        }
    }
}
