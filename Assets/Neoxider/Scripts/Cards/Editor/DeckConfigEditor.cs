using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Neo.Cards.Editor
{
    /// <summary>
    ///     Кастомный редактор для DeckConfig с превью спрайтов
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

        private void OnEnable()
        {
            _deckType = serializedObject.FindProperty("_deckType");
            _gameDeckType = serializedObject.FindProperty("_gameDeckType");
            _backSprite = serializedObject.FindProperty("_backSprite");
            _hearts = serializedObject.FindProperty("_hearts");
            _diamonds = serializedObject.FindProperty("_diamonds");
            _clubs = serializedObject.FindProperty("_clubs");
            _spades = serializedObject.FindProperty("_spades");
            _redJoker = serializedObject.FindProperty("_redJoker");
            _blackJoker = serializedObject.FindProperty("_blackJoker");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawDeckSettings();
            EditorGUILayout.Space(10);

            DrawBackSprite();
            EditorGUILayout.Space(10);

            DrawSuitSection("♥ Червы (Hearts)", _hearts, Color.red, ref _showHearts);
            DrawSuitSection("♦ Бубны (Diamonds)", _diamonds, Color.red, ref _showDiamonds);
            DrawSuitSection("♣ Трефы (Clubs)", _clubs, Color.black, ref _showClubs);
            DrawSuitSection("♠ Пики (Spades)", _spades, Color.black, ref _showSpades);

            EditorGUILayout.Space(10);
            DrawJokers();

            EditorGUILayout.Space(15);
            DrawValidation();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUIStyle headerStyle = new(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Deck Configuration", headerStyle);
            EditorGUILayout.EndVertical();
        }

        private void DrawDeckSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Настройки колоды", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_deckType,
                new GUIContent("Тип для спрайтов", "Сколько карт загружено в конфиг"));

            int expectedCount = GetExpectedCardCount();
            EditorGUILayout.HelpBox($"Ожидается {expectedCount} карт на каждую масть", MessageType.Info);

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(_gameDeckType,
                new GUIContent("Тип для игры", "Сколько карт использовать в игре"));

            DeckType spriteType = (DeckType)_deckType.enumValueIndex;
            DeckType gameType = (DeckType)_gameDeckType.enumValueIndex;

            int gameCardCount = GetGameCardCount(gameType);
            string gameInfo = gameType == DeckType.Standard54
                ? $"В игре: {gameCardCount} карт (52 + 2 джокера)"
                : $"В игре: {gameCardCount} карт";

            if (IsGameTypeValid(spriteType, gameType))
            {
                EditorGUILayout.HelpBox(gameInfo, MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"⚠ GameDeckType ({gameType}) требует карты, которых нет в DeckType ({spriteType})",
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

        private void DrawBackSprite()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Рубашка карты", EditorStyles.boldLabel);

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

        private void DrawSuitSection(string title, SerializedProperty property, Color titleColor, ref bool foldout)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            Color oldColor = GUI.contentColor;
            GUI.contentColor = titleColor;
            foldout = EditorGUILayout.Foldout(foldout, title, true, EditorStyles.foldoutHeader);
            GUI.contentColor = oldColor;

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

            bool isRequired = (DeckType)_deckType.enumValueIndex == DeckType.Standard54;
            string title = isRequired ? "🃏 Джокеры (обязательно для 54 карт)" : "🃏 Джокеры (опционально)";

            _showJokers = EditorGUILayout.Foldout(_showJokers, title, true, EditorStyles.foldoutHeader);

            if (_showJokers)
            {
                if (!isRequired)
                {
                    EditorGUILayout.HelpBox("Джокеры опциональны для колод 36 и 52 карт", MessageType.Info);
                }

                EditorGUILayout.Space(5);

                // Красный джокер
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Красный джокер", GUILayout.Width(110));
                EditorGUILayout.PropertyField(_redJoker, GUIContent.none);
                if (_redJoker.objectReferenceValue != null)
                {
                    Rect rect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.Width(PreviewSize),
                        GUILayout.Height(PreviewSize));
                    DrawSpritePreview(rect, (Sprite)_redJoker.objectReferenceValue);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);

                // Чёрный джокер
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Чёрный джокер", GUILayout.Width(110));
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
            _showValidation = EditorGUILayout.Foldout(_showValidation, "Валидация конфигурации", true,
                EditorStyles.foldoutHeader);

            if (!_showValidation)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DeckConfig config = (DeckConfig)target;
            bool isValid = config.Validate(out List<string> errors, out List<string> warnings);

            if (isValid && warnings.Count == 0)
            {
                EditorGUILayout.HelpBox("✓ Конфигурация валидна", MessageType.Info);
            }
            else
            {
                foreach (string error in errors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }

                foreach (string warning in warnings)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
            }

            if (GUILayout.Button("Проверить конфигурацию"))
            {
                config.Validate(out _, out _);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawValidationBadge(bool isValid, int actual, int expected)
        {
            Color oldBgColor = GUI.backgroundColor;

            if (isValid)
            {
                GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
                EditorGUILayout.HelpBox($"✓ {actual}/{expected} спрайтов", MessageType.None);
            }
            else
            {
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                EditorGUILayout.HelpBox($"✗ {actual}/{expected} спрайтов", MessageType.Warning);
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
                    GUI.Label(rect, "?", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
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

            Texture2D texture = sprite.texture;
            Rect spriteRect = sprite.textureRect;

            Rect texCoords = new(
                spriteRect.x / texture.width,
                spriteRect.y / texture.height,
                spriteRect.width / texture.width,
                spriteRect.height / texture.height
            );

            GUI.DrawTextureWithTexCoords(rect, texture, texCoords);
        }

        private int GetExpectedCardCount()
        {
            DeckType type = (DeckType)_deckType.enumValueIndex;
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