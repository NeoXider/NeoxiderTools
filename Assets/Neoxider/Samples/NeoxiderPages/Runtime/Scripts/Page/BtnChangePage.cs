using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Neo.Pages
{
    [MovedFrom("")]
    [AddComponentMenu("Neo/Pages/" + nameof(BtnChangePage))]
    /// <summary>
    /// UI-кнопка для смены страниц через <see cref="PM"/>.
    /// Поддерживает анимацию нажатия и опциональное выполнение <see cref="GameState.State"/> перед переключением.
    /// </summary>
    public class BtnChangePage : MonoBehaviour, IPointerClickHandler, IPointerUpHandler, IPointerDownHandler
    {
        public enum Action
        {
            OpenPage = 0,
            Back = 1,
            CloseCurrent = 2
        }

        public bool intecactable = true;
        [SerializeField] private Image _imageTarget;

        [FormerlySerializedAs("_pageType")]
        [FormerlySerializedAs("page")]
        [FormerlySerializedAs("pageSelectMode")]
        [Space]
        [Header("Page Settings")]
        [SerializeField]
        private Action action = Action.OpenPage;

        [FormerlySerializedAs("pageId")] [SerializeField]
        private PageId targetPageId;

        [SerializeField] private bool _canSwitchPage = true;

        [SerializeField] private GameState.State _executeState;

        [Space] [Header("Animation")] [SerializeField]
        private bool _useAnimImage = true;

        [SerializeField] private float _timeAnimImage = 0.3f;
        [SerializeField] private float _scaleAnim = -0.15f;

        [Space] [Header("SetText")] [SerializeField]
        private bool _changeText = true;

        [SerializeField] private TMP_Text _textPage;

        public UnityEvent OnClick;
        private LayoutGroup _layoutGroup;
        private RectTransform rect;

        private Vector3 startScale;


        private void Awake()
        {
            startScale = transform.localScale;
            rect = transform.GetComponent<RectTransform>();
            _layoutGroup = transform.parent.GetComponent<LayoutGroup>();
        }

        private void OnEnable()
        {
            transform.localScale = startScale;
            if (_layoutGroup != null)
            {
                rect.pivot = Vector2.one * 0.5f;
            }
        }

        private void OnValidate()
        {
            if (_textPage != null)
            {
                if (_changeText)
                {
                    if (action == Action.Back)
                    {
                        _textPage.text = "Back";
                    }
                    else if (action == Action.CloseCurrent)
                    {
                        _textPage.text = "Close";
                    }
                    else if (targetPageId != null)
                    {
                        _textPage.text = targetPageId.DisplayName;
                    }
                    else
                    {
                        _textPage.text = "Page";
                    }
                }
            }

            _imageTarget ??= GetComponent<Image>();
        }

        /// <summary>
        ///     Обработчик клика: выполняет состояние (если задано), переключает страницу и вызывает <see cref="OnClick" />.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!intecactable)
            {
                return;
            }

            GameState.Set(_executeState);

            if (_canSwitchPage)
            {
                ChangePage();
            }

            OnClick?.Invoke();
        }

        /// <summary>
        ///     Обработчик нажатия: уменьшает scale (если включена анимация).
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (intecactable && _useAnimImage)
            {
                if (intecactable && _useAnimImage)
                {
                    float scale = startScale.x * (_scaleAnim > 0 ? 1 + _scaleAnim : 1 + _scaleAnim);
                    transform.DOScale(scale, _timeAnimImage).SetUpdate(true);
                }
            }
        }

        /// <summary>
        ///     Обработчик отпускания: возвращает scale к исходному (если включена анимация).
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (intecactable && _useAnimImage)
            {
                transform.DOScale(startScale, _timeAnimImage).SetUpdate(true);
            }
        }

        /// <summary>
        ///     Выполняет действие (открыть страницу/назад/закрыть текущую).
        /// </summary>
        public void ChangePage()
        {
            if (action == Action.Back)
            {
                PM.I.SwitchToPreviousPage();
            }
            else if (action == Action.CloseCurrent)
            {
                PM.I.CloseCurrentPage();
            }
            else
            {
                if (targetPageId != null)
                {
                    PM.I.ChangePage(targetPageId);
                }
                else
                {
                    Debug.LogWarning("[BtnChangePage] Target PageId is null.", this);
                }
            }
        }

#if UNITY_EDITOR
        private const string DefaultPageIdFolder = "Assets/NeoxiderPages/Pages";
        private const string DefaultPageOpenAssetName = "PageOpen";

        private void Reset()
        {
            action = Action.OpenPage;

            if (targetPageId == null)
            {
                targetPageId = GetOrCreateDefaultPageOpen();
            }
        }

        private static string GetPreferredPageIdFolder()
        {
            if (AssetDatabase.IsValidFolder(DefaultPageIdFolder))
            {
                return DefaultPageIdFolder;
            }

            string[] guids = AssetDatabase.FindAssets("t:PageId", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string dir = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(dir))
                {
                    return dir;
                }
            }

            return DefaultPageIdFolder;
        }

        private static PageId GetOrCreateDefaultPageOpen()
        {
            string folder = GetPreferredPageIdFolder();
            EnsureFolder(folder);

            string assetPath = $"{folder}/{DefaultPageOpenAssetName}.asset";
            PageId existing = AssetDatabase.LoadAssetAtPath<PageId>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            PageId instance = ScriptableObject.CreateInstance<PageId>();
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return instance;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] parts = folder.Split('/');
            if (parts.Length < 2 || parts[0] != "Assets")
            {
                return;
            }

            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
#endif
    }
}