using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Abilities;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Abilities
{
    public sealed class Audit20261023NeoAbilitiesEditorTests
    {
        private const string CastPath = "_blueprint.CastEffects";
        private const string ImpactPath = "_blueprint.ImpactEffects";
        private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly List<ScriptableObject> _assets = new List<ScriptableObject>();
        private EditorWindow _window;

        [TearDown]
        public void TearDown()
        {
            if (_window != null)
            {
                _window.Close();
                _window = null;
            }

            foreach (ScriptableObject asset in _assets)
            {
                Undo.ClearUndo(asset);
                Object.DestroyImmediate(asset);
            }

            _assets.Clear();
        }

        [TestCase(CastPath)]
        [TestCase(ImpactPath)]
        public void AddEffect_UsesFreshDefaults_AndSupportsUndoRedo(string listPath)
        {
            AbilityDefinition ability = NewAbility();
            List<EffectNodeData> effects = listPath == CastPath
                ? ability.Blueprint.CastEffects
                : ability.Blueprint.ImpactEffects;
            effects.Add(new EffectNodeData
            {
                Target = EffectTargetSelector.Caster,
                TeamFilter = AbilityTeamFilter.Allies,
                Amount = 91f,
                AmountByLevel = new float[] { 13f, 29f },
                AmountKey = "inherited_special",
                AmountScaleProperty = "spell_power",
                AmountScalePerPoint = 0.5f,
                AmountScaleSource = ScaleAmountSource.Target,
                AmountLevelSource = LevelSource.AbilityLevel,
                Radius = 7f,
                RadiusByLevel = new float[] { 2f, 6f },
                MaxTargets = 2,
                DamageType = AbilityDamageTypes.Pure,
                ModifierId = "inherited_modifier",
                ResourceId = "inherited_resource",
                ArchetypeId = "inherited_prefab",
                CustomParam = "inherited_custom",
                Chance = 0.25f
            });
            OpenWindow(ability);

            Undo.IncrementCurrentGroup();
            InvokeWindow("AddNode", listPath, AbilityEffectOps.Heal);

            effects = listPath == CastPath ? ability.Blueprint.CastEffects : ability.Blueprint.ImpactEffects;
            Assert.That(effects.Count, Is.EqualTo(2));
            Assert.That(effects[0].Amount, Is.EqualTo(91f), "The preceding node must stay unchanged.");
            EffectNodeData added = effects[1];
            Assert.That(added.OpId, Is.EqualTo(AbilityEffectOps.Heal));
            Assert.That(added.Target, Is.EqualTo(EffectTargetSelector.Target));
            Assert.That(added.TeamFilter, Is.EqualTo(AbilityTeamFilter.Enemies));
            Assert.That(added.Amount, Is.Zero);
            Assert.That(added.AmountByLevel, Is.Null.Or.Empty);
            Assert.That(added.AmountKey, Is.Null.Or.Empty);
            Assert.That(added.AmountScaleProperty, Is.Null.Or.Empty);
            Assert.That(added.AmountScalePerPoint, Is.Zero);
            Assert.That(added.AmountScaleSource, Is.EqualTo(ScaleAmountSource.Caster));
            Assert.That(added.AmountLevelSource, Is.EqualTo(LevelSource.None));
            Assert.That(added.Radius, Is.Zero);
            Assert.That(added.RadiusByLevel, Is.Null.Or.Empty);
            Assert.That(added.MaxTargets, Is.Zero);
            Assert.That(added.DamageType, Is.EqualTo(AbilityDamageTypes.Magical));
            Assert.That(added.ModifierId, Is.Null.Or.Empty);
            Assert.That(added.ResourceId, Is.Null.Or.Empty);
            Assert.That(added.ArchetypeId, Is.Null.Or.Empty);
            Assert.That(added.CustomParam, Is.Null.Or.Empty);
            Assert.That(added.Chance, Is.EqualTo(1f));

            Undo.PerformUndo();
            effects = listPath == CastPath ? ability.Blueprint.CastEffects : ability.Blueprint.ImpactEffects;
            Assert.That(effects.Count, Is.EqualTo(1));
            Assert.That(effects[0].Amount, Is.EqualTo(91f));

            Undo.PerformRedo();
            effects = listPath == CastPath ? ability.Blueprint.CastEffects : ability.Blueprint.ImpactEffects;
            Assert.That(effects.Count, Is.EqualTo(2));
            Assert.That(effects[1].OpId, Is.EqualTo(AbilityEffectOps.Heal));
            Assert.That(effects[1].Amount, Is.Zero);
        }

        [Test]
        public void ChainCard_DisplaysTheAuthoredTargetLimit()
        {
            AbilityDefinition ability = NewAbility();
            ability.Blueprint.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Chain,
                Amount = 17f,
                Radius = 6f,
                MaxTargets = 2
            });
            OpenWindow(ability);

            Assert.That(CardSummary(), Does.Contain("× 2 hits"));
            Assert.That(ability.Blueprint.ImpactEffects[0].MaxTargets, Is.EqualTo(2));
        }

        [Test]
        public void SelectionChange_CommandsEditOnlyTheNewTarget()
        {
            AbilityDefinition first = NewAbility();
            first.Blueprint.ImpactEffects.Add(new EffectNodeData { Amount = 7f });
            OpenWindow(first);
            AbilityDefinition second = NewAbility();
            second.Blueprint.Id = "second_ability";

            InvokeWindow("SelectAsset", second);
            InvokeWindow("AddNode", ImpactPath, AbilityEffectOps.Heal);

            Assert.That(first.Blueprint.ImpactEffects.Count, Is.EqualTo(1));
            Assert.That(first.Blueprint.ImpactEffects[0].Amount, Is.EqualTo(7f));
            Assert.That(second.Blueprint.ImpactEffects.Count, Is.EqualTo(1));
            Assert.That(second.Blueprint.ImpactEffects[0].OpId, Is.EqualTo(AbilityEffectOps.Heal));
            Assert.That(CardSummary(), Does.Contain("0 healing"));

            InvokeWindow("SelectAsset", first);
            Assert.That(CardSummary(), Does.Contain("7"));
        }

        [Test]
        public void InspectorEdit_RefreshesBoardLibraryAndValidation()
        {
            AbilityDefinition ability = NewAbility();
            ability.Blueprint.ImpactEffects.Add(new EffectNodeData { Amount = 12f });
            OpenWindow(ability);
            InspectorElement inspector = _window.rootVisualElement.Q<InspectorElement>(className: "nad-so-inspector");

            using (SerializedObject edit = new SerializedObject(ability))
            {
                edit.FindProperty("_blueprint.Id").stringValue = "edited_ability";
                edit.FindProperty("_blueprint.Cooldown").floatValue = -2f;
                edit.FindProperty(ImpactPath).GetArrayElementAtIndex(0)
                    .FindPropertyRelative(nameof(EffectNodeData.Amount)).floatValue = 31f;
                edit.ApplyModifiedPropertiesWithoutUndo();
                SendPropertyChange(inspector, edit.FindProperty("_blueprint"));
            }

            Assert.That(CardSummary(), Does.Contain("31"));
            Assert.That(_window.rootVisualElement.Q<Label>(className: "nad-row-id").text,
                Is.EqualTo("edited_ability"));
            Assert.That(IssueMessages(), Has.Some.Contains("Negative cooldown"));
            Assert.That(_window.rootVisualElement.Q<Label>(className: "nad-row-warn"), Is.Not.Null);
        }

        [Test]
        public void InlineEdit_RefreshesSummaryWithoutReplacingTheActiveFoldout()
        {
            AbilityDefinition ability = NewAbility();
            ability.Blueprint.ImpactEffects.Add(new EffectNodeData { Amount = 12f });
            OpenWindow(ability);
            Foldout active = _window.rootVisualElement.Q<Foldout>();
            active.value = true;

            using (SerializedObject edit = new SerializedObject(ability))
            {
                SerializedProperty node = edit.FindProperty(ImpactPath).GetArrayElementAtIndex(0);
                node.FindPropertyRelative(nameof(EffectNodeData.OpId)).stringValue = AbilityEffectOps.Heal;
                node.FindPropertyRelative(nameof(EffectNodeData.Amount)).floatValue = 31f;
                node.FindPropertyRelative(nameof(EffectNodeData.Chance)).floatValue = 2f;
                edit.ApplyModifiedPropertiesWithoutUndo();
                SendPropertyChange(active, node);
            }

            Assert.That(_window.rootVisualElement.Q<Foldout>(), Is.SameAs(active));
            Assert.That(active.value, Is.True);
            Assert.That(CardSummary(), Does.Contain("31 healing"));
            Assert.That(_window.rootVisualElement.Q<Label>(className: "nad-card-op").text,
                Is.EqualTo(AbilityEffectOps.Heal));
            VisualElement card = _window.rootVisualElement.Q<VisualElement>(className: "nad-card");
            Assert.That(card.ClassListContains("nad-card--heal"), Is.True);
            Assert.That(card.ClassListContains("nad-card--damage"), Is.False);
            Assert.That(IssueMessages(), Has.Some.Contains("Chance"));
            Assert.That(_window.rootVisualElement.Q<Label>(className: "nad-row-warn"), Is.Not.Null);
        }

        [Test]
        public void ModifierBoard_SkipsNullTickNodesAndKeepsValidSummaries()
        {
            ModifierDefinition modifier = NewAsset<ModifierDefinition>();
            modifier.Blueprint.Id = "audit_modifier";
            OpenWindow(modifier);

            // Inline serialization cannot retain null list entries. Arrange the managed-data edge case
            // after Inspector binding, then exercise the board renderer without another serialization.
            ModifierBlueprint blueprint = modifier.Blueprint;
            blueprint.TickInterval = 1f;
            blueprint.TickEffects.Add(null);
            EffectNodeData valid = new EffectNodeData { OpId = AbilityEffectOps.Heal, Amount = 3f };
            blueprint.TickEffects.Add(valid);
            blueprint.EventReactions.Add(null);
            Assert.That(blueprint.TickEffects[0], Is.Null);
            Assert.That(blueprint.EventReactions[0], Is.Null);

            VisualElement board = RebuildModifierBoardWithoutSerialization();

            List<Label> cards = board.Query<Label>(className: "nad-card").ToList();
            Assert.That(cards.Count, Is.EqualTo(1));
            Assert.That(cards[0].text, Does.Contain("3 healing"));
            Assert.That(cards[0].ClassListContains("nad-card--heal"), Is.True);
            Assert.That(board.Query<VisualElement>(className: "nad-card--reaction").ToList(), Is.Empty);
            Assert.That(blueprint.TickEffects.Count, Is.EqualTo(2));
            Assert.That(blueprint.TickEffects[0], Is.Null, "Rendering must not replace or remove the null slot.");
            Assert.That(blueprint.TickEffects[1], Is.SameAs(valid));
            Assert.That(blueprint.EventReactions[0], Is.Null);
        }

        [Test]
        public void ModifierBoard_RendersNonNullZeroAmountNodesAfterSerializedRefresh()
        {
            ModifierDefinition modifier = NewAsset<ModifierDefinition>();
            modifier.Blueprint.Id = "audit_modifier";
            modifier.Blueprint.TickInterval = 1f;
            modifier.Blueprint.TickEffects.Add(new EffectNodeData());
            modifier.Blueprint.TickEffects.Add(new EffectNodeData { OpId = AbilityEffectOps.Heal, Amount = 3f });
            OpenWindow(modifier);

            VisualElement board = _window.rootVisualElement.Q<VisualElement>(className: "nad-mod-board");
            Assert.That(board, Is.Not.Null);
            List<Label> cards = board.Query<Label>(className: "nad-card").ToList();
            Assert.That(cards.Count, Is.EqualTo(2), "A nonnull zero-amount node is not a null entry.");
            Assert.That(cards[0].text, Does.Contain("0 magical damage"));
            Assert.That(cards[1].text, Does.Contain("3 healing"));
            Assert.That(modifier.Blueprint.TickEffects.Count, Is.EqualTo(2));
            Assert.That(modifier.Blueprint.TickEffects[0], Is.Not.Null);
            Assert.That(modifier.Blueprint.TickEffects[0].OpId, Is.EqualTo(AbilityEffectOps.Damage));
            Assert.That(modifier.Blueprint.TickEffects[0].Amount, Is.Zero);
        }

        [Test]
        public void ModifierBoard_MissingBlueprintShowsADiagnostic()
        {
            ModifierDefinition modifier = NewAsset<ModifierDefinition>();
            modifier.Blueprint.Id = "audit_modifier";
            OpenWindow(modifier);
            FieldInfo blueprint = typeof(ModifierDefinition).GetField("_blueprint", InstanceMembers);
            Assert.That(blueprint, Is.Not.Null);
            blueprint.SetValue(modifier, null);
            Assert.That(modifier.Blueprint, Is.Null);

            // A full refresh serializes _blueprint by value and materializes a placeholder. Test the
            // real managed null through the existing renderer and validation paths before that boundary.
            InvokeWindow("RefreshValidationAndLibrary");
            VisualElement board = RebuildModifierBoardWithoutSerialization();

            HelpBox diagnostic = board.Q<HelpBox>();
            Assert.That(diagnostic, Is.Not.Null);
            Assert.That(diagnostic.text, Is.EqualTo("Modifier blueprint is missing."));
            Assert.That(diagnostic.messageType, Is.EqualTo(HelpBoxMessageType.Error));
            Assert.That(board.Query<Label>(className: "nad-card").ToList(), Is.Empty);
            Assert.That(IssueMessages(), Has.Some.Contains("Blueprint is missing"));
            Assert.That(_window.rootVisualElement.Q<Label>(className: "nad-row-warn"), Is.Not.Null);
            Assert.That(modifier.Blueprint, Is.Null, "Rendering and validation must not repair the blueprint.");
        }

        [TestCase(AbilityEffectOps.Damage, 6f, 0f, 3f, true)]
        [TestCase(AbilityEffectOps.Damage, 6f, 3f, 0f, true)]
        [TestCase(AbilityEffectOps.Damage, 0f, 2f, 3f, false)]
        [TestCase(AbilityEffectOps.Chain, 6f, 0f, 3f, true)]
        [TestCase(AbilityEffectOps.Chain, 6f, 3f, 0f, true)]
        [TestCase(AbilityEffectOps.Chain, 0f, 2f, 3f, false)]
        public void RadiusValidation_UsesEveryOverridingLevel(string op, float flatRadius,
            float levelOne, float levelTwo, bool expectsIssue)
        {
            AbilityDefinition ability = NewAbility();
            ability.Blueprint.ImpactEffects.Add(new EffectNodeData
            {
                OpId = op,
                Target = op == AbilityEffectOps.Chain
                    ? EffectTargetSelector.Target
                    : EffectTargetSelector.AreaAroundCaster,
                Radius = flatRadius,
                RadiusByLevel = new float[] { levelOne, levelTwo }
            });
            OpenWindow(ability);

            List<string> messages = IssueMessages();
            Assert.That(messages.Exists(message => message.Contains("positive") && message.Contains("radius")),
                Is.EqualTo(expectsIssue));
            Assert.That(LeveledValueResolver.SampleByLevel(ability.Blueprint.ImpactEffects[0].RadiusByLevel,
                1, flatRadius), Is.EqualTo(levelOne));
        }

        [TestCase("DAMAGE", "17 pure damage", "nad-card--damage")]
        [TestCase("hEaL", "17 healing", "nad-card--heal")]
        [TestCase("APPLY_MODIFIER", "apply 'Burn'", "nad-card--modifier")]
        [TestCase("rEmOvE_mOdIfIeR", "remove 'Burn'", "nad-card--utility")]
        [TestCase("DISPEL", "dispel", "nad-card--utility")]
        [TestCase("ReSoUrCe_ChAnGe", "+17 mana", "nad-card--utility")]
        [TestCase("SPAWN", "spawn 'Wolf'", "nad-card--spawn")]
        [TestCase("KNOCKBACK", "knockback 17u", "nad-card--utility")]
        [TestCase("PuLl", "pull 17u", "nad-card--utility")]
        [TestCase("TELEPORT", "teleport", "nad-card--utility")]
        [TestCase("ExEcUtE", "execute 17", "nad-card--damage")]
        [TestCase("CHAIN", "chain 17 dmg × 2 hits", "nad-card--damage")]
        public void BuiltinCards_RecognizeMixedCaseWithoutRewritingIds(string opId,
            string expectedSummary, string expectedClass)
        {
            AbilitySystem system = new AbilitySystem();
            Assert.That(system.Ops.TryGet(opId, out IEffectOperation operation), Is.True);
            Assert.That(string.Equals(operation.Id, opId, StringComparison.OrdinalIgnoreCase), Is.True);

            AbilityDefinition ability = NewAbility();
            ability.Blueprint.ImpactEffects.Add(new EffectNodeData
            {
                OpId = opId,
                Amount = 17f,
                Radius = 3f,
                MaxTargets = 2,
                DamageType = AbilityDamageTypes.Pure,
                ModifierId = "Burn",
                ResourceId = "mana",
                ArchetypeId = "Wolf"
            });
            ModifierDefinition modifier = NewAsset<ModifierDefinition>();
            modifier.Blueprint.Id = "Burn";
            OpenWindow(ability);

            Assert.That(CardSummary(), Does.Contain(expectedSummary));
            VisualElement card = _window.rootVisualElement.Q<VisualElement>(className: "nad-card");
            Assert.That(card.ClassListContains(expectedClass), Is.True);
            Assert.That(_window.rootVisualElement.Q<Label>(className: "nad-card-op").text, Is.EqualTo(opId));
            Assert.That(IssueMessages(), Is.Empty);
            Assert.That(ability.Blueprint.ImpactEffects[0].OpId, Is.EqualTo(opId));
        }

        [TestCase("apply_modifier", "bUrN")]
        [TestCase("APPLY_MODIFIER", "burn")]
        [TestCase("ReMoVe_MoDiFiEr", "BURN")]
        public void ModifierReferences_AcceptRuntimeCaseVariantsWithoutRewriting(string opId, string modifierId)
        {
            ModifierDefinition modifier = NewAsset<ModifierDefinition>();
            modifier.Blueprint.Id = "BuRn";
            AbilitySystem system = new AbilitySystem();
            system.RegisterModifier(modifier.Blueprint);
            Assert.That(system.TryGetModifier(modifierId, out ModifierBlueprint resolved), Is.True);
            Assert.That(resolved, Is.SameAs(modifier.Blueprint));

            AbilityDefinition ability = NewAbility();
            ability.Blueprint.ImpactEffects.Add(new EffectNodeData { OpId = opId, ModifierId = modifierId });
            OpenWindow(ability);

            Assert.That(IssueMessages(), Is.Empty);
            Assert.That(CardSummary(), Does.Contain("'" + modifierId + "'"));
            Assert.That(modifier.Blueprint.Id, Is.EqualTo("BuRn"));
            Assert.That(ability.Blueprint.ImpactEffects[0].ModifierId, Is.EqualTo(modifierId));
            Assert.That(ability.Blueprint.ImpactEffects[0].OpId, Is.EqualTo(opId));
        }

        [TestCase("APPLY_MODIFIER", "")]
        [TestCase("rEmOvE_mOdIfIeR", "")]
        [TestCase("APPLY_MODIFIER", "Missing")]
        [TestCase("REMOVE_MODIFIER", "Missing")]
        public void ModifierOperations_ReportInvalidReferencesRegardlessOfCase(string opId, string modifierId)
        {
            AbilityDefinition ability = NewAbility();
            ability.Blueprint.ImpactEffects.Add(new EffectNodeData { OpId = opId, ModifierId = modifierId });
            OpenWindow(ability);

            List<string> messages = IssueMessages();
            Assert.That(messages.Count, Is.EqualTo(1));
            Assert.That(messages[0], Does.Contain(modifierId.Length == 0
                ? opId + " has no ModifierId."
                : "ModifierId 'Missing' does not match any ModifierDefinition."));
            Assert.That(ability.Blueprint.ImpactEffects[0].OpId, Is.EqualTo(opId));
            Assert.That(ability.Blueprint.ImpactEffects[0].ModifierId, Is.EqualTo(modifierId));
        }

        [Test]
        public void AbilityIds_DetectCaseVariantDuplicatesWithoutRewriting()
        {
            AbilityDefinition first = NewAbility();
            first.Blueprint.Id = "Fireball";
            first.Blueprint.ImpactEffects.Add(new EffectNodeData());
            AbilityDefinition second = NewAbility();
            second.Blueprint.Id = "fIrEbAlL";
            second.Blueprint.ImpactEffects.Add(new EffectNodeData());
            AbilitySystem system = new AbilitySystem();
            system.RegisterAbility(first.Blueprint);
            system.RegisterAbility(second.Blueprint);
            Assert.That(system.TryGetAbility("FIREBALL", out AbilityBlueprint resolved), Is.True);
            Assert.That(resolved, Is.SameAs(second.Blueprint), "Runtime catalog registration overwrites by id.");
            OpenWindow(second);

            List<string> messages = IssueMessages();
            Assert.That(messages.Count, Is.EqualTo(1));
            Assert.That(messages[0], Does.Contain("Duplicate ability id 'fIrEbAlL'"));
            Assert.That(first.Blueprint.Id, Is.EqualTo("Fireball"));
            Assert.That(second.Blueprint.Id, Is.EqualTo("fIrEbAlL"));
        }

        [Test]
        public void ModifierIds_DetectCaseVariantDuplicatesWithoutRewriting()
        {
            ModifierDefinition first = NewAsset<ModifierDefinition>();
            first.Blueprint.Id = "Burn";
            ModifierDefinition second = NewAsset<ModifierDefinition>();
            second.Blueprint.Id = "bUrN";
            AbilitySystem system = new AbilitySystem();
            system.RegisterModifier(first.Blueprint);
            system.RegisterModifier(second.Blueprint);
            Assert.That(system.TryGetModifier("BURN", out ModifierBlueprint resolved), Is.True);
            Assert.That(resolved, Is.SameAs(second.Blueprint));
            OpenWindow(second);

            List<string> messages = IssueMessages();
            Assert.That(messages.Count, Is.EqualTo(1));
            Assert.That(messages[0], Does.Contain("Duplicate modifier id 'bUrN'"));
            Assert.That(first.Blueprint.Id, Is.EqualTo("Burn"));
            Assert.That(second.Blueprint.Id, Is.EqualTo("bUrN"));
        }

        [TestCase(6f, 0f, true)]
        [TestCase(0f, 2f, false)]
        public void ChainCards_ValidateUppercaseHopRadius(float flatRadius, float levelOne, bool expectsIssue)
        {
            AbilityDefinition ability = NewAbility();
            ability.Blueprint.ImpactEffects.Add(new EffectNodeData
            {
                OpId = "CHAIN",
                Target = EffectTargetSelector.Target,
                Radius = flatRadius,
                RadiusByLevel = new float[] { levelOne, 3f },
                MaxTargets = 2
            });
            OpenWindow(ability);

            List<string> messages = IssueMessages();
            Assert.That(messages.Count, Is.EqualTo(expectsIssue ? 1 : 0));
            if (expectsIssue)
            {
                Assert.That(messages[0], Does.Contain("chain needs a positive hop radius"));
            }

            Assert.That(CardSummary(), Does.Contain("× 2 hits"));
            Assert.That(ability.Blueprint.ImpactEffects[0].OpId, Is.EqualTo("CHAIN"));
        }

        [TestCase("CHAIN_CUSTOM")]
        [TestCase("APPLY_MODIFIER_CUSTOM")]
        [TestCase(" HEAL ")]
        [TestCase("CuStOm.Spell")]
        public void UnknownOperations_KeepCustomSummaryAndSkipBuiltinOnlyChecks(string opId)
        {
            AbilitySystem system = new AbilitySystem();
            Assert.That(system.Ops.TryGet(opId, out IEffectOperation _), Is.False);
            AbilityDefinition ability = NewAbility();
            ability.Blueprint.ImpactEffects.Add(new EffectNodeData
            {
                OpId = opId,
                Target = EffectTargetSelector.Target,
                Radius = 0f,
                ModifierId = "Missing",
                CustomParam = "custom payload"
            });
            OpenWindow(ability);

            Assert.That(CardSummary(), Does.Contain("custom payload"));
            VisualElement card = _window.rootVisualElement.Q<VisualElement>(className: "nad-card");
            Assert.That(card.ClassListContains("nad-card--custom"), Is.True);
            Assert.That(IssueMessages(), Is.Empty);
            Assert.That(_window.rootVisualElement.Q<Label>(className: "nad-card-op").text, Is.EqualTo(opId));
            Assert.That(ability.Blueprint.ImpactEffects[0].OpId, Is.EqualTo(opId));
        }

        private AbilityDefinition NewAbility()
        {
            AbilityDefinition ability = NewAsset<AbilityDefinition>();
            ability.Blueprint.Id = "audit_ability";
            return ability;
        }

        private T NewAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            asset.name = "Audit20261023NeoAbilitiesEditor";
            _assets.Add(asset);
            return asset;
        }

        private void OpenWindow(ScriptableObject selected)
        {
            // Late binding avoids changing the shared test asmdef. Reflection only bridges the window's
            // private UI commands and its in-memory library; assertions observe authored data and UI output.
            Type windowType = Type.GetType("Neo.Abilities.Editor.AbilityDesignerWindow, Neo.Abilities.Editor", true);
            _window = (EditorWindow)ScriptableObject.CreateInstance(windowType);
            _window.Show();
            if (_window.rootVisualElement.childCount == 0)
            {
                InvokeWindow("CreateGUI");
            }

            List<AbilityDefinition> abilities = (List<AbilityDefinition>)windowType
                .GetField("_abilities", InstanceMembers).GetValue(_window);
            List<ModifierDefinition> modifiers = (List<ModifierDefinition>)windowType
                .GetField("_modifiers", InstanceMembers).GetValue(_window);
            abilities.Clear();
            modifiers.Clear();
            foreach (ScriptableObject asset in _assets)
            {
                if (asset is AbilityDefinition ability)
                {
                    abilities.Add(ability);
                }
                else if (asset is ModifierDefinition modifier)
                {
                    modifiers.Add(modifier);
                }
            }

            InvokeWindow("SelectAsset", selected);
            InvokeWindow("RefreshAfterEdit");
        }

        private void InvokeWindow(string name, params object[] arguments)
        {
            MethodInfo method = _window.GetType().GetMethod(name, InstanceMembers);
            Assert.That(method, Is.Not.Null, name);
            method.Invoke(_window, arguments);
        }

        private VisualElement RebuildModifierBoardWithoutSerialization()
        {
            // BuildModifierBoard appends to the retained center pane. Remove only its previous board
            // so neither a stale summary nor an unrelated Inspector element can satisfy the assertions.
            VisualElement previous = _window.rootVisualElement.Q<VisualElement>(className: "nad-mod-board");
            Assert.That(previous, Is.Not.Null);
            previous.RemoveFromHierarchy();
            InvokeWindow("BuildModifierBoard");

            List<VisualElement> boards = _window.rootVisualElement
                .Query<VisualElement>(className: "nad-mod-board").ToList();
            Assert.That(boards.Count, Is.EqualTo(1));
            Assert.That(boards[0], Is.Not.SameAs(previous));
            return boards[0];
        }

        private static void SendPropertyChange(VisualElement target, SerializedProperty property)
        {
            Assert.That(target, Is.Not.Null);
            using (SerializedPropertyChangeEvent change = SerializedPropertyChangeEvent.GetPooled(property))
            {
                change.target = target;
                target.SendEvent(change);
            }
        }

        private string CardSummary()
        {
            Label summary = _window.rootVisualElement.Q<Label>(className: "nad-card-summary");
            Assert.That(summary, Is.Not.Null);
            return summary.text;
        }

        private List<string> IssueMessages()
        {
            List<string> messages = new List<string>();
            _window.rootVisualElement.Query<Label>(className: "nad-issue-msg")
                .ForEach(label => messages.Add(label.text));
            return messages;
        }
    }
}
