using System.Collections.Generic;
using UnityEngine;

namespace Flexalon
{
    /// <summary>
    /// Singleton class which tracks and updates all FlexalonNodes in the scene.
    /// See [core concepts](/docs/coreConcepts) for more information.
    /// </summary>
    [ExecuteAlways, HelpURL("https://www.flexalon.com/docs/coreConcepts")]
    public class Flexalon : MonoBehaviour
#if UNITY_UI
    , UnityEngine.UI.ICanvasElement
#endif
    {
        [SerializeField]
        private bool _updateInEditMode = true;
        /// <summary> Determines if Flexalon should automatically update in edit mode. </summary>
        public bool UpdateInEditMode
        {
            get { return _updateInEditMode; }
            set { _updateInEditMode = value; }
        }

        [SerializeField]
        private bool _updateInPlayMode = true;
        /// <summary> Determines if Flexalon should automatically update in play mode. </summary>
        public bool UpdateInPlayMode
        {
            get { return _updateInPlayMode; }
            set { _updateInPlayMode = value; }
        }

        [SerializeField]
        private bool _skipInactiveObjects = true;
        /// <summary> Determines if Flexalon should automatically skip inactive gameObjects in a layout. </summary>
        public bool SkipInactiveObjects
        {
            get { return _skipInactiveObjects; }
            set { _skipInactiveObjects = value; }
        }

        [SerializeField]
        private GameObject _inputProvider = null;
        private InputProvider _input;
        /// <summary>
        /// Override the default InputProvider used by FlexalonInteractables to support other input devices.
        /// </summary>
        public InputProvider InputProvider {
            get => _input;
            set => _input = value;
        }

        /// <summary>
        /// Set of nodes representing GameObjects tracked by Flexalon.
        /// </summary>
        public IReadOnlyCollection<FlexalonNode> Nodes => _nodes;

        private static Flexalon _instance;

        private HashSet<Node> _nodes = new HashSet<Node>();
        private Dictionary<GameObject, Node> _gameObjects = new Dictionary<GameObject, Node>();
        private DefaultTransformUpdater _defaultTransformUpdater = new DefaultTransformUpdater();
        private HashSet<Node> _roots = new HashSet<Node>();
        private static Vector3 _defaultSize = Vector3.one;
        private List<GameObject> _destroyed = new List<GameObject>();

        private static bool _undoRedo = false;
        private static bool _recordFrameChanges = false;
        public static bool RecordFrameChanges
        {
            get => _recordFrameChanges && !_undoRedo;
            set => _recordFrameChanges = value;
        }

        /// <summary> Event invoked before Flexalon updates. </summary>
        public System.Action PreUpdate;

        /// <summary> Returns the singleton Flexalon component. </summary>
        /// <returns> The singleton Flexalon component, or null if it doesn't exist. </returns>
        public static Flexalon Get()
        {
            return _instance;
        }

        public static Flexalon GetOrCreate()
        {
            TryGetOrCreate(out _);
            return _instance;
        }

        /// <summary> Returns the singleton Flexalon component, or creates one if it doesn't exist. </summary>
        /// <returns> The singleton Flexalon component. </returns>
        internal static bool TryGetOrCreate(out Flexalon instance)
        {
            bool created = false;
            if (!_instance)
            {
                #if UNITY_2023_1_OR_NEWER
                    _instance = FindFirstObjectByType<Flexalon>();
                #else
                    _instance = FindObjectOfType<Flexalon>();
                #endif
                if (!_instance)
                {
                    FlexalonLog.Log("New Flexalon Instance Created");
                    var FlexalonGO = new GameObject("Flexalon");
                    #if UNITY_EDITOR
                        UnityEditor.Undo.RegisterCreatedObjectUndo(FlexalonGO, "Create Flexalon");
                    #endif
                    _instance = AddComponent<Flexalon>(FlexalonGO);
                    created = true;
                }
                else
                {
                    FlexalonLog.Log("Flexalon Instance Found in Scene");
                }
            }

            instance = _instance;
            return created;
        }

        /// <summary> Returns the FlexalonNode associated with the gameObject. </summary>
        /// <param name="go"> The gameObject to get the FlexalonNode for. </param>
        /// <returns> The FlexalonNode associated with the gameObject, or null if it doesn't exist. </returns>
        public static FlexalonNode GetNode(GameObject go)
        {
            if (_instance != null && _instance && _instance._gameObjects.TryGetValue(go, out var node))
            {
                return node;
            }

            return null;
        }

        /// <summary>
        /// Returns the FlexalonNode associated with the gameObject,
        /// or creates it if it doesn't exist.
        /// </summary>
        /// <param name="go"> The gameObject to get the FlexalonNode for. </param>
        /// <returns> The FlexalonNode associated with the gameObject. </returns>
        public static FlexalonNode GetOrCreateNode(GameObject go)
        {
            if (go == null)
            {
                return null;
            }

            GetOrCreate();

            if (!_instance._gameObjects.TryGetValue(go, out var node))
            {
                node = _instance.CreateNode();
                node._gameObject = go;
                node.RefreshResult();
                node.SetResultToCurrentTransform();

                // If inactive or disabled, FlexalonObject won't register itself, so do it here.
                node.SetFlexalonObject(go.GetComponent<FlexalonObject>());

                _instance._gameObjects.Add(go, node);
            }
            else if (!node._result)
            {
                node.RefreshResult();
            }

            return node;
        }

        /// <summary> Gets the current InputProvider used by FlexalonInteractables. </summary>
        public static InputProvider GetInputProvider()
        {
            GetOrCreate();

            if (_instance)
            {
                if (_instance._input == null)
                {
                    if (_instance._inputProvider)
                    {
                        _instance._input = _instance._inputProvider.GetComponent<InputProvider>();
                    }

                    if (_instance._input == null)
                    {
                        _instance._input = new FlexalonMouseInputProvider();
                    }
                }

                return _instance._input;
            }

            return null;
        }

        /// <summary> Marks every node and FlexalonComponent as dirty and calls UpdateDirtyNodes. </summary>
        public void ForceUpdate()
        {
            foreach (var node in _nodes)
            {
                foreach (var flexalonComponent in node.GameObject.GetComponents<FlexalonComponent>())
                {
                    flexalonComponent.MarkDirty();
                }

                node.MarkDirty();
            }

            UpdateDirtyNodes();
        }

        #if UNITY_EDITOR
            internal static bool DisableUndoForTest = false;
            private static HashSet<GameObject> _avoidAddComponentUndo = new HashSet<GameObject>();
        #endif

        /// <summary> Helper to ensure undo operation on AddComponent is handled correctly. </summary>
        public static T AddComponent<T>(GameObject go) where T : Component
        {
            return (T)AddComponent(go, typeof(T));
        }

        /// <summary> Helper to ensure undo operation on AddComponent is handled correctly. </summary>
        public static Component AddComponent(GameObject go, System.Type type)
        {
            #if UNITY_EDITOR
                if (!DisableUndoForTest && !_avoidAddComponentUndo.Contains(go))
                {
                    // Avoid recursively recording undo because it
                    // causes warnings in some versions of Unity.
                    _avoidAddComponentUndo.Add(go);
                    // Debug.Log("AddComponent " + type.Name + " to " + go.name + " WITH UNDO");
                    var c = UnityEditor.Undo.AddComponent(go, type);
                    _avoidAddComponentUndo.Remove(go);
                    return c;
                }
                else
                {
                    // Debug.Log("AddComponent " + type.Name + " to " + go.name);
                    return go.AddComponent(type);
                }
            #else
                return go.AddComponent(type);
            #endif
        }

        private Node CreateNode()
        {
            var node = new Node();
            node._transformUpdater = _defaultTransformUpdater;
            _nodes.Add(node);
            _roots.Add(node);
            return node;
        }

        private void DestroyNode(GameObject go)
        {
            if (_instance != null && _instance._gameObjects.TryGetValue(go, out var node))
            {
                _instance._gameObjects.Remove(go);
                node.Detach();
                node.DetachAllChildren();
                node.SetDependency(null);
                node.ClearDependents();
                _nodes.Remove(node);
                _roots.Remove(node);
            }
        }

        void LateUpdate()
        {
            if (_instance != this)
            {
                return;
            }

            if (Application.isPlaying && _updateInPlayMode)
            {
                UpdateDirtyNodes();
            }

            if (!Application.isPlaying && _updateInEditMode)
            {
                UpdateDirtyNodes();
            }

            _undoRedo = false;
            RecordFrameChanges = false;
        }

        /// <summary> Updates all dirty nodes. </summary>
        public void UpdateDirtyNodes()
        {
            PreUpdate?.Invoke();

            _destroyed.Clear();
            foreach (var kv in _gameObjects)
            {
                var go = kv.Key;
                var node = kv.Value;
                if (!go)
                {
                    _destroyed.Add(go);
                }
                else
                {
                    if (!Application.isPlaying &&
                        (node._parent != null || node._dependency != null || node.HasFlexalonObject || node.Method != null))
                    {
                        node.CheckDefaultAdapter();
                    }

                    node.DetectRectTransformChanged();
                }
            }

            foreach (var go in _destroyed)
            {
                DestroyNode(go);
            }

            foreach (var root in _roots)
            {
                if (root._dependency == null && root.GameObject.activeInHierarchy)
                {
                    root.UpdateRootFillSize();
                    Compute(root);
                }
            }

            _undoRedo = false;
            RecordFrameChanges = false;
        }

        private void UpdateTransforms(Node node)
        {
            var rectTransform = node.GameObject.transform as RectTransform;
            if (rectTransform != null && !node.ReachedTargetPosition)
            {
                rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }

            if (!node.ReachedTargetPosition)
            {
                node.ReachedTargetPosition = node._transformUpdater.UpdatePosition(node, node._result.TargetPosition);
                foreach (Node child in node._children)
                {
                    child.ReachedTargetPosition = false;
                }
            }

            if (!node.ReachedTargetRotation)
            {
                node.ReachedTargetRotation = node._transformUpdater.UpdateRotation(node, node._result.TargetRotation);
                foreach (Node child in node._children)
                {
                    child.ReachedTargetRotation = false;
                }
            }

            if (!node.ReachedTargetScale)
            {
                node.ReachedTargetScale = node._transformUpdater.UpdateScale(node, node._result.TargetScale);
                foreach (Node child in node._children)
                {
                    child.ReachedTargetScale = false;
                }
            }

            if (!node.ReachedTargetRectSize)
            {
                node.ReachedTargetRectSize = node._transformUpdater.UpdateRectSize(node, node._result.TargetRectSize);
            }

            node._result.TransformPosition = node.GameObject.transform.localPosition;
            node._result.TransformRotation = node.GameObject.transform.localRotation;
            node._result.TransformScale = node.GameObject.transform.localScale;

            if (rectTransform != null)
            {
                node._result.TransformRectSize = rectTransform.rect.size;
            }

            node.NotifyResultChanged();

            foreach (var child in node._children)
            {
                UpdateTransforms(child);
            }
        }

        void Awake()
        {
            if (_instance == this)
            {
                RecordFrameChanges = false;
            }
        }

#if UNITY_EDITOR
        void OnEnable()
        {
            if (_instance == this)
            {
                UnityEditor.Undo.undoRedoPerformed += OnUndoRedo;
            }
        }
#endif

#if UNITY_EDITOR
        void OnDisable()
        {
            if (_instance == this)
            {
                UnityEditor.Undo.undoRedoPerformed -= OnUndoRedo;
            }
        }

#endif
        void OnDestroy()
        {
            if (_instance == this)
            {
                FlexalonLog.Log("Flexalon Instance Destroyed");
                _instance = null;
            }
        }

        private void OnUndoRedo()
        {
            _undoRedo = true;
        }

        private void Compute(Node node)
        {
            if (node.Dirty && !node.IsDragging)
            {
                FlexalonLog.Log("LAYOUT COMPUTE", node);
                MeasureRoot(node);
                Arrange(node);
                Constrain(node);
            }

            if (node.HasResult)
            {
                ComputeTransforms(node);
                UpdateTransforms(node);
                ComputeDependents(node);
            }
        }

        private void ComputeDependents(Node node)
        {
            if (node._dependents != null)
            {
                var fillSize = Math.Mul(node._result.AdapterBounds.size, node.GetWorldBoxScale(true));
                foreach (var dep in node._dependents)
                {
                    if (dep.GameObject)
                    {
                        dep._dirty = dep._dirty || node.UpdateDependents;

                        var fillSizeForDep = fillSize;
                        if (dep.GameObject.transform.parent)
                        {
                            fillSizeForDep = Math.Div(fillSize, dep.GameObject.transform.parent.lossyScale);
                        }

                        dep.SetFillSize(fillSizeForDep);
                        Compute(dep);
                    }
                }
            }

            node.UpdateDependents = false;

            foreach (var child in node._children)
            {
                ComputeDependents(child);
            }
        }

        private static Vector3 GetChildAvailableSize(Node node)
        {
            return Vector3.Max(Vector3.zero,
                node._result.AdapterBounds.size - node.Padding.Size);
        }

        private void MeasureAdapterSize(Node node, Vector3 center, Vector3 size, Vector3 min, Vector3 max)
        {
            var adapterBounds = node.Adapter.Measure(node, size, min, max);

            node.RecordResultUndo();
            node._result.AdapterBounds = adapterBounds;
            FlexalonLog.Log("MeasureAdapterSize", node, adapterBounds);

            node._result.LayoutBounds = new Bounds(adapterBounds.center + center, adapterBounds.size);
            FlexalonLog.Log("LayoutBounds", node, node._result.LayoutBounds);
        }

        private void MeasureRoot(Node node)
        {
            Vector3 min, max;

            if (node.GameObject.transform.parent && node.GameObject.transform.parent is RectTransform parentRect)
            {
                min = node.GetMinSize(parentRect.rect.size, false);
                max = node.GetMaxSize(parentRect.rect.size, false);
            }
            else
            {
                min = node.GetMinSize(Vector3.zero, false);
                max = node.GetMaxSize(Math.MaxVector, false);
            }

            Measure(node, min, max, true);
        }

        private void MeasureChild(Node node, bool includeChildren = true)
        {
            var min = node.GetMinSize(Vector3.zero, false);
            var max = node.GetMaxSize(Math.MaxVector, false);
            Measure(node, min, max, includeChildren);
        }

        private void MeasureChild(Node node, Vector3 parentLayoutSize, bool includeChildren)
        {
            var min = node.GetMinSize(parentLayoutSize, false);
            var max = Vector3.Min(node._result.ShrinkSize, node.GetMaxSize(parentLayoutSize, false));
            Measure(node, min, max, includeChildren);
        }

        private void Measure(Node node, Vector3 min, Vector3 max, bool includeChildren = true)
        {
            FlexalonLog.Log($"Measure | {node.GameObject.name} {min} {max} {includeChildren}");

            node.RecordResultUndo();

            // Start by measuring whatever size we can. This might change after we
            // run the layout method later if the size is set to children.
            var size = MeasureSize(node);
            MeasureAdapterSize(node, Vector3.zero, size, min, max);

            if (includeChildren && node.Method != null)
            {
                MeasureLayout(node, min, max);
            }

            node.ApplyScaleAndRotation();
        }

        private void MeasureLayout(Node node, Vector3 min, Vector3 max)
        {
            // Now let the children run their measure before running our own.
            // Assume empty fill size for now just to gather fixed and component values.
            foreach (var child in node._children)
            {
                bool wasShrunk = child.IsShrunk();
                child.ResetShrinkFillSize();
                child.ResetFillShrinkChanged();

                if (AnyAxisIsFill(child))
                {
                    MeasureChild(child, false);
                }
                else if (child.Dirty || !child.HasResult || wasShrunk)
                {
                    MeasureChild(child);
                }
            }

            // Figure out how much space we have for the children
            var childAvailableSize = GetChildAvailableSize(node);

            var minChildAvailableSize = Vector3.Max(Vector3.zero, min - node.Padding.Size);
            var maxChildAvailableSize = Vector3.Max(Vector3.zero, max - node.Padding.Size);
            childAvailableSize = Math.Clamp(childAvailableSize, minChildAvailableSize, maxChildAvailableSize);
            FlexalonLog.Log("Measure | ChildAvailableSize", node, childAvailableSize, minChildAvailableSize, maxChildAvailableSize);

            // Measure what this node's size is given child sizes.
            var layoutBounds = node.Method.Measure(node, childAvailableSize, minChildAvailableSize, maxChildAvailableSize);
            FlexalonLog.Log("Measure | LayoutBounds 1", node, layoutBounds);
            MeasureAdapterSize(node, layoutBounds.center, layoutBounds.size + node.Padding.Size, min, max);

            // Measure any children that depend on our size
            bool anyChildSizeChanged = false;
            foreach (var child in node._children)
            {
                if (AnyAxisIsFill(child) || child.IsShrunk())
                {
                    var previousSize = child.GetArrangeSize();

                    MeasureChild(child, layoutBounds.size, true);

                    if (previousSize != child.GetArrangeSize())
                    {
                        anyChildSizeChanged = true;
                        child._dirty = true;
                    }

                    child.ResetFillShrinkChanged();
                }
            }

            if (anyChildSizeChanged)
            {
                // Re-measure given new child sizes.
                layoutBounds = node.Method.Measure(node, childAvailableSize, minChildAvailableSize, maxChildAvailableSize);
                FlexalonLog.Log("Measure | LayoutBounds 2", node, layoutBounds);
                MeasureAdapterSize(node, layoutBounds.center, layoutBounds.size + node.Padding.Size, min, max);

                // Measure any children that depend on our size in case it was wrong the first time.
                // This cycle can continue forever, but this is the last time we'll do it.
                foreach (var child in node._children)
                {
                    if (AnyFillOrShrinkSizeChanged(child))
                    {
                        MeasureChild(child, layoutBounds.size, true);
                        child._dirty = true;
                    }
                }
            }
        }

        private void Arrange(Node node)
        {
            node._dirty = false;
            node._hasResult = true;
            node.SetPositionResult(Vector3.zero);
            node.SetRotationResult(Quaternion.identity);

            // If there's no children, there's nothing left to do.
            if (node.Children.Count == 0 || node.Method == null)
            {
                return;
            }

            FlexalonLog.Log("Arrange", node, node._result.AdapterBounds.size);

            // Run child arrange algorithm
            foreach (var child in node._children)
            {
                if (child._dirty)
                {
                    Arrange(child);
                }
            }

            // Figure out how much space we have for the children
            var childAvailableSize = GetChildAvailableSize(node);
            FlexalonLog.Log("Arrange | ChildAvailableSize", node, childAvailableSize);

            // Run our arrange algorithm
            node.Method.Arrange(node, childAvailableSize);

            // Run any attached modifiers
            if (node.Modifiers != null)
            {
                foreach (var modifier in node.Modifiers)
                {
                    modifier.PostArrange(node);
                }
            }
        }

        private void ComputeScale(Node node)
        {
            bool canScale = true;
            canScale = node.Adapter.TryGetScale(node, out var componentScale);
            node.SetComponentScale(componentScale);

            bool shouldScale = canScale && (node.Parent != null || node.HasFlexalonObject);
            if (!shouldScale)
            {
                node.ReachedTargetScale = true;
                return;
            }

            var scale = node.Result.ComponentScale;
            if (node.Parent != null)
            {
                var s = scale;
                scale = Math.SafeDivOne(scale, node.Parent.Result.ComponentScale);
            }

            FlexalonLog.Log("ComputeTransform:Scale", node, scale);
            scale.Scale(node.Scale);
            node.RecordResultUndo();
            node._result.TargetScale = scale;
            node.ReachedTargetScale = false;
        }

        private void ComputeRectSize(Node node)
        {
            if (node.GameObject.transform is RectTransform && node.Adapter.TryGetRectSize(node, out var rectSize))
            {
                node.RecordResultUndo();
                node.Result.TargetRectSize = rectSize;
                node.ReachedTargetRectSize = false;
            }
            else
            {
                node.ReachedTargetRectSize = true;
            }
        }

        private void ComputeTransforms(Node node)
        {
            if (node.HasSizeUpdate)
            {
                node.HasSizeUpdate = false;
                ComputeScale(node);
                ComputeRectSize(node);
                if (node.Parent != null || node.HasFlexalonObject)
                {
                    foreach (var child in node._children)
                    {
                        child.HasSizeUpdate = true;
                    }
                }
            }

            if (node.Dependency != null)
            {
                if (node.HasPositionUpdate)
                {
                    var position = node._result.LayoutPosition;
                    FlexalonLog.Log("ComputeTransform:Constrait:LayoutPosition", node, position);
                    node.RecordResultUndo();
                    node._result.TargetPosition = position;
                    node.ReachedTargetPosition = false;
                }

                if (node.HasRotationUpdate)
                {
                    node.RecordResultUndo();
                    node._result.TargetRotation = node._result.LayoutRotation * node.Rotation;
                    FlexalonLog.Log("ComputeTransform:Constrait:Rotation", node, node._result.TargetRotation);
                    node.ReachedTargetRotation = false;
                }
            }
            else if (node.Parent != null)
            {
                if (node.HasRotationUpdate)
                {
                    node.RecordResultUndo();
                    node._result.TargetRotation = node._result.LayoutRotation * node.Rotation;
                    FlexalonLog.Log("ComputeTransform:Layout:Rotation", node, node._result.TargetRotation);
                    node.ReachedTargetRotation = false;
                }

                if (node.HasPositionUpdate)
                {
                    var position = node._result.LayoutPosition
                        - node._parent.Padding.Center
                        + node._parent._result.AdapterBounds.center
                        - node.Margin.Center
                        - node._result.TargetRotation * node._result.RotatedAndScaledBounds.center
                        + node.Offset;

                    position = Math.SafeDivZero(position, node.Parent.Result.ComponentScale);
                    FlexalonLog.Log("ComputeTransform:Layout:Position", node, position);
                    node.RecordResultUndo();
                    node._result.TargetPosition = position;
                    node.ReachedTargetPosition = false;
                }
            }
            else
            {
                node.ReachedTargetPosition = true;
                node.ReachedTargetRotation = true;
            }

            node.HasPositionUpdate = false;
            node.HasRotationUpdate = false;

            node._transformUpdater.PreUpdate(node);

            foreach (var child in node._children)
            {
                ComputeTransforms(child);
            }
        }

        private void Constrain(Node node)
        {
            if (node.Constraint != null)
            {
                FlexalonLog.Log("Constrain", node);
                node.Constraint.Constrain(node);
            }
        }

        private static Vector3 MeasureSize(Node node)
        {
            Vector3 result = new Vector3();
            for (int axis = 0; axis < 3; axis++)
            {
                var unit = node.GetSizeType(axis);
                if (unit == SizeType.Layout)
                {
                    result[axis] = node._method != null ? node.Padding.Size[axis] : 0;
                }
                else if (unit == SizeType.Component)
                {
                    result[axis] = 0;
                }
                else if (unit == SizeType.Fill)
                {
                    var scale = node.Scale[axis];
                    var inverseScale = scale == 0 ? 0 : 1f / scale;
                    result[axis] = (node.Result.FillSize[axis] * node.SizeOfParent[axis] * inverseScale) - node.Margin.Size[axis];
                }
                else
                {
                    result[axis] = node.Size[axis];
                }
            }

            FlexalonLog.Log("MeasureSize", node, result);
            return result;
        }

        private static bool AnyAxisIsFill(Node child)
        {
            return AxisIsFill(child, 0) || AxisIsFill(child, 1) || AxisIsFill(child, 2);
        }

        private static bool AxisIsFill(Node child, int axis)
        {
            return child.GetSizeType(axis) == SizeType.Fill ||
                child.GetMaxSizeType(axis) == MinMaxSizeType.Fill ||
                child.GetMinSizeType(axis) == MinMaxSizeType.Fill;
        }

        private static bool FillSizeChanged(Node child, int axis)
        {
            return AxisIsFill(child, axis) && child.FillSizeChanged[axis];
        }

        private static bool AnyFillSizeChanged(Node child)
        {
            return FillSizeChanged(child, 0) ||
                FillSizeChanged(child, 1) ||
                FillSizeChanged(child, 2);
        }

        private static bool ShrinkSizeChanged(Node child, int axis)
        {
            return child.CanShrink(axis) && child.ShrinkSizeChanged[axis];
        }

        private static bool AnyShrinkSizeChanged(Node child)
        {
            return ShrinkSizeChanged(child, 0) ||
                ShrinkSizeChanged(child, 1) ||
                ShrinkSizeChanged(child, 2);
        }

        private static bool AnyFillOrShrinkSizeChanged(Node child)
        {
            return AnyFillSizeChanged(child) || AnyShrinkSizeChanged(child);
        }

        internal static bool IsRootCanvas(GameObject go)
        {
#if UNITY_UI
            if (go.TryGetComponent<Canvas>(out var canvas))
            {
                return canvas.isRootCanvas;
            }
#endif
            return false;
        }

#if UNITY_UI
        public void Rebuild(UnityEngine.UI.CanvasUpdate executing) {}

        public void LayoutComplete() => UpdateDirtyNodes();

        public void GraphicUpdateComplete() {}

        public bool IsDestroyed() => this == null;
#endif

        private class Node : FlexalonNode
        {
            public Node _parent;
            public FlexalonNode Parent => _parent;
            public int _index;
            public int Index => _index;
            public List<Node> _children = new List<Node>();
            public IReadOnlyList<FlexalonNode> Children => _children;
            public bool _dirty = false;
            public bool Dirty => _dirty;
            public bool _hasResult = false;
            public bool HasResult => _hasResult;
            public bool HasPositionUpdate = false;
            public bool HasSizeUpdate = false;
            public bool HasRotationUpdate = false;
            public bool ReachedTargetPosition = true;
            public bool ReachedTargetRotation = true;
            public bool ReachedTargetScale = true;
            public bool ReachedTargetRectSize = true;
            public bool UpdateDependents = false;

            public GameObject _gameObject;
            public GameObject GameObject => _gameObject;
            public Layout _method;
            public Layout Method { get => _method; set => _method = value; }
            public Constraint _constraint;
            public Constraint Constraint => _constraint;
            private Adapter _adapter = null;
            public Adapter Adapter => (_adapter == null) ? _adapter = new DefaultAdapter(GameObject, this) : _adapter;
            public bool _customAdapter = false;
            public FlexalonResult _result;
            public FlexalonResult Result => _result;
            public FlexalonObject _flexalonObject;
            public FlexalonObject FlexalonObject => _flexalonObject;
            public Vector3 Size => HasFlexalonObject ? _flexalonObject.Size : Vector3.one;
            public Vector3 SizeOfParent => HasFlexalonObject ? _flexalonObject.SizeOfParent : Vector3.one;
            public Vector3 Offset => HasFlexalonObject ? _flexalonObject.Offset : Vector3.zero;
            public Vector3 Scale => HasFlexalonObject ? _flexalonObject.Scale : Vector3.one;
            public Quaternion Rotation => HasFlexalonObject ? _flexalonObject.Rotation : Quaternion.identity;
            public Directions Margin => HasFlexalonObject ? _flexalonObject.Margin : Directions.zero;
            public Directions Padding => HasFlexalonObject ? _flexalonObject.Padding : Directions.zero;
            public Node _dependency;
            public FlexalonNode Dependency => _dependency;
            public bool HasDependents => _dependents != null && _dependents.Count > 0;
            public List<Node> _dependents;
            public TransformUpdater _transformUpdater;
            public List<FlexalonModifier> _modifiers = null;
            public IReadOnlyList<FlexalonModifier> Modifiers => _modifiers;
            public event System.Action<FlexalonNode> ResultChanged;
            public bool IsDragging { get; set; }
            private bool SkipInactive => _instance._skipInactiveObjects && !_gameObject.activeInHierarchy;
            public bool SkipLayout => SkipInactive || (HasFlexalonObject ? _flexalonObject.SkipLayout : false);
            private bool _hasFlexalonObject;
            public bool HasFlexalonObject => _hasFlexalonObject;
            public bool[] FillSizeChanged = new bool[3];
            public bool[] ShrinkSizeChanged = new bool[3];

            public void SetFillSize(Vector3 fillSize)
            {
                SetFillSize(0, fillSize.x);
                SetFillSize(1, fillSize.y);
                SetFillSize(2, fillSize.z);
            }

            public void SetFillSize(int axis, float size)
            {
                FlexalonLog.Log("SetFillSize", this, axis, size);
                FillSizeChanged[axis] = FillSizeChanged[axis] || (_result.FillSize[axis] != size);
                _result.FillSize[axis] = size;
            }

            public void ResetFillShrinkChanged()
            {
                FillSizeChanged[0] = FillSizeChanged[1] = FillSizeChanged[2] = false;
                ShrinkSizeChanged[0] = ShrinkSizeChanged[1] = ShrinkSizeChanged[2] = false;
            }

            public void ResetShrinkFillSize()
            {
                _result.ShrinkSize = Math.MaxVector;
                _result.FillSize = Vector3.zero;
            }

            public void SetShrinkSize(int axis, float size)
            {
                FlexalonLog.Log("SetShrinkSize", this, axis, size);
                ShrinkSizeChanged[axis] = ShrinkSizeChanged[axis] || (_result.ShrinkSize[axis] != size);
                _result.ShrinkSize[axis] = size;
            }

            public void SetShrinkFillSize(Vector3 childSize, Vector3 layoutSize, bool includesSizeOfParent)
            {
                for (int axis = 0; axis < 3; axis++)
                {
                    SetShrinkFillSize(axis, childSize[axis], layoutSize[axis], includesSizeOfParent);
                }
            }

            public void SetShrinkFillSize(int axis, float childSize, float layoutSize, bool includesSizeOfParent)
            {
                if (AxisIsFill(this, axis))
                {
                    var fillSize = includesSizeOfParent ?
                        (SizeOfParent[axis] > 0 ? childSize / SizeOfParent[axis] : 0) :
                        childSize;
                    SetFillSize(axis, fillSize);
                }

                if (GetMinSizeType(axis) != MinMaxSizeType.None)
                {
                    var measureSize = GetMeasureSize(axis, layoutSize);
                    if (measureSize > childSize)
                    {
                        SetShrinkSize(axis, Mathf.Max(GetMinSize(axis, layoutSize), childSize));
                    }
                }
            }

            public bool IsShrunk()
            {
                return _result.ShrinkSize != Math.MaxVector;
            }

            public bool CanShrink(int axis)
            {
                return GetSizeType(axis) != SizeType.Fill && GetMinSizeType(axis) != MinMaxSizeType.None;
            }

            public void UpdateRootFillSize()
            {
                var newSize = GetRootFillSize();
                if (newSize != _result.FillSize)
                {
                    FlexalonLog.Log("UpdateRootFillSize", this, newSize);
                    SetFillSize(newSize);
                    MarkDirty();
                }
            }

            private Vector3 GetRootFillSize()
            {
                var fillSize = _defaultSize;
                if (GameObject.transform.parent && GameObject.transform.parent is RectTransform parentRect)
                {
                    fillSize = parentRect.rect.size;
                }

                return fillSize;
            }

            public SizeType GetSizeType(Axis axis)
            {
                if (HasFlexalonObject)
                {
                    switch (axis)
                    {
                        case Axis.X: return _flexalonObject.WidthType;
                        case Axis.Y: return _flexalonObject.HeightType;
                        case Axis.Z: return _flexalonObject.DepthType;
                    }
                }

                return SizeType.Component;
            }

            public SizeType GetSizeType(int axis)
            {
                return GetSizeType((Axis)axis);
            }

            public MinMaxSizeType GetMinSizeType(int axis)
            {
                return GetMinSizeType((Axis)axis);
            }

            public MinMaxSizeType GetMinSizeType(Axis axis)
            {
                if (HasFlexalonObject)
                {
                    switch (axis)
                    {
                        case Axis.X: return _flexalonObject.MinWidthType;
                        case Axis.Y: return _flexalonObject.MinHeightType;
                        case Axis.Z: return _flexalonObject.MinDepthType;
                    }
                }

                return MinMaxSizeType.None;
            }

            public float GetMinSize(int axis, float parentLayoutSize)
            {
                return GetMinSize((Axis)axis, parentLayoutSize);
            }

            public float GetMinSize(Axis axis, float parentLayoutSize)
            {
                return GetMinSize(axis, parentLayoutSize, true);
            }

            public float GetMinSize(Axis axis, float parentLayoutSize, bool withMargin)
            {
                var margin = withMargin ? Margin.Size[(int)axis] : 0;

                if (HasFlexalonObject)
                {
                    switch (GetMinSizeType(axis))
                    {
                        case MinMaxSizeType.None:
                            return margin;
                        case MinMaxSizeType.Fixed:
                            return Mathf.Max(_flexalonObject.MinSize[(int)axis], 0) + margin;
                        case MinMaxSizeType.Fill:
                            return Mathf.Max(_flexalonObject.MinSizeOfParent[(int)axis] * parentLayoutSize, 0);
                    }
                }

                return 0;
            }

            public Vector3 GetMinSize(Vector3 parentLayoutSize)
            {
                return GetMinSize(parentLayoutSize, true);
            }

            public Vector3 GetMinSize(Vector3 parentLayoutSize, bool withMargin)
            {
                return new Vector3(GetMinSize(Axis.X, parentLayoutSize.x, withMargin),
                    GetMinSize(Axis.Y, parentLayoutSize.y, withMargin),
                    GetMinSize(Axis.Z, parentLayoutSize.z, withMargin));
            }

            public MinMaxSizeType GetMaxSizeType(int axis)
            {
                return GetMaxSizeType((Axis)axis);
            }

            public MinMaxSizeType GetMaxSizeType(Axis axis)
            {
                if (HasFlexalonObject)
                {
                    switch (axis)
                    {
                        case Axis.X: return _flexalonObject.MaxWidthType;
                        case Axis.Y: return _flexalonObject.MaxHeightType;
                        case Axis.Z: return _flexalonObject.MaxDepthType;
                    }
                }

                return MinMaxSizeType.None;
            }

            public Vector3 GetMaxSize(Vector3 parentLayoutSize)
            {
                return GetMaxSize(parentLayoutSize, true);
            }

            public float GetMaxSize(int axis, float parentLayoutSize)
            {
                return GetMaxSize((Axis)axis, parentLayoutSize);
            }

            public float GetMaxSize(Axis axis, float fillSize)
            {
                return GetMaxSize(axis, fillSize, true);
            }

            public float GetMaxSize(Axis axis, float parentLayoutSize, bool withMargin)
            {
                var margin = withMargin ? Margin.Size[(int)axis] : 0;

                if (HasFlexalonObject)
                {
                    switch (GetMaxSizeType(axis))
                    {
                        case MinMaxSizeType.None:
                            return Math.MaxValue;
                        case MinMaxSizeType.Fixed:
                            return Mathf.Max(0, _flexalonObject.MaxSize[(int)axis]) + margin;
                        case MinMaxSizeType.Fill:
                            return Mathf.Max(0, _flexalonObject.MaxSizeOfParent[(int)axis] * parentLayoutSize);
                    }
                }

                return Math.MaxValue;
            }

            public Vector3 GetMaxSize(Vector3 parentLayoutSize, bool withMargin)
            {
                return new Vector3(GetMaxSize(Axis.X, parentLayoutSize.x, withMargin),
                    GetMaxSize(Axis.Y, parentLayoutSize.y, withMargin),
                    GetMaxSize(Axis.Z, parentLayoutSize.z, withMargin));
            }

            public void SetPositionResult(Vector3 position)
            {
                RecordResultUndo();
                _result.LayoutPosition = position;
                HasPositionUpdate = true;
                UpdateDependents = true;
            }

            public void SetRotationResult(Quaternion quaternion)
            {
                RecordResultUndo();
                _result.LayoutRotation = quaternion;
                HasRotationUpdate = true;
                UpdateDependents = true;
            }

            public void SetComponentScale(Vector3 scale)
            {
                RecordResultUndo();
                _result.ComponentScale = scale;
            }

            public void SetMethod(Layout method)
            {
                _method = method;
            }

            public void SetConstraint(Constraint constraint, FlexalonNode target)
            {
                _constraint = constraint;
                SetDependency(target);
            }

            public void SetTransformUpdater(TransformUpdater updater)
            {
                updater = updater != null ? updater : _instance?._defaultTransformUpdater;
                if (updater != _transformUpdater)
                {
                    _transformUpdater = updater;
                }
            }

            public void SetFlexalonObject(FlexalonObject obj)
            {
                _hasFlexalonObject = obj != null;
                _flexalonObject = obj;
            }

            public void MarkDirty()
            {
                if (Dirty) return;

#if FLEXALON_LOG
                var callStack = new System.Diagnostics.StackTrace().ToString();
                if (!callStack.Contains("OnDestroy"))
                {
                    FlexalonLog.Log("MarkDirty", this);
                }
#endif

                var node = this;
                while (node != null)
                {
                    node._dirty = true;
                    node.HasPositionUpdate = true;
                    node.HasRotationUpdate = true;
                    node.HasSizeUpdate = true;
                    node = node._parent;
                }

                if (_dependency != null && !_dependency.HasResult)
                {
                    _dependency?.MarkDirty();
                }

#if UNITY_EDITOR
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
            }

            public void ForceUpdate()
            {
                MarkDirty();
                MarkDirtyDown();
                Flexalon.GetOrCreate().UpdateDirtyNodes();
            }

            private void MarkDirtyDown()
            {
                foreach (var child in _children)
                {
                    child.MarkDirty();
                    child.MarkDirtyDown();
                }

                if (HasDependents)
                {
                    foreach (var dep in _dependents)
                    {
                        dep.MarkDirty();
                        dep.MarkDirtyDown();
                    }
                }
            }

            public void AddChild(FlexalonNode child)
            {
                InsertChild(child, _children.Count);
            }

            public void InsertChild(FlexalonNode child, int index)
            {
                var childNode = child as Node;
                if (childNode._parent == this && childNode._index == index)
                {
                    return;
                }

                child.Detach();

                childNode._parent = this;
                childNode._index = index;
                _children.Insert(index, childNode);
                _instance?._roots.Remove(childNode);
            }

            public FlexalonNode GetChild(int index)
            {
                return _children[index];
            }

            public void Detach()
            {
                if (_parent != null)
                {
                    _parent._children.Remove(this);
                    _parent = null;
                    _index = 0;

                    if (_instance != null && _instance._gameObjects.ContainsKey(GameObject))
                    {
                        _instance._roots.Add(this);
                    }
                }
            }

            public void DetachAllChildren()
            {
                while (Children.Count > 0)
                {
                    Children[Children.Count - 1].Detach();
                }
            }

            public Vector3 GetMeasureSize(Vector3 layoutSize)
            {
                return new Vector3(GetMeasureSize(0, layoutSize.x),
                    GetMeasureSize(1, layoutSize.y),
                    GetMeasureSize(2, layoutSize.z));
            }

            public float GetMeasureSize(int axis, float layoutSize)
            {
                var size = GetSizeType(axis) == SizeType.Fill ? 0 : _result.RotatedAndScaledBounds.size[axis] + Margin.Size[axis];
                return Mathf.Clamp(size, GetMinSize(axis, layoutSize), GetMaxSize(axis, layoutSize));
            }

            public Vector3 GetArrangeSize()
            {
                return _result.RotatedAndScaledBounds.size + Margin.Size;
            }

            public Vector3 GetBoxScale()
            {
                bool shouldScale = Adapter.TryGetScale(this, out var _);
                if (!shouldScale)
                {
                    return Math.Abs(GameObject.transform.localScale);
                }
                else if (HasFlexalonObject)
                {
                    // FlexalonObject size/scale always applies, even without a layout.
                    return Math.Abs(_flexalonObject.Scale);
                }
                else if (_parent != null)
                {
                    return Vector3.one;
                }
                else
                {
                    return Math.Abs(GameObject.transform.localScale);
                }
            }

            public Quaternion GetBoxRotation()
            {
                // FlexalonObject rotation only takes effect if there's a layout.
                if (_parent != null || _dependency != null)
                {
                    return HasFlexalonObject ? _flexalonObject.Rotation : Quaternion.identity;
                }
                else
                {
                    return GameObject.transform.localRotation;
                }
            }

            public Vector3 GetWorldBoxScale(bool includeLocalScale)
            {
                Vector3 scale = includeLocalScale ? GetBoxScale() : Vector3.one;
                var node = this;
                while (node._parent != null)
                {
                    scale.Scale(node._parent.GetBoxScale());
                    node = node._parent;
                }

                if (node.GameObject.transform.parent != null)
                {
                    scale.Scale(node.GameObject.transform.parent.lossyScale);
                }

                return scale;
            }

            public Vector3 GetWorldBoxPosition(Vector3 scale, bool includePadding)
            {
                var pos = _result.LayoutBounds.center;
                if (includePadding)
                {
                    pos -= Padding.Center;
                }

                pos.Scale(scale);
                pos = GameObject.transform.rotation * pos + GameObject.transform.position;
                return pos;
            }

            public void SetDependency(FlexalonNode node)
            {
                if (_dependency != node)
                {
                    _dependency?._dependents.Remove(this);

                    _dependency = node as Node;

                    if (node != null)
                    {
                        if (_dependency._dependents == null)
                        {
                            _dependency._dependents = new List<Node>();
                        }

                        _dependency._dependents.Add(this);
                    }
                }
            }

            public void ClearDependents()
            {
                if (_dependents != null)
                {
                    while (_dependents.Count > 0)
                    {
                        _dependents[_dependents.Count - 1].SetDependency(null);
                    }
                }
            }

            public void SetAdapter(Adapter adapter)
            {
                _adapter = adapter;
                _customAdapter = (_adapter != null);
            }

            public void CheckDefaultAdapter()
            {
                if (!_customAdapter)
                {
                    if ((Adapter as DefaultAdapter).CheckComponent(GameObject, this))
                    {
                        MarkDirty();
                    }
                }
            }

            public void ApplyScaleAndRotation()
            {
                var bounds = Math.ScaleBounds(_result.LayoutBounds, GetBoxScale());
                bounds = Math.RotateBounds(bounds, GetBoxRotation());
                RecordResultUndo();
                _result.RotatedAndScaledBounds = bounds;
                FlexalonLog.Log("Measure | RotatedAndScaledBounds", this, bounds);
                HasSizeUpdate = true;
                UpdateDependents = true;
            }

            public void RefreshResult()
            {
                _result = _gameObject.GetComponent<FlexalonResult>();
                _hasResult = _result != null;
                if (!_hasResult)
                {
                    _result = AddComponent<FlexalonResult>(GameObject);
                    _dirty = true;
                }
            }

            public void SetResultToCurrentTransform()
            {
                _result.TransformPosition = GameObject.transform.localPosition;
                _result.TransformRotation = GameObject.transform.localRotation;
                _result.TransformScale = GameObject.transform.localScale;
                _result.TargetPosition = GameObject.transform.localPosition;
                _result.TargetRotation = GameObject.transform.localRotation;
                _result.TargetScale = GameObject.transform.localScale;
            }

            public void RecordResultUndo()
            {
#if UNITY_EDITOR
                if (Flexalon.RecordFrameChanges && _result != null)
                {
                    UnityEditor.Undo.RecordObject(_result, "Result changed");
                    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(_result);
                }
#endif
            }

            public void AddModifier(FlexalonModifier modifier)
            {
                if (_modifiers == null)
                {
                    _modifiers = new List<FlexalonModifier>();
                }

                _modifiers.RemoveAll(m => m == modifier);
                _modifiers.Add(modifier);
            }

            public void RemoveModifier(FlexalonModifier modifier)
            {
                _modifiers?.Remove(modifier);
            }

            public void NotifyResultChanged()
            {
                ResultChanged?.Invoke(this);
            }

            public void DetectRectTransformChanged()
            {
                if (GameObject.transform is RectTransform rectTransform)
                {
                    // Check if the rect size changed unexpectedly, either by the user or a UGUI component.
                    if (ReachedTargetRectSize && _result.TransformRectSize != rectTransform.rect.size)
                    {
                        MarkDirty();
                    }
                }
            }
        }
    }
}