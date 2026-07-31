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
            var window = GetWindow<AbilityDesignerWindow>();
            window.titleContent = new GUIContent("Ability Designer");
            window.minSize = new Vector2(980f, 540f);
        }

        /// <summary>Opens the window with an asset pre-selected (used by the SO inspectors).</summary>
        public static void Open(ScriptableObject select)
        {
            Open();
            var window = GetWindow<AbilityDesignerWindow>();
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
            root.Clear();
            StyleSheet sheet = AbilityDesignerUI.LoadStyleSheet();
            if (sheet != null)
            {
                root.styleSheets.Add(sheet);
            }

            root.AddToClassList("nad-root");

            var main = new VisualElement();
            main.AddToClassList("nad-main-split");
            main.style.flexDirection = FlexDirection.Row;
            main.style.flexGrow = 1f;
            root.Add(main);

            var left = new VisualElement();
            left.AddToClassList("nad-left");
            left.style.width = 292f;
            left.style.flexShrink = 0f;
            main.Add(left);
            BuildLeftToolbar(left);
            var libraryScroll = new ScrollView(ScrollViewMode.Vertical);
            libraryScroll.AddToClassList("nad-library");
            libraryScroll.style.flexGrow = 1f;
            left.Add(libraryScroll);
            _libraryPane = libraryScroll.contentContainer;

            var centerScroll = new ScrollView(ScrollViewMode.Vertical);
            centerScroll.AddToClassList("nad-center-scroll");
            centerScroll.style.flexGrow = 1f;
            main.Add(centerScroll);
            _centerPane = centerScroll.contentContainer;
            _centerPane.AddToClassList("nad-center");

            var right = new VisualElement();
            right.AddToClassList("nad-inspector");
            right.style.width = 360f;
            right.style.flexShrink = 0f;
            main.Add(right);
            var inspectorScroll = new ScrollView(ScrollViewMode.Vertical);
            inspectorScroll.AddToClassList("nad-inspector-scroll");
            inspectorScroll.style.flexGrow = 1f;
            right.Add(inspectorScroll);
            _inspectorPane = inspectorScroll.contentContainer;

            _statusBar = new VisualElement();
            _statusBar.AddToClassList("nad-statusbar");
            root.Add(_statusBar);

            RefreshLibrary();
        }

        private void BuildLeftToolbar(VisualElement left)
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("nad-left-toolbar");
            left.Add(toolbar);

            var search = new TextField();
            search.AddToClassList("nad-search");
            search.value = _search;
            search.RegisterValueChangedCallback(evt =>
            {
                _search = evt.newValue ?? string.Empty;
                RebuildLibraryLists();
            });
            toolbar.Add(search);

            var createRow = new VisualElement();
            createRow.AddToClassList("nad-create-row");
            createRow.style.flexDirection = FlexDirection.Row;
            toolbar.Add(createRow);

            var newAbility = new Button(() => CreateAsset<AbilityDefinition>("New Ability"))
            {
                text = "+ Ability"
            };
            newAbility.AddToClassList("nad-create-btn");
            createRow.Add(newAbility);

            var newModifier = new Button(() => CreateAsset<ModifierDefinition>("New Modifier"))
            {
                text = "+ Modifier"
            };
            newModifier.AddToClassList("nad-create-btn");
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

            var asset = CreateInstance<T>();
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
                var asset = AssetDatabase.LoadAssetAtPath<AbilityDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    _abilities.Add(asset);
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(ModifierDefinition)))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ModifierDefinition>(AssetDatabase.GUIDToAssetPath(guid));
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
            var header = new VisualElement();
            header.AddToClassList("nad-lib-header");
            header.style.flexDirection = FlexDirection.Row;
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("nad-lib-title");
            header.Add(titleLabel);
            var count = new Label(items.Count.ToString());
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
            var row = new VisualElement();
            row.AddToClassList("nad-row");
            if (item == _selected)
            {
                row.AddToClassList("nad-row--selected");
            }

            row.style.flexDirection = FlexDirection.Row;

            var main = new VisualElement();
            main.AddToClassList("nad-row-main");
            main.style.flexGrow = 1f;
            var nameLabel = new Label(item.name);
            nameLabel.AddToClassList("nad-row-name");
            main.Add(nameLabel);
            var idLabel = new Label(string.IsNullOrEmpty(id) ? "no id" : id);
            idLabel.AddToClassList("nad-row-id");
            main.Add(idLabel);
            row.Add(main);

            string chipClass = "nad-chip--ability";
            string chipText = "ability";
            if (isModifier)
            {
                var def = item as ModifierDefinition;
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
                var warn = new Label("!");
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

            _selected = asset;
            _serialized = new SerializedObject(asset);
            RebuildLibraryLists();
            RebuildCenter();
            RebuildInspector();
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
                var empty = new VisualElement();
                empty.AddToClassList("nad-empty");
                var title = new Label("No ability selected");
                title.AddToClassList("nad-empty-title");
                empty.Add(title);
                var sub = new Label("Create or select an ability or modifier on the left.");
                sub.AddToClassList("nad-empty-sub");
                empty.Add(sub);
                _centerPane.Add(empty);
                return;
            }

            if (_serialized == null || _serialized.targetObject != _selected)
            {
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
            var board = new VisualElement();
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
            var column = new VisualElement();
            column.AddToClassList("nad-phase-column");
            column.style.flexGrow = 1f;

            var head = new VisualElement();
            head.AddToClassList("nad-column-head");
            var titleRow = new VisualElement();
            titleRow.AddToClassList("nad-column-title-row");
            titleRow.style.flexDirection = FlexDirection.Row;
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("nad-section-title");
            titleRow.Add(titleLabel);
            head.Add(titleRow);
            var sub = new Label(subtitle);
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

            var add = new Button(() => ShowAddNodeMenu(propertyPath)) { text = "+ Add effect" };
            add.AddToClassList("nad-add-btn");
            column.Add(add);
            return column;
        }

        private VisualElement BuildNodeCard(string listPath, int index, int count)
        {
            SerializedProperty element = _serialized.FindProperty(listPath).GetArrayElementAtIndex(index);
            EffectNodeData node = ReadNode(element);

            var card = new VisualElement();
            card.AddToClassList("nad-card");
            card.AddToClassList(AbilityDesignerUI.OpCardClass(node.OpId));

            var top = new VisualElement();
            top.AddToClassList("nad-card-top");
            top.style.flexDirection = FlexDirection.Row;

            var indexLabel = new Label((index + 1).ToString("00"));
            indexLabel.AddToClassList("nad-card-index");
            top.Add(indexLabel);

            var opLabel = new Label(string.IsNullOrEmpty(node.OpId) ? "op?" : node.OpId);
            opLabel.AddToClassList("nad-card-op");
            top.Add(opLabel);

            var spacer = new VisualElement();
            spacer.AddToClassList("nad-flex-spacer");
            spacer.style.flexGrow = 1f;
            top.Add(spacer);

            top.Add(SmallButton("▲", index > 0, () => MoveNode(listPath, index, index - 1)));
            top.Add(SmallButton("▼", index < count - 1, () => MoveNode(listPath, index, index + 1)));
            top.Add(SmallButton("✕", true, () => RemoveNode(listPath, index), true));
            card.Add(top);

            var summary = new Label(AbilityDesignerUI.NodeSummary(node));
            summary.AddToClassList("nad-card-summary");
            card.Add(summary);

            var foldout = new Foldout { text = "Edit", value = false };
            foldout.Add(new PropertyField(element));
            foldout.RegisterCallback<SerializedPropertyChangeEvent>(_ => RefreshAfterEdit());
            card.Add(foldout);
            card.Bind(_serialized);
            return card;
        }

        private Button SmallButton(string text, bool enabled, Action onClick, bool danger = false)
        {
            var button = new Button(onClick) { text = text };
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
            var menu = new GenericMenu();
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
            element.FindPropertyRelative(nameof(EffectNodeData.OpId)).stringValue = opId;
            element.FindPropertyRelative(nameof(EffectNodeData.Chance)).floatValue = 1f;
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
            var node = new EffectNodeData
            {
                OpId = element.FindPropertyRelative(nameof(EffectNodeData.OpId)).stringValue,
                Target = (EffectTargetSelector)element.FindPropertyRelative(nameof(EffectNodeData.Target)).enumValueIndex,
                TeamFilter = (AbilityTeamFilter)element.FindPropertyRelative(nameof(EffectNodeData.TeamFilter)).enumValueIndex,
                Radius = element.FindPropertyRelative(nameof(EffectNodeData.Radius)).floatValue,
                Amount = element.FindPropertyRelative(nameof(EffectNodeData.Amount)).floatValue,
                DamageType = element.FindPropertyRelative(nameof(EffectNodeData.DamageType)).stringValue,
                ModifierId = element.FindPropertyRelative(nameof(EffectNodeData.ModifierId)).stringValue,
                ResourceId = element.FindPropertyRelative(nameof(EffectNodeData.ResourceId)).stringValue,
                ArchetypeId = element.FindPropertyRelative(nameof(EffectNodeData.ArchetypeId)).stringValue,
                CustomParam = element.FindPropertyRelative(nameof(EffectNodeData.CustomParam)).stringValue,
                Chance = element.FindPropertyRelative(nameof(EffectNodeData.Chance)).floatValue
            };
            return node;
        }

        private void BuildModifierBoard()
        {
            var board = new VisualElement();
            board.AddToClassList("nad-mod-board");
            _centerPane.Add(board);

            var definition = (ModifierDefinition)_selected;
            ModifierBlueprint blueprint = definition.Blueprint;

            VisualElement properties = Section(board, "Property contributions",
                "add → mul → max, applied per stack");
            if (blueprint.Properties != null)
            {
                foreach (PropertyContribution contribution in blueprint.Properties)
                {
                    var row = new VisualElement();
                    row.AddToClassList("nad-prop-row");
                    row.style.flexDirection = FlexDirection.Row;
                    var name = new Label(contribution.PropertyId);
                    name.AddToClassList("nad-row-name");
                    name.style.flexGrow = 1f;
                    row.Add(name);
                    var op = new Label(contribution.Op + " " + contribution.Value.ToString("0.##"));
                    op.AddToClassList("nad-prop-op");
                    row.Add(op);
                    if (contribution.PerStackValue != 0f)
                    {
                        var stack = new Label("+" + contribution.PerStackValue.ToString("0.##") + "/stack");
                        stack.AddToClassList("nad-prop-stack");
                        row.Add(stack);
                    }

                    properties.Add(row);
                }
            }

            VisualElement states = Section(board, "States", "any-true-wins while active");
            var chipRow = new VisualElement();
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
                    var card = new Label(AbilityDesignerUI.NodeSummary(node));
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

                    var card = new VisualElement();
                    card.AddToClassList("nad-card");
                    card.AddToClassList("nad-card--reaction");
                    var head = new Label("on " + reaction.EventId +
                                         (reaction.TargetEventSource ? " → event source" : " → owner"));
                    head.AddToClassList("nad-card-op");
                    card.Add(head);
                    if (reaction.Effects != null)
                    {
                        foreach (EffectNodeData node in reaction.Effects)
                        {
                            var line = new Label(AbilityDesignerUI.NodeSummary(node));
                            line.AddToClassList("nad-card-summary");
                            card.Add(line);
                        }
                    }

                    reactions.Add(card);
                }
            }

            var hint = new Label("Use the inspector on the right to edit lists; this board is a live summary.");
            hint.AddToClassList("nad-empty-note");
            board.Add(hint);
        }

        private static VisualElement Section(VisualElement parent, string title, string subtitle)
        {
            var section = new VisualElement();
            section.AddToClassList("nad-section");
            var head = new VisualElement();
            head.AddToClassList("nad-section-head");
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("nad-section-title");
            head.Add(titleLabel);
            var sub = new Label(subtitle);
            sub.AddToClassList("nad-column-sub");
            head.Add(sub);
            section.Add(head);
            var body = new VisualElement();
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
                _serialized = new SerializedObject(_selected);
            }

            var inspector = new InspectorElement(_serialized);
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

            var summaryRow = new VisualElement();
            summaryRow.AddToClassList("nad-status-row");
            summaryRow.style.flexDirection = FlexDirection.Row;
            var dot = new VisualElement();
            dot.AddToClassList("nad-status-dot");
            summaryRow.Add(dot);
            var summary = new Label(_issues.Count == 0
                ? "All ability data valid"
                : _issues.Count + " validation issue" + (_issues.Count == 1 ? "" : "s"));
            summary.AddToClassList("nad-status-summary");
            summaryRow.Add(summary);
            var counts = new Label(_abilities.Count + " abilities · " + _modifiers.Count + " modifiers");
            counts.AddToClassList("nad-counts");
            summaryRow.Add(counts);
            _statusBar.Add(summaryRow);

            if (_issues.Count == 0)
            {
                return;
            }

            var issues = new ScrollView(ScrollViewMode.Vertical);
            issues.AddToClassList("nad-issues");
            issues.style.maxHeight = 96f;
            foreach (AbilityIssue issue in _issues)
            {
                var row = new VisualElement();
                row.AddToClassList("nad-issue-row");
                row.style.flexDirection = FlexDirection.Row;
                var issueDot = new VisualElement();
                issueDot.AddToClassList("nad-issue-dot");
                row.Add(issueDot);
                var asset = new Label(issue.Asset != null ? issue.Asset.name : "?");
                asset.AddToClassList("nad-issue-asset");
                row.Add(asset);
                var message = new Label(issue.Message);
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
            _issues = AbilityValidation.Scan(_abilities, _modifiers);
            RebuildCenter();
            RebuildStatusBar();
        }

        private void OnUndoRedo()
        {
            if (_serialized != null)
            {
                _serialized.Update();
            }

            RefreshAfterEdit();
            RebuildLibraryLists();
        }
    }
}
