using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neo.Abilities.Editor
{
    /// <summary>
    ///     The v10 flagship editor: a UI Toolkit window for browsing, authoring and validating
    ///     <see cref="AbilityDefinition" /> and <see cref="ModifierDefinition" /> assets.
    ///     Left: searchable library. Center: phase board / modifier board. Right: full inspector.
    ///     Bottom: live validation. Menu: Neoxider → Windows → Ability Designer.
    /// </summary>
    public sealed class AbilityDesignerWindow : EditorWindow
    {
        private readonly List<AbilityDefinition> _abilities = new List<AbilityDefinition>();
        private readonly List<ModifierDefinition> _modifiers = new List<ModifierDefinition>();
        private List<AbilityIssue> _issues = new List<AbilityIssue>();

        private ScriptableObject _selected;
        private SerializedObject _serialized;
        private string _search = string.Empty;

        private VisualElement _libraryPane;
        private VisualElement _centerPane;
        private VisualElement _inspectorPane;
        private VisualElement _statusBar;

        [MenuItem("Neoxider/Windows/Ability Designer", false, 0)]
        public static void Open()
        {
            AbilityDesignerWindow window = GetWindow<AbilityDesignerWindow>();
            window.titleContent = new GUIContent("Ability Designer");
            window.minSize = new Vector2(980f, 540f);
        }

        /// <summary>Opens the window with an asset pre-selected (used by the SO inspectors).</summary>
        public static void Open(ScriptableObject select)
        {
            Open();
            AbilityDesignerWindow window = GetWindow<AbilityDesignerWindow>();
            window.RefreshLibrary();
            window.SelectAsset(select);
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            ReleaseSerializedObject();
        }

        private void OnFocus()
        {
            if (rootVisualElement.childCount > 0)
            {
                RefreshLibrary();
            }
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Unbind();
            root.Clear();
            StyleSheet sheet = AbilityDesignerUI.LoadStyleSheet();
            if (sheet != null)
            {
                root.styleSheets.Add(sheet);
            }

            root.AddToClassList("nad-root");

            VisualElement main = new VisualElement();
            main.AddToClassList("nad-main-split");
            main.style.flexDirection = FlexDirection.Row;
            main.style.flexGrow = 1f;
            root.Add(main);

            VisualElement left = new VisualElement();
            left.AddToClassList("nad-left");
            left.style.width = 292f;
            left.style.flexShrink = 0f;
            main.Add(left);
            BuildLeftToolbar(left);
            ScrollView libraryScroll = new ScrollView(ScrollViewMode.Vertical);
            libraryScroll.AddToClassList("nad-library");
            libraryScroll.style.flexGrow = 1f;
            left.Add(libraryScroll);
            _libraryPane = libraryScroll.contentContainer;

            ScrollView centerScroll = new ScrollView(ScrollViewMode.Vertical);
            centerScroll.AddToClassList("nad-center-scroll");
            centerScroll.style.flexGrow = 1f;
            main.Add(centerScroll);
            _centerPane = centerScroll.contentContainer;
            _centerPane.AddToClassList("nad-center");

            VisualElement right = new VisualElement();
            right.AddToClassList("nad-inspector");
            right.style.width = 360f;
            right.style.flexShrink = 0f;
            main.Add(right);
            ScrollView inspectorScroll = new ScrollView(ScrollViewMode.Vertical);
            inspectorScroll.AddToClassList("nad-inspector-scroll");
            inspectorScroll.style.flexGrow = 1f;
            right.Add(inspectorScroll);
            _inspectorPane = inspectorScroll.contentContainer;
            _inspectorPane.RegisterCallback<SerializedPropertyChangeEvent>(_ => RefreshAfterEdit());

            _statusBar = new VisualElement();
            _statusBar.AddToClassList("nad-statusbar");
            root.Add(_statusBar);

            RefreshLibrary();
        }

        private void BuildLeftToolbar(VisualElement left)
        {
            VisualElement toolbar = new VisualElement();
            toolbar.AddToClassList("nad-left-toolbar");
            left.Add(toolbar);

            TextField search = new TextField();
            search.AddToClassList("nad-search");
            search.value = _search;
            search.RegisterValueChangedCallback(evt =>
            {
                _search = evt.newValue ?? string.Empty;
                RebuildLibraryLists();
            });
            toolbar.Add(search);

            VisualElement createRow = new VisualElement();
            createRow.AddToClassList("nad-create-row");
            createRow.style.flexDirection = FlexDirection.Row;
            toolbar.Add(createRow);

            Button newAbility = new Button(() => CreateAsset<AbilityDefinition>("New Ability"))
            {
                text = "+ Ability"
            };
            newAbility.AddToClassList("nad-create-btn");
            createRow.Add(newAbility);

            Button newModifier = new Button(() => CreateAsset<ModifierDefinition>("New Modifier"))
            {
                text = "+ Modifier"
            };
            newModifier.AddToClassList("nad-create-btn");
            newModifier.AddToClassList("nad-create-btn--last");
            createRow.Add(newModifier);
        }

        private void CreateAsset<T>(string baseName) where T : ScriptableObject
        {
            string path = EditorUtility.SaveFilePanelInProject("Create " + typeof(T).Name,
                baseName, "asset", "Choose where to save the new asset.");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            T asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            RefreshLibrary();
            SelectAsset(asset);
        }

        private void RefreshLibrary()
        {
            _abilities.Clear();
            _modifiers.Clear();

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(AbilityDefinition)))
            {
                AbilityDefinition asset = AssetDatabase.LoadAssetAtPath<AbilityDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    _abilities.Add(asset);
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(ModifierDefinition)))
            {
                ModifierDefinition asset = AssetDatabase.LoadAssetAtPath<ModifierDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    _modifiers.Add(asset);
                }
            }

            _abilities.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            _modifiers.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            _issues = AbilityValidation.Scan(_abilities, _modifiers);

            if (_selected == null && _abilities.Count > 0)
            {
                _selected = _abilities[0];
            }

            RebuildLibraryLists();
            RebuildCenter();
            RebuildInspector();
            RebuildStatusBar();
        }

        private void RebuildLibraryLists()
        {
            if (_libraryPane == null)
            {
                return;
            }

            _libraryPane.Clear();
            AddLibrarySection("Abilities", _abilities, false);
            AddLibrarySection("Modifiers", _modifiers, true);
        }

        private void AddLibrarySection<T>(string title, List<T> items, bool isModifier)
            where T : ScriptableObject
        {
            VisualElement header = new VisualElement();
            header.AddToClassList("nad-lib-header");
            header.style.flexDirection = FlexDirection.Row;
            Label titleLabel = new Label(title);
            titleLabel.AddToClassList("nad-lib-title");
            header.Add(titleLabel);
            Label count = new Label(items.Count.ToString());
            count.AddToClassList("nad-lib-count");
            header.Add(count);
            _libraryPane.Add(header);

            string filter = _search.Trim();
            foreach (T item in items)
            {
                string id = GetId(item);
                if (filter.Length > 0 &&
                    item.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                    (id == null || id.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0))
                {
                    continue;
                }

                _libraryPane.Add(BuildLibraryRow(item, id, isModifier));
            }
        }

        private VisualElement BuildLibraryRow(ScriptableObject item, string id, bool isModifier)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("nad-row");
            if (item == _selected)
            {
                row.AddToClassList("nad-row--selected");
            }

            row.style.flexDirection = FlexDirection.Row;

            VisualElement main = new VisualElement();
            main.AddToClassList("nad-row-main");
            main.style.flexGrow = 1f;
            Label nameLabel = new Label(item.name);
            nameLabel.AddToClassList("nad-row-name");
            main.Add(nameLabel);
            Label idLabel = new Label(string.IsNullOrEmpty(id) ? "no id" : id);
            idLabel.AddToClassList("nad-row-id");
            main.Add(idLabel);
            row.Add(main);

            string chipClass = "nad-chip--ability";
            string chipText = "ability";
            if (isModifier)
            {
                ModifierDefinition def = item as ModifierDefinition;
                bool debuff = def != null && def.Blueprint != null && def.Blueprint.IsDebuff;
                chipClass = debuff ? "nad-chip--debuff" : "nad-chip--buff";
                chipText = debuff ? "debuff" : "buff";
            }

            row.Add(AbilityDesignerUI.Chip(chipText, chipClass));

            bool hasIssue = false;
            for (int i = 0; i < _issues.Count; i++)
            {
                if (_issues[i].Asset == item)
                {
                    hasIssue = true;
                    break;
                }
            }

            if (hasIssue)
            {
                Label warn = new Label("!");
                warn.AddToClassList("nad-row-warn");
                row.Add(warn);
            }

            row.RegisterCallback<ClickEvent>(_ =>
            {
                SelectAsset(item);
                EditorGUIUtility.PingObject(item);
            });
            return row;
        }

        private static string GetId(ScriptableObject asset)
        {
            switch (asset)
            {
                case AbilityDefinition ability:
                    return ability.Id;
                case ModifierDefinition modifier:
                    return modifier.Id;
                default:
                    return null;
            }
        }

        private void SelectAsset(ScriptableObject asset)
        {
            if (asset == null)
            {
                return;
            }

            ReleaseSerializedObject();
            _selected = asset;
            _serialized = new SerializedObject(asset);
            RebuildLibraryLists();
            RebuildCenter();
            RebuildInspector();
        }

        private void ReleaseSerializedObject()
        {
            rootVisualElement.Unbind();
            _serialized?.Dispose();
            _serialized = null;
        }

        private void RebuildCenter()
        {
            if (_centerPane == null)
            {
                return;
            }

            _centerPane.Clear();

            if (_selected == null)
            {
                ReleaseSerializedObject();
                VisualElement empty = new VisualElement();
                empty.AddToClassList("nad-empty");
                Label title = new Label("No ability selected");
                title.AddToClassList("nad-empty-title");
                empty.Add(title);
                Label sub = new Label("Create or select an ability or modifier on the left.");
                sub.AddToClassList("nad-empty-sub");
                empty.Add(sub);
                _centerPane.Add(empty);
                return;
            }

            if (_serialized == null || _serialized.targetObject != _selected)
            {
                ReleaseSerializedObject();
                _serialized = new SerializedObject(_selected);
            }

            _serialized.Update();
            bool isModifier = _selected is ModifierDefinition;
            _centerPane.Add(AbilityDesignerUI.BuildInspectorHeader(_serialized, isModifier));

            if (isModifier)
            {
                BuildModifierBoard();
            }
            else
            {
                BuildAbilityBoard();
            }
        }

        private void BuildAbilityBoard()
        {
            VisualElement board = new VisualElement();
            board.AddToClassList("nad-board");
            board.style.flexDirection = FlexDirection.Row;
            _centerPane.Add(board);

            board.Add(BuildPhaseColumn("Cast", "runs immediately at the caster",
                "_blueprint.CastEffects"));
            board.Add(BuildPhaseColumn("Impact", "runs on delivery (instant or projectile hit)",
                "_blueprint.ImpactEffects"));
        }

        private VisualElement BuildPhaseColumn(string title, string subtitle, string propertyPath)
        {
            VisualElement column = new VisualElement();
            column.AddToClassList("nad-phase-column");
            column.style.flexGrow = 1f;

            VisualElement head = new VisualElement();
            head.AddToClassList("nad-column-head");
            VisualElement titleRow = new VisualElement();
            titleRow.AddToClassList("nad-column-title-row");
            titleRow.style.flexDirection = FlexDirection.Row;
            Label titleLabel = new Label(title);
            titleLabel.AddToClassList("nad-section-title");
            titleRow.Add(titleLabel);
            head.Add(titleRow);
            Label sub = new Label(subtitle);
            sub.AddToClassList("nad-column-sub");
            head.Add(sub);
            column.Add(head);

            SerializedProperty list = _serialized.FindProperty(propertyPath);
            if (list == null)
            {
                return column;
            }

            for (int i = 0; i < list.arraySize; i++)
            {
                column.Add(BuildNodeCard(propertyPath, i, list.arraySize));
            }

            Button add = new Button(() => ShowAddNodeMenu(propertyPath)) { text = "+ Add effect" };
            add.AddToClassList("nad-add-btn");
            column.Add(add);
            return column;
        }

        private VisualElement BuildNodeCard(string listPath, int index, int count)
        {
            SerializedProperty element = _serialized.FindProperty(listPath).GetArrayElementAtIndex(index);
            EffectNodeData node = ReadNode(element);

            VisualElement card = new VisualElement();
            card.AddToClassList("nad-card");
            string opClass = AbilityDesignerUI.OpCardClass(node.OpId);
            card.AddToClassList(opClass);

            VisualElement top = new VisualElement();
            top.AddToClassList("nad-card-top");
            top.style.flexDirection = FlexDirection.Row;

            Label indexLabel = new Label((index + 1).ToString("00"));
            indexLabel.AddToClassList("nad-card-index");
            top.Add(indexLabel);

            Label opLabel = new Label(string.IsNullOrEmpty(node.OpId) ? "op?" : node.OpId);
            opLabel.AddToClassList("nad-card-op");
            top.Add(opLabel);

            VisualElement spacer = new VisualElement();
            spacer.AddToClassList("nad-flex-spacer");
            spacer.style.flexGrow = 1f;
            top.Add(spacer);

            top.Add(SmallButton("▲", index > 0, () => MoveNode(listPath, index, index - 1)));
            top.Add(SmallButton("▼", index < count - 1, () => MoveNode(listPath, index, index + 1)));
            top.Add(SmallButton("✕", true, () => RemoveNode(listPath, index), true));
            card.Add(top);

            Label summary = new Label(AbilityDesignerUI.NodeSummary(node));
            summary.AddToClassList("nad-card-summary");
            card.Add(summary);

            Foldout foldout = new Foldout { text = "Edit", value = false };
            foldout.Add(new PropertyField(element));
            foldout.RegisterCallback<SerializedPropertyChangeEvent>(_ =>
            {
                _serialized.Update();
                card.RemoveFromClassList(opClass);
                node = ReadNode(element);
                opClass = AbilityDesignerUI.OpCardClass(node.OpId);
                card.AddToClassList(opClass);
                opLabel.text = string.IsNullOrEmpty(node.OpId) ? "op?" : node.OpId;
                summary.text = AbilityDesignerUI.NodeSummary(node);
                RefreshValidationAndLibrary();
            });
            card.Add(foldout);
            card.Bind(_serialized);
            return card;
        }

        private Button SmallButton(string text, bool enabled, Action onClick, bool danger = false)
        {
            Button button = new Button(onClick) { text = text };
            button.AddToClassList("nad-icon-btn");
            if (danger)
            {
                button.AddToClassList("nad-icon-btn--danger");
            }

            button.SetEnabled(enabled);
            return button;
        }

        private void ShowAddNodeMenu(string listPath)
        {
            GenericMenu menu = new GenericMenu();
            string[] ops =
            {
                AbilityEffectOps.Damage, AbilityEffectOps.Heal, AbilityEffectOps.ApplyModifier,
                AbilityEffectOps.RemoveModifier, AbilityEffectOps.Dispel,
                AbilityEffectOps.ResourceChange, AbilityEffectOps.Spawn,
                AbilityEffectOps.Knockback, AbilityEffectOps.Pull, AbilityEffectOps.Teleport,
                AbilityEffectOps.Execute, AbilityEffectOps.Chain
            };
            foreach (string op in ops)
            {
                string captured = op;
                menu.AddItem(new GUIContent(captured), false, () => AddNode(listPath, captured));
            }

            menu.ShowAsContext();
        }

        private void AddNode(string listPath, string opId)
        {
            _serialized.Update();
            SerializedProperty list = _serialized.FindProperty(listPath);
            int index = list.arraySize;
            list.InsertArrayElementAtIndex(index);
            SerializedProperty element = list.GetArrayElementAtIndex(index);
            // Insertion can copy the previous element; initialize the entire node, including leveled fields.
            element.boxedValue = new EffectNodeData { OpId = opId };
            _serialized.ApplyModifiedProperties();
            RefreshAfterEdit();
        }

        private void MoveNode(string listPath, int from, int to)
        {
            _serialized.Update();
            SerializedProperty list = _serialized.FindProperty(listPath);
            list.MoveArrayElement(from, to);
            _serialized.ApplyModifiedProperties();
            RefreshAfterEdit();
        }

        private void RemoveNode(string listPath, int index)
        {
            _serialized.Update();
            SerializedProperty list = _serialized.FindProperty(listPath);
            list.DeleteArrayElementAtIndex(index);
            _serialized.ApplyModifiedProperties();
            RefreshAfterEdit();
        }

        private static EffectNodeData ReadNode(SerializedProperty element)
        {
            return element.boxedValue as EffectNodeData ?? new EffectNodeData();
        }

        private void BuildModifierBoard()
        {
            VisualElement board = new VisualElement();
            board.AddToClassList("nad-mod-board");
            _centerPane.Add(board);

            ModifierDefinition definition = (ModifierDefinition)_selected;
            ModifierBlueprint blueprint = definition.Blueprint;
            if (blueprint == null)
            {
                board.Add(new HelpBox("Modifier blueprint is missing.", HelpBoxMessageType.Error));
                return;
            }

            VisualElement properties = Section(board, "Property contributions",
                "add → mul → max, applied per stack");
            if (blueprint.Properties != null)
            {
                foreach (PropertyContribution contribution in blueprint.Properties)
                {
                    VisualElement row = new VisualElement();
                    row.AddToClassList("nad-prop-row");
                    row.style.flexDirection = FlexDirection.Row;
                    Label name = new Label(contribution.PropertyId);
                    name.AddToClassList("nad-row-name");
                    name.style.flexGrow = 1f;
                    row.Add(name);
                    Label op = new Label(contribution.Op + " " + contribution.Value.ToString("0.##"));
                    op.AddToClassList("nad-prop-op");
                    row.Add(op);
                    if (contribution.PerStackValue != 0f)
                    {
                        Label stack = new Label("+" + contribution.PerStackValue.ToString("0.##") + "/stack");
                        stack.AddToClassList("nad-prop-stack");
                        row.Add(stack);
                    }

                    properties.Add(row);
                }
            }

            VisualElement states = Section(board, "States", "any-true-wins while active");
            VisualElement chipRow = new VisualElement();
            chipRow.AddToClassList("nad-chip-row");
            chipRow.style.flexDirection = FlexDirection.Row;
            chipRow.style.flexWrap = Wrap.Wrap;
            if (blueprint.States != null)
            {
                foreach (string state in blueprint.States)
                {
                    chipRow.Add(AbilityDesignerUI.Chip(state, "nad-chip--state"));
                }
            }

            states.Add(chipRow);

            VisualElement ticks = Section(board, "Tick effects",
                blueprint.TickInterval > 0f ? "every " + blueprint.TickInterval.ToString("0.##") + "s" : "no ticking");
            if (blueprint.TickEffects != null)
            {
                foreach (EffectNodeData node in blueprint.TickEffects)
                {
                    if (node == null)
                    {
                        continue;
                    }

                    Label card = new Label(AbilityDesignerUI.NodeSummary(node));
                    card.AddToClassList("nad-card");
                    card.AddToClassList(AbilityDesignerUI.OpCardClass(node.OpId));
                    ticks.Add(card);
                }
            }

            VisualElement reactions = Section(board, "Event reactions", "declarative, depth-capped");
            if (blueprint.EventReactions != null)
            {
                foreach (ModifierEventReaction reaction in blueprint.EventReactions)
                {
                    if (reaction == null)
                    {
                        continue;
                    }

                    VisualElement card = new VisualElement();
                    card.AddToClassList("nad-card");
                    card.AddToClassList("nad-card--reaction");
                    Label head = new Label("on " + reaction.EventId +
                                         (reaction.TargetEventSource ? " → event source" : " → owner"));
                    head.AddToClassList("nad-card-op");
                    card.Add(head);
                    if (reaction.Effects != null)
                    {
                        foreach (EffectNodeData node in reaction.Effects)
                        {
                            Label line = new Label(AbilityDesignerUI.NodeSummary(node));
                            line.AddToClassList("nad-card-summary");
                            card.Add(line);
                        }
                    }

                    reactions.Add(card);
                }
            }

            Label hint = new Label("Use the inspector on the right to edit lists; this board is a live summary.");
            hint.AddToClassList("nad-empty-note");
            board.Add(hint);
        }

        private static VisualElement Section(VisualElement parent, string title, string subtitle)
        {
            VisualElement section = new VisualElement();
            section.AddToClassList("nad-section");
            VisualElement head = new VisualElement();
            head.AddToClassList("nad-section-head");
            Label titleLabel = new Label(title);
            titleLabel.AddToClassList("nad-section-title");
            head.Add(titleLabel);
            Label sub = new Label(subtitle);
            sub.AddToClassList("nad-column-sub");
            head.Add(sub);
            section.Add(head);
            VisualElement body = new VisualElement();
            body.AddToClassList("nad-section-body");
            section.Add(body);
            parent.Add(section);
            return body;
        }

        private void RebuildInspector()
        {
            if (_inspectorPane == null)
            {
                return;
            }

            _inspectorPane.Clear();
            if (_selected == null)
            {
                return;
            }

            if (_serialized == null || _serialized.targetObject != _selected)
            {
                ReleaseSerializedObject();
                _serialized = new SerializedObject(_selected);
            }

            InspectorElement inspector = new InspectorElement(_serialized);
            inspector.AddToClassList("nad-so-inspector");
            _inspectorPane.Add(inspector);
        }

        private void RebuildStatusBar()
        {
            if (_statusBar == null)
            {
                return;
            }

            _statusBar.Clear();
            _statusBar.EnableInClassList("nad-statusbar--issues", _issues.Count > 0);

            VisualElement summaryRow = new VisualElement();
            summaryRow.AddToClassList("nad-status-row");
            summaryRow.style.flexDirection = FlexDirection.Row;
            VisualElement dot = new VisualElement();
            dot.AddToClassList("nad-status-dot");
            summaryRow.Add(dot);
            Label summary = new Label(_issues.Count == 0
                ? "All ability data valid"
                : _issues.Count + " validation issue" + (_issues.Count == 1 ? "" : "s"));
            summary.AddToClassList("nad-status-summary");
            summaryRow.Add(summary);
            Label counts = new Label(_abilities.Count + " abilities · " + _modifiers.Count + " modifiers");
            counts.AddToClassList("nad-counts");
            summaryRow.Add(counts);
            _statusBar.Add(summaryRow);

            if (_issues.Count == 0)
            {
                return;
            }

            ScrollView issues = new ScrollView(ScrollViewMode.Vertical);
            issues.AddToClassList("nad-issues");
            issues.style.maxHeight = 96f;
            foreach (AbilityIssue issue in _issues)
            {
                VisualElement row = new VisualElement();
                row.AddToClassList("nad-issue-row");
                row.style.flexDirection = FlexDirection.Row;
                VisualElement issueDot = new VisualElement();
                issueDot.AddToClassList("nad-issue-dot");
                row.Add(issueDot);
                Label asset = new Label(issue.Asset != null ? issue.Asset.name : "?");
                asset.AddToClassList("nad-issue-asset");
                row.Add(asset);
                Label message = new Label(issue.Message);
                message.AddToClassList("nad-issue-msg");
                row.Add(message);
                AbilityIssue captured = issue;
                row.RegisterCallback<ClickEvent>(_ =>
                {
                    if (captured.Asset != null)
                    {
                        SelectAsset(captured.Asset);
                        EditorGUIUtility.PingObject(captured.Asset);
                    }
                });
                issues.Add(row);
            }

            _statusBar.Add(issues);
        }

        private void RefreshAfterEdit()
        {
            RefreshValidationAndLibrary();
            RebuildCenter();
        }

        private void RefreshValidationAndLibrary()
        {
            _issues = AbilityValidation.Scan(_abilities, _modifiers);
            RebuildLibraryLists();
            RebuildStatusBar();
        }

        private void OnUndoRedo()
        {
            if (_serialized != null)
            {
                _serialized.Update();
            }

            RefreshAfterEdit();
        }
    }
}
