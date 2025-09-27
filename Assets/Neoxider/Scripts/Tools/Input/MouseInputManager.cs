using System;
using Neo.Tools;
using UnityEngine;

/// <summary>
///     Optimised mouse input manager – zero GC allocations per frame.
///     Provides Press, Hold, Release, Click events with MouseEventData struct.
/// </summary>
public class MouseInputManager : Singleton<MouseInputManager>
{
    /* non-alloc raycast buffers */
    private static readonly RaycastHit[] _hits3D = new RaycastHit[1];
    private static readonly RaycastHit2D[] _hits2D = new RaycastHit2D[1];

    private bool _pressed;
    private GameObject _pressedObj;

    private void Update()
    {
        if (enablePress && Input.GetMouseButtonDown(0))
        {
            BuildEventData(out var data);
            _pressed = true;
            _pressedObj = data.HitObject;
            OnPress(data);
        }

        if (enableHold && _pressed && Input.GetMouseButton(0))
        {
            BuildEventData(out var data);
            OnHold(data);
        }

        if (Input.GetMouseButtonUp(0))
        {
            BuildEventData(out var data);

            if (enableRelease) OnRelease(data);
            if (enableClick && _pressedObj && _pressedObj == data.HitObject)
                OnClick(data);

            _pressed = false;
            _pressedObj = null;
        }
    }

    protected override void Init()
    {
        base.Init();
        if (!targetCamera) targetCamera = Camera.main;
    }

    /*────────────────── helpers ──────────────────*/
    private void BuildEventData(out MouseEventData data)
    {
        Vector2 screenPos = Input.mousePosition;
        Vector3 worldPos;
        GameObject hitObj = null;
        RaycastHit hit3D = default;
        RaycastHit2D hit2D = default;

        var ray = targetCamera.ScreenPointToRay(screenPos);

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
    }

    #region DATA

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

    #endregion

    #region EVENTS (no GC)

    public event Action<MouseEventData> OnPress = delegate { };
    public event Action<MouseEventData> OnHold = delegate { };
    public event Action<MouseEventData> OnRelease = delegate { };
    public event Action<MouseEventData> OnClick = delegate { };

    #endregion

    #region INSPECTOR

    [Header("Enabled Modes")] public bool enablePress = true;
    public bool enableHold;
    public bool enableRelease = true;
    public bool enableClick = true;

    [Header("Raycast Settings")] [SerializeField]
    private Camera targetCamera;

    [SerializeField] private LayerMask interactableLayers = ~0;
    [SerializeField] private float fallbackDepth = 10f;

    #endregion
}