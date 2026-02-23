using Neo.Tools;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neo.Tools.Editor
{
    public sealed class DialogueEditorWindow : EditorWindow
    {
        private const float LeftPanelWidth = 260f;
        private const float RightPanelMinWidth = 580f;
        private const int ItemHeight = 26;

        private static readonly Color AccentPrimary = new(0.22f, 0.55f, 0.75f, 1f);
        private static readonly Color AccentSecondary = new(0.35f, 0.72f, 0.55f, 1f);
        private static readonly Color AccentTertiary = new(0.85f, 0.55f, 0.25f, 1f);
        private static readonly Color SelectedBg = new(0.22f, 0.48f, 0.68f, 0.45f);
        private static readonly Color PanelBg = new(0.22f, 0.22f, 0.26f, 0.4f);
        private static readonly Color HeaderBg = new(0.18f, 0.18f, 0.22f, 0.95f);

        private DialogueController _target;
        private SerializedObject _serializedObject;
        private SerializedProperty _dialoguesProp;

        private VisualElement _leftContent;
        private VisualElement _rightDetailsContainer;
        private ObjectField _controllerField;
        private Button _selectBtn;
        private ScrollView _rightScrollView;

        private int _selectedDialogue = -1;
        private int _selectedMonolog = -1;
        private int _selectedSentence = -1;

        [MenuItem("Neoxider/Tools/Dialogue/Open Dialogue Editor")]
        private static void OpenEmpty()
        {
            GetWindow<DialogueEditorWindow>("Dialogue Editor");
        }

        public static void ShowFor(DialogueController controller)
        {
            if (controller == null) return;
            var window = GetWindow<DialogueEditorWindow>("Dialogue Editor");
            window.BindTarget(controller);
            window.Focus();
        }

        private void OnEnable()
        {
            minSize = new Vector2(LeftPanelWidth + RightPanelMinWidth + 60f, 320f);
            if (_target != null) BindTarget(_target);
        }

        private void OnDisable()
        {
            if (_serializedObject != null && _target != null)
            {
                _serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_target);
            }
        }

        private void BindTarget(DialogueController controller)
        {
            _target = controller;
            _serializedObject = _target != null ? new SerializedObject(_target) : null;
            _dialoguesProp = _serializedObject?.FindProperty("dialogues");
            EnsureSelection();
            if (_controllerField != null) _controllerField.SetValueWithoutNotify(_target);
            if (_selectBtn != null) _selectBtn.SetEnabled(_target != null);
            if (_leftContent != null) RefreshAll();
        }

        private void CreateGUI()
        {
            rootVisualElement.style.flexGrow = 1;
            rootVisualElement.style.minHeight = 300;

            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.height = 28;
            toolbar.style.backgroundColor = HeaderBg;
            toolbar.style.paddingLeft = 8;
            toolbar.style.paddingRight = 8;
            toolbar.style.paddingTop = 4;
            toolbar.style.alignItems = Align.Center;

            var controllerLabel = new Label("Controller");
            controllerLabel.style.width = 58;
            controllerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            toolbar.Add(controllerLabel);

            _controllerField = new ObjectField { objectType = typeof(DialogueController), value = _target, allowSceneObjects = true };
            _controllerField.style.width = 180;
            _controllerField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is DialogueController c) BindTarget(c);
            });
            toolbar.Add(_controllerField);

            _selectBtn = new Button(() =>
            {
                if (_target != null) { Selection.activeGameObject = _target.gameObject; EditorGUIUtility.PingObject(_target); }
            }) { text = "Select In Scene" };
            _selectBtn.style.width = 92;
            _selectBtn.style.backgroundColor = AccentSecondary;
            _selectBtn.SetEnabled(_target != null);
            toolbar.Add(_selectBtn);

            var docsBtn = new Button(() =>
                EditorUtility.DisplayDialog("Dialogue Editor", "Documentation: Assets/Neoxider/Docs/Tools/Dialogue/README.md", "OK"))
            { text = "Docs" };
            docsBtn.style.width = 70;
            docsBtn.style.backgroundColor = AccentTertiary;
            toolbar.Add(docsBtn);

            rootVisualElement.Add(toolbar);

            var main = new VisualElement();
            main.style.flexDirection = FlexDirection.Row;
            main.style.flexGrow = 1;
            main.style.minHeight = 200;

            var leftPanel = new VisualElement();
            leftPanel.style.width = LeftPanelWidth;
            leftPanel.style.backgroundColor = PanelBg;
            leftPanel.style.paddingLeft = 10;
            leftPanel.style.paddingRight = 10;
            leftPanel.style.paddingTop = 10;
            leftPanel.style.paddingBottom = 10;

            var leftScroll = new ScrollView(ScrollViewMode.Vertical);
            leftScroll.style.flexGrow = 1;
            _leftContent = new VisualElement();
            leftScroll.Add(_leftContent);
            leftPanel.Add(leftScroll);
            main.Add(leftPanel);

            var divider = new VisualElement();
            divider.style.width = 2;
            divider.style.backgroundColor = new StyleColor(new Color(AccentPrimary.r, AccentPrimary.g, AccentPrimary.b, 0.5f));
            main.Add(divider);

            var rightPanel = new VisualElement();
            rightPanel.style.flexGrow = 1;
            rightPanel.style.backgroundColor = PanelBg;
            rightPanel.style.paddingLeft = 10;
            rightPanel.style.paddingRight = 10;
            rightPanel.style.paddingTop = 10;
            rightPanel.style.paddingBottom = 10;
            rightPanel.style.minWidth = RightPanelMinWidth;

            _rightScrollView = new ScrollView(ScrollViewMode.Vertical);
            _rightScrollView.style.flexGrow = 1;

            var rightHeader = new Label("Details");
            rightHeader.style.height = 24;
            rightHeader.style.backgroundColor = AccentPrimary;
            rightHeader.style.color = Color.white;
            rightHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            rightHeader.style.paddingLeft = 8;
            rightHeader.style.paddingTop = 2;
            _rightScrollView.Add(rightHeader);

            var breadcrumbs = new Label();
            breadcrumbs.name = "breadcrumbs";
            breadcrumbs.style.height = 22;
            breadcrumbs.style.marginTop = 6;
            breadcrumbs.style.backgroundColor = new StyleColor(new Color(AccentPrimary.r, AccentPrimary.g, AccentPrimary.b, 0.2f));
            breadcrumbs.style.color = AccentPrimary;
            breadcrumbs.style.unityTextAlign = TextAnchor.MiddleCenter;
            breadcrumbs.style.unityFontStyleAndWeight = FontStyle.Bold;
            _rightScrollView.Add(breadcrumbs);

            _rightDetailsContainer = new VisualElement();
            _rightDetailsContainer.style.marginTop = 12;
            _rightScrollView.Add(_rightDetailsContainer);

            rightPanel.Add(_rightScrollView);
            main.Add(rightPanel);

            rootVisualElement.Add(main);

            RefreshAll();
        }

        private void Update()
        {
            if (_target == null || _serializedObject == null) return;
            if (_serializedObject.targetObject != _target) BindTarget(_target);
            _serializedObject.Update();
        }

        private void RefreshAll()
        {
            RefreshBreadcrumbs();
            RefreshLeftPanel();
            RefreshRightPanel();
        }

        private void RefreshBreadcrumbs()
        {
            var bc = rootVisualElement.Q<Label>("breadcrumbs");
            if (bc == null) return;
            string d = _selectedDialogue >= 0 ? $"D{_selectedDialogue + 1}" : "−";
            string m = _selectedMonolog >= 0 ? $"M{_selectedMonolog + 1}" : "−";
            string s = _selectedSentence >= 0 ? $"S{_selectedSentence + 1}" : "−";
            bc.text = $"  {d}  →  {m}  →  {s}";
        }

        private void RefreshLeftPanel()
        {
            if (_leftContent == null) return;
            _leftContent.Clear();
            if (_dialoguesProp == null)
            {
                var hint = new Label("Assign a Controller above.");
                hint.style.marginTop = 8;
                hint.style.color = new Color(0.7f, 0.7f, 0.75f);
                _leftContent.Add(hint);
                return;
            }

            var structureHeader = new Label("Structure");
            structureHeader.style.height = 24;
            structureHeader.style.backgroundColor = AccentPrimary;
            structureHeader.style.color = Color.white;
            structureHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            structureHeader.style.paddingLeft = 8;
            structureHeader.style.paddingTop = 2;
            _leftContent.Add(structureHeader);
            _leftContent.Add(new VisualElement { style = { height = 6 } });

            AddSectionHeader(_leftContent, "Dialogues", AccentPrimary,
                AddDialogue,
                _selectedDialogue >= 0 ? RemoveDialogue : null,
                _selectedDialogue > 0 ? MoveDialogueUp : null,
                _selectedDialogue >= 0 && _selectedDialogue < _dialoguesProp.arraySize - 1 ? MoveDialogueDown : null);
            _leftContent.Add(new VisualElement { style = { height = 4 } });

            for (int i = 0; i < _dialoguesProp.arraySize; i++)
            {
                int idx = i;
                AddItemButton(_leftContent, _selectedDialogue == idx, $"  Dialogue {idx + 1}", () =>
                {
                    _selectedDialogue = idx;
                    _selectedMonolog = -1;
                    _selectedSentence = -1;
                    RefreshAll();
                });
            }
            if (_dialoguesProp.arraySize == 0)
                AddEmptyHint(_leftContent, "No dialogues. Click + to add.");
            _leftContent.Add(new VisualElement { style = { height = 12 } });

            if (_selectedDialogue >= 0)
            {
                SerializedProperty monologues = GetMonologues();
                if (monologues != null)
                {
                    AddSectionHeader(_leftContent, "Monologues", AccentSecondary,
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
                        string name = string.IsNullOrWhiteSpace(nameProp.stringValue) ? "Unnamed" : nameProp.stringValue;
                        AddItemButton(_leftContent, _selectedMonolog == idx, $"  {idx + 1}. {name}", () =>
                        {
                            _selectedMonolog = idx;
                            _selectedSentence = -1;
                            RefreshAll();
                        });
                    }
                    if (monologues.arraySize == 0)
                        AddEmptyHint(_leftContent, "No monologues. Click + to add.");
                    _leftContent.Add(new VisualElement { style = { height = 12 } });
                }
            }

            if (_selectedDialogue >= 0 && _selectedMonolog >= 0)
            {
                SerializedProperty sentences = GetSentences();
                if (sentences != null)
                {
                    AddSectionHeader(_leftContent, "Sentences", AccentTertiary,
                        AddSentence,
                        _selectedSentence >= 0 ? RemoveSentence : null,
                        _selectedSentence > 0 ? MoveSentenceUp : null,
                        _selectedSentence >= 0 && _selectedSentence < sentences.arraySize - 1 ? MoveSentenceDown : null);
                    _leftContent.Add(new VisualElement { style = { height = 4 } });
                    for (int i = 0; i < sentences.arraySize; i++)
                    {
                        int idx = i;
                        SerializedProperty item = sentences.GetArrayElementAtIndex(idx);
                        string preview = BuildSentencePreview(item.FindPropertyRelative("sentence").stringValue);
                        AddItemButton(_leftContent, _selectedSentence == idx, $"  {idx + 1}. {preview}", () =>
                        {
                            _selectedSentence = idx;
                            RefreshAll();
                        });
                    }
                    if (sentences.arraySize == 0)
                        AddEmptyHint(_leftContent, "No sentences. Click + to add.");
                }
            }
        }

        private void RefreshRightPanel()
        {
            if (_rightDetailsContainer == null) return;
            _rightDetailsContainer.Clear();
            _rightDetailsContainer.Unbind();
            if (_serializedObject == null || _selectedDialogue < 0)
            {
                AddEmptyHint(_rightDetailsContainer, "Select a dialogue from the left panel.");
                return;
            }

            _serializedObject.Update();
            SerializedProperty dialogue = GetDialogue();
            if (dialogue == null) return;

            AddDetailBlock(_rightDetailsContainer, $"Dialogue {_selectedDialogue + 1}", AccentPrimary, box =>
            {
                var prop = dialogue.FindPropertyRelative("OnChangeDialog");
                var pf = new PropertyField(prop, "On Change Dialogue");
                pf.BindProperty(prop);
                box.Add(pf);
            });

            if (_selectedMonolog >= 0)
            {
                SerializedProperty monolog = GetMonolog();
                if (monolog != null)
                    AddDetailBlock(_rightDetailsContainer, $"Monologue {_selectedMonolog + 1}", AccentSecondary, box =>
                    {
                        var p1 = monolog.FindPropertyRelative("characterName");
                        var p2 = monolog.FindPropertyRelative("OnChangeMonolog");
                        var pf1 = new PropertyField(p1, "Character");
                        var pf2 = new PropertyField(p2, "On Change Monolog");
                        pf1.BindProperty(p1);
                        pf2.BindProperty(p2);
                        box.Add(pf1);
                        box.Add(pf2);
                    });
            }

            if (_selectedSentence >= 0)
            {
                SerializedProperty sentence = GetSentence();
                if (sentence != null)
                {
                    AddDetailBlock(_rightDetailsContainer, $"Sentence {_selectedSentence + 1}", AccentTertiary, box =>
                    {
                        var spriteProp = sentence.FindPropertyRelative("sprite");
                        var textProp = sentence.FindPropertyRelative("sentence");
                        var onChangeProp = sentence.FindPropertyRelative("OnChangeSentence");
                        var pfSprite = new PropertyField(spriteProp);
                        var pfText = new PropertyField(textProp, "Text");
                        var pfOnChange = new PropertyField(onChangeProp, "On Change Sentence");
                        pfSprite.BindProperty(spriteProp);
                        pfText.BindProperty(textProp);
                        pfOnChange.BindProperty(onChangeProp);
                        box.Add(pfSprite);
                        box.Add(pfText);
                        box.Add(pfOnChange);
                    });

                    var previewBox = new VisualElement();
                    previewBox.style.height = 80 + 2 * 18;
                    previewBox.style.marginTop = 8;
                    previewBox.style.backgroundColor = new Color(0.2f, 0.2f, 0.24f, 0.6f);
                    previewBox.style.flexDirection = FlexDirection.Row;
                    previewBox.style.paddingLeft = 10;
                    previewBox.style.paddingTop = 8;
                    previewBox.style.paddingRight = 10;
                    previewBox.style.paddingBottom = 8;
                    var previewLabel = new Label("Preview");
                    previewLabel.style.color = Color.white;
                    previewLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    previewLabel.style.marginBottom = 4;
                    previewLabel.style.backgroundColor = new StyleColor(new Color(AccentTertiary.r, AccentTertiary.g, AccentTertiary.b, 0.25f));
                    previewLabel.style.paddingLeft = 8;
                    _rightDetailsContainer.Add(previewLabel);
                    var previewRow = new VisualElement();
                    previewRow.style.flexDirection = FlexDirection.Row;
                    Texture2D previewTex = null;
                    if (sentence.FindPropertyRelative("sprite").objectReferenceValue is Sprite sp)
                        previewTex = AssetPreview.GetAssetPreview(sp) ?? AssetPreview.GetMiniThumbnail(sp);
                    if (previewTex != null)
                    {
                        var img = new Image { image = previewTex, scaleMode = ScaleMode.ScaleToFit };
                        img.style.width = 64;
                        img.style.height = 64;
                        img.style.marginRight = 8;
                        previewRow.Add(img);
                    }
                    string textPreview = sentence.FindPropertyRelative("sentence").stringValue;
                    if (string.IsNullOrWhiteSpace(textPreview)) textPreview = "(empty)";
                    var textLabel = new Label(textPreview);
                    textLabel.style.color = new Color(0.9f, 0.9f, 0.92f);
                    textLabel.style.whiteSpace = WhiteSpace.Normal;
                    textLabel.style.flexGrow = 1;
                    previewRow.Add(textLabel);
                    previewBox.Add(previewRow);
                    _rightDetailsContainer.Add(previewBox);
                }
            }
        }

        private static void AddSectionHeader(VisualElement parent, string title, Color accent,
            System.Action onAdd, System.Action onRemove, System.Action onUp, System.Action onDown)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.height = 28;
            var titleEl = new Label(title);
            titleEl.style.flexGrow = 1;
            titleEl.style.backgroundColor = new StyleColor(new Color(accent.r, accent.g, accent.b, 0.35f));
            titleEl.style.color = Color.white;
            titleEl.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleEl.style.paddingLeft = 8;
            titleEl.style.paddingTop = 4;
            row.Add(titleEl);
            var addBtn = new Button(onAdd ?? (() => { })) { text = "+" };
            addBtn.style.width = 26;
            addBtn.style.height = 24;
            addBtn.style.backgroundColor = AccentSecondary;
            row.Add(addBtn);
            var remBtn = new Button(onRemove ?? (() => { })) { text = "−" };
            remBtn.style.width = 26;
            remBtn.style.height = 24;
            remBtn.style.backgroundColor = AccentTertiary;
            remBtn.SetEnabled(onRemove != null);
            row.Add(remBtn);
            var upBtn = new Button(onUp ?? (() => { })) { text = "↑" };
            upBtn.style.width = 26;
            upBtn.style.height = 24;
            upBtn.style.backgroundColor = accent;
            upBtn.SetEnabled(onUp != null);
            row.Add(upBtn);
            var downBtn = new Button(onDown ?? (() => { })) { text = "↓" };
            downBtn.style.width = 26;
            downBtn.style.height = 24;
            downBtn.style.backgroundColor = accent;
            downBtn.SetEnabled(onDown != null);
            row.Add(downBtn);
            parent.Add(row);
        }

        private static void AddItemButton(VisualElement parent, bool selected, string label, System.Action onClick)
        {
            var btn = new Button(onClick) { text = label };
            btn.style.height = ItemHeight;
            btn.style.unityTextAlign = TextAnchor.MiddleLeft;
            btn.style.paddingLeft = 12;
            if (selected)
                btn.style.backgroundColor = SelectedBg;
            else
                btn.style.backgroundColor = Color.clear;
            parent.Add(btn);
        }

        private static void AddEmptyHint(VisualElement parent, string text)
        {
            var hint = new Label(text);
            hint.style.height = 32;
            hint.style.backgroundColor = new Color(0.3f, 0.3f, 0.35f, 0.4f);
            hint.style.unityTextAlign = TextAnchor.MiddleCenter;
            hint.style.color = new Color(0.7f, 0.7f, 0.75f);
            parent.Add(hint);
        }

        private static void AddDetailBlock(VisualElement parent, string title, Color accent, System.Action<VisualElement> addFields)
        {
            var header = new Label(title);
            header.style.height = 22;
            header.style.backgroundColor = new StyleColor(new Color(accent.r, accent.g, accent.b, 0.4f));
            header.style.color = Color.white;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.paddingLeft = 8;
            header.style.paddingTop = 2;
            parent.Add(header);
            var box = new VisualElement();
            box.style.backgroundColor = new Color(0.24f, 0.24f, 0.28f, 0.6f);
            box.style.paddingLeft = 6;
            box.style.paddingRight = 6;
            box.style.paddingTop = 8;
            box.style.paddingBottom = 8;
            box.style.marginBottom = 8;
            addFields?.Invoke(box);
            parent.Add(box);
        }

        private void AddDialogue()
        {
            if (_dialoguesProp == null) return;
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
            if (_selectedDialogue < 0 || _dialoguesProp == null || _selectedDialogue >= _dialoguesProp.arraySize) return;
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
            if (_selectedDialogue <= 0 || _dialoguesProp == null) return;
            _serializedObject.Update();
            _dialoguesProp.MoveArrayElement(_selectedDialogue, _selectedDialogue - 1);
            _serializedObject.ApplyModifiedProperties();
            _selectedDialogue--;
            RefreshAll();
        }

        private void MoveDialogueDown()
        {
            if (_selectedDialogue < 0 || _selectedDialogue >= _dialoguesProp.arraySize - 1) return;
            _serializedObject.Update();
            _dialoguesProp.MoveArrayElement(_selectedDialogue, _selectedDialogue + 1);
            _serializedObject.ApplyModifiedProperties();
            _selectedDialogue++;
            RefreshAll();
        }

        private void AddMonolog()
        {
            SerializedProperty monologues = GetMonologues();
            if (monologues == null) return;
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
            if (monologues == null || _selectedMonolog < 0 || _selectedMonolog >= monologues.arraySize) return;
            _serializedObject.Update();
            monologues.DeleteArrayElementAtIndex(_selectedMonolog);
            _serializedObject.ApplyModifiedProperties();
            _selectedMonolog = Mathf.Clamp(_selectedMonolog, 0, monologues.arraySize - 1);
            if (monologues.arraySize == 0) _selectedMonolog = -1;
            _selectedSentence = -1;
            RefreshAll();
        }

        private void MoveMonologUp()
        {
            SerializedProperty monologues = GetMonologues();
            if (monologues == null || _selectedMonolog <= 0) return;
            _serializedObject.Update();
            monologues.MoveArrayElement(_selectedMonolog, _selectedMonolog - 1);
            _serializedObject.ApplyModifiedProperties();
            _selectedMonolog--;
            RefreshAll();
        }

        private void MoveMonologDown()
        {
            SerializedProperty monologues = GetMonologues();
            if (monologues == null || _selectedMonolog < 0 || _selectedMonolog >= monologues.arraySize - 1) return;
            _serializedObject.Update();
            monologues.MoveArrayElement(_selectedMonolog, _selectedMonolog + 1);
            _serializedObject.ApplyModifiedProperties();
            _selectedMonolog++;
            RefreshAll();
        }

        private void AddSentence()
        {
            SerializedProperty sentences = GetSentences();
            if (sentences == null) return;
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
            if (sentences == null || _selectedSentence < 0 || _selectedSentence >= sentences.arraySize) return;
            _serializedObject.Update();
            sentences.DeleteArrayElementAtIndex(_selectedSentence);
            _serializedObject.ApplyModifiedProperties();
            _selectedSentence = Mathf.Clamp(_selectedSentence, 0, sentences.arraySize - 1);
            if (sentences.arraySize == 0) _selectedSentence = -1;
            RefreshAll();
        }

        private void MoveSentenceUp()
        {
            SerializedProperty sentences = GetSentences();
            if (sentences == null || _selectedSentence <= 0) return;
            _serializedObject.Update();
            sentences.MoveArrayElement(_selectedSentence, _selectedSentence - 1);
            _serializedObject.ApplyModifiedProperties();
            _selectedSentence--;
            RefreshAll();
        }

        private void MoveSentenceDown()
        {
            SerializedProperty sentences = GetSentences();
            if (sentences == null || _selectedSentence < 0 || _selectedSentence >= sentences.arraySize - 1) return;
            _serializedObject.Update();
            sentences.MoveArrayElement(_selectedSentence, _selectedSentence + 1);
            _serializedObject.ApplyModifiedProperties();
            _selectedSentence++;
            RefreshAll();
        }

        private SerializedProperty GetDialogue()
        {
            if (_dialoguesProp == null || _selectedDialogue < 0 || _selectedDialogue >= _dialoguesProp.arraySize) return null;
            return _dialoguesProp.GetArrayElementAtIndex(_selectedDialogue);
        }

        private SerializedProperty GetMonologues()
        {
            return GetDialogue()?.FindPropertyRelative("monologues");
        }

        private SerializedProperty GetMonolog()
        {
            SerializedProperty monologues = GetMonologues();
            if (monologues == null || _selectedMonolog < 0 || _selectedMonolog >= monologues.arraySize) return null;
            return monologues.GetArrayElementAtIndex(_selectedMonolog);
        }

        private SerializedProperty GetSentences()
        {
            return GetMonolog()?.FindPropertyRelative("sentences");
        }

        private SerializedProperty GetSentence()
        {
            SerializedProperty sentences = GetSentences();
            if (sentences == null || _selectedSentence < 0 || _selectedSentence >= sentences.arraySize) return null;
            return sentences.GetArrayElementAtIndex(_selectedSentence);
        }

        private void EnsureSelection()
        {
            if (_dialoguesProp == null || _dialoguesProp.arraySize == 0)
            {
                _selectedDialogue = _selectedMonolog = _selectedSentence = -1;
                return;
            }
            _selectedDialogue = Mathf.Clamp(_selectedDialogue < 0 ? 0 : _selectedDialogue, 0, _dialoguesProp.arraySize - 1);
            SerializedProperty monologues = GetMonologues();
            if (monologues == null || monologues.arraySize == 0) { _selectedMonolog = _selectedSentence = -1; return; }
            _selectedMonolog = Mathf.Clamp(_selectedMonolog < 0 ? 0 : _selectedMonolog, 0, monologues.arraySize - 1);
            SerializedProperty sentences = GetSentences();
            if (sentences == null || sentences.arraySize == 0) { _selectedSentence = -1; return; }
            _selectedSentence = Mathf.Clamp(_selectedSentence < 0 ? 0 : _selectedSentence, 0, sentences.arraySize - 1);
        }

        private static void ResetDialogue(SerializedProperty dialogue)
        {
            dialogue?.FindPropertyRelative("monologues")?.ClearArray();
        }

        private static void ResetMonolog(SerializedProperty monolog)
        {
            if (monolog == null) return;
            SerializedProperty name = monolog.FindPropertyRelative("characterName");
            if (name != null) name.stringValue = string.Empty;
            monolog.FindPropertyRelative("sentences")?.ClearArray();
        }

        private static void ResetSentence(SerializedProperty sentence)
        {
            if (sentence == null) return;
            sentence.FindPropertyRelative("sprite").objectReferenceValue = null;
            sentence.FindPropertyRelative("sentence").stringValue = string.Empty;
        }

        private static string BuildSentencePreview(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "(empty)";
            string oneLine = text.Replace("\n", " ").Trim();
            return oneLine.Length > 42 ? oneLine.Substring(0, 42) + "..." : oneLine;
        }
    }
}
