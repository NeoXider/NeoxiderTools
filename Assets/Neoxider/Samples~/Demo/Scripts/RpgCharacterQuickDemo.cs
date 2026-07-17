using System;
using Neo.Rpg.Components;
using Neo.Samples.Survivor;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.Rpg.Demo
{
    /// <summary>
    ///     Self-contained smoke demo for RpgCharacter.
    ///     Open the scene, press Play, then use the on-screen buttons to exercise the universal API.
    ///     The control panel is built in code with uGUI (no prefabs, no IMGUI).
    /// </summary>
    [AddComponentMenu("Neoxider/Demo/" + nameof(RpgCharacterQuickDemo))]
    public sealed class RpgCharacterQuickDemo : MonoBehaviour
    {
        private const float PanelWidth = 430f;
        private const int SortingOrder = 25000;

        [SerializeField] private RpgCharacter _player;
        [SerializeField] private RpgCharacter _enemy;
        [SerializeField] private bool _configureOnAwake = true;
        [SerializeField] private string _customManaId = "DarkMana";

        private RpgCharacterTemplate _playerTemplate;
        private RpgCharacterTemplate _enemyTemplate;
        private RpgProgressionDefinition _progression;

        private TMP_Text _playerResources;
        private TMP_Text _playerProgress;
        private TMP_Text _enemyResources;
        private TMP_Text _enemyProgress;

        private void Awake()
        {
            EnsureCharacters();
            if (_configureOnAwake) ConfigureCharacters();
        }

        private void Start()
        {
            BuildUi();
        }

        private void Update()
        {
            if (_player == null || _enemy == null || _playerResources == null) return;

            // WHY: labels refresh every frame to mirror the old immediate-mode readout (regen ticks live).
            RefreshCharacter(_playerResources, _playerProgress, "Player", _player);
            RefreshCharacter(_enemyResources, _enemyProgress, "Enemy", _enemy);
        }

        private void OnDestroy()
        {
            DestroyRuntimeAsset(_playerTemplate);
            DestroyRuntimeAsset(_enemyTemplate);
            DestroyRuntimeAsset(_progression);
        }

        private void BuildUi()
        {
            if (_player == null || _enemy == null) return;

            EnsureEventSystem();

            var canvasGo = new GameObject("[RpgCharacterQuickDemo] UI", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            RectTransform panel = BuildPanel(canvas.transform);
            BuildTitle(panel);

            (_playerResources, _playerProgress) = BuildCharacterBlock(panel, "PLAYER", SurvivorUI.Good);
            (_enemyResources, _enemyProgress) = BuildCharacterBlock(panel, "ENEMY", SurvivorUI.Danger);

            AddButtonRow(panel,
                ("Damage Player 25", () => _player.Damage(25f)),
                ("Heal Player 15", () => _player.Heal(15f)));
            AddButtonRow(panel,
                ("Spend Stamina 30", () => _player.Spend("Stamina", 30f)),
                ("Refill DarkMana 15", () => _player.Refill(_customManaId, 15f)));
            AddButtonRow(panel,
                ("Add Level", () => _player.AddLevel(1)),
                ("Upgrade Vitality", UpgradeVitality));
            AddButtonRow(panel,
                ("Damage Enemy 40", () => _enemy.DamageType("Fire", 40f)),
                ("Restore All", RestoreAll));

            BuildChecklist(panel);
        }

        private void UpgradeVitality()
        {
            _player.AddUpgradePoints(1);
            _player.UpgradeStat(nameof(RpgStatPreset.Vitality));
        }

        private void RestoreAll()
        {
            _player.Restore();
            _enemy.Restore();
        }

        /// <summary>Compact rounded corner card (top-left) that auto-sizes to its rows.</summary>
        private static RectTransform BuildPanel(Transform parent)
        {
            Image card = SurvivorUI.Image("Panel", parent, new Color(0.11f, 0.12f, 0.17f, 0.96f));
            RectTransform rt = card.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, -16f);
            rt.sizeDelta = new Vector2(PanelWidth, 0f);

            var shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
            shadow.effectDistance = new Vector2(0f, -4f);

            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            var fitter = card.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rt;
        }

        private static void BuildTitle(RectTransform panel)
        {
            TMP_Text title = SurvivorUI.Label("Title", panel, "RpgCharacter Quick Demo", 22f,
                SurvivorUI.Text, TextAlignmentOptions.Left, FontStyles.Bold);
            title.raycastTarget = false;

            Image underline = SurvivorUI.Image("Underline", panel, SurvivorUI.Accent);
            underline.raycastTarget = false;
            var line = underline.gameObject.AddComponent<LayoutElement>();
            line.minHeight = 3f;
            line.preferredHeight = 3f;
        }

        /// <summary>Accent header + two live stat lines for one character; returns the lines to update.</summary>
        private static (TMP_Text resources, TMP_Text progress) BuildCharacterBlock(
            RectTransform panel, string header, Color accent)
        {
            TMP_Text head = SurvivorUI.Label("Header_" + header, panel, header, 14f, accent,
                TextAlignmentOptions.Left, FontStyles.Bold);
            head.characterSpacing = 6f;

            TMP_Text resources = SurvivorUI.Label("Resources", panel, "-", 15f, SurvivorUI.Text);
            TMP_Text progress = SurvivorUI.Label("Progress", panel, "-", 15f, SurvivorUI.Muted);
            return (resources, progress);
        }

        private static void RefreshCharacter(TMP_Text resources, TMP_Text progress, string label,
            RpgCharacter character)
        {
            resources.text = $"{label}: HP {character.HpValue:0}/{character.MaxHpValue:0} | " +
                             $"Stamina {character.GetResource("Stamina"):0}/{character.GetResourceMax("Stamina"):0} | " +
                             $"DarkMana {character.GetResource("DarkMana"):0}/{character.GetResourceMax("DarkMana"):0}";
            progress.text = $"Level {character.LevelValue} | XP {character.XpValue:0} | " +
                            $"Upgrade Points {character.UpgradePointsValue} | " +
                            $"Vitality {character.GetStat(nameof(RpgStatPreset.Vitality)):0}";
        }

        private static void AddButtonRow(RectTransform panel, params (string label, Action onClick)[] buttons)
        {
            RectTransform row = SurvivorUI.Rect("Row", panel);
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 42f;
            element.preferredHeight = 42f;

            var group = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = 8f;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;

            foreach ((string label, Action onClick) in buttons)
            {
                Button btn = SurvivorUI.Button("Btn_" + label, row, SurvivorUI.Accent);
                TMP_Text txt = SurvivorUI.Label("Label", btn.transform, label, 16f, SurvivorUI.Ink,
                    TextAlignmentOptions.Center, FontStyles.Bold);
                SurvivorUI.Stretch(txt.rectTransform, 2f);
                Action click = onClick;
                btn.onClick.AddListener(() => click?.Invoke());
            }
        }

        private static void BuildChecklist(RectTransform panel)
        {
            TMP_Text head = SurvivorUI.Label("ChecksHeader", panel, "Inspector/NoCode checks:", 14f,
                SurvivorUI.Muted, TextAlignmentOptions.Left, FontStyles.Bold);
            head.raycastTarget = false;

            TMP_Text body = SurvivorUI.Label("ChecksBody", panel,
                "- RpgCharacter public API works offline and routes to server when networked.\n" +
                "- Resources use preset IDs or custom strings.\n" +
                "- Upgrade Vitality increases stat and Max HP.",
                13f, SurvivorUI.Muted);
            body.raycastTarget = false;
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            Type moduleType = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (moduleType != null)
            {
                go.AddComponent(moduleType);
            }
            else
            {
                go.AddComponent<StandaloneInputModule>();
            }
#else
            go.AddComponent<StandaloneInputModule>();
#endif
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

        private static void DestroyRuntimeAsset(UnityEngine.Object asset)
        {
            if (asset == null) return;
            if (Application.isPlaying) Destroy(asset);
            else DestroyImmediate(asset);
        }
    }
}
