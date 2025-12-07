using UnityEngine;

namespace Flexalon
{
    /// <summary>
    /// Adapters determine how Flexalon measures other Unity components.
    /// See [adapters](/docs/adapters) documentation.
    /// </summary>
    public interface Adapter
    {
        /// <summary> Measure the size of this node. </summary>
        /// <param name="node"> The node to measure. </param>
        /// <param name="size"> The size set by the Flexalon Object Component. The adapter should update any axis set to SizeType.Component. </param>
        /// <param name="min"> The maximum size, determined by the MinSizeType. </param>
        /// <param name="max"> The maximum size, determined by the MaxSizeType and the parent layout. </param>
        /// <returns> The measured bounds to use in layout. </returns>
        Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max);

        /// <summary>
        /// Return what the gameObject's scale should be in local space.
        /// </summary>
        /// <param name="node"> The node to update. </param>
        /// <param name="scale"> The desired scale. </param>
        /// <returns> True if the scale should be modified. </returns>
        bool TryGetScale(FlexalonNode node, out Vector3 scale);

        /// <summary> Return what the rect transform size should be. </summary>
        /// <param name="node"> The node to update. </param>
        /// <param name="rectSize"> The desired rect size. </param>
        /// <returns> True if the rect size should be modified. </returns>
        bool TryGetRectSize(FlexalonNode node, out Vector2 rectSize);
    }

    internal interface InternalAdapter : Adapter
    {
        bool IsValid();
        bool SizeChanged();
    }

    internal class DefaultAdapter : Adapter
    {
        private InternalAdapter _adapter;

        public DefaultAdapter(GameObject gameObject, FlexalonNode node)
        {
            CheckComponent(gameObject, node);

            // Prevent detecting a change immediately after creating the adapter.
            _adapter?.SizeChanged();
        }

        public bool CheckComponent(GameObject gameObject, FlexalonNode node)
        {
            if (node.HasFlexalonObject && !node.FlexalonObject.UseDefaultAdapter)
            {
                _adapter = null;
                return false;
            }

            if (_adapter == null)
            {
                CreateAdapter(gameObject);
                return _adapter != null;
            }

            if (!_adapter.IsValid())
            {
                CreateAdapter(gameObject);
                return true;
            }

            if (_adapter != null && _adapter.SizeChanged())
            {
                return true;
            }

            return false;
        }

        public void CreateAdapter(GameObject gameObject)
        {
            _adapter = null;

#if UNITY_TMPRO
            if (gameObject.TryGetComponent<TMPro.TMP_Text>(out var text))
            {
                _adapter = new TextAdapter(text);
            } else
#endif
#if UNITY_UI
            if (gameObject.TryGetComponent<Canvas>(out var canvas))
            {
                _adapter = new CanvasAdapter(canvas);
            }
            else if (gameObject.TryGetComponent<UnityEngine.UI.ILayoutElement>(out var layoutElement))
            {
                _adapter = new LayoutElementAdapter(layoutElement as MonoBehaviour);
            } else
#endif
            if (gameObject.TryGetComponent<RectTransform>(out var rectTransform))
            {
                _adapter = new RectTransformAdapter(rectTransform);
            }
            else if (gameObject.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
            {
                _adapter = new SpriteRendererAdapter(spriteRenderer);
            }
            else if (gameObject.TryGetComponent<MeshRenderer>(out var renderer) && gameObject.TryGetComponent<MeshFilter>(out var meshFilter) && meshFilter.sharedMesh)
            {
                _adapter = new MeshRendererAdapter(renderer, meshFilter);
            }
#if UNITY_PHYSICS
            else if (gameObject.TryGetComponent<Collider>(out var collider))
            {
                _adapter = new ColliderAdapter(collider);
            }
#endif
#if UNITY_PHYSICS_2D
            else if (gameObject.TryGetComponent<Collider2D>(out var collider2d))
            {
                _adapter = new Collider2DAdapter(collider2d);
            }
#endif
        }

        public Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            if (_adapter != null)
            {
                return _adapter.Measure(node, size, min, max);
            }
            else
            {
                return new Bounds(Vector3.zero, Math.Clamp(size, min, max));
            }
        }

        public bool TryGetScale(FlexalonNode node, out Vector3 scale)
        {
            if (_adapter != null)
            {
                return _adapter.TryGetScale(node, out scale);
            }
            else
            {
                scale = Vector3.one;
                return true;
            }
        }

        public bool TryGetRectSize(FlexalonNode node, out Vector2 rectSize)
        {
            if (_adapter != null)
            {
                return _adapter.TryGetRectSize(node, out rectSize);
            }
            else
            {
                rectSize = Vector2.zero;
                return false;
            }
        }
    }

    internal class SpriteRendererAdapter : InternalAdapter
    {
        private SpriteRenderer _renderer;
        private Bounds _lastRendererBounds;

        public SpriteRendererAdapter(SpriteRenderer renderer)
        {
            _renderer = renderer;
        }

        public bool IsValid()
        {
            return _renderer && _renderer.enabled;
        }

        public bool SizeChanged()
        {
            var spriteBounds = GetBounds();
            if (_lastRendererBounds != spriteBounds)
            {
                _lastRendererBounds = spriteBounds;
                return true;
            }

            return false;
        }

        private Bounds GetBounds()
        {
            return _renderer.sprite?.bounds ?? new Bounds();
        }

        public Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            return Math.MeasureComponentBounds2D(GetBounds(), node, size, min, max);
        }

        public bool TryGetScale(FlexalonNode node, out Vector3 scale)
        {
            var bounds = GetBounds();
            if (bounds.size == Vector3.zero) // Invalid bounds
            {
                scale = Vector3.one;
                return true;
            }

            var r = node.Result;
            scale = new Vector3(
                r.AdapterBounds.size.x / bounds.size.x,
                r.AdapterBounds.size.y / bounds.size.y,
                1);
            return true;
        }

        public bool TryGetRectSize(FlexalonNode node, out Vector2 rectSize)
        {
            rectSize = Vector2.zero;
            return false;
        }
    }

    internal class MeshRendererAdapter : InternalAdapter
    {
        private MeshRenderer _renderer;
        private MeshFilter _meshFilter;
        private Bounds _lastRendererBounds;

        public MeshRendererAdapter(MeshRenderer renderer, MeshFilter meshFilter)
        {
            _renderer = renderer;
            _meshFilter = meshFilter;
        }

        public bool IsValid()
        {
            return _renderer && _meshFilter && _meshFilter.sharedMesh && _renderer.enabled;
        }

        public bool SizeChanged()
        {
            if (_lastRendererBounds != _meshFilter.sharedMesh.bounds)
            {
                _lastRendererBounds = _meshFilter.sharedMesh.bounds;
                return true;
            }

            return false;
        }

        public Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            return Math.MeasureComponentBounds(_meshFilter.sharedMesh.bounds, node, size, min, max);
        }

        public bool TryGetScale(FlexalonNode node, out Vector3 scale)
        {
            var bounds = _meshFilter.sharedMesh.bounds;
            if (bounds.size == Vector3.zero) // Invalid bounds
            {
                scale = Vector3.one;
                return true;
            }

            var r = node.Result;
            scale = Math.Div(r.AdapterBounds.size, bounds.size);
            scale.x = scale.x > 100000f ? 1 : scale.x;
            scale.y = scale.y > 100000f ? 1 : scale.y;
            scale.z = scale.z > 100000f ? 1 : scale.z;
            return true;
        }

        public bool TryGetRectSize(FlexalonNode node, out Vector2 rectSize)
        {
            rectSize = Vector2.zero;
            return false;
        }
    }

    internal class RectTransformAdapter : InternalAdapter
    {
        private RectTransform _rectTransform;

        public RectTransformAdapter(RectTransform rectTransform)
        {
            _rectTransform = rectTransform;
        }

        public bool IsValid()
        {
            return _rectTransform;
        }

        public bool SizeChanged()
        {
            return false;
        }

        public Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            bool componentX = node.GetSizeType(Axis.X) == SizeType.Component;
            bool componentY = node.GetSizeType(Axis.Y) == SizeType.Component;
            bool componentZ = node.GetSizeType(Axis.Z) == SizeType.Component;

            var measureSize = new Vector3(
                componentX ? _rectTransform.rect.size.x : size.x,
                componentY ? _rectTransform.rect.size.y : size.y,
                componentZ ? 0 : size.z);

            measureSize = Math.Clamp(measureSize, min, max);

            var center = new Vector3((0.5f - _rectTransform.pivot.x) * measureSize.x, (0.5f - _rectTransform.pivot.y) * measureSize.y, 0);
            return new Bounds(center, measureSize);
        }

        public bool TryGetScale(FlexalonNode node, out Vector3 scale)
        {
            scale = Vector3.one;
            return true;
        }

        public bool TryGetRectSize(FlexalonNode node, out Vector2 rectSize)
        {
            rectSize = node.Result.AdapterBounds.size;
            return true;
        }
    }

#if UNITY_TMPRO
    internal class TextAdapter : InternalAdapter, Adapter
    {
        private TMPro.TMP_Text _text;

        public TextAdapter(TMPro.TMP_Text text)
        {
            _text = text;
        }

        public bool IsValid()
        {
            return _text && _text.enabled;
        }

        public bool SizeChanged()
        {
            return false;
        }

        public Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            bool componentX = node.GetSizeType(Axis.X) == SizeType.Component;
            bool componentY = node.GetSizeType(Axis.Y) == SizeType.Component;
            bool componentZ = node.GetSizeType(Axis.Z) == SizeType.Component;

            size = Math.Clamp(size, min, max);

            if (componentX && componentY)
            {
                size = Math.Clamp(_text.GetPreferredValues(max.x, 0), min, max);
            }
            else if (componentX && !componentY)
            {
                size.x = Mathf.Clamp(_text.GetPreferredValues(0, size.y).x, min.x, max.x);
            }
            else if (!componentX && componentY)
            {
                size.y = Mathf.Clamp(_text.GetPreferredValues(size.x, 0).y, min.y, max.y);
            }

            var bounds = new Bounds();
            bounds.size = size;
            return bounds;
        }

        public bool TryGetScale(FlexalonNode node, out Vector3 scale)
        {
            scale = Vector3.one;
            return true;
        }

        public bool TryGetRectSize(FlexalonNode node, out Vector2 rectSize)
        {
            rectSize = node.Result.AdapterBounds.size;
            return true;
        }
    }
#endif

#if UNITY_UI
    internal class CanvasAdapter : InternalAdapter
    {
        private Canvas _canvas;
        private RenderMode _lastRenderMode;
        private float _lastScaleFactor;
        private RectTransformAdapter _rectTransformAdapter;

        public CanvasAdapter(Canvas canvas)
        {
            _canvas = canvas;
            _rectTransformAdapter = new RectTransformAdapter(canvas.transform as RectTransform);
        }

        public bool IsValid()
        {
            return _canvas && _canvas.enabled;
        }

        public bool SizeChanged()
        {
            bool renderModeChanged = false;
            if (_lastRenderMode != _canvas.renderMode)
            {
                _lastRenderMode = _canvas.renderMode;
                renderModeChanged = true;
            }

            bool rectChanged = _rectTransformAdapter.SizeChanged();
            return renderModeChanged || rectChanged;
        }

        public Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            if (!_canvas.isRootCanvas || _canvas.renderMode == RenderMode.WorldSpace)
            {
                return _rectTransformAdapter.Measure(node, size, min, max);
            }
            else
            {
                Vector3 canvasSize = (_canvas.transform as RectTransform).rect.size;
                canvasSize.z = Mathf.Clamp(size.z, min.z, max.z); // Canvas XY size can't be changed
                return new Bounds(Vector3.zero, canvasSize);
            }
        }

        public bool TryGetScale(FlexalonNode node, out Vector3 scale)
        {
            if (_canvas.renderMode == RenderMode.WorldSpace)
            {
                return _rectTransformAdapter.TryGetScale(node, out scale);
            }

            scale = Vector3.one;
            return false;
        }

        public bool TryGetRectSize(FlexalonNode node, out Vector2 rectSize)
        {
            if (_canvas.renderMode == RenderMode.WorldSpace)
            {
                return _rectTransformAdapter.TryGetRectSize(node, out rectSize);
            }

            rectSize = Vector2.zero;
            return false;
        }
    }

    internal class LayoutElementAdapter : InternalAdapter
    {
        private MonoBehaviour _layoutElement;
        private RectTransformAdapter _rectTransformAdapter;

        public LayoutElementAdapter(MonoBehaviour layoutElement)
        {
            _rectTransformAdapter = new RectTransformAdapter(layoutElement.transform as RectTransform);
            _layoutElement = layoutElement;
        }

        public bool IsValid()
        {
            return _layoutElement && _layoutElement.enabled;
        }

        public bool SizeChanged()
        {
            return false;
        }

        public Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            var le = _layoutElement as UnityEngine.UI.ILayoutElement;
            var preferredSize = new Vector2(le.preferredWidth, le.preferredHeight);
            var rectTransform = _layoutElement.transform as RectTransform;
            bool componentX = node.GetSizeType(Axis.X) == SizeType.Component;
            bool componentY = node.GetSizeType(Axis.Y) == SizeType.Component;
            bool componentZ = node.GetSizeType(Axis.Z) == SizeType.Component;

            if (preferredSize.x < 0)
            {
                preferredSize.x = rectTransform.rect.size.x;
            }

            if (preferredSize.y < 0)
            {
                preferredSize.y = rectTransform.rect.size.y;
            }

            // Implement preserveAspect for the image component
            if (_layoutElement is UnityEngine.UI.Image image)
            {
                if (image.preserveAspect && (componentX ^ componentY))
                {
                    return Math.MeasureComponentBounds2D(new Bounds(Vector3.zero, preferredSize), node, size, min, max);
                }
                else
                {
                    preferredSize.x = rectTransform.rect.size.x;
                    preferredSize.y = rectTransform.rect.size.y;
                }
            }

            var measureSize = new Vector3(
                componentX ? preferredSize.x : size.x,
                componentY ? preferredSize.y : size.y,
                componentZ ? 0 : size.z);

            measureSize = Math.Clamp(measureSize, min, max);

            var center = new Vector3((0.5f - rectTransform.pivot.x) * measureSize.x, (0.5f - rectTransform.pivot.y) * measureSize.y, 0);
            return new Bounds(center, measureSize);
        }

        public bool TryGetScale(FlexalonNode node, out Vector3 scale)
        {
            return _rectTransformAdapter.TryGetScale(node, out scale);
        }

        public bool TryGetRectSize(FlexalonNode node, out Vector2 rectSize)
        {
            return _rectTransformAdapter.TryGetRectSize(node, out rectSize);
        }
    }
#endif

#if UNITY_PHYSICS
    internal class ColliderAdapter : InternalAdapter
    {
        private Collider _collider;
        private Bounds _lastBounds;

        public ColliderAdapter(Collider collider)
        {
            _collider = collider;
        }

        public bool IsValid()
        {
            return _collider && _collider.enabled;
        }

        public Bounds GetBounds()
        {
            if (_collider is BoxCollider)
            {
                var box = _collider as BoxCollider;
                return new Bounds(box.center, box.size);
            }
            else if (_collider is SphereCollider)
            {
                var sphere = _collider as SphereCollider;
                return new Bounds(sphere.center, Vector3.one * sphere.radius * 2);
            }
            else if (_collider is CapsuleCollider)
            {
                var capsule = _collider as CapsuleCollider;
                var size = Vector3.one * capsule.radius;
                size[capsule.direction] = capsule.height;
                return new Bounds(capsule.center, size);
            }
            else if (_collider is MeshCollider)
            {
                var mesh = _collider as MeshCollider;
                return mesh.sharedMesh?.bounds ?? new Bounds(Vector3.zero, Vector3.zero);
            }

            return new Bounds(Vector3.zero, Vector3.zero);
        }

        public bool SizeChanged()
        {
            var bounds = GetBounds();
            if (_lastBounds != bounds)
            {
                _lastBounds = bounds;
                return true;
            }

            return false;
        }

        public Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            return Math.MeasureComponentBounds(GetBounds(), node, size, min, max);
        }

        public bool TryGetScale(FlexalonNode node, out Vector3 scale)
        {
            scale = Math.SafeDivOne(node.Result.AdapterBounds.size, GetBounds().size);
            return true;
        }

        public bool TryGetRectSize(FlexalonNode node, out Vector2 rectSize)
        {
            rectSize = Vector2.zero;
            return false;
        }
    }
#endif

#if UNITY_PHYSICS_2D
    internal class Collider2DAdapter : InternalAdapter
    {
        private Collider2D _collider;
        private Bounds _lastBounds;

        public Collider2DAdapter(Collider2D collider)
        {
            _collider = collider;
        }

        public bool IsValid()
        {
            return _collider && _collider.enabled;
        }

        public Bounds GetBounds()
        {
            if (_collider is BoxCollider2D)
            {
                var box = _collider as BoxCollider2D;
                return new Bounds(box.offset, box.size + Vector2.one * box.edgeRadius);
            }
            else if (_collider is CircleCollider2D)
            {
                var circle = _collider as CircleCollider2D;
                return new Bounds(circle.offset, Vector2.one * circle.radius * 2);
            }
            else if (_collider is CapsuleCollider2D)
            {
                var capsule = _collider as CapsuleCollider2D;
                return new Bounds(capsule.offset, capsule.size);
            }

            return new Bounds(Vector3.zero, Vector3.zero);
        }

        public bool SizeChanged()
        {
            var bounds = GetBounds();
            if (_lastBounds != bounds)
            {
                _lastBounds = bounds;
                return true;
            }

            return false;
        }

        public Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            return Math.MeasureComponentBounds2D(GetBounds(), node, size, min, max);
        }

        public bool TryGetScale(FlexalonNode node, out Vector3 scale)
        {
            var bounds = GetBounds();
            if (bounds.size == Vector3.zero) // Invalid bounds
            {
                scale = Vector3.one;
                return true;
            }

            var r = node.Result;
            scale = new Vector3(
                r.AdapterBounds.size.x / bounds.size.x,
                r.AdapterBounds.size.y / bounds.size.y,
                1);
            return true;
        }

        public bool TryGetRectSize(FlexalonNode node, out Vector2 rectSize)
        {
            rectSize = Vector2.zero;
            return false;
        }
    }
#endif
}