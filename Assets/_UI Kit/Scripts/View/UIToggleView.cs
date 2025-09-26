using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;



[AddComponentMenu("UI/" + nameof(UIToggleView))]
public class UIToggleView : MonoBehaviour
{
    [SerializeField] private Toggle _toggle;
    [SerializeField] private Image _image;
    [SerializeField] private GameObject[] _visuals;
    [SerializeField] private bool _setNativeSize;

    public Sprite[] sprites;


    public bool activ;
    public UnityEvent On;
    public UnityEvent Off;
    public UnityEvent<bool> OnValueChanged;

    private void Awake()
    {
        if (_toggle != null)
        {
            _toggle?.onValueChanged.AddListener(Set);
            Actions(_toggle.isOn);
        }
    }

    private void Start()
    {
        if (_toggle != null)
            Set(_toggle.isOn);
    }

    private void OnEnable()
    {
        if (_toggle != null)
            _toggle.isOn = activ;
    }

    private void OnDestroy()
    {
        if (_toggle != null) _toggle?.onValueChanged.RemoveListener(Set);
    }

    private void OnValidate()
    {
        _image ??= GetComponent<Image>();
        _toggle ??= GetComponent<Toggle>();

        if (_toggle != null)
        {
            activ = _toggle.isOn;
        }
        else
        {
        }

        Visual(activ);
    }

    public void Switch()
    {
        Set(!activ);
    }

    public void Set(bool activ)
    {
        Set(activ, false);
    }

    public void Set(bool activ, bool invoke = false)
    {
        if (activ != this.activ)
        {
            if (_toggle != null)
            {
                if (invoke)
                    _toggle.isOn = activ;
                else
                    _toggle.SetIsOnWithoutNotify(activ);
            }

            this.activ = activ;
            Actions(activ);
        }

        Visual(activ);
    }

    private void Actions(bool activ)
    {
        if (activ)
            On?.Invoke();
        else
            Off?.Invoke();

        OnValueChanged?.Invoke(activ);
    }

    private void Visual(bool activ)
    {
        var id = activ ? 1 : 0;

        if (sprites != null && sprites.Length == 2)
            if (_image != null)
            {
                _image.sprite = sprites[id];

                if (_setNativeSize)
                    _image.SetNativeSize();
            }

        if (_visuals != null && _visuals.Length == 2)
            for (var i = 0; i < _visuals.Length; i++)
                _visuals[i].SetActive(id == i);
    }
}