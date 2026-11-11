using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Neo.Cards.Editor
{
    /// <summary>
    ///     Custom inspector for <see cref="DeckConfig" /> with sprite previews.
    /// </summary>
    [CustomEditor(typeof(DeckConfig))]
    public class DeckConfigEditor : UnityEditor.Editor
    {
        private const float PreviewSize = 60f;
        private const float PreviewSpacing = 5f;
        private const int MaxPreviewsPerRow = 7;
        private SerializedProperty _backSprite;
        private SerializedProperty _blackJoker;
        private SerializedProperty _clubs;
        private SerializedProperty _customCards;

        private SerializedProperty _deckType;
        private SerializedProperty _diamonds;
        private SerializedProperty _gameDeckType;
        private SerializedProperty _hearts;
        private SerializedProperty _redJoker;
        private bool _showClubs = true;
        private bool _showDiamonds = true;

        private bool _showHearts = true;
        private bool _showJokers = true;
        private bool _showSpades = true;
        private bool _showValidation = true;
        private SerializedProperty _spades;
        private SerializedProperty _useCustomDeck;

        private static readonly GUIContent CustomCardsContent = new GUIContent("Custom Cards");
        private static readonly GUIContent UseCustomDeckContent = new GUIContent("Use Custom Deck");

        private static readonly GUIContent SpriteDeckTypeContent =
            new GUIContent("Sprite deck type", "How many sprites are stored in this asset");

        private static readonly GUIContent PlayDeckTypeContent =
            new GUIContent("Play deck type", "How many cards gameplay uses");

        private static readonly GUIContent ValidateButtonContent = new GUIContent("Validate configuration",
            "Re-runs the check now, e.g. after sprites were deleted or re-imported.");

        private static GUIStyle s_headerStyle;
        private static GUIStyle s_suitFoldoutStyle;
        private static GUIStyle s_missingSpriteStyle;
        private static bool s_stylesProSkin;

        // WHY: DeckConfig.Validate builds a fresh message list on every call, and an inspector over a 52-card
        // deck with empty slots used to rebuild dozens of strings on every repaint. The result is kept until
        // the asset's dirty count moves, an undo/redo happens or the project changes (a deleted sprite turns
        // a reference into "missing" without touching this asset). It is only refreshed on the Layout pass so
        // Layout and Repaint always see the same number of help boxes.
        private readonly List<string> _validationErrors = new List<string>();
        private readonly List<string> _validationWarnings = new List<string>();
        private bool _validationValid;
        private bool _validationStale = true;
        private int _validationDirtyCount = -1;

        private void OnEnable()
        {
            EditorApplication.projectChanged -= InvalidateValidation;
            EditorApplication.projectChanged += InvalidateValidation;
            Undo.undoRedoPerformed -= InvalidateValidation;
            Undo.undoRedoPerformed += InvalidateValidation;
            _validationStale = true;

            _deckType = serializedObject.FindProperty("_deckType");
            _gameDeckType = serializedObject.FindProperty("_gameDeckType");
            _backSprite = serializedObject.FindProperty("_backSprite");
            _hearts = serializedObject.FindProperty("_hearts");
            _diamonds = serializedObject.FindProperty("_diamonds");
            _clubs = serializedObject.FindProperty("_clubs");
            _spades = serializedObject.FindProperty("_spades");
            _redJoker = serializedObject.FindProperty("_redJoker");
            _blackJoker = serializedObject.FindProperty("_blackJoker");
            _useCustomDeck = serializedObject.FindProperty("_useCustomDeck");
            _customCards = serializedObject.FindProperty("_customCards");
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= InvalidateValidation;
            Undo.undoRedoPerformed -= InvalidateValidation;
        }

        private void InvalidateValidation()
        {
            _validationStale = true;
        }

        public override void OnInspectorGUI()
        {
            if (s_stylesProSkin != EditorGUIUtility.isProSkin)
            {
                // WHY: the cached style copies bake the skin's textures and colours; rebuild them after a switch.
                s_stylesProSkin = EditorGUIUtility.isProSkin;
                s_headerStyle = null;
                s_suitFoldoutStyle = null;
                s_missingSpriteStyle = null;
            }

            serializedObject.Update();

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawDeckSettings();
            EditorGUILayout.Space(10);

            if (!_useCustomDeck.boolValue)
            {
                DrawAutoFill();
                EditorGUILayout.Space(10);
            }

            DrawBackSprite();
            EditorGUILayout.Space(10);

            if (_useCustomDeck.boolValue)
            {
                EditorGUILayout.PropertyField(_customCards, CustomCardsContent, true);
            }
            else
            {
                DrawSuitSection("♥ Hearts", _hearts, true, ref _showHearts);
                DrawSuitSection("♦ Diamonds", _diamonds, true, ref _showDiamonds);
                DrawSuitSection("♣ Clubs", _clubs, false, ref _showClubs);
                DrawSuitSection("♠ Spades", _spades, false, ref _showSpades);

                EditorGUILayout.Space(10);
                DrawJokers();
            }

            EditorGUILayout.Space(15);
            serializedObject.ApplyModifiedProperties();
            DrawValidation();
        }

        public override bool RequiresConstantRepaint()
        {
            return AssetPreview.IsLoadingAssetPreviews();
        }

        private new void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            s_headerStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Deck Configuration", s_headerStyle);
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        ///     Suit title colour that stays readable on both editor skins. The old black tint for clubs and
        ///     spades turned the title black-on-charcoal in the dark skin.
        /// </summary>
        private static Color SuitTitleColor(bool redSuit)
        {
            bool dark = EditorGUIUtility.isProSkin;
            if (redSuit)
            {
                return dark ? new Color(1f, 0.47f, 0.47f, 1f) : new Color(0.70f, 0.08f, 0.10f, 1f);
            }

            return dark ? new Color(0.86f, 0.88f, 0.92f, 1f) : new Color(0.10f, 0.10f, 0.12f, 1f);
        }

        private static GUIStyle SuitFoldoutStyle(Color color)
        {
            s_suitFoldoutStyle ??= new GUIStyle(EditorStyles.foldoutHeader);
            // WHY: GUI.contentColor multiplies the skin's own text colour, so a tint can never brighten dark
            // text or darken light text. Setting the style's text colour gives the exact colour in every state.
            s_suitFoldoutStyle.normal.textColor = color;
            s_suitFoldoutStyle.onNormal.textColor = color;
            s_suitFoldoutStyle.hover.textColor = color;
            s_suitFoldoutStyle.onHover.textColor = color;
            s_suitFoldoutStyle.focused.textColor = color;
            s_suitFoldoutStyle.onFocused.textColor = color;
            s_suitFoldoutStyle.active.textColor = color;
            s_suitFoldoutStyle.onActive.textColor = color;
            return s_suitFoldoutStyle;
        }

        private void DrawDeckSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Deck settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_useCustomDeck, UseCustomDeckContent);
            if (_useCustomDeck.boolValue)
            {
                EditorGUILayout.HelpBox("GenerateDeck() uses Custom Cards. Standard suit settings are preserved.",
                    MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.PropertyField(_deckType, SpriteDeckTypeContent);

            int expectedCount = GetExpectedCardCount();
            EditorGUILayout.HelpBox($"Expected {expectedCount} sprites per suit", MessageType.Info);

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(_gameDeckType, PlayDeckTypeContent);

            DeckType spriteType = (DeckType)_deckType.intValue;
            DeckType gameType = (DeckType)_gameDeckType.intValue;

            int gameCardCount = GetGameCardCount(gameType);
            string gameInfo = gameType == DeckType.Standard54
                ? $"In play: {gameCardCount} cards (52 + 2 jokers)"
                : $"In play: {gameCardCount} cards";

            if (IsGameTypeValid(spriteType, gameType))
            {
                EditorGUILayout.HelpBox(gameInfo, MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"GameDeckType ({gameType}) needs ranks that Neo.Cards.DeckType ({spriteType}) does not include",
                    MessageType.Error);
            }

            EditorGUILayout.EndVertical();
        }

        private bool IsGameTypeValid(DeckType spriteType, DeckType gameType)
        {
            Rank spriteMinRank = spriteType.GetMinRank();
            Rank gameMinRank = gameType.GetMinRank();
            return gameMinRank >= spriteMinRank;
        }

        private int GetGameCardCount(DeckType gameType)
        {
            return gameType switch
            {
                DeckType.Standard36 => 36,
                DeckType.Standard52 => 52,
                DeckType.Standard54 => 54,
                _ => 52
            };
        }

        private void DrawAutoFill()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Auto-fill", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Fills suits, back and jokers from sprite names in a folder. " +
                "Supported names: hearts_02..hearts_14, ace_of_spades, KH, 10c, туз пик, card_back, joker_red.",
                MessageType.Info);

            if (GUILayout.Button("Auto-Fill From Folder..."))
            {
                AutoFillFromFolder();
                // WHY: the folder panel and the report dialog are modal; once they return, the layout groups
                // opened by this pass no longer match and IMGUI logs "EndLayoutGroup" errors. The auto-fill
                // has already applied its own changes, so abandon the rest of this pass cleanly.
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndVertical();
        }

        private void AutoFillFromFolder()
        {
            string absolutePath = EditorUtility.OpenFolderPanel("Select card sprites folder", Application.dataPath, "");
            if (string.IsNullOrEmpty(absolutePath))
            {
                return;
            }

            if (!TryGetAssetFolder(absolutePath, out string assetFolder))
            {
                EditorUtility.DisplayDialog("Auto-Fill", "The folder must be inside this project's Assets folder.", "OK");
                return;
            }

            List<Sprite> sprites = LoadSpritesInFolder(assetFolder);

            if (sprites.Count == 0)
            {
                EditorUtility.DisplayDialog("Auto-Fill", $"No sprites found in {assetFolder}.", "OK");
                return;
            }

            ApplyAutoFill(sprites, out int assignedCards, out List<string> unrecognized, out List<string> conflicts);

            System.Text.StringBuilder report = new();
            report.AppendLine($"Assigned {assignedCards} card sprites from {sprites.Count} in {assetFolder}.");

            if (conflicts.Count > 0)
            {
                report.AppendLine($"Conflicts (slot already filled): {string.Join(", ", conflicts)}");
            }

            if (unrecognized.Count > 0)
            {
                report.AppendLine($"Unrecognized names: {string.Join(", ", unrecognized)}");
            }

            Debug.Log($"[DeckConfig] Auto-fill: {report}", target);
            EditorUtility.DisplayDialog("Auto-Fill", report.ToString(), "OK");
        }

        private static bool TryGetAssetFolder(string absolutePath, out string assetFolder)
        {
            assetFolder = FileUtil.GetProjectRelativePath(absolutePath.Replace('\\', '/')).TrimEnd('/');
            return (assetFolder == "Assets" || assetFolder.StartsWith("Assets/", System.StringComparison.Ordinal)) &&
                   AssetDatabase.IsValidFolder(assetFolder);
        }

        private static List<Sprite> LoadSpritesInFolder(string assetFolder)
        {
            List<Sprite> sprites = new();
            HashSet<string> processedPaths = new();

            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { assetFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!processedPaths.Add(path))
                {
                    continue;
                }

                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is Sprite sprite)
                    {
                        sprites.Add(sprite);
                    }
                }
            }

            return sprites;
        }

        private void ApplyAutoFill(
            List<Sprite> sprites,
            out int assignedCards,
            out List<string> unrecognized,
            out List<string> conflicts)
        {
            assignedCards = 0;
            unrecognized = new List<string>();
            conflicts = new List<string>();

            DeckType spriteDeckType = (DeckType)_deckType.intValue;
            Rank minRank = spriteDeckType.GetMinRank();
            int cardsPerSuit = GetExpectedCardCount();
            HashSet<string> assignedSlots = new();

            foreach (SerializedProperty suitProperty in new[] { _hearts, _diamonds, _clubs, _spades })
            {
                suitProperty.arraySize = cardsPerSuit;
                for (int i = 0; i < cardsPerSuit; i++)
                {
                    suitProperty.GetArrayElementAtIndex(i).objectReferenceValue = null;
                }
            }

            foreach (Sprite sprite in sprites)
            {
                if (!CardSpriteNameParser.TryParse(sprite.name, out CardSpriteParseResult parsed))
                {
                    unrecognized.Add(sprite.name);
                    continue;
                }

                SerializedProperty slot;
                switch (parsed.Kind)
                {
                    case CardSpriteKind.Back:
                        slot = _backSprite;
                        break;
                    case CardSpriteKind.JokerRed:
                        slot = _redJoker;
                        break;
                    case CardSpriteKind.JokerBlack:
                        slot = _blackJoker;
                        break;
                    case CardSpriteKind.Card:
                        int index = (int)parsed.Rank - (int)minRank;
                        if (index < 0 || index >= cardsPerSuit)
                        {
                            unrecognized.Add($"{sprite.name} (rank outside {spriteDeckType})");
                            continue;
                        }

                        slot = GetSuitProperty(parsed.Suit).GetArrayElementAtIndex(index);
                        break;
                    default:
                        continue;
                }

                if (!assignedSlots.Add(slot.propertyPath))
                {
                    if (slot.objectReferenceValue != sprite)
                    {
                        conflicts.Add(sprite.name);
                    }

                    continue;
                }

                slot.objectReferenceValue = sprite;
                if (parsed.Kind == CardSpriteKind.Card)
                {
                    assignedCards++;
                }
            }

            serializedObject.ApplyModifiedProperties();
            _validationStale = true;
        }

        private SerializedProperty GetSuitProperty(Suit suit)
        {
            return suit switch
            {
                Suit.Hearts => _hearts,
                Suit.Diamonds => _diamonds,
                Suit.Clubs => _clubs,
                Suit.Spades => _spades,
                _ => _hearts
            };
        }

        private void DrawBackSprite()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Card back", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_backSprite, GUIContent.none, GUILayout.Width(200));

            if (_backSprite.objectReferenceValue != null)
            {
                Sprite sprite = (Sprite)_backSprite.objectReferenceValue;
                Rect rect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.Width(PreviewSize));
                DrawSpritePreview(rect, sprite);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawSuitSection(string title, SerializedProperty property, bool redSuit, ref bool foldout)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            foldout = EditorGUILayout.Foldout(foldout, title, true, SuitFoldoutStyle(SuitTitleColor(redSuit)));

            if (foldout)
            {
                EditorGUILayout.PropertyField(property, true);
                EditorGUILayout.Space(5);

                int expectedCount = GetExpectedCardCount();
                int actualCount = property.arraySize;
                bool isValid = actualCount == expectedCount;

                DrawValidationBadge(isValid, actualCount, expectedCount);
                DrawSpritePreviewGrid(property);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawJokers()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            bool isRequired = (DeckType)_deckType.intValue == DeckType.Standard54 ||
                              (DeckType)_gameDeckType.intValue == DeckType.Standard54;
            string title = isRequired ? "Jokers (required for 54-card deck)" : "Jokers (optional)";

            _showJokers = EditorGUILayout.Foldout(_showJokers, title, true, EditorStyles.foldoutHeader);

            if (_showJokers)
            {
                if (!isRequired)
                {
                    EditorGUILayout.HelpBox("Jokers are optional for 36- and 52-card decks.", MessageType.Info);
                }

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Red joker", GUILayout.Width(110));
                EditorGUILayout.PropertyField(_redJoker, GUIContent.none);
                if (_redJoker.objectReferenceValue != null)
                {
                    Rect rect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.Width(PreviewSize),
                        GUILayout.Height(PreviewSize));
                    DrawSpritePreview(rect, (Sprite)_redJoker.objectReferenceValue);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Black joker", GUILayout.Width(110));
                EditorGUILayout.PropertyField(_blackJoker, GUIContent.none);
                if (_blackJoker.objectReferenceValue != null)
                {
                    Rect rect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.Width(PreviewSize),
                        GUILayout.Height(PreviewSize));
                    DrawSpritePreview(rect, (Sprite)_blackJoker.objectReferenceValue);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawValidation()
        {
            _showValidation = EditorGUILayout.Foldout(_showValidation, "Configuration validation", true,
                EditorStyles.foldoutHeader);

            if (!_showValidation)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DeckConfig config = (DeckConfig)target;
            RefreshValidationIfNeeded(config);

            if (_validationValid && _validationWarnings.Count == 0)
            {
                EditorGUILayout.HelpBox("✓ Configuration is valid", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < _validationErrors.Count; i++)
                {
                    EditorGUILayout.HelpBox(_validationErrors[i], MessageType.Error);
                }

                for (int i = 0; i < _validationWarnings.Count; i++)
                {
                    EditorGUILayout.HelpBox(_validationWarnings[i], MessageType.Warning);
                }
            }

            if (GUILayout.Button(ValidateButtonContent))
            {
                _validationStale = true;
                Repaint();
            }

            EditorGUILayout.EndVertical();
        }

        private void RefreshValidationIfNeeded(DeckConfig config)
        {
            RefreshValidationIfNeeded(config, Event.current == null || Event.current.type == EventType.Layout);
        }

        /// <summary>Re-validates the deck only after it changed or a refresh was requested.</summary>
        /// <param name="config">Inspected deck.</param>
        /// <param name="layoutPass">
        ///     True during a Layout event (or outside IMGUI). A cached result is only replaced then, so Layout and
        ///     Repaint of one frame always draw the same number of messages.
        /// </param>
        private void RefreshValidationIfNeeded(DeckConfig config, bool layoutPass)
        {
            if (config == null)
            {
                return;
            }

            int dirtyCount = EditorUtility.GetDirtyCount(config);
            bool hasResult = _validationDirtyCount >= 0;
            bool changed = _validationStale || dirtyCount != _validationDirtyCount;
            if (hasResult && (!changed || !layoutPass))
            {
                return;
            }

            _validationValid = config.Validate(out List<string> errors, out List<string> warnings);
            _validationErrors.Clear();
            _validationErrors.AddRange(errors);
            _validationWarnings.Clear();
            _validationWarnings.AddRange(warnings);
            _validationDirtyCount = dirtyCount;
            _validationStale = false;
        }

        private void DrawValidationBadge(bool isValid, int actual, int expected)
        {
            Color oldBgColor = GUI.backgroundColor;

            if (isValid)
            {
                GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
                EditorGUILayout.HelpBox($"✓ {actual}/{expected} sprites", MessageType.None);
            }
            else
            {
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                EditorGUILayout.HelpBox($"✗ {actual}/{expected} sprites", MessageType.Warning);
            }

            GUI.backgroundColor = oldBgColor;
        }

        private void DrawSpritePreviewGrid(SerializedProperty arrayProperty)
        {
            if (arrayProperty.arraySize == 0)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();

            int count = 0;
            for (int i = 0; i < arrayProperty.arraySize; i++)
            {
                SerializedProperty element = arrayProperty.GetArrayElementAtIndex(i);

                if (element.objectReferenceValue != null)
                {
                    Sprite sprite = (Sprite)element.objectReferenceValue;
                    Rect rect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize,
                        GUILayout.Width(PreviewSize), GUILayout.Height(PreviewSize));
                    DrawSpritePreview(rect, sprite);
                }
                else
                {
                    Rect rect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize,
                        GUILayout.Width(PreviewSize), GUILayout.Height(PreviewSize));
                    EditorGUI.DrawRect(rect, new Color(1f, 0.3f, 0.3f, 0.3f));
                    s_missingSpriteStyle ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
                    GUI.Label(rect, "?", s_missingSpriteStyle);
                }

                count++;
                if (count >= MaxPreviewsPerRow && i < arrayProperty.arraySize - 1)
                {
                    count = 0;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSpritePreview(Rect rect, Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            Texture2D preview = AssetPreview.GetAssetPreview(sprite);
            if (preview == null)
            {
                preview = AssetPreview.GetMiniThumbnail(sprite);
            }

            if (preview != null)
            {
                GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
            }
        }

        private int GetExpectedCardCount()
        {
            DeckType type = (DeckType)_deckType.intValue;
            return type switch
            {
                DeckType.Standard36 => 9,
                DeckType.Standard52 => 13,
                DeckType.Standard54 => 13,
                _ => 13
            };
        }
    }
}
