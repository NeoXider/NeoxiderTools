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
        private bool _limitsVisibleCharacters;
        private TMP_Text _limitedLabel;
        private int _limitedLabelOwnVisibleCharacters;

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
                    NeoDiagnostics.LogError(
                        $"[DialogueUI] dialogueText '{dialogueText.name}' has invalid font! Please assign a valid TMP Font Asset.",
                        dialogueText);
                    HasFontError = true;
                }
                else
                {
                    ValidateFallbackFonts(dialogueText);
                }
            }

            if (characterNameText != null)
            {
                if (!IsFontValid(characterNameText))
                {
                    NeoDiagnostics.LogError(
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
                    NeoDiagnostics.LogWarning(
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
            if (characterNameText != null && IsFontValid(characterNameText))
            {
                characterNameText.text = characterName;
            }

            if (characterName == _lastCharacterName)
            {
                return;
            }

            _lastCharacterName = characterName;
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
        ///     Sets dialogue body text, fully visible.
        /// </summary>
        public void SetDialogueText(string text)
        {
            if (dialogueText == null || !IsFontValid(dialogueText))
            {
                return;
            }

            RestoreVisibleCharacters();
            dialogueText.text = text;
        }

        /// <summary>
        ///     Sets the whole body text at once but shows only its first <paramref name="visibleCharacters" />
        ///     characters (TMP <c>maxVisibleCharacters</c>; rich-text tags do not count). Used by the
        ///     MaxVisibleCharacters typewriter mode: the layout is final from the first character, so words do not
        ///     jump between lines, and revealing more needs no new string.
        /// </summary>
        public void SetDialogueTextRevealed(string text, int visibleCharacters)
        {
            if (dialogueText == null || !IsFontValid(dialogueText))
            {
                return;
            }

            LimitVisibleCharacters(visibleCharacters);
            dialogueText.text = text;
        }

        /// <summary>
        ///     Shows the first <paramref name="visibleCharacters" /> characters of the current body text without
        ///     reassigning it. <see cref="ShowAllCharacters" />, <see cref="SetDialogueText" /> and
        ///     <see cref="ClearDialogueText" /> lift the limit again.
        /// </summary>
        public void SetVisibleCharacters(int visibleCharacters)
        {
            // WHY: same skip rule as the text setters, so a label they ignore never gets a limit they cannot lift.
            if (dialogueText == null || !IsFontValid(dialogueText))
            {
                return;
            }

            LimitVisibleCharacters(visibleCharacters);
        }

        /// <summary>
        ///     Lifts a limit set by <see cref="SetDialogueTextRevealed" /> or <see cref="SetVisibleCharacters" />
        ///     without reassigning the text: the label gets back the <c>maxVisibleCharacters</c> it had before this
        ///     component first lowered it (TMP's default, 99999, means no limit). Does nothing when no such limit is
        ///     active.
        /// </summary>
        public void ShowAllCharacters()
        {
            RestoreVisibleCharacters();
        }

        /// <summary>
        ///     Clears dialogue body text.
        /// </summary>
        public void ClearDialogueText()
        {
            if (dialogueText != null && IsFontValid(dialogueText))
            {
                RestoreVisibleCharacters();
                dialogueText.text = string.Empty;
            }
        }

        // WHY: the label's own maxVisibleCharacters is captured when this component first lowers it, so lifting the
        // reveal limit restores a limit the project assigned to the label instead of replacing it with "unlimited".
        private void LimitVisibleCharacters(int visibleCharacters)
        {
            if (!_limitsVisibleCharacters || _limitedLabel != dialogueText)
            {
                RestoreVisibleCharacters();
                _limitsVisibleCharacters = true;
                _limitedLabel = dialogueText;
                _limitedLabelOwnVisibleCharacters = dialogueText.maxVisibleCharacters;
            }

            dialogueText.maxVisibleCharacters = Mathf.Max(0, visibleCharacters);
        }

        // WHY: only a limit this component set is lifted, and on the label it was set on, even after a rebind.
        private void RestoreVisibleCharacters()
        {
            if (!_limitsVisibleCharacters)
            {
                return;
            }

            _limitsVisibleCharacters = false;
            TMP_Text label = _limitedLabel;
            _limitedLabel = null;
            if (label != null)
            {
                label.maxVisibleCharacters = _limitedLabelOwnVisibleCharacters;
            }
        }
    }
}
