using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Tools
{
    /// <summary>
    ///     Компонент для управления UI элементами диалога.
    /// </summary>
    [NeoDoc("Tools/Dialogue/DialogueUI.md")]
    [CreateFromMenu("Neoxider/Tools/Dialogue/DialogueUI")]
    [AddComponentMenu("Neoxider/" + "Tools/Dialogue/" + nameof(DialogueUI))]
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI Elements")] public Image characterImage;

        public TMP_Text characterNameText;
        public TMP_Text dialogueText;

        [Header("Settings")] public bool setNativeSize = true;

        public UnityEvent<string> OnCharacterChange;

        private string _lastCharacterName = string.Empty;

        /// <summary>
        ///     Есть ли ошибка шрифта.
        /// </summary>
        public bool HasFontError { get; private set; }

        private void Awake()
        {
            ValidateFonts();
        }

        /// <summary>
        ///     Сбрасывает состояние UI.
        /// </summary>
        public void Reset()
        {
            _lastCharacterName = string.Empty;
            ClearDialogueText();
        }

        /// <summary>
        ///     Проверяет валидность шрифтов на TMP компонентах.
        /// </summary>
        public bool ValidateFonts()
        {
            HasFontError = false;

            if (dialogueText != null)
            {
                if (!IsFontValid(dialogueText))
                {
                    Debug.LogError(
                        $"[DialogueUI] dialogueText '{dialogueText.name}' has invalid font! Please assign a valid TMP Font Asset.",
                        dialogueText);
                    HasFontError = true;
                }
                else
                {
                    // Проверяем fallback шрифты
                    ValidateFallbackFonts(dialogueText);
                }
            }

            if (characterNameText != null)
            {
                if (!IsFontValid(characterNameText))
                {
                    Debug.LogError(
                        $"[DialogueUI] characterNameText '{characterNameText.name}' has invalid font! Please assign a valid TMP Font Asset.",
                        characterNameText);
                    HasFontError = true;
                }
                else
                {
                    ValidateFallbackFonts(characterNameText);
                }
            }

            return !HasFontError;
        }

        private bool IsFontValid(TMP_Text tmpText)
        {
            if (tmpText == null)
            {
                return false;
            }

            if (tmpText.font == null)
            {
                return false;
            }

            if (tmpText.font.material == null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Проверяет и очищает невалидные fallback шрифты.
        /// </summary>
        private void ValidateFallbackFonts(TMP_Text tmpText)
        {
            if (tmpText == null || tmpText.font == null)
            {
                return;
            }

            List<TMP_FontAsset> fallbackList = tmpText.font.fallbackFontAssetTable;
            if (fallbackList == null || fallbackList.Count == 0)
            {
                return;
            }

            for (int i = fallbackList.Count - 1; i >= 0; i--)
            {
                TMP_FontAsset fallbackFont = fallbackList[i];
                if (fallbackFont == null || fallbackFont.material == null)
                {
                    Debug.LogWarning(
                        $"[DialogueUI] Fallback font at index {i} in '{tmpText.font.name}' has no material! Removing from list to prevent errors.",
                        tmpText);
                    fallbackList.RemoveAt(i);
                }
            }
        }

        /// <summary>
        ///     Устанавливает имя персонажа.
        /// </summary>
        public void SetCharacterName(string characterName)
        {
            if (characterName == _lastCharacterName)
            {
                return;
            }

            _lastCharacterName = characterName;

            if (characterNameText != null && IsFontValid(characterNameText))
            {
                characterNameText.text = characterName;
            }

            OnCharacterChange?.Invoke(characterName);
        }

        /// <summary>
        ///     Устанавливает спрайт персонажа.
        /// </summary>
        public void SetCharacterSprite(Sprite sprite)
        {
            if (characterImage == null || sprite == null)
            {
                return;
            }

            characterImage.sprite = sprite;

            if (setNativeSize)
            {
                characterImage.SetNativeSize();
            }
        }

        /// <summary>
        ///     Устанавливает текст диалога.
        /// </summary>
        public void SetDialogueText(string text)
        {
            if (dialogueText == null || !IsFontValid(dialogueText))
            {
                return;
            }

            dialogueText.text = text;
        }

        /// <summary>
        ///     Очищает текст диалога.
        /// </summary>
        public void ClearDialogueText()
        {
            if (dialogueText != null && IsFontValid(dialogueText))
            {
                dialogueText.text = string.Empty;
            }
        }
    }
}