using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Tools
{
    /// <summary>
    /// Компонент для управления UI элементами диалога.
    /// </summary>
    [AddComponentMenu("Neo/" + "Tools/Dialogue/" + nameof(DialogueUI))]
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public Image characterImage;
        public TMP_Text characterNameText;
        public TMP_Text dialogueText;
        
        [Header("Settings")]
        public bool setNativeSize = true;
        
        [Header("Events")]
        public UnityEvent<string> OnCharacterChange;
        
        private string _lastCharacterName = string.Empty;
        private bool _fontError;

        private void Awake()
        {
            ValidateFonts();
        }

        /// <summary>
        /// Проверяет валидность шрифтов на TMP компонентах.
        /// </summary>
        public bool ValidateFonts()
        {
            _fontError = false;
            
            if (dialogueText != null)
            {
                if (!IsFontValid(dialogueText))
                {
                    Debug.LogError($"[DialogueUI] dialogueText '{dialogueText.name}' has invalid font! Please assign a valid TMP Font Asset.", dialogueText);
                    _fontError = true;
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
                    Debug.LogError($"[DialogueUI] characterNameText '{characterNameText.name}' has invalid font! Please assign a valid TMP Font Asset.", characterNameText);
                    _fontError = true;
                }
                else
                {
                    ValidateFallbackFonts(characterNameText);
                }
            }

            return !_fontError;
        }

        private bool IsFontValid(TMP_Text tmpText)
        {
            if (tmpText == null) return false;
            if (tmpText.font == null) return false;
            if (tmpText.font.material == null) return false;
            return true;
        }

        /// <summary>
        /// Проверяет и очищает невалидные fallback шрифты.
        /// </summary>
        private void ValidateFallbackFonts(TMP_Text tmpText)
        {
            if (tmpText == null || tmpText.font == null) return;
            
            var fallbackList = tmpText.font.fallbackFontAssetTable;
            if (fallbackList == null || fallbackList.Count == 0) return;
            
            for (int i = fallbackList.Count - 1; i >= 0; i--)
            {
                var fallbackFont = fallbackList[i];
                if (fallbackFont == null || fallbackFont.material == null)
                {
                    Debug.LogWarning($"[DialogueUI] Fallback font at index {i} in '{tmpText.font.name}' has no material! Removing from list to prevent errors.", tmpText);
                    fallbackList.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Устанавливает имя персонажа.
        /// </summary>
        public void SetCharacterName(string characterName)
        {
            if (characterName == _lastCharacterName)
                return;
            
            _lastCharacterName = characterName;
            
            if (characterNameText != null && IsFontValid(characterNameText))
            {
                characterNameText.text = characterName;
            }
            
            OnCharacterChange?.Invoke(characterName);
        }

        /// <summary>
        /// Устанавливает спрайт персонажа.
        /// </summary>
        public void SetCharacterSprite(Sprite sprite)
        {
            if (characterImage == null || sprite == null)
                return;
            
            characterImage.sprite = sprite;
            
            if (setNativeSize)
                characterImage.SetNativeSize();
        }

        /// <summary>
        /// Устанавливает текст диалога.
        /// </summary>
        public void SetDialogueText(string text)
        {
            if (dialogueText == null || !IsFontValid(dialogueText))
                return;
            
            dialogueText.text = text;
        }

        /// <summary>
        /// Очищает текст диалога.
        /// </summary>
        public void ClearDialogueText()
        {
            if (dialogueText != null && IsFontValid(dialogueText))
                dialogueText.text = string.Empty;
        }

        /// <summary>
        /// Сбрасывает состояние UI.
        /// </summary>
        public void Reset()
        {
            _lastCharacterName = string.Empty;
            ClearDialogueText();
        }
        
        /// <summary>
        /// Есть ли ошибка шрифта.
        /// </summary>
        public bool HasFontError => _fontError;
    }
}
