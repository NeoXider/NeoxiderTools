using System;
using System.Collections.Generic;
using Neo.Samples;
using Neo.Tools;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.Tools.Dialogue demo into Dialogue.unity: a visual-novel dialogue box (a
    ///     <see cref="DialogueUI" /> the scene's <see cref="DialogueController" /> drives; clicking it calls
    ///     <see cref="DialogueController.SkipOrNext" />), playback controls, a live panel fed by the controller's
    ///     events and a bark panel for <see cref="DialogueLinePlayer" /> (pooled lines by tag). The hand-authored
    ///     'DialogueComponent' keeps its conversation and its own listeners (the Audio Source); the pre-kit Canvas
    ///     with the old DialogueUI is retired on the first build.
    /// </summary>
    internal static class DialogueBuilder
    {
        private const string SceneName = "Dialogue";
        private const string BarkSpeaker = "Blacksmith";

        private static readonly Color Accent = new(0.33f, 0.83f, 0.86f);
        private static readonly Color StageColor = new(0.07f, 0.08f, 0.12f, 0.96f);

        [DemoSceneBuilder("Scenes/Tools/Dialogue.unity")]
        public static void Build(DemoSceneBuildContext ctx)
        {
            string step = "services";
            try
            {
                // WHY: the hand-authored 'DialogueComponent' wins, so the shipped conversation, portraits and its
                // Audio Source listener stay exactly as authored.
                DialogueController dialogue = ctx.FindOrCreate<DialogueController>("Services", "Dialogue Controller");
                DialogueLinePlayer barks = ctx.FindOrCreate<DialogueLinePlayer>("Services", "Blacksmith Barks");
                ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
                DialogueDemoController controller = ctx.FindOrCreate<DialogueDemoController>("Services", "Demo Controller");

                step = "conversation and bark lines";
                ConfigureConversation(ctx, dialogue);
                ConfigureBarks(ctx, barks);

                step = "retire pre-kit layout";
                RetireLegacyLayout(ctx);

                step = "frame";
                DemoFrame frame = DemoSceneKit.Frame(ctx,
                    new DemoFrameOptions("Neo.Tools.Dialogue", Accent) { ContentWidth = 920f });
                DemoPanel card = frame.Content;

                step = "dialogue box";
                card.Section("Dialogue box · DialogueUI · click it to continue");
                DialogueUI ui = BuildStage(ctx, card, dialogue);
                ctx.Set(dialogue, "_dialogueUI", ui);

                step = "playback and live columns";
                DemoPanel[] columns = card.Columns("Playback | Live", 2, 20f);
                PlaybackControls playback = BuildPlayback(columns[0], dialogue, controller);
                LiveValues live = BuildLive(columns[1]);

                step = "bark panel";
                BarkControls bark = BuildBarkPanel(ctx, frame, barks, controller);

                step = "controller events";
                WireDialogueEvents(ctx, dialogue, controller);
                DemoWiring.AddDynamic<string>(barks.OnLineStarted, controller.OnBarkStarted);
                DemoWiring.AddVoid(barks.OnLineFinished, controller.OnBarkFinished);

                step = "info card";
                frame.InfoCard(info, InfoContent());

                step = "controller references";
                ctx.Set(controller, "_dialogue", dialogue);
                ctx.Set(controller, "_barks", barks);
                ctx.Set(controller, "_typewriterToggle", playback.Typewriter);
                ctx.Set(controller, "_autoAdvanceToggle", playback.AutoAdvance);
                ctx.Set(controller, "_speedSlider", playback.Speed);
                ctx.Set(controller, "_autoIdleToggle", bark.AutoIdle);
                ctx.Set(controller, "_speakerValue", live.Speaker);
                ctx.Set(controller, "_cursorValue", live.Cursor);
                ctx.Set(controller, "_stateValue", live.State);
                ctx.Set(controller, "_typingFill", live.TypingFill);
                ctx.Set(controller, "_barkStatusValue", bark.Status);
                ctx.Set(controller, "_log", frame.Log);
            }
            catch (Exception exception)
            {
                throw DemoSceneKit.StepFailed(SceneName, step, exception);
            }
        }

        private static void ConfigureConversation(DemoSceneBuildContext ctx, DialogueController dialogue)
        {
            bool generated = DemoSceneBuildContext.IsGenerated(dialogue.gameObject);
            if (!generated && dialogue.dialogues != null && dialogue.dialogues.Length > 0)
            {
                return;
            }

            Sprite hero = DemoSprite("Frame 6.png");
            Sprite smith = DemoSprite("Frame 5.png");
            Sprite steel = DemoSprite("Frame 4.png");
            dialogue.dialogues = new[]
            {
                new Dialogue
                {
                    OnChangeDialog = new UnityEvent<int>(),
                    monologues = new[]
                    {
                        NewMonolog("Hero", NewSentence(hero, "Hi! I need a new sword.")),
                        NewMonolog(BarkSpeaker, NewSentence(smith, "Hello. Which one - iron or steel? I have both."),
                            NewSentence(steel, "Actually... steel suits you better.")),
                        NewMonolog("Hero", NewSentence(hero, "Great, I'll take it!"))
                    }
                }
            };
            dialogue.useTypewriterEffect = true;
            dialogue.autoStart = true;
            dialogue.allowRestart = true;
            dialogue.autoNextSentence = false;
            dialogue.autoNextMonolog = false;
            dialogue.autoNextDialogue = false;
            dialogue.Typewriter.CharactersPerSecond = 50f;
            ctx.MarkModified(dialogue);
            if (!generated)
            {
                ctx.Note("'" + dialogue.name + "' had no conversation; filled it with the demo conversation.");
            }
        }

        private static void ConfigureBarks(DemoSceneBuildContext ctx, DialogueLinePlayer barks)
        {
            bool generated = DemoSceneBuildContext.IsGenerated(barks.gameObject);
            if (!generated && barks.Lines.Count > 0)
            {
                return;
            }

            List<PooledDialogueLine> lines = barks.Lines;
            lines.Clear();
            lines.Add(Bark("idle", "The forge never sleeps."));
            lines.Add(Bark("idle", "Steel or iron, friend?"));
            lines.Add(Bark("idle", "Mind the sparks!"));
            PooledDialogueLine late = Bark("idle", "Still browsing? Take your time.");
            late.MinTime = 15f;
            lines.Add(late);
            PooledDialogueLine welcome = Bark("greet", "Welcome to my forge, stranger!");
            welcome.Slot = PooledLineSlot.FirstOnly;
            lines.Add(welcome);
            PooledDialogueLine again = Bark("greet", "Back again? Need another blade?");
            again.Slot = PooledLineSlot.AfterFirst;
            lines.Add(again);
            PooledDialogueLine friend = Bark("greet", "Good to see you, friend.");
            friend.Slot = PooledLineSlot.AfterFirst;
            lines.Add(friend);
            lines.Add(Bark("hit", "Ow! Watch the hammer!"));
            lines.Add(Bark("hit", "Hey, that's my good arm!"));
            lines.Add(Bark("hit", "Careful, the metal is still hot!"));
            ctx.MarkModified(barks);

            ctx.Set(barks, "avoidBackToBackRepeat", true);
            ctx.Set(barks, "idleTag", "idle");
            ctx.Set(barks, "idleInterval", 0f);
            ctx.Set(barks, "contextPriority", DialogueLinePool.ContextPriority);
            ctx.Set(barks, "lineDuration", 3.5f);
            ctx.Set(barks, "useUnscaledTime", false);
            ctx.Set(barks, "clearTextOnFinish", true);
        }

        private static void RetireLegacyLayout(DemoSceneBuildContext ctx)
        {
            GameObject canvas = ctx.FindHandAuthoredRoot("Canvas");
            if (canvas == null)
            {
                return;
            }

            // WHY: a canvas that hosts the DialogueController (or the bark player) is gameplay, not just the old view.
            if (canvas.GetComponent<Canvas>() != null && canvas.GetComponentInChildren<DialogueUI>(true) != null &&
                canvas.GetComponentInChildren<DialogueController>(true) == null &&
                canvas.GetComponentInChildren<DialogueLinePlayer>(true) == null)
            {
                ctx.Retire(canvas,
                    "pre-kit full-screen DialogueUI; '[Demo] Frame' re-authors the dialogue box with the same SkipOrNext click");
            }
            else
            {
                ctx.Warn("Kept hand-authored root 'Canvas': it is not the pre-kit dialogue layout (no DialogueUI, or it " +
                         "hosts the DialogueController / DialogueLinePlayer). Delete it by hand if it duplicates '[Demo] Frame'.");
            }
        }

        private static DialogueUI BuildStage(DemoSceneBuildContext ctx, DemoPanel card, DialogueController dialogue)
        {
            Sentence preview = FirstSentence(dialogue, out string speaker);
            RectTransform stage = card.Row("Dialogue Box", 232f);
            Image background = ctx.Ensure<Image>(stage.gameObject);
            DemoSceneKit.StyleImage(background, StageColor, true, true);
            Button click = ctx.Ensure<Button>(stage.gameObject);
            click.interactable = true;
            click.transition = Selectable.Transition.ColorTint;
            click.targetGraphic = background;
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.8f, 1f);
            colors.selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            click.colors = colors;
            DemoWiring.AddVoid(click.onClick, dialogue.SkipOrNext);

            Image portraitFrame = DemoSceneKit.Image(ctx, stage, "Portrait Frame", DemoPalette.WithAlpha(Accent, 0.16f));
            DemoSceneKit.Anchor(portraitFrame.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(196f, 196f));
            Image portrait = DemoSceneKit.Image(ctx, portraitFrame.transform, "Portrait", Color.white, false);
            DemoSceneKit.Stretch(portrait.rectTransform, 14f);
            portrait.sprite = preview != null ? preview.sprite : null;
            portrait.preserveAspect = true;

            Image plate = DemoSceneKit.Image(ctx, stage, "Name Plate", Accent);
            DemoSceneKit.Anchor(plate.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(232f, -16f), new Vector2(240f, 40f));
            TMP_Text name = DemoSceneKit.Text(ctx, plate.transform, "Name", speaker, 22f, DemoPalette.Ink,
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.Stretch(name.rectTransform, 4f);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.enableAutoSizing = true;
            name.fontSizeMin = 12f;
            name.fontSizeMax = 22f;

            TMP_Text body = DemoSceneKit.Text(ctx, stage, "Body", preview != null ? preview.sentence : string.Empty, 25f,
                DemoPalette.Text, TextAlignmentOptions.TopLeft);
            DemoSceneKit.SetAnchors(body.rectTransform, Vector2.zero, Vector2.one, new Vector2(232f, 40f),
                new Vector2(-24f, -70f));

            TMP_Text hint = DemoSceneKit.Text(ctx, stage, "Continue Hint", "click the box or Skip or next  »", 15f,
                DemoPalette.Muted, TextAlignmentOptions.Right);
            DemoSceneKit.SetAnchors(hint.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(232f, 10f),
                new Vector2(-24f, 34f));

            DialogueUI ui = ctx.Ensure<DialogueUI>(stage.gameObject);
            ctx.Set(ui, "characterImage", portrait);
            ctx.Set(ui, "characterNameText", name);
            ctx.Set(ui, "dialogueText", body);
            ctx.Set(ui, "setNativeSize", false);
            ctx.Set(ui, "clearImageWhenNoSprite", false);
            return ui;
        }

        private static PlaybackControls BuildPlayback(DemoPanel column, DialogueController dialogue,
            DialogueDemoController controller)
        {
            column.Section("Playback · DialogueController");
            Button[] buttons = column.ButtonRow("Skip or next", "Restart");
            DemoWiring.AddVoid(buttons[0].onClick, dialogue.SkipOrNext);
            DemoWiring.AddVoid(buttons[1].onClick, dialogue.RestartAll);

            PlaybackControls result = new()
            {
                Typewriter = column.Toggle("Typewriter effect", dialogue.useTypewriterEffect),
                AutoAdvance = column.Toggle("Auto-advance sentences", dialogue.autoNextSentence)
            };
            DemoWiring.AddDynamic<bool>(result.Typewriter.onValueChanged, controller.SetTypewriter);
            DemoWiring.AddDynamic<bool>(result.AutoAdvance.onValueChanged, controller.SetAutoAdvance);

            float speed = dialogue.Typewriter != null ? dialogue.Typewriter.CharactersPerSecond : 50f;
            DemoSliderRow slider = column.SliderRow("Typing speed (cps)", 5f, 120f,
                Mathf.Clamp(speed, 5f, 120f), true, "0");
            result.Speed = slider.Slider;
            DemoWiring.AddDynamic<float>(result.Speed.onValueChanged, controller.SetCharactersPerSecond);
            return result;
        }

        private static LiveValues BuildLive(DemoPanel column)
        {
            column.Section("Live · controller events");
            LiveValues result = new()
            {
                Speaker = column.ValueRow("Speaker · OnCharacterChange", "-", true).Value,
                Cursor = column.ValueRow("Cursor · OnSentenceEnd", "-", true).Value,
                State = column.ValueRow("State", "-", true).Value
            };
            result.TypingFill = column.Bar("Typing progress · OnTypewriterProgress", Accent);
            column.Note("Each value is set by a DialogueController event");
            return result;
        }

        private static BarkControls BuildBarkPanel(DemoSceneBuildContext ctx, DemoFrame frame, DialogueLinePlayer barks,
            DialogueDemoController controller)
        {
            DemoPanel panel = frame.Panel("Barks", DemoPanelPlacement.Right, 440f);
            panel.Section("Barks · DialogueLinePlayer");
            panel.Note("Object '" + barks.name + "' · lines picked by tag");

            RectTransform bubble = panel.Row("Bubble", 108f);
            Image bubbleImage = ctx.Ensure<Image>(bubble.gameObject);
            DemoSceneKit.StyleImage(bubbleImage, DemoPalette.WithAlpha(Accent, 0.12f), true, false);
            TMP_Text speaker = DemoSceneKit.Text(ctx, bubble, "Speaker", BarkSpeaker, 15f, Accent,
                TextAlignmentOptions.TopLeft, FontStyles.Bold);
            DemoSceneKit.SetAnchors(speaker.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(14f, -30f),
                new Vector2(-14f, -8f));
            TMP_Text line = DemoSceneKit.Text(ctx, bubble, "Line", "Press Idle line, then Hit! while it plays.", 20f,
                DemoPalette.Text, TextAlignmentOptions.TopLeft);
            DemoSceneKit.SetAnchors(line.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 8f),
                new Vector2(-14f, -32f));
            ctx.Set(barks, "lineText", line);
            ctx.Set(barks, "speakerText", speaker);

            BarkControls result = new()
            {
                Status = panel.ValueRow("Status", "silent", true).Value
            };

            Button[] play = panel.ButtonRow("Idle line", "Hit!");
            DemoWiring.AddVoid(play[0].onClick, barks.RequestIdle);
            DemoWiring.AddString(play[1].onClick, barks.RequestContext, "hit");
            Button[] more = panel.ButtonRow("Greet", "New session");
            DemoWiring.AddString(more[0].onClick, barks.RequestContext, "greet");
            DemoWiring.AddVoid(more[1].onClick, barks.ResetSession);

            result.AutoIdle = panel.Toggle("Auto idle line every 4 s", false);
            DemoWiring.AddDynamic<bool>(result.AutoIdle.onValueChanged, controller.SetAutoIdle);
            panel.Note("Hit! (priority 10) interrupts an idle line (0)");
            panel.Note("No back-to-back repeats per tag");
            panel.Note("Greet: FirstOnly line first, then AfterFirst");
            return result;
        }

        private static void WireDialogueEvents(DemoSceneBuildContext ctx, DialogueController dialogue,
            DialogueDemoController controller)
        {
            ctx.OwnListeners(dialogue, dialogue.OnCharacterChange);
            ctx.OwnListeners(dialogue, dialogue.OnSentenceEnd);
            ctx.OwnListeners(dialogue, dialogue.OnTypewriterProgress);
            ctx.OwnListeners(dialogue, dialogue.OnMonologEnd);
            ctx.OwnListeners(dialogue, dialogue.OnDialogueEnd);
            ctx.OwnListeners(dialogue, dialogue.OnAllDialoguesEnd);
            ctx.OwnListeners(dialogue, dialogue.OnCharacterTyped);
            if (dialogue.dialogues != null)
            {
                foreach (Dialogue entry in dialogue.dialogues)
                {
                    if (entry != null)
                    {
                        ctx.OwnListeners(dialogue, entry.OnChangeDialog);
                    }
                }
            }

            DemoWiring.AddDynamic<string>(dialogue.OnCharacterChange, controller.OnSpeakerChanged);
            DemoWiring.AddVoid(dialogue.OnSentenceEnd, controller.OnSentenceShown);
            DemoWiring.AddDynamic<float>(dialogue.OnTypewriterProgress, controller.OnTypingProgress);
            DemoWiring.AddVoid(dialogue.OnMonologEnd, controller.OnMonologEnded);
            DemoWiring.AddVoid(dialogue.OnAllDialoguesEnd, controller.OnAllDialoguesEnded);
            ctx.MarkModified(dialogue);
        }

        private static DemoInfoContent InfoContent()
        {
            return new DemoInfoContent
            {
                Title = "Neo.Tools.Dialogue",
                Purpose = "Linear conversations with DialogueController + DialogueUI (typewriter, portraits, events) " +
                          "and pooled barks with DialogueLinePlayer: lines by tag, no back-to-back repeats, " +
                          "first-pick slots, contextual lines interrupting idle chatter.",
                RuntimeApi = "DialogueController.StartDialogue / SkipOrNext / RestartAll, Typewriter.CharactersPerSecond · " +
                             "DialogueLinePool.TryPlay(tag, time, priority) / Finish.",
                SceneWorkflow = "'DialogueComponent' holds the conversation (Dialogue -> Monolog -> Sentence) and " +
                                "drives the Dialogue Box's DialogueUI; its events feed the Live column. Bark buttons " +
                                "call DialogueLinePlayer directly.",
                Verification = "Play: the conversation types itself; click the box to finish a line, click again " +
                               "to advance. Press Idle line, then Hit! - the contextual line cuts in.",
                Hints = new[]
                {
                    "Dialogue Box / Btn_Skip or next -> DialogueController.SkipOrNext (NoCode)",
                    "Btn_Idle line -> DialogueLinePlayer.RequestIdle · Btn_Hit! -> RequestContext(\"hit\")",
                    "Toggles and speed -> DialogueDemoController -> DialogueController fields"
                }
            };
        }

        private static Sentence FirstSentence(DialogueController dialogue, out string speaker)
        {
            speaker = "-";
            if (dialogue.dialogues == null)
            {
                return null;
            }

            foreach (Dialogue entry in dialogue.dialogues)
            {
                if (entry?.monologues == null)
                {
                    continue;
                }

                foreach (Monolog monolog in entry.monologues)
                {
                    if (monolog?.sentences == null || monolog.sentences.Length == 0 || monolog.sentences[0] == null)
                    {
                        continue;
                    }

                    speaker = string.IsNullOrEmpty(monolog.characterName) ? "-" : monolog.characterName;
                    return monolog.sentences[0];
                }
            }

            return null;
        }

        private static Monolog NewMonolog(string speaker, params Sentence[] sentences)
        {
            return new Monolog { OnChangeMonolog = new UnityEvent<int>(), characterName = speaker, sentences = sentences };
        }

        private static Sentence NewSentence(Sprite sprite, string text)
        {
            return new Sentence { OnChangeSentence = new UnityEvent(), sprite = sprite, sentence = text };
        }

        private static PooledDialogueLine Bark(string tag, string text)
        {
            return new PooledDialogueLine(tag, text) { Speaker = BarkSpeaker };
        }

        private static Sprite DemoSprite(string fileName)
        {
            string path = DemoSampleRoots.Demo + "/Sprites/" + fileName;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException("Demo sprite missing or not imported as a Sprite: " + path + ".");
            }

            return sprite;
        }

        private sealed class PlaybackControls
        {
            public Toggle Typewriter { get; set; }
            public Toggle AutoAdvance { get; set; }
            public Slider Speed { get; set; }
        }

        private sealed class LiveValues
        {
            public TMP_Text Speaker { get; set; }
            public TMP_Text Cursor { get; set; }
            public TMP_Text State { get; set; }
            public Image TypingFill { get; set; }
        }

        private sealed class BarkControls
        {
            public TMP_Text Status { get; set; }
            public Toggle AutoIdle { get; set; }
        }
    }
}
