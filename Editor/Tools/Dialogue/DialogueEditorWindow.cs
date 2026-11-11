using System;
using Neo.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neo.Tools.Editor
{
    public sealed class DialogueEditorWindow : EditorWindow
    {
        private const float LeftPanelWidth = 260f;
        private const float RightPanelMinWidth = 380f;
        private const int ItemHeight = 26;
        private const string ReadmeRelativePath = "Tools/Dialogue/README.md";

        private static readonly Color AccentPrimary = new(0.22f, 0.55f, 0.75f, 1f);
        private static readonly Color AccentSecondary = new(0.35f, 0.72f, 0.55f, 1f);
        private static readonly Color AccentTertiary = new(0.85f, 0.55f, 0.25f, 1f);

        private ObjectField _controllerField;
        private SerializedProperty _dialoguesProp;

        private VisualElement _leftContent;
        private VisualElement _rightDetailsContainer;
        private ScrollView _rightScrollView;
        private Button _selectBtn;
        private VisualElement _changeTracker;

        // WHY: the bound controller and the selection survive a domain reload, so a script compile no longer
        // drops the user back to an empty window. After an editor restart only the indices come back: a scene
        // object reference stored in the window layout does not survive it.
        [SerializeField] private int _selectedDialogue = -1;
        [SerializeField] private int _selectedMonolog = -1;
        [SerializeField] private int _selectedSentence = -1;
        [SerializeField] private DialogueController _target;

        private SerializedObject _serializedObject;
        private Palette _palette;

        /// <summary>
        ///     Window colours for the active editor skin. The window used to paint a fixed dark theme, which left
        ///     the embedded property fields (drawn with the light skin's dark text) unreadable on light cards.
        /// </summary>
        private readonly struct Palette
        {
            public readonly Color RootBg;
            public readonly Color HeaderBg;
            public readonly Color PanelBg;
            public readonly Color PanelBorder;
            public readonly Color CardBg;
            public readonly Color SelectedBg;
            public readonly Color Text;
            public readonly Color MutedText;
            public readonly Color Hairline;
            public readonly float HeaderAlpha;
            public readonly bool IsDark;

            private Palette(bool dark)
            {
                IsDark = dark;
                if (dark)
                {
                    RootBg = new Color(0.11f, 0.12f, 0.15f, 1f);
                    HeaderBg = new Color(0.18f, 0.18f, 0.22f, 0.95f);
                    PanelBg = new Color(0.22f, 0.22f, 0.26f, 0.4f);
                    PanelBorder = new Color(0.34f, 0.38f, 0.46f, 0.55f);
                    CardBg = new Color(0.16f, 0.17f, 0.21f, 0.92f);
                    SelectedBg = new Color(0.22f, 0.48f, 0.68f, 0.45f);
                    Text = Color.white;
                    MutedText = new Color(0.70f, 0.70f, 0.75f, 1f);
                    Hairline = new Color(1f, 1f, 1f, 0.06f);
                    HeaderAlpha = 0.35f;
                }
                else
                {
                    RootBg = new Color(0.78f, 0.79f, 0.82f, 1f);
                    HeaderBg = new Color(0.70f, 0.71f, 0.75f, 1f);
                    PanelBg = new Color(0.86f, 0.87f, 0.90f, 0.7f);
                    PanelBorder = new Color(0.55f, 0.58f, 0.64f, 0.6f);
                    CardBg = new Color(0.94f, 0.95f, 0.97f, 1f);
                    SelectedBg = new Color(0.40f, 0.62f, 0.82f, 0.55f);
                    Text = new Color(0.08f, 0.09f, 0.11f, 1f);
                    MutedText = new Color(0.30f, 0.32f, 0.36f, 1f);
                    Hairline = new Color(0f, 0f, 0f, 0.10f);
                    HeaderAlpha = 0.30f;
                }
            }

            public static Palette ForCurrentSkin()
            {
                return new Palette(EditorGUIUtility.isProSkin);
            }
        }

        private void Update()
        {
            // WHY: this used to snapshot the whole controller with SerializedObject.Update() on every editor
            // tick (about a hundred times a second while the window was visible). Bound fields poll on their
            // own; the window only has to notice that its controller was destroyed (scene closed, object
            // deleted) and stop holding a dead SerializedObject.
            if (_serializedObject != null && _serializedObject.targetObject == null)
            {
                BindTarget(null);
            }

            // WHY: the palette is baked into inline styles when the GUI is built, so an open window kept the
            // previous skin's colours after a light/dark switch until it was reopened.
            if (_leftContent != null && _palette.IsDark != EditorGUIUtility.isProSkin)
            {
                RebuildGui();
            }
        }

        private void RebuildGui()
        {
            rootVisualElement.Clear();
            _changeTracker = null;
            CreateGUI();
        }

        private void OnEnable()
        {
            minSize = new Vector2(LeftPanelWidth + RightPanelMinWidth + 60f, 320f);
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;
            if (_target != null)
            {
                BindTarget(_target, false);
            }
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            // WHY: ApplyModifiedProperties already marks the controller dirty when something changed. The old
            // unconditional SetDirty dirtied the scene every time the window was merely closed.
            if (_serializedObject != null && _serializedObject.targetObject != null)
            {
                _serializedObject.ApplyModifiedProperties();
            }
        }

        private void OnUndoRedo()
        {
            if (_serializedObject == null || _serializedObject.targetObject == null)
            {
                return;
            }

            _serializedObject.Update();
            EnsureSelection();
            RefreshAll();
        }

        private void CreateGUI()
        {
            _palette = Palette.ForCurrentSkin();
            rootVisualElement.style.flexGrow = 1;
            rootVisualElement.style.minHeight = 300;
            rootVisualElement.style.backgroundColor = _palette.RootBg;

            VisualElement toolbar = new();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.flexWrap = Wrap.Wrap;
            toolbar.style.minHeight = 36;
            toolbar.style.backgroundColor = _palette.HeaderBg;
            toolbar.style.borderBottomWidth = 1;
            toolbar.style.borderBottomColor = _palette.Hairline;
            toolbar.style.paddingLeft = 10;
            toolbar.style.paddingRight = 10;
            toolbar.style.paddingTop = 6;
            toolbar.style.paddingBottom = 4;
            toolbar.style.alignItems = Align.Center;

            Label controllerLabel = new("Dialogue Editor");
            controllerLabel.style.width = 104;
            controllerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            controllerLabel.style.color = _palette.Text;
            toolbar.Add(controllerLabel);

            _controllerField = new ObjectField
                { objectType = typeof(DialogueController), value = _target, allowSceneObjects = true };
            _controllerField.style.width = 220;
            _controllerField.RegisterValueChangedCallback(evt => BindTarget(evt.newValue as DialogueController));
            toolbar.Add(_controllerField);

            _selectBtn = new Button(() =>
            {
                if (_target != null)
                {
                    Selection.activeGameObject = _target.gameObject;
                    EditorGUIUtility.PingObject(_target);
                }
            }) { text = "Select In Scene" };
            _selectBtn.style.width = 108;
            _selectBtn.style.backgroundColor = AccentSecondary;
            _selectBtn.SetEnabled(_target != null);
            toolbar.Add(_selectBtn);

            Button docsBtn = new(OpenReadme) { text = "Docs", tooltip = "Open the Dialogue module guide." };
            docsBtn.style.width = 70;
            docsBtn.style.backgroundColor = AccentTertiary;
            toolbar.Add(docsBtn);

            rootVisualElement.Add(toolbar);

            VisualElement main = new();
            main.style.flexDirection = FlexDirection.Row;
            main.style.flexGrow = 1;
            main.style.minHeight = 200;

            VisualElement leftPanel = new();
            leftPanel.style.width = LeftPanelWidth;
            leftPanel.style.flexShrink = 0;
            leftPanel.style.backgroundColor = _palette.PanelBg;
            leftPanel.style.paddingLeft = 10;
            leftPanel.style.paddingRight = 10;
            leftPanel.style.paddingTop = 10;
            leftPanel.style.paddingBottom = 10;
            leftPanel.style.borderRightWidth = 1;
            leftPanel.style.borderRightColor = _palette.PanelBorder;

            ScrollView leftScroll = new(ScrollViewMode.Vertical);
            leftScroll.style.flexGrow = 1;
            _leftContent = new VisualElement();
            leftScroll.Add(_leftContent);
            leftPanel.Add(leftScroll);
            main.Add(leftPanel);

            VisualElement divider = new();
            divider.style.width = 2;
            divider.style.backgroundColor =
                new StyleColor(new Color(AccentPrimary.r, AccentPrimary.g, AccentPrimary.b, 0.5f));
            main.Add(divider);

            VisualElement rightPanel = new();
            rightPanel.style.flexGrow = 1;
            rightPanel.style.backgroundColor = _palette.PanelBg;
            rightPanel.style.paddingLeft = 10;
            rightPanel.style.paddingRight = 10;
            rightPanel.style.paddingTop = 10;
            rightPanel.style.paddingBottom = 10;
            rightPanel.style.minWidth = RightPanelMinWidth;
            rightPanel.style.borderLeftWidth = 1;
            rightPanel.style.borderLeftColor = _palette.PanelBorder;

            _rightScrollView = new ScrollView(ScrollViewMode.Vertical);
            _rightScrollView.style.flexGrow = 1;

            Label rightHeader = new("Details");
            rightHeader.style.height = 24;
            rightHeader.style.backgroundColor = AccentPrimary;
            rightHeader.style.color = Color.white;
            rightHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            rightHeader.style.paddingLeft = 8;
            rightHeader.style.paddingTop = 2;
            _rightScrollView.Add(rightHeader);

            Label breadcrumbs = new();
            breadcrumbs.name = "breadcrumbs";
            breadcrumbs.style.height = 22;
            breadcrumbs.style.marginTop = 6;
            breadcrumbs.style.backgroundColor =
                new StyleColor(new Color(AccentPrimary.r, AccentPrimary.g, AccentPrimary.b, 0.2f));
            breadcrumbs.style.color = AccentPrimary;
            breadcrumbs.style.unityTextAlign = TextAnchor.MiddleCenter;
            breadcrumbs.style.unityFontStyleAndWeight = FontStyle.Bold;
            breadcrumbs.style.borderLeftWidth = 3;
            breadcrumbs.style.borderLeftColor = AccentPrimary;
            _rightScrollView.Add(breadcrumbs);

            _rightDetailsContainer = new VisualElement();
            _rightDetailsContainer.style.marginTop = 12;
            _rightScrollView.Add(_rightDetailsContainer);

            rightPanel.Add(_rightScrollView);
            main.Add(rightPanel);

            rootVisualElement.Add(main);

            RebindChangeTracker();
            RefreshAll();
        }

        private void OpenReadme()
        {
            string path = NeoDocHelper.ResolveDocPath(CreateMenuObject.startPath, ReadmeRelativePath);
            if (!string.IsNullOrEmpty(path) && AssetDatabase.LoadAssetAtPath<TextAsset>(path) != null)
            {
                NeoDocHelper.OpenDocInWindow(path);
                return;
            }

            EditorUtility.DisplayDialog("Dialogue Editor",
                "Documentation: Docs/" + ReadmeRelativePath + " in the Neoxider package.", "OK");
        }

        /// <summary>
        ///     Rebuilds the structure list when the controller changes outside the window (Inspector edits, a
        ///     renamed character or an edited sentence), so the left column never shows stale names.
        /// </summary>
        private void RebindChangeTracker()
        {
            if (rootVisualElement == null)
            {
                return;
            }

            _changeTracker?.RemoveFromHierarchy();
            _changeTracker = null;
            if (_serializedObject == null)
            {
                return;
            }

            _changeTracker = new VisualElement { name = "dialogue-change-tracker" };
            _changeTracker.style.display = DisplayStyle.None;
            rootVisualElement.Add(_changeTracker);
            _changeTracker.TrackSerializedObjectValue(_serializedObject, _ =>
            {
                RefreshBreadcrumbs();
                RefreshLeftPanel();
            });
        }

        [MenuItem("Neoxider/Windows/Dialogue Editor", false, 1)]
        private static void OpenEmpty()
        {
            GetWindow<DialogueEditorWindow>("Dialogue Editor");
        }

        public static void ShowFor(DialogueController controller)
        {
            if (controller == null)
            {
                return;
            }

            DialogueEditorWindow window = GetWindow<DialogueEditorWindow>("Dialogue Editor");
            window.BindTarget(controller);
            window.Focus();
        }

        private void BindTarget(DialogueController controller)
        {
            BindTarget(controller, true);
        }

        private void BindTarget(DialogueController controller, bool resetSelection)
        {
            bool changedTarget = controller != _target;
            _target = controller;
            _serializedObject?.Dispose();
            _serializedObject = _target != null ? new SerializedObject(_target) : null;
            _dialoguesProp = _serializedObject?.FindProperty("dialogues");
            if (changedTarget && resetSelection)
            {
                _selectedDialogue = _selectedMonolog = _selectedSentence = -1;
            }

            EnsureSelection();
            if (_controllerField != null)
            {
                _controllerField.SetValueWithoutNotify(_target);
            }

            if (_selectBtn != null)
            {
                _selectBtn.SetEnabled(_target != null);
            }

            RebindChangeTracker();
            if (_leftContent != null)
            {
                RefreshAll();
            }
        }

        private void RefreshAll()
        {
            _serializedObject?.Update();
            RefreshBreadcrumbs();
            RefreshLeftPanel();
            RefreshRightPanel();
        }

        private void RefreshBreadcrumbs()
        {
            Label bc = rootVisualElement.Q<Label>("breadcrumbs");
            if (bc == null)
            {
                return;
            }

            string d = _selectedDialogue >= 0 ? $"D{_selectedDialogue + 1}" : "−";
            string m = _selectedMonolog >= 0 ? $"M{_selectedMonolog + 1}" : "−";
            string s = _selectedSentence >= 0 ? $"S{_selectedSentence + 1}" : "−";
            bc.text = $" Selection  {d}  →  {m}  →  {s}";
        }

        private void RefreshLeftPanel()
        {
            if (_leftContent == null)
            {
                return;
            }

            _leftContent.Clear();
            if (_dialoguesProp == null)
            {
                AddEmptyHint(_leftContent, "Assign a Dialogue Controller in the toolbar above.");
                return;
            }

            Label structureHeader = new("Structure");
            structureHeader.style.height = 24;
            structureHeader.style.backgroundColor = AccentPrimary;
            structureHeader.style.color = Color.white;
            structureHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            structureHeader.style.paddingLeft = 8;
            structureHeader.style.paddingTop = 2;
            structureHeader.style.borderLeftWidth = 4;
            structureHeader.style.borderLeftColor = Color.white;
            _leftContent.Add(structureHeader);
            _leftContent.Add(new VisualElement { style = { height = 6 } });

            AddSectionHeader(_leftContent, $"Dialogues ({_dialoguesProp.arraySize})", AccentPrimary, _palette,
                AddDialogue,
                _selectedDialogue >= 0 ? RemoveDialogue : null,
                _selectedDialogue > 0 ? MoveDialogueUp : null,
                _selectedDialogue >= 0 && _selectedDialogue < _dialoguesProp.arraySize - 1 ? MoveDialogueDown : null);
            _leftContent.Add(new VisualElement { style = { height = 4 } });

            for (int i = 0; i < _dialoguesProp.arraySize; i++)
            {
                int idx = i;
                AddItemButton(_leftContent, _selectedDialogue == idx, $"  Dialogue {idx + 1}", _palette, () =>
                {
                    _selectedDialogue = idx;
                    _selectedMonolog = -1;
                    _selectedSentence = -1;
                    RefreshAll();
                });
            }

            if (_dialoguesProp.arraySize == 0)
            {
                AddEmptyHint(_leftContent, "No dialogues. Click + to add.");
            }

            _leftContent.Add(new VisualElement { style = { height = 12 } });

            if (_selectedDialogue >= 0)
            {
                SerializedProperty monologues = GetMonologues();
                if (monologues != null)
                {
                    AddSectionHeader(_leftContent, $"Monologues ({monologues.arraySize})", AccentSecondary, _palette,
                        AddMonolog,
                        _selectedMonolog >= 0 ? RemoveMonolog : null,
                        _selectedMonolog > 0 ? MoveMonologUp : null,
                        _selectedMonolog >= 0 && _selectedMonolog < monologues.arraySize - 1 ? MoveMonologDown : null);
                    _leftContent.Add(new VisualElement { style = { height = 4 } });
                    for (int i = 0; i < monologues.arraySize; i++)
                    {
                        int idx = i;
                        SerializedProperty item = monologues.GetArrayElementAtIndex(idx);
                        SerializedProperty nameProp = item.FindPropertyRelative("characterName");
                        string name = string.IsNullOrWhiteSpace(nameProp.stringValue)
                            ? "Unnamed"
                            : nameProp.stringValue;
                        AddItemButton(_leftContent, _selectedMonolog == idx, $"  {idx + 1}. {name}", _palette, () =>
                        {
                            _selectedMonolog = idx;
                            _selectedSentence = -1;
                            RefreshAll();
                        });
                    }

                    if (monologues.arraySize == 0)
                    {
                        AddEmptyHint(_leftContent, "No monologues. Click + to add.");
                    }

                    _leftContent.Add(new VisualElement { style = { height = 12 } });
                }
            }

            if (_selectedDialogue >= 0 && _selectedMonolog >= 0)
            {
                SerializedProperty sentences = GetSentences();
                if (sentences != null)
                {
                    AddSectionHeader(_leftContent, $"Sentences ({sentences.arraySize})", AccentTertiary, _palette,
                        AddSentence,
                        _selectedSentence >= 0 ? RemoveSentence : null,
                        _selectedSentence > 0 ? MoveSentenceUp : null,
                        _selectedSentence >= 0 && _selectedSentence < sentences.arraySize - 1
                            ? MoveSentenceDown
                            : null);
                    _leftContent.Add(new VisualElement { style = { height = 4 } });
                    for (int i = 0; i < sentences.arraySize; i++)
                    {
                        int idx = i;
                        SerializedProperty item = sentences.GetArrayElementAtIndex(idx);
                        string preview = BuildSentencePreview(item.FindPropertyRelative("sentence").stringValue);
                        AddItemButton(_leftContent, _selectedSentence == idx, $"  {idx + 1}. {preview}", _palette, () =>
                        {
                            _selectedSentence = idx;
                            RefreshAll();
                        });
                    }

                    if (sentences.arraySize == 0)
                    {
                        AddEmptyHint(_leftContent, "No sentences. Click + to add.");
                    }
                }
            }
        }

        private void RefreshRightPanel()
        {
            if (_rightDetailsContainer == null)
            {
                return;
            }

            _rightDetailsContainer.Clear();
            _rightDetailsContainer.Unbind();
            if (_serializedObject == null)
            {
                AddEmptyHint(_rightDetailsContainer, "Pick a Dialogue Controller to edit its dialogues.");
                return;
            }

            if (_selectedDialogue < 0)
            {
                AddEmptyHint(_rightDetailsContainer, "Select a dialogue from the left panel, or press + to add one.");
                return;
            }

            SerializedProperty dialogue = GetDialogue();
            if (dialogue == null)
            {
                return;
            }

            AddDetailBlock(_rightDetailsContainer, $"Dialogue {_selectedDialogue + 1}", AccentPrimary, _palette, box =>
            {
                SerializedProperty prop = dialogue.FindPropertyRelative("OnChangeDialog");
                PropertyField pf = new(prop, "On Change Dialogue");
                pf.BindProperty(prop);
                box.Add(pf);
            });

            if (_selectedMonolog >= 0)
            {
                SerializedProperty monolog = GetMonolog();
                if (monolog != null)
                {
                    AddDetailBlock(_rightDetailsContainer, $"Monologue {_selectedMonolog + 1}", AccentSecondary,
                        _palette, box =>
                    {
                        SerializedProperty p1 = monolog.FindPropertyRelative("characterName");
                        SerializedProperty p2 = monolog.FindPropertyRelative("OnChangeMonolog");
                        PropertyField pf1 = new(p1, "Character");
                        PropertyField pf2 = new(p2, "On Change Monolog");
                        pf1.BindProperty(p1);
                        pf2.BindProperty(p2);
                        box.Add(pf1);
                        box.Add(pf2);
                    });
                }
            }

            if (_selectedSentence >= 0)
            {
                SerializedProperty sentence = GetSentence();
                if (sentence != null)
                {
                    AddDetailBlock(_rightDetailsContainer, $"Sentence {_selectedSentence + 1}", AccentTertiary,
                        _palette, box =>
                    {
                        SerializedProperty spriteProp = sentence.FindPropertyRelative("sprite");
                        SerializedProperty textProp = sentence.FindPropertyRelative("sentence");
                        SerializedProperty onChangeProp = sentence.FindPropertyRelative("OnChangeSentence");
                        PropertyField pfSprite = new(spriteProp);
                        PropertyField pfText = new(textProp, "Text");
                        PropertyField pfOnChange = new(onChangeProp, "On Change Sentence");
                        pfSprite.BindProperty(spriteProp);
                        pfText.BindProperty(textProp);
                        pfOnChange.BindProperty(onChangeProp);
                        box.Add(pfSprite);
                        box.Add(pfText);
                        box.Add(pfOnChange);
                    });

                    VisualElement previewBox = new();
                    previewBox.style.minHeight = 80 + 2 * 18;
                    previewBox.style.marginTop = 8;
                    previewBox.style.backgroundColor = _palette.CardBg;
                    previewBox.style.flexDirection = FlexDirection.Row;
                    previewBox.style.paddingLeft = 10;
                    previewBox.style.paddingTop = 8;
                    previewBox.style.paddingRight = 10;
                    previewBox.style.paddingBottom = 8;
                    Label previewLabel = new("Preview");
                    previewLabel.style.color = _palette.Text;
                    previewLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    previewLabel.style.marginBottom = 4;
                    previewLabel.style.fontSize = 12;
                    previewLabel.style.backgroundColor =
                        new StyleColor(new Color(AccentTertiary.r, AccentTertiary.g, AccentTertiary.b, 0.25f));
                    previewLabel.style.paddingLeft = 8;
                    previewLabel.style.borderLeftWidth = 4;
                    previewLabel.style.borderLeftColor = AccentTertiary;
                    _rightDetailsContainer.Add(previewLabel);
                    VisualElement previewRow = new();
                    previewRow.style.flexDirection = FlexDirection.Row;
                    if (sentence.FindPropertyRelative("sprite").objectReferenceValue is Sprite sp)
                    {
                        Texture2D previewTex = AssetPreview.GetAssetPreview(sp);
                        Image img = new()
                        {
                            image = previewTex != null ? previewTex : AssetPreview.GetMiniThumbnail(sp),
                            scaleMode = ScaleMode.ScaleToFit
                        };
                        img.style.width = 64;
                        img.style.height = 64;
                        img.style.marginRight = 8;
                        previewRow.Add(img);
                        if (previewTex == null)
                        {
                            // WHY: AssetPreview renders asynchronously; without a follow-up the preview stayed a
                            // mini icon until something else rebuilt the panel. Poll briefly, then give up.
                            double giveUpAt = EditorApplication.timeSinceStartup + 3d;
                            img.schedule.Execute(() =>
                                {
                                    Texture2D loaded = sp != null ? AssetPreview.GetAssetPreview(sp) : null;
                                    if (loaded != null)
                                    {
                                        img.image = loaded;
                                    }
                                })
                                .Every(100)
                                .Until(() => sp == null || img.panel == null ||
                                             AssetPreview.GetAssetPreview(sp) != null ||
                                             EditorApplication.timeSinceStartup > giveUpAt);
                        }
                    }

                    string textPreview = sentence.FindPropertyRelative("sentence").stringValue;
                    if (string.IsNullOrWhiteSpace(textPreview))
                    {
                        textPreview = "(empty)";
                    }

                    Label textLabel = new(textPreview);
                    textLabel.style.color = _palette.Text;
                    textLabel.style.whiteSpace = WhiteSpace.Normal;
                    textLabel.style.flexGrow = 1;
                    textLabel.style.fontSize = 13;
                    textLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    textLabel.style.marginTop = 2;
                    previewRow.Add(textLabel);
                    previewBox.Add(previewRow);
                    _rightDetailsContainer.Add(previewBox);
                }
            }
        }

        private static void AddSectionHeader(VisualElement parent, string title, Color accent, Palette palette,
            Action onAdd, Action onRemove, Action onUp, Action onDown)
        {
            VisualElement row = new();
            row.style.flexDirection = FlexDirection.Row;
            row.style.height = 30;
            row.style.marginBottom = 2;
            Label titleEl = new(title);
            titleEl.style.flexGrow = 1;
            titleEl.style.flexShrink = 1;
            titleEl.style.overflow = Overflow.Hidden;
            titleEl.style.textOverflow = TextOverflow.Ellipsis;
            titleEl.style.backgroundColor =
                new StyleColor(new Color(accent.r, accent.g, accent.b, palette.HeaderAlpha));
            titleEl.style.color = palette.Text;
            titleEl.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleEl.style.fontSize = 12;
            titleEl.style.paddingLeft = 8;
            titleEl.style.paddingTop = 4;
            titleEl.style.borderLeftWidth = 4;
            titleEl.style.borderLeftColor = accent;
            row.Add(titleEl);
            Button addBtn = new(onAdd ?? (() => { })) { text = "+", tooltip = "Add" };
            addBtn.style.width = 26;
            addBtn.style.height = 24;
            addBtn.style.backgroundColor = AccentSecondary;
            row.Add(addBtn);
            Button remBtn = new(onRemove ?? (() => { })) { text = "−", tooltip = "Remove the selected item" };
            remBtn.style.width = 26;
            remBtn.style.height = 24;
            remBtn.style.backgroundColor = AccentTertiary;
            remBtn.SetEnabled(onRemove != null);
            row.Add(remBtn);
            Button upBtn = new(onUp ?? (() => { })) { text = "↑", tooltip = "Move up" };
            upBtn.style.width = 26;
            upBtn.style.height = 24;
            upBtn.style.backgroundColor = accent;
            upBtn.SetEnabled(onUp != null);
            row.Add(upBtn);
            Button downBtn = new(onDown ?? (() => { })) { text = "↓", tooltip = "Move down" };
            downBtn.style.width = 26;
            downBtn.style.height = 24;
            downBtn.style.backgroundColor = accent;
            downBtn.SetEnabled(onDown != null);
            row.Add(downBtn);
            parent.Add(row);
        }

        private static void AddItemButton(VisualElement parent, bool selected, string label, Palette palette,
            Action onClick)
        {
            Button btn = new(onClick) { text = label };
            btn.style.height = ItemHeight + 4;
            btn.style.unityTextAlign = TextAnchor.MiddleLeft;
            btn.style.overflow = Overflow.Hidden;
            btn.style.textOverflow = TextOverflow.Ellipsis;
            btn.style.paddingLeft = 12;
            btn.style.paddingRight = 10;
            btn.style.marginBottom = 3;
            btn.style.borderLeftWidth = 4;
            btn.style.borderTopWidth = 1;
            btn.style.borderBottomWidth = 1;
            btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = palette.Hairline;
            btn.style.borderBottomColor = palette.Hairline;
            btn.style.borderRightColor = palette.Hairline;
            if (selected)
            {
                btn.style.backgroundColor = palette.SelectedBg;
                btn.style.borderLeftColor = AccentPrimary;
                btn.style.color = palette.Text;
                btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            else
            {
                btn.style.backgroundColor = new Color(palette.CardBg.r, palette.CardBg.g, palette.CardBg.b, 0.72f);
                btn.style.borderLeftColor = palette.Hairline;
                btn.style.color = palette.Text;
                btn.style.unityFontStyleAndWeight = FontStyle.Normal;
            }

            parent.Add(btn);
        }

        private void AddEmptyHint(VisualElement parent, string text)
        {
            Label hint = new(text);
            hint.style.minHeight = 36;
            hint.style.whiteSpace = WhiteSpace.Normal;
            hint.style.paddingLeft = 8;
            hint.style.paddingRight = 8;
            hint.style.backgroundColor = new Color(_palette.CardBg.r, _palette.CardBg.g, _palette.CardBg.b, 0.80f);
            hint.style.unityTextAlign = TextAnchor.MiddleCenter;
            hint.style.color = _palette.MutedText;
            hint.style.fontSize = 12;
            hint.style.borderLeftWidth = 4;
            hint.style.borderLeftColor = new Color(0.50f, 0.52f, 0.58f, 1f);
            parent.Add(hint);
        }

        private static void AddDetailBlock(VisualElement parent, string title, Color accent, Palette palette,
            Action<VisualElement> addFields)
        {
            Label header = new(title);
            header.style.height = 22;
            header.style.backgroundColor = new StyleColor(new Color(accent.r, accent.g, accent.b, palette.HeaderAlpha));
            header.style.color = palette.Text;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 12;
            header.style.paddingLeft = 8;
            header.style.paddingTop = 2;
            header.style.borderLeftWidth = 4;
            header.style.borderLeftColor = accent;
            parent.Add(header);
            VisualElement box = new();
            box.style.backgroundColor = new Color(palette.CardBg.r, palette.CardBg.g, palette.CardBg.b, 0.88f);
            box.style.paddingLeft = 6;
            box.style.paddingRight = 6;
            box.style.paddingTop = 8;
            box.style.paddingBottom = 8;
            box.style.marginBottom = 8;
            box.style.borderBottomWidth = 1;
            box.style.borderBottomColor = palette.Hairline;
            addFields?.Invoke(box);
            parent.Add(box);
        }

        private void AddDialogue()
        {
            if (_dialoguesProp == null)
            {
                return;
            }

            _serializedObject.Update();
            int index = _dialoguesProp.arraySize;
            _dialoguesProp.InsertArrayElementAtIndex(index);
            SerializedProperty dialogue = _dialoguesProp.GetArrayElementAtIndex(index);
            ResetDialogue(dialogue);
            _serializedObject.ApplyModifiedProperties();
            _selectedDialogue = index;
            _selectedMonolog = -1;
            _selectedSentence = -1;
            RefreshAll();
        }

        private void RemoveDialogue()
        {
            if (_selectedDialogue < 0 || _dialoguesProp == null || _selectedDialogue >= _dialoguesProp.arraySize)
            {
                return;
            }

            _serializedObject.Update();
            _dialoguesProp.DeleteArrayElementAtIndex(_selectedDialogue);
            _serializedObject.ApplyModifiedProperties();
            EnsureSelection();
            _selectedMonolog = -1;
            _selectedSentence = -1;
            RefreshAll();
        }

        private void MoveDialogueUp()
        {
            if (_selectedDialogue <= 0 || _dialoguesProp == null)
            {
                return;
            }

            _serializedObject.Update();
            _dialoguesProp.MoveArrayElement(_selectedDialogue, _selectedDialogue - 1);
            _serializedObject.ApplyModifiedProperties();
            _selectedDialogue--;
            RefreshAll();
        }

        private void MoveDialogueDown()
        {
            if (_selectedDialogue < 0 || _selectedDialogue >= _dialoguesProp.arraySize - 1)
            {
                return;
            }

            _serializedObject.Update();
            _dialoguesProp.MoveArrayElement(_selectedDialogue, _selectedDialogue + 1);
            _serializedObject.ApplyModifiedProperties();
            _selectedDialogue++;
            RefreshAll();
        }

        private void AddMonolog()
        {
            SerializedProperty monologues = GetMonologues();
            if (monologues == null)
            {
                return;
            }

            _serializedObject.Update();
            int index = monologues.arraySize;
            monologues.InsertArrayElementAtIndex(index);
            ResetMonolog(monologues.GetArrayElementAtIndex(index));
            _serializedObject.ApplyModifiedProperties();
            _selectedMonolog = index;
            _selectedSentence = -1;
            RefreshAll();
        }

        private void RemoveMonolog()
        {
            SerializedProperty monologues = GetMonologues();
            if (monologues == null || _selectedMonolog < 0 || _selectedMonolog >= monologues.arraySize)
            {
                return;
            }

            _serializedObject.Update();
            monologues.DeleteArrayElementAtIndex(_selectedMonolog);
            _serializedObject.ApplyModifiedProperties();
            _selectedMonolog = Mathf.Clamp(_selectedMonolog, 0, monologues.arraySize - 1);
            if (monologues.arraySize == 0)
            {
                _selectedMonolog = -1;
            }

            _selectedSentence = -1;
            RefreshAll();
        }

        private void MoveMonologUp()
        {
            SerializedProperty monologues = GetMonologues();
            if (monologues == null || _selectedMonolog <= 0)
            {
                return;
            }

            _serializedObject.Update();
            monologues.MoveArrayElement(_selectedMonolog, _selectedMonolog - 1);
            _serializedObject.ApplyModifiedProperties();
            _selectedMonolog--;
            RefreshAll();
        }

        private void MoveMonologDown()
        {
            SerializedProperty monologues = GetMonologues();
            if (monologues == null || _selectedMonolog < 0 || _selectedMonolog >= monologues.arraySize - 1)
            {
                return;
            }

            _serializedObject.Update();
            monologues.MoveArrayElement(_selectedMonolog, _selectedMonolog + 1);
            _serializedObject.ApplyModifiedProperties();
            _selectedMonolog++;
            RefreshAll();
        }

        private void AddSentence()
        {
            SerializedProperty sentences = GetSentences();
            if (sentences == null)
            {
                return;
            }

            _serializedObject.Update();
            int index = sentences.arraySize;
            sentences.InsertArrayElementAtIndex(index);
            ResetSentence(sentences.GetArrayElementAtIndex(index));
            _serializedObject.ApplyModifiedProperties();
            _selectedSentence = index;
            RefreshAll();
        }

        private void RemoveSentence()
        {
            SerializedProperty sentences = GetSentences();
            if (sentences == null || _selectedSentence < 0 || _selectedSentence >= sentences.arraySize)
            {
                return;
            }

            _serializedObject.Update();
            sentences.DeleteArrayElementAtIndex(_selectedSentence);
            _serializedObject.ApplyModifiedProperties();
            _selectedSentence = Mathf.Clamp(_selectedSentence, 0, sentences.arraySize - 1);
            if (sentences.arraySize == 0)
            {
                _selectedSentence = -1;
            }

            RefreshAll();
        }

        private void MoveSentenceUp()
        {
            SerializedProperty sentences = GetSentences();
            if (sentences == null || _selectedSentence <= 0)
            {
                return;
            }

            _serializedObject.Update();
            sentences.MoveArrayElement(_selectedSentence, _selectedSentence - 1);
            _serializedObject.ApplyModifiedProperties();
            _selectedSentence--;
            RefreshAll();
        }

        private void MoveSentenceDown()
        {
            SerializedProperty sentences = GetSentences();
            if (sentences == null || _selectedSentence < 0 || _selectedSentence >= sentences.arraySize - 1)
            {
                return;
            }

            _serializedObject.Update();
            sentences.MoveArrayElement(_selectedSentence, _selectedSentence + 1);
            _serializedObject.ApplyModifiedProperties();
            _selectedSentence++;
            RefreshAll();
        }

        private SerializedProperty GetDialogue()
        {
            if (_dialoguesProp == null || _selectedDialogue < 0 || _selectedDialogue >= _dialoguesProp.arraySize)
            {
                return null;
            }

            return _dialoguesProp.GetArrayElementAtIndex(_selectedDialogue);
        }

        private SerializedProperty GetMonologues()
        {
            return GetDialogue()?.FindPropertyRelative("monologues");
        }

        private SerializedProperty GetMonolog()
        {
            SerializedProperty monologues = GetMonologues();
            if (monologues == null || _selectedMonolog < 0 || _selectedMonolog >= monologues.arraySize)
            {
                return null;
            }

            return monologues.GetArrayElementAtIndex(_selectedMonolog);
        }

        private SerializedProperty GetSentences()
        {
            return GetMonolog()?.FindPropertyRelative("sentences");
        }

        private SerializedProperty GetSentence()
        {
            SerializedProperty sentences = GetSentences();
            if (sentences == null || _selectedSentence < 0 || _selectedSentence >= sentences.arraySize)
            {
                return null;
            }

            return sentences.GetArrayElementAtIndex(_selectedSentence);
        }

        private void EnsureSelection()
        {
            if (_dialoguesProp == null || _dialoguesProp.arraySize == 0)
            {
                _selectedDialogue = _selectedMonolog = _selectedSentence = -1;
                return;
            }

            _selectedDialogue = Mathf.Clamp(_selectedDialogue < 0 ? 0 : _selectedDialogue, 0,
                _dialoguesProp.arraySize - 1);
            SerializedProperty monologues = GetMonologues();
            if (monologues == null || monologues.arraySize == 0)
            {
                _selectedMonolog = _selectedSentence = -1;
                return;
            }

            _selectedMonolog = Mathf.Clamp(_selectedMonolog < 0 ? 0 : _selectedMonolog, 0, monologues.arraySize - 1);
            SerializedProperty sentences = GetSentences();
            if (sentences == null || sentences.arraySize == 0)
            {
                _selectedSentence = -1;
                return;
            }

            _selectedSentence = Mathf.Clamp(_selectedSentence < 0 ? 0 : _selectedSentence, 0, sentences.arraySize - 1);
        }

        private static void ResetDialogue(SerializedProperty dialogue)
        {
            dialogue?.FindPropertyRelative("monologues")?.ClearArray();
        }

        private static void ResetMonolog(SerializedProperty monolog)
        {
            if (monolog == null)
            {
                return;
            }

            SerializedProperty name = monolog.FindPropertyRelative("characterName");
            if (name != null)
            {
                name.stringValue = string.Empty;
            }

            monolog.FindPropertyRelative("sentences")?.ClearArray();
        }

        private static void ResetSentence(SerializedProperty sentence)
        {
            if (sentence == null)
            {
                return;
            }

            sentence.FindPropertyRelative("sprite").objectReferenceValue = null;
            sentence.FindPropertyRelative("sentence").stringValue = string.Empty;
        }

        private static string BuildSentencePreview(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "(empty)";
            }

            string oneLine = text.Replace("\n", " ").Trim();
            return oneLine.Length > 42 ? oneLine.Substring(0, 42) + "..." : oneLine;
        }
    }
}
