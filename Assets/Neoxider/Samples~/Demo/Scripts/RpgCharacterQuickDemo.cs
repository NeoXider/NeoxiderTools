using Neo.Rpg.Components;
using UnityEngine;

namespace Neo.Rpg.Demo
{
    /// <summary>
    ///     Self-contained smoke demo for RpgCharacter.
    ///     Open the scene, press Play, then use the on-screen buttons to exercise the universal API.
    /// </summary>
    [AddComponentMenu("Neoxider/Demo/" + nameof(RpgCharacterQuickDemo))]
    public sealed class RpgCharacterQuickDemo : MonoBehaviour
    {
        [SerializeField] private RpgCharacter _player;
        [SerializeField] private RpgCharacter _enemy;
        [SerializeField] private bool _configureOnAwake = true;
        [SerializeField] private string _customManaId = "DarkMana";

        private RpgCharacterTemplate _playerTemplate;
        private RpgCharacterTemplate _enemyTemplate;
        private RpgProgressionDefinition _progression;

        private void Awake()
        {
            EnsureCharacters();
            if (_configureOnAwake) ConfigureCharacters();
        }

        private void OnDestroy()
        {
            DestroyRuntimeAsset(_playerTemplate);
            DestroyRuntimeAsset(_enemyTemplate);
            DestroyRuntimeAsset(_progression);
        }

        private void OnGUI()
        {
            if (_player == null || _enemy == null) return;

            const int width = 360;
            GUILayout.BeginArea(new Rect(16, 16, width, 520), GUI.skin.box);
            GUILayout.Label("RpgCharacter Quick Demo");
            GUILayout.Space(6);

            DrawCharacter("Player", _player);
            GUILayout.Space(6);
            DrawCharacter("Enemy", _enemy);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Damage Player 25")) _player.Damage(25f);
            if (GUILayout.Button("Heal Player 15")) _player.Heal(15f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spend Stamina 30")) _player.Spend("Stamina", 30f);
            if (GUILayout.Button("Refill DarkMana 15")) _player.Refill(_customManaId, 15f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Level")) _player.AddLevel(1);
            if (GUILayout.Button("Upgrade Vitality"))
            {
                _player.AddUpgradePoints(1);
                _player.UpgradeStat(nameof(RpgStatPreset.Vitality));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Damage Enemy 40")) _enemy.DamageType("Fire", 40f);
            if (GUILayout.Button("Restore All")) { _player.Restore(); _enemy.Restore(); }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("Inspector/NoCode checks:");
            GUILayout.Label("- RpgCharacter public API works offline and routes to server when networked.");
            GUILayout.Label("- Resources use preset IDs or custom strings.");
            GUILayout.Label("- Upgrade Vitality increases stat and Max HP.");
            GUILayout.EndArea();
        }

        private static void DrawCharacter(string label, RpgCharacter character)
        {
            GUILayout.Label($"{label}: HP {character.HpValue:0}/{character.MaxHpValue:0} | " +
                            $"Stamina {character.GetResource("Stamina"):0}/{character.GetResourceMax("Stamina"):0} | " +
                            $"DarkMana {character.GetResource("DarkMana"):0}/{character.GetResourceMax("DarkMana"):0}");
            GUILayout.Label($"Level {character.LevelValue} | XP {character.XpValue:0} | " +
                            $"Upgrade Points {character.UpgradePointsValue} | " +
                            $"Vitality {character.GetStat(nameof(RpgStatPreset.Vitality)):0}");
        }

        private void EnsureCharacters()
        {
            if (_player == null) _player = FindOrCreate("Rpg Demo Player", PrimitiveType.Capsule, new Vector3(-1.4f, 1.5f, -3.2f));
            if (_enemy == null) _enemy = FindOrCreate("Rpg Demo Enemy", PrimitiveType.Cube, new Vector3(1.4f, 1f, -3.2f));
        }

        private static RpgCharacter FindOrCreate(string objectName, PrimitiveType primitive, Vector3 position)
        {
            GameObject existing = GameObject.Find(objectName);
            GameObject go = existing != null ? existing : GameObject.CreatePrimitive(primitive);
            go.name = objectName;
            go.transform.position = position;
            if (go.TryGetComponent(out RpgCharacter character)) return character;
            character = go.AddComponent<RpgCharacter>();
            character.enabled = false;
            return character;
        }

        private void ConfigureCharacters()
        {
            _progression = ScriptableObject.CreateInstance<RpgProgressionDefinition>();
            _progression.growthMode = RpgLevelGrowthMode.Hybrid;
            _progression.upgradePointsPerLevel = 1;
            _progression.autoApplyGrowthOnLevelUp = true;
            _progression.upgradeRules = new[]
            {
                new RpgStatUpgradeRule
                {
                    statId = new RpgStatId(RpgStatPreset.Vitality),
                    increasePerPoint = 1f,
                    costPerUpgrade = 1,
                    maxUpgradeCount = -1,
                    derivedResourceModifiers = new[]
                    {
                        new RpgResourceModifier
                        {
                            resourceId = new RpgStatId(RpgStatPreset.Hp),
                            kind = RpgResourceModifierKind.AddMaxFlat,
                            value = 15f
                        }
                    }
                }
            };

            _playerTemplate = BuildTemplate("Demo Player", 100f, 100f, 40f, 12f, 10f);
            _enemyTemplate = BuildTemplate("Demo Enemy", 80f, 40f, 0f, 8f, 6f);

            _player.ApplyTemplate(_playerTemplate);
            _enemy.ApplyTemplate(_enemyTemplate);
        }

        private RpgCharacterTemplate BuildTemplate(
            string displayName,
            float hp,
            float stamina,
            float darkMana,
            float strength,
            float vitality)
        {
            RpgCharacterTemplate template = ScriptableObject.CreateInstance<RpgCharacterTemplate>();
            template.displayName = displayName;
            template.resources = new[]
            {
                Resource(RpgStatPreset.Hp, hp, hp, regen: 1f),
                Resource(RpgStatPreset.Stamina, stamina, stamina, regen: 15f, pauseAfterSpend: 1f),
                Resource(_customManaId, darkMana, darkMana, regen: 2f)
            };
            template.stats = new[]
            {
                Stat(RpgStatPreset.Strength, strength, affectedByLevel: true),
                Stat(RpgStatPreset.Vitality, vitality, affectedByLevel: false),
                Stat(RpgStatPreset.Defense, 5f, affectedByLevel: true)
            };
            template.progression = _progression;
            return template;
        }

        private static RpgResourceDefinition Resource(
            RpgStatPreset preset,
            float current,
            float max,
            float regen = 0f,
            float pauseAfterSpend = 0f)
        {
            return Resource(new RpgStatId(preset), current, max, regen, pauseAfterSpend);
        }

        private static RpgResourceDefinition Resource(
            string customId,
            float current,
            float max,
            float regen = 0f,
            float pauseAfterSpend = 0f)
        {
            return Resource(new RpgStatId(customId), current, max, regen, pauseAfterSpend);
        }

        private static RpgResourceDefinition Resource(
            RpgStatId id,
            float current,
            float max,
            float regen,
            float pauseAfterSpend)
        {
            return new RpgResourceDefinition
            {
                id = id,
                startCurrent = current,
                startMax = max,
                restoreOnAwake = true,
                restoreToFull = false,
                regen = new RpgRegenDefinition
                {
                    enabled = regen > 0f,
                    mode = RpgRegenMode.FlatPerSecond,
                    value = regen,
                    pauseAfterSpend = pauseAfterSpend > 0f,
                    pauseAfterSpendSeconds = pauseAfterSpend
                }
            };
        }

        private static RpgStatDefinition Stat(RpgStatPreset preset, float value, bool affectedByLevel)
        {
            return new RpgStatDefinition
            {
                id = new RpgStatId(preset),
                baseValue = value,
                affectedByLevel = affectedByLevel,
                growth = new RpgStatGrowthRule { BaseValue = 0f, AddPerLevel = 1f }
            };
        }

        private static void DestroyRuntimeAsset(Object asset)
        {
            if (asset == null) return;
            if (Application.isPlaying) Destroy(asset);
            else DestroyImmediate(asset);
        }
    }
}
