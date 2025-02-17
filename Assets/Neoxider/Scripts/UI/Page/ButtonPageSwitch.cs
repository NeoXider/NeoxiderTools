using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo
{
    namespace UI
    {
        [AddComponentMenu("Neoxider/" + "UI/" + "Page/" + nameof(ButtonPageSwitch))]
        public class ButtonPageSwitch : MonoBehaviour
        {
            [SerializeField]
            private Button _button;

            [SerializeField]
            private PageType _pageType = PageType.Menu;

            /// <summary>
            /// ������ ���� �� ������ ��� ���������� ��� � �������� ������ ����
            /// </summary>
            [SerializeField]
            private bool _change = true;

            /// <summary>
            /// ������� �� ���������� ��������
            /// </summary>
            [SerializeField]
            private bool _switchLastPage = false;

            [SerializeField]
            private TextMeshProUGUI _textPage;

            [SerializeField]
            private bool _changeText = true;

            [SerializeField]
            private PagesManager _pagesManager;

            private void Awake()
            {

            }

            private void OnEnable()
            {
                if (_button != null)
                {
                    _button.onClick.AddListener(SwitchPage);
                }
            }

            private void OnDisable()
            {
                if (_button != null)
                {
                    _button.onClick.RemoveListener(SwitchPage);
                }
            }

            private void OnMouseDown()
            {
                if (_button == null)
                {
                    SwitchPage();
                }
            }

            private void SwitchLastPage()
            {
                _pagesManager.SwitchLastPage();
            }

            public void SwitchPage()
            {


                if (_change)
                {
                    if (_switchLastPage)
                    {

                        SwitchLastPage();
                    }
                    else
                    {
                        _pagesManager.ChangePage(_pageType);
                    }

                }
                else
                {
                    _pagesManager.SetPage(_pageType);
                }

            }

            private void OnValidate()
            {
#if UNITY_2023_1_OR_NEWER
                _pagesManager = FindFirstObjectByType<PagesManager>();
#else
                _pagesManager = FindObjectOfType<PagesManager>();
#endif

#if UNITY_EDITOR
                if (!UnityEditor.AssetDatabase.Contains(this))
                {
                    if (_pagesManager == null)
                    {
                        Debug.LogWarning("Need UiManager");
                    }
                }
#endif
                if (_change)
                {
                    if (_switchLastPage)
                        _pageType = PageType.None;
                }
                else
                {
                    _switchLastPage = false;
                }

                if (_textPage != null)
                {
                    if (_changeText)
                    {
                        _textPage.text = _switchLastPage ? "Back" : _pageType.ToString();
                    }
                }


                _button = GetComponent<Button>();
            }
        }
    }
}
