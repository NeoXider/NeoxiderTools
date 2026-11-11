using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Abilities.Editor
{
    /// <summary>
    ///     Finds ability wiring that fails silently at runtime: an auto-caster, a cooldown source or a NoCode action
    ///     that names an ability nothing in the scene registers, an effect that asks for a spawn archetype the hub
    ///     does not map, a payload ability or a modifier that does not exist, and a cue no cue player knows.
    /// </summary>
    public static class AbilitySceneValidator
    {
        private const string MenuPath = "Neoxider/Abilities/Validate Open Scenes";

        [MenuItem(MenuPath, false, 20)]
        private static void ValidateOpenScenes()
        {
            int total = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                List<string> issues = Validate(scene);
                total += issues.Count;
                for (int j = 0; j < issues.Count; j++)
                {
                    Debug.LogWarning("[AbilitySceneValidator] " + scene.name + ": " + issues[j]);
                }
            }

            Debug.Log("[AbilitySceneValidator] " + (total == 0 ? "No ability wiring issues." : total + " issue(s) found."));
        }

        /// <summary>Checks one loaded scene. Returns one line per problem; an empty list means the wiring resolves.</summary>
        public static List<string> Validate(Scene scene)
        {
            List<string> issues = new List<string>();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return issues;
            }

            List<AbilitySystemBehaviour> hubs = Collect<AbilitySystemBehaviour>(scene);
            List<AbilityUnitBehaviour> units = Collect<AbilityUnitBehaviour>(scene);
            List<AbilityAutoCaster> autoCasters = Collect<AbilityAutoCaster>(scene);
            List<AbilityCooldownSource> cooldownSources = Collect<AbilityCooldownSource>(scene);
            List<AbilityNoCodeAction> actions = Collect<AbilityNoCodeAction>(scene);
            List<AbilityCuePlayer> cuePlayers = Collect<AbilityCuePlayer>(scene);
            if (hubs.Count == 0)
            {
                if (units.Count + autoCasters.Count + actions.Count > 0)
                {
                    issues.Add("The scene has ability components but no AbilitySystemBehaviour.");
                }

                return issues;
            }

            Dictionary<string, AbilityBlueprint> abilities =
                new Dictionary<string, AbilityBlueprint>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, ModifierBlueprint> modifiers =
                new Dictionary<string, ModifierBlueprint>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> archetypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> cues = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < hubs.Count; i++)
            {
                SerializedObject hub = new SerializedObject(hubs[i]);
                SerializedProperty libraries = hub.FindProperty("_libraries");
                for (int j = 0; libraries != null && j < libraries.arraySize; j++)
                {
                    AbilityLibrary library = libraries.GetArrayElementAtIndex(j).objectReferenceValue as AbilityLibrary;
                    if (library == null)
                    {
                        issues.Add(Path(hubs[i]) + ": library slot " + j + " is empty.");
                        continue;
                    }

                    AddAbilities(library.Abilities, abilities);
                    AddModifiers(library.Modifiers, modifiers);
                }

                SerializedProperty entries = hub.FindProperty("_archetypes");
                for (int j = 0; entries != null && j < entries.arraySize; j++)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(j);
                    string id = entry.FindPropertyRelative("Id").stringValue;
                    if (entry.FindPropertyRelative("Prefab").objectReferenceValue == null)
                    {
                        issues.Add(Path(hubs[i]) + ": archetype '" + id + "' has no prefab.");
                    }

                    if (!string.IsNullOrEmpty(id))
                    {
                        archetypes.Add(id);
                    }
                }
            }

            for (int i = 0; i < units.Count; i++)
            {
                UnitTemplate template = units[i].Template;
                if (template != null)
                {
                    AddAbilities(template.Abilities, abilities);
                    AddModifiers(template.InnateModifiers, modifiers);
                }
            }

            for (int i = 0; i < actions.Count; i++)
            {
                SerializedObject action = new SerializedObject(actions[i]);
                AbilityDefinition ability = action.FindProperty("_ability").objectReferenceValue as AbilityDefinition;
                ModifierDefinition modifier = action.FindProperty("_modifier").objectReferenceValue as ModifierDefinition;
                if (ability != null && !string.IsNullOrEmpty(ability.Id))
                {
                    abilities[ability.Id] = ability.Blueprint;
                }

                if (modifier != null && !string.IsNullOrEmpty(modifier.Id))
                {
                    modifiers[modifier.Id] = modifier.Blueprint;
                }
            }

            for (int i = 0; i < cuePlayers.Count; i++)
            {
                IReadOnlyList<AbilityCuePlayer.Cue> list = cuePlayers[i].Cues;
                for (int j = 0; j < list.Count; j++)
                {
                    if (list[j] != null && !string.IsNullOrEmpty(list[j].Id))
                    {
                        cues.Add(list[j].Id);
                    }
                }
            }

            for (int i = 0; i < autoCasters.Count; i++)
            {
                SerializedProperty ids = new SerializedObject(autoCasters[i]).FindProperty("_abilityIds");
                for (int j = 0; ids != null && j < ids.arraySize; j++)
                {
                    string id = ids.GetArrayElementAtIndex(j).stringValue;
                    if (!string.IsNullOrEmpty(id) && !abilities.ContainsKey(id))
                    {
                        issues.Add(Path(autoCasters[i]) + ": auto-caster lists the unknown ability '" + id + "'.");
                    }
                }
            }

            for (int i = 0; i < cooldownSources.Count; i++)
            {
                string id = cooldownSources[i].AbilityId;
                if (!string.IsNullOrEmpty(id) && !abilities.ContainsKey(id))
                {
                    issues.Add(Path(cooldownSources[i]) + ": cooldown source reads the unknown ability '" + id + "'.");
                }
            }

            for (int i = 0; i < actions.Count; i++)
            {
                CheckAction(actions[i], abilities, modifiers, issues);
            }

            foreach (KeyValuePair<string, AbilityBlueprint> pair in abilities)
            {
                AbilityBlueprint blueprint = pair.Value;
                string owner = "Ability '" + pair.Key + "'";
                if (!string.IsNullOrEmpty(blueprint.ProjectileArchetypeId) &&
                    !archetypes.Contains(blueprint.ProjectileArchetypeId))
                {
                    issues.Add(owner + ": projectile archetype '" + blueprint.ProjectileArchetypeId +
                               "' is not mapped by the ability hub.");
                }

                CheckCue(owner, "Cast Cue", blueprint.CastCue, cues, issues);
                CheckCue(owner, "Impact Cue", blueprint.ImpactCue, cues, issues);
                CheckNodes(owner, blueprint.CastEffects, abilities, modifiers, archetypes, issues);
                CheckNodes(owner, blueprint.ImpactEffects, abilities, modifiers, archetypes, issues);
            }

            foreach (KeyValuePair<string, ModifierBlueprint> pair in modifiers)
            {
                CheckNodes("Modifier '" + pair.Key + "'", pair.Value.TickEffects, abilities, modifiers, archetypes, issues);
            }

            return issues;
        }

        private static void CheckAction(AbilityNoCodeAction action, Dictionary<string, AbilityBlueprint> abilities,
            Dictionary<string, ModifierBlueprint> modifiers, List<string> issues)
        {
            SerializedObject serialized = new SerializedObject(action);
            AbilityNoCodeActionType type = (AbilityNoCodeActionType)serialized.FindProperty("_actionType").enumValueIndex;
            bool needsAbility = type == AbilityNoCodeActionType.CastById || type == AbilityNoCodeActionType.CastAtUnit ||
                                type == AbilityNoCodeActionType.CastAtSelf ||
                                type == AbilityNoCodeActionType.GrantAbility ||
                                type == AbilityNoCodeActionType.RevokeAbility ||
                                type == AbilityNoCodeActionType.SetAbilityLevel ||
                                type == AbilityNoCodeActionType.LevelUpAbility;
            bool needsModifier = type == AbilityNoCodeActionType.ApplyModifier ||
                                 type == AbilityNoCodeActionType.RemoveModifier;
            if (needsAbility && serialized.FindProperty("_ability").objectReferenceValue == null)
            {
                string id = serialized.FindProperty("_abilityId").stringValue;
                if (string.IsNullOrEmpty(id))
                {
                    issues.Add(Path(action) + ": " + type + " has neither an ability asset nor an ability id.");
                }
                else if (!abilities.ContainsKey(id))
                {
                    issues.Add(Path(action) + ": " + type + " names the unknown ability '" + id + "'.");
                }
            }

            if (needsModifier && serialized.FindProperty("_modifier").objectReferenceValue == null)
            {
                string id = serialized.FindProperty("_modifierId").stringValue;
                if (string.IsNullOrEmpty(id))
                {
                    issues.Add(Path(action) + ": " + type + " has neither a modifier asset nor a modifier id.");
                }
                else if (!modifiers.ContainsKey(id))
                {
                    issues.Add(Path(action) + ": " + type + " names the unknown modifier '" + id + "'.");
                }
            }
        }

        private static void CheckNodes(string owner, List<EffectNodeData> nodes,
            Dictionary<string, AbilityBlueprint> abilities, Dictionary<string, ModifierBlueprint> modifiers,
            HashSet<string> archetypes, List<string> issues)
        {
            for (int i = 0; nodes != null && i < nodes.Count; i++)
            {
                EffectNodeData node = nodes[i];
                if (node == null)
                {
                    continue;
                }

                string at = owner + ", effect " + i + " (" + node.OpId + ")";
                if (!string.IsNullOrEmpty(node.ArchetypeId) && !archetypes.Contains(node.ArchetypeId))
                {
                    issues.Add(at + ": archetype '" + node.ArchetypeId + "' is not mapped by the ability hub.");
                }

                if (!string.IsNullOrEmpty(node.AbilityId) && !abilities.ContainsKey(node.AbilityId))
                {
                    issues.Add(at + ": payload ability '" + node.AbilityId + "' is not registered.");
                }

                if (!string.IsNullOrEmpty(node.ModifierId) && !modifiers.ContainsKey(node.ModifierId))
                {
                    issues.Add(at + ": modifier '" + node.ModifierId + "' is not registered.");
                }
            }
        }

        private static void CheckCue(string owner, string field, string cue, HashSet<string> cues, List<string> issues)
        {
            if (!string.IsNullOrEmpty(cue) && !cues.Contains(cue))
            {
                issues.Add(owner + ": " + field + " '" + cue + "' is not played by any AbilityCuePlayer in the scene.");
            }
        }

        private static void AddAbilities(IReadOnlyList<AbilityDefinition> source,
            Dictionary<string, AbilityBlueprint> target)
        {
            for (int i = 0; source != null && i < source.Count; i++)
            {
                if (source[i] != null && !string.IsNullOrEmpty(source[i].Id))
                {
                    target[source[i].Id] = source[i].Blueprint;
                }
            }
        }

        private static void AddModifiers(IReadOnlyList<ModifierDefinition> source,
            Dictionary<string, ModifierBlueprint> target)
        {
            for (int i = 0; source != null && i < source.Count; i++)
            {
                if (source[i] != null && !string.IsNullOrEmpty(source[i].Id))
                {
                    target[source[i].Id] = source[i].Blueprint;
                }
            }
        }

        private static List<T> Collect<T>(Scene scene) where T : Component
        {
            List<T> found = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                found.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return found;
        }

        private static string Path(Component component)
        {
            Transform current = component.transform;
            string path = current.name;
            while (current.parent != null)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }

            return path;
        }
    }
}
