using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Tools
{
    /// <summary>
    ///     Drives dialogue UI (name, portrait, body text).
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

        [Tooltip(
            "If true, clear/hide character image when sprite is null. If false, keep previous image (e.g. one character speaking)")]
        public bool clearImageWhenNoSprite;

        public UnityEvent<string> OnCharacterChange;

        private string _lastCharacterName = string.Empty;

        /// <summary>
        ///     Whether a TMP font validation error was detected.
        /// </summary>
        public bool HasFontError { get; private set; }

        private void Awake()
        {
            ValidateFonts();
        }

        /// <summary>
        ///     Resets UI state.
        /// </summary>
        public void Reset()
        {
            _lastCharacterName = string.Empty;
            ClearDialogueText();
        }

        /// <summary>
        ///     Validates TMP fonts on referenced texts.
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
                    // Validate fallback fonts
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
        ///     Validates and removes invalid TMP fallback fonts.
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
        ///     Sets character name text.
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
        ///     Sets character portrait; null may clear/hide per settings.
        /// </summary>
        public void SetCharacterSprite(Sprite sprite)
        {
            if (characterImage == null)
            {
                return;
            }

            if (sprite == null)
            {
                if (clearImageWhenNoSprite)
                {
                    characterImage.sprite = null;
                    characterImage.enabled = false;
                }

                return;
            }

            characterImage.sprite = sprite;
            characterImage.enabled = true;

            if (setNativeSize)
            {
                characterImage.SetNativeSize();
            }
        }

        /// <summary>
        ///     Sets dialogue body text.
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
        ///     Clears dialogue body text.
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
