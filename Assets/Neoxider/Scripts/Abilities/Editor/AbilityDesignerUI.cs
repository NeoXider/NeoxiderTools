using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neo.Abilities.Editor
{
    /// <summary>
    ///     Shared building blocks for the Ability Designer window and the ability/modifier
    ///     custom inspectors: theme colors, stylesheet loading, gradient primitives, chips,
    ///     effect-node summaries and the shared header card.
    /// </summary>
    internal static class AbilityDesignerUI
    {
        public static readonly Color32 AccentA = new Color32(124, 92, 255, 255);
        public static readonly Color32 AccentB = new Color32(76, 201, 240, 255);

        private static StyleSheet _sheet;

        /// <summary>Loads the AbilityDesigner.uss theme (cached), located via the asset database.</summary>
        public static StyleSheet LoadStyleSheet()
        {
            if (_sheet != null)
            {
                return _sheet;
            }

            string[] guids = AssetDatabase.FindAssets("AbilityDesigner t:StyleSheet");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith("/AbilityDesigner.uss", StringComparison.Ordinal))
                {
                    _sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                    break;
                }
            }

            return _sheet;
        }

        /// <summary>Small pill label. Variant class controls the color (see AbilityDesigner.uss).</summary>
        public static Label Chip(string text, string variantClass = null)
        {
            Label chip = new Label(text);
            chip.AddToClassList("nad-chip");
            if (!string.IsNullOrEmpty(variantClass))
            {
                chip.AddToClassList(variantClass);
            }

            return chip;
        }

        /// <summary>Rounded gradient icon badge with a centered initial letter.</summary>
        public static VisualElement IconBadge(out Label letter, bool big = false)
        {
            VisualElement badge = new VisualElement();
            badge.AddToClassList("nad-icon-badge");
            if (big)
            {
                badge.AddToClassList("nad-icon-badge--big");
            }

            GradientRect fill = new GradientRect(AccentA, AccentB);
            fill.AddToClassList("nad-fill");
            badge.Add(fill);

            letter = new Label("?");
            letter.AddToClassList("nad-icon-letter");
            if (big)
            {
                letter.AddToClassList("nad-icon-letter--big");
            }

            badge.Add(letter);
            return badge;
        }

        /// <summary>Thin accent gradient rule used under section titles.</summary>
        public static VisualElement Underline()
        {
            GradientRect line = new GradientRect(AccentA, AccentB);
            line.AddToClassList("nad-underline");
            return line;
        }

        /// <summary>First visible character of the text (falls back), uppercased for icon badges.</summary>
        public static string Initial(string text, string fallback)
        {
            string source = string.IsNullOrWhiteSpace(text) ? fallback : text;
            if (string.IsNullOrWhiteSpace(source))
            {
                return "?";
            }

            return char.ToUpperInvariant(source.Trim()[0]).ToString();
        }

        /// <summary>USS card class for an effect operation id (colors the card by op kind).</summary>
        public static string OpCardClass(string opId)
        {
            switch (opId)
            {
                case AbilityEffectOps.Damage: return "nad-card--damage";
                case AbilityEffectOps.Heal: return "nad-card--heal";
                case AbilityEffectOps.ApplyModifier: return "nad-card--modifier";
                case AbilityEffectOps.RemoveModifier:
                case AbilityEffectOps.Dispel:
                case AbilityEffectOps.ResourceChange: return "nad-card--utility";
                case AbilityEffectOps.Spawn: return "nad-card--spawn";
                default: return "nad-card--custom";
            }
        }

        /// <summary>One-line human readable summary of an effect node ("35 magical damage  → target · 45%").</summary>
        public static string NodeSummary(EffectNodeData node)
        {
            if (node == null)
            {
                return string.Empty;
            }

            string core;
            switch (node.OpId)
            {
                case AbilityEffectOps.Damage:
                    core = string.Format("{0:0.##} {1} damage", node.Amount,
                        string.IsNullOrEmpty(node.DamageType) ? "untyped" : node.DamageType);
                    break;
                case AbilityEffectOps.Heal:
                    core = string.Format("{0:0.##} healing", node.Amount);
                    break;
                case AbilityEffectOps.ApplyModifier:
                    core = string.Format("apply '{0}'", string.IsNullOrEmpty(node.ModifierId) ? "?" : node.ModifierId);
                    break;
                case AbilityEffectOps.RemoveModifier:
                    core = string.Format("remove '{0}'", string.IsNullOrEmpty(node.ModifierId) ? "?" : node.ModifierId);
                    break;
                case AbilityEffectOps.Dispel:
                    core = "dispel";
                    break;
                case AbilityEffectOps.ResourceChange:
                    core = string.Format("{0:+0.##;-0.##;0} {1}", node.Amount,
                        string.IsNullOrEmpty(node.ResourceId) ? "resource" : node.ResourceId);
                    break;
                case AbilityEffectOps.Spawn:
                    core = string.Format("spawn '{0}'", string.IsNullOrEmpty(node.ArchetypeId) ? "?" : node.ArchetypeId);
                    break;
                default:
                    core = string.IsNullOrEmpty(node.CustomParam) ? "custom op" : node.CustomParam;
                    break;
            }

            string target;
            switch (node.Target)
            {
                case EffectTargetSelector.Caster:
                    target = "→ caster";
                    break;
                case EffectTargetSelector.AreaAroundTarget:
                    target = string.Format("→ r{0:0.##} area at target · {1}", node.Radius,
                        node.TeamFilter.ToString().ToLowerInvariant());
                    break;
                case EffectTargetSelector.AreaAroundCaster:
                    target = string.Format("→ r{0:0.##} area at caster · {1}", node.Radius,
                        node.TeamFilter.ToString().ToLowerInvariant());
                    break;
                default:
                    target = "→ target";
                    break;
            }

            string chance = node.Chance < 1f
                ? string.Format(" · {0}%", Mathf.RoundToInt(Mathf.Clamp01(node.Chance) * 100f))
                : string.Empty;

            return core + "  " + target + chance;
        }

        /// <summary>
        ///     Branded header card used by the ScriptableObject inspectors: gradient strip, icon
        ///     initial, display name, id badge, type chip and asset path. Live-updates when the
        ///     serialized object changes.
        /// </summary>
        public static VisualElement BuildInspectorHeader(SerializedObject serializedObject, bool isModifier)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("nad-header-card");
            card.AddToClassList("nad-header-card--inspector");

            GradientRect accent = new GradientRect(AccentA, AccentB);
            accent.AddToClassList("nad-accent-strip");
            card.Add(accent);

            VisualElement row = new VisualElement();
            row.AddToClassList("nad-header-row");

            Label letter;
            row.Add(IconBadge(out letter));

            VisualElement main = new VisualElement();
            main.AddToClassList("nad-header-main");

            Label nameLabel = new Label();
            nameLabel.AddToClassList("nad-header-name");
            main.Add(nameLabel);

            VisualElement meta = new VisualElement();
            meta.AddToClassList("nad-header-meta");

            Label idBadge = new Label();
            idBadge.AddToClassList("nad-id-badge");
            meta.Add(idBadge);

            Label typeChip = Chip(string.Empty);
            meta.Add(typeChip);

            Label pathLabel = new Label();
            pathLabel.AddToClassList("nad-asset-path");
            pathLabel.tooltip = "Click to ping the asset in the Project window";
            UnityEngine.Object target = serializedObject.targetObject;
            pathLabel.RegisterCallback<ClickEvent>(_ =>
            {
                if (target != null)
                {
                    EditorGUIUtility.PingObject(target);
                }
            });
            meta.Add(pathLabel);

            main.Add(meta);
            row.Add(main);
            card.Add(row);

            Action refresh = () =>
            {
                UnityEngine.Object obj = serializedObject.targetObject;
                if (obj == null)
                {
                    return;
                }

                string display = null;
                string id = null;
                bool isDebuff = false;

                AbilityDefinition ability = obj as AbilityDefinition;
                if (ability != null && ability.Blueprint != null)
                {
                    display = ability.Blueprint.DisplayName;
                    id = ability.Blueprint.Id;
                }

                ModifierDefinition modifier = obj as ModifierDefinition;
                if (modifier != null && modifier.Blueprint != null)
                {
                    display = modifier.Blueprint.DisplayName;
                    id = modifier.Blueprint.Id;
                    isDebuff = modifier.Blueprint.IsDebuff;
                }

                letter.text = Initial(display, obj.name);
                nameLabel.text = string.IsNullOrEmpty(display) ? obj.name : display;

                bool missingId = string.IsNullOrEmpty(id);
                idBadge.text = missingId ? "no id" : id;
                idBadge.EnableInClassList("nad-id-badge--missing", missingId);

                if (isModifier)
                {
                    typeChip.text = isDebuff ? "DEBUFF" : "BUFF";
                    typeChip.EnableInClassList("nad-chip--buff", !isDebuff);
                    typeChip.EnableInClassList("nad-chip--debuff", isDebuff);
                }
                else
                {
                    typeChip.text = "ABILITY";
                    typeChip.EnableInClassList("nad-chip--ability", true);
                }

                pathLabel.text = AssetDatabase.GetAssetPath(obj);
            };

            refresh();
            card.TrackSerializedObjectValue(serializedObject, _ => refresh());
            return card;
        }

        /// <summary>Default property fields of the serialized object (everything except m_Script).</summary>
        public static VisualElement BuildDefaultFields(SerializedObject serializedObject)
        {
            VisualElement container = new VisualElement();
            container.AddToClassList("nad-so-fields");

            SerializedProperty iterator = serializedObject.GetIterator();
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (iterator.propertyPath == "m_Script")
                    {
                        continue;
                    }

                    container.Add(new PropertyField(iterator.Copy()));
                } while (iterator.NextVisible(false));
            }

            return container;
        }
    }

    /// <summary>
    ///     A quad filled with a horizontal two-color gradient, drawn through the UITK mesh API.
    ///     USS cannot express linear gradients; this element is the accent-gradient primitive of
    ///     the Ability Designer theme (header strips, icon badges, section underlines).
    /// </summary>
    internal sealed class GradientRect : VisualElement
    {
        private static readonly ushort[] Indices = { 0, 1, 2, 2, 3, 0 };

        private readonly Color32 _left;
        private readonly Color32 _right;

        public GradientRect(Color32 left, Color32 right)
        {
            _left = left;
            _right = right;
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            Rect rect = contentRect;
            if (rect.width < 0.01f || rect.height < 0.01f)
            {
                return;
            }

            Vertex[] vertices = new Vertex[4];
            vertices[0].position = new Vector3(rect.xMin, rect.yMax, Vertex.nearZ);
            vertices[0].tint = _left;
            vertices[1].position = new Vector3(rect.xMin, rect.yMin, Vertex.nearZ);
            vertices[1].tint = _left;
            vertices[2].position = new Vector3(rect.xMax, rect.yMin, Vertex.nearZ);
            vertices[2].tint = _right;
            vertices[3].position = new Vector3(rect.xMax, rect.yMax, Vertex.nearZ);
            vertices[3].tint = _right;

            MeshWriteData mesh = context.Allocate(4, 6);
            mesh.SetAllVertices(vertices);
            mesh.SetAllIndices(Indices);
        }
    }

    /// <summary>One validation finding: a message attached to the offending asset.</summary>
    internal sealed class AbilityIssue
    {
        public readonly ScriptableObject Asset;
        public readonly string Message;

        public AbilityIssue(ScriptableObject asset, string message)
        {
            Asset = asset;
            Message = message;
        }
    }

    /// <summary>
    ///     Cross-asset live validation for the Ability Designer status bar: missing/duplicate ids,
    ///     dangling modifier references, chance range, negative cooldowns, empty effect lists and
    ///     zero-radius area selectors.
    /// </summary>
    internal static class AbilityValidation
    {
        public static List<AbilityIssue> Scan(IReadOnlyList<AbilityDefinition> abilities,
            IReadOnlyList<ModifierDefinition> modifiers)
        {
            List<AbilityIssue> issues = new List<AbilityIssue>();

            HashSet<string> knownModifierIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < modifiers.Count; i++)
            {
                ModifierDefinition modifier = modifiers[i];
                if (modifier != null && !string.IsNullOrEmpty(modifier.Id))
                {
                    knownModifierIds.Add(modifier.Id);
                }
            }

            Dictionary<string, AbilityDefinition> abilityIds =
                new Dictionary<string, AbilityDefinition>(StringComparer.Ordinal);
            for (int i = 0; i < abilities.Count; i++)
            {
                AbilityDefinition ability = abilities[i];
                if (ability == null)
                {
                    continue;
                }

                AbilityBlueprint blueprint = ability.Blueprint;
                if (blueprint == null)
                {
                    issues.Add(new AbilityIssue(ability, "Blueprint is missing."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(blueprint.Id))
                {
                    issues.Add(new AbilityIssue(ability, "Ability has no id."));
                }
                else if (abilityIds.ContainsKey(blueprint.Id))
                {
                    issues.Add(new AbilityIssue(ability, string.Format(
                        "Duplicate ability id '{0}' (also used by '{1}').", blueprint.Id, abilityIds[blueprint.Id].name)));
                }
                else
                {
                    abilityIds.Add(blueprint.Id, ability);
                }

                if (blueprint.Cooldown < 0f)
                {
                    issues.Add(new AbilityIssue(ability,
                        string.Format("Negative cooldown ({0:0.##}s).", blueprint.Cooldown)));
                }

                int castCount = blueprint.CastEffects != null ? blueprint.CastEffects.Count : 0;
                int impactCount = blueprint.ImpactEffects != null ? blueprint.ImpactEffects.Count : 0;
                if (castCount + impactCount == 0)
                {
                    issues.Add(new AbilityIssue(ability, "No effect nodes (Cast and Impact are both empty)."));
                }

                CheckNodes(ability, "Cast", blueprint.CastEffects, knownModifierIds, issues);
                CheckNodes(ability, "Impact", blueprint.ImpactEffects, knownModifierIds, issues);
            }

            Dictionary<string, ModifierDefinition> modifierIds =
                new Dictionary<string, ModifierDefinition>(StringComparer.Ordinal);
            for (int i = 0; i < modifiers.Count; i++)
            {
                ModifierDefinition modifier = modifiers[i];
                if (modifier == null)
                {
                    continue;
                }

                ModifierBlueprint blueprint = modifier.Blueprint;
                if (blueprint == null)
                {
                    issues.Add(new AbilityIssue(modifier, "Blueprint is missing."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(blueprint.Id))
                {
                    issues.Add(new AbilityIssue(modifier, "Modifier has no id."));
                }
                else if (modifierIds.ContainsKey(blueprint.Id))
                {
                    issues.Add(new AbilityIssue(modifier, string.Format(
                        "Duplicate modifier id '{0}' (also used by '{1}').", blueprint.Id,
                        modifierIds[blueprint.Id].name)));
                }
                else
                {
                    modifierIds.Add(blueprint.Id, modifier);
                }

                if (blueprint.TickInterval > 0f &&
                    (blueprint.TickEffects == null || blueprint.TickEffects.Count == 0))
                {
                    issues.Add(new AbilityIssue(modifier, "Tick interval is set but there are no tick effects."));
                }

                if (blueprint.StackPolicy == ModifierStackPolicy.Stack && blueprint.MaxStacks < 1)
                {
                    issues.Add(new AbilityIssue(modifier,
                        string.Format("Stack policy with MaxStacks {0} (must be >= 1).", blueprint.MaxStacks)));
                }

                CheckNodes(modifier, "Tick", blueprint.TickEffects, knownModifierIds, issues);

                if (blueprint.EventReactions != null)
                {
                    for (int r = 0; r < blueprint.EventReactions.Count; r++)
                    {
                        ModifierEventReaction reaction = blueprint.EventReactions[r];
                        if (reaction == null)
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(reaction.EventId))
                        {
                            issues.Add(new AbilityIssue(modifier,
                                string.Format("[Reaction #{0}] has no event id.", r + 1)));
                        }

                        CheckNodes(modifier, string.Format("Reaction #{0}", r + 1), reaction.Effects,
                            knownModifierIds, issues);
                    }
                }
            }

            return issues;
        }

        private static void CheckNodes(ScriptableObject asset, string phase, List<EffectNodeData> nodes,
            HashSet<string> knownModifierIds, List<AbilityIssue> issues)
        {
            if (nodes == null)
            {
                return;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                EffectNodeData node = nodes[i];
                if (node == null)
                {
                    continue;
                }

                string prefix = string.Format("[{0} #{1}] ", phase, i + 1);

                if (node.Chance < 0f || node.Chance > 1f)
                {
                    issues.Add(new AbilityIssue(asset,
                        prefix + string.Format("Chance {0:0.###} is outside 0..1.", node.Chance)));
                }

                bool isArea = node.Target == EffectTargetSelector.AreaAroundTarget ||
                              node.Target == EffectTargetSelector.AreaAroundCaster;
                if (isArea && node.Radius <= 0f)
                {
                    issues.Add(new AbilityIssue(asset,
                        prefix + string.Format("Area selector with radius {0:0.###} (must be > 0).", node.Radius)));
                }

                bool referencesModifier = node.OpId == AbilityEffectOps.ApplyModifier ||
                                          node.OpId == AbilityEffectOps.RemoveModifier;
                if (referencesModifier)
                {
                    if (string.IsNullOrWhiteSpace(node.ModifierId))
                    {
                        issues.Add(new AbilityIssue(asset,
                            prefix + string.Format("{0} has no ModifierId.", node.OpId)));
                    }
                    else if (!knownModifierIds.Contains(node.ModifierId))
                    {
                        issues.Add(new AbilityIssue(asset, prefix + string.Format(
                            "ModifierId '{0}' does not match any ModifierDefinition.", node.ModifierId)));
                    }
                }
            }
        }
    }
}
