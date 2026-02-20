using System;
using Neo;
using Neo.Tools;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
///     Optimised mouse input manager – zero GC allocations per frame.
///     Provides Press, Hold, Release, Click events with MouseEventData struct.
/// </summary>
[CreateFromMenu("Neoxider/Tools/Input/MouseInputManager")]
[AddComponentMenu("Neoxider/Tools/" + nameof(MouseInputManager))]
[NeoDoc("Tools/Input/MouseInputManager.md")]
public class MouseInputManager : Singleton<MouseInputManager>
{
    public delegate void MouseEventHandler(in MouseEventData data);

    /* non-alloc raycast buffers */
    private static readonly RaycastHit[] _hits3D = new RaycastHit[1];
    private static readonly RaycastHit2D[] _hits2D = new RaycastHit2D[1];

    /* last event data for polling */
    public static MouseEventData LastEventData;
    public static bool HasEventData;

    [Header("Gizmos")] public bool drawGizmos = true;
    public Color gizmoColor = Color.yellow;
    public Color gizmoTextColor = Color.white;
    public float gizmoRadius = 0.1f;
    public bool gizmoDrawText = true;
    public float gizmoTextScale = 1f;
    public int gizmoBaseFontSize = 18;
    public Vector3 gizmoTextOffset = new(0f, 1f, 0f);

    [Header("Enabled Modes")] public bool enablePress = true;
    public bool enableHold;
    public bool enableRelease = true;
    public bool enableClick = true;

    [Header("Raycast Settings")] [SerializeField]
    private Camera targetCamera;

    [SerializeField] private LayerMask interactableLayers = ~0;
    [SerializeField] private float fallbackDepth = 10f;
    private bool _hasData;
    private GameObject _lastHitObj;
    private Vector3 _lastWorldPos;

    private bool _pressed;
    private GameObject _pressedObj;
    public static ref readonly MouseEventData LastEventDataRef => ref LastEventData;

    protected override bool SetInstanceOnAwakeEnabled => true;
    protected override bool DontDestroyOnLoadEnabled => true;

    private void Update()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                return;
            }
        }

        if (enablePress && Input.GetMouseButtonDown(0))
        {
            BuildEventData(out MouseEventData data);
            CacheGizmoData(in data);
            _pressed = true;
            _pressedObj = data.HitObject;
            OnPress(data);
            OnPressIn(in data);
        }

        if (enableHold && _pressed && Input.GetMouseButton(0))
        {
            BuildEventData(out MouseEventData data);
            CacheGizmoData(in data);
            OnHold(data);
            OnHoldIn(in data);
        }

        if (Input.GetMouseButtonUp(0))
        {
            BuildEventData(out MouseEventData data);
            CacheGizmoData(in data);

            if (enableRelease)
            {
                OnRelease(data);
                OnReleaseIn(in data);
            }

            if (enableClick && _pressedObj && _pressedObj == data.HitObject)
            {
                OnClick(data);
                OnClickIn(in data);
            }

            _pressed = false;
            _pressedObj = null;
        }
    }

    protected override void Init()
    {
        base.Init();
        if (!targetCamera)
        {
            targetCamera = Camera.main;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticsOnReload()
    {
        LastEventData = default;
        HasEventData = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        CreateInstance = true;
    }

    private void CacheGizmoData(in MouseEventData data)
    {
        ref readonly MouseEventData d = ref LastEventDataRef;
        _lastWorldPos = d.WorldPosition;
        _lastHitObj = d.HitObject;
        _hasData = true;
    }

    /*────────────────── helpers ──────────────────*/
    private void BuildEventData(out MouseEventData data)
    {
        Vector2 screenPos = Input.mousePosition;
        Vector3 worldPos;
        GameObject hitObj = null;
        RaycastHit hit3D = default;
        RaycastHit2D hit2D = default;

        if (targetCamera == null)
        {
            data = new MouseEventData(screenPos, Vector3.zero, null, hit3D, hit2D);
            return;
        }

        Ray ray = targetCamera.ScreenPointToRay(screenPos);

        /* 3D */
        if (Physics.RaycastNonAlloc(ray, _hits3D, float.MaxValue, interactableLayers) > 0)
        {
            hit3D = _hits3D[0];
            worldPos = hit3D.point;
            hitObj = hit3D.collider.gameObject;
        }
        /* 2D */
        else if ((_hits2D[0] = Physics2D.GetRayIntersection(ray, float.MaxValue, interactableLayers)).collider)
        {
            hit2D = _hits2D[0];
            worldPos = hit2D.point;
            hitObj = hit2D.collider.gameObject;
        }
        /* fallback */
        else
        {
            worldPos = targetCamera.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, fallbackDepth));
        }

        data = new MouseEventData(screenPos, worldPos, hitObj, hit3D, hit2D);
        /* update static polling data as well */
        LastEventData = data;
        HasEventData = true;
    }

    public event Action<MouseEventData> OnPress = delegate { };
    public event Action<MouseEventData> OnHold = delegate { };
    public event Action<MouseEventData> OnRelease = delegate { };
    public event Action<MouseEventData> OnClick = delegate { };
    public event MouseEventHandler OnPressIn = delegate { };
    public event MouseEventHandler OnHoldIn = delegate { };
    public event MouseEventHandler OnReleaseIn = delegate { };
    public event MouseEventHandler OnClickIn = delegate { };

    public readonly struct MouseEventData
    {
        public readonly Vector2 ScreenPosition;
        public readonly Vector3 WorldPosition;
        public readonly GameObject HitObject;
        public readonly RaycastHit Hit3D;
        public readonly RaycastHit2D Hit2D;

        public MouseEventData(Vector2 sp, Vector3 wp,
            GameObject hitObj,
            in RaycastHit hit3D,
            in RaycastHit2D hit2D)
        {
            ScreenPosition = sp;
            WorldPosition = wp;
            HitObject = hitObj;
            Hit3D = hit3D;
            Hit2D = hit2D;
        }
    }

#if UNITY_EDITOR
    private static GUIStyle _labelStyle;
    private void OnDrawGizmos()
    {
        if (!drawGizmos || !_hasData)
        {
            return;
        }

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(_lastWorldPos, gizmoRadius);

        if (!gizmoDrawText)
        {
            return;
        }

        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(EditorStyles.boldLabel);
        }

        _labelStyle.normal.textColor = gizmoTextColor;

        float handleSize = HandleUtility.GetHandleSize(_lastWorldPos);
        int fontSize = Mathf.Clamp((int)(gizmoBaseFontSize / handleSize * gizmoTextScale), 8, 64);
        _labelStyle.fontSize = fontSize;

        string label = _lastHitObj ? _lastHitObj.name : "No Hit";

        Camera cam = SceneView.currentDrawingSceneView != null
            ? SceneView.currentDrawingSceneView.camera
            : Camera.current;
        Vector3 up = cam != null ? cam.transform.up : Vector3.up;
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        Vector3 forward = cam != null ? cam.transform.forward : Vector3.forward;
        Vector3 pos = _lastWorldPos + right * gizmoTextOffset.x + up * (gizmoRadius + gizmoTextOffset.y) +
                      forward * gizmoTextOffset.z;

        Handles.Label(pos, label, _labelStyle);
    }
#endif
}