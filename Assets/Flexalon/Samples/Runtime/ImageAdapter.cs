using UnityEngine;

namespace Flexalon.Samples
{
    // An example adapter which maintains the aspect ratio of the material's mainTexture.
    // Expects to be used with a Quad mesh.
    public class ImageAdapter : FlexalonComponent, Adapter
    {
        private Texture _texture;
        private Renderer _renderer;

        // Returns a size which will maintain the aspect ratio of whichever
        // axis is set to SizeType.Component.
        public Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            if (_texture)
            {
                var textureSize = new Vector2(_texture.width, _texture.height);
                return Math.MeasureComponentBounds2D(new Bounds(Vector3.zero, textureSize), node, size, min, max);
            }

            return new Bounds(Vector3.zero, size);
        }

        // Returns the desired scale which fits the measured bounds on X/Y.
        public bool TryGetScale(FlexalonNode node, out Vector3 scale)
        {
            if (_texture && _renderer)
            {
                var r = node.Result;
                Vector3 size = _renderer.GetComponent<MeshFilter>()?.sharedMesh.bounds.size ?? Vector3.one;
                scale = new Vector3(
                    r.AdapterBounds.size.x / size.x,
                    r.AdapterBounds.size.y / size.y,
                    1);
                return true;
            }

            scale = Vector3.one;
            return false;
        }

        public bool TryGetRectSize(FlexalonNode node, out Vector2 rectSize)
        {
            rectSize = Vector2.zero;
            return false;
        }

        protected override void UpdateProperties()
        {
            Node.SetAdapter(this);
        }

        protected override void ResetProperties()
        {
            Node.SetAdapter(null);
        }

        public override void DoUpdate()
        {
            // Detect if the texture changes, in which case we need to invalidate the layout.
            if (TryGetComponent<Renderer>(out var renderer))
            {
                _renderer = renderer;
                Texture texture = null;
                if (renderer.sharedMaterial)
                {
                    if (renderer.sharedMaterial.HasProperty("_BaseColorMap")) // HDRP.Lit
                    {
                        texture = renderer.sharedMaterial.GetTexture("_BaseColorMap");
                    }
                    else if (renderer.sharedMaterial.HasProperty("_BaseMap")) // URP.Lit
                    {
                        texture = renderer.sharedMaterial.GetTexture("_BaseMap");
                    }
                    else if (renderer.sharedMaterial.HasProperty("_MainTex")) // Standard
                    {
                        texture = renderer.sharedMaterial.GetTexture("_MainTex");
                    }
                }

                if (texture != _texture)
                {
                    _texture = texture;
                    MarkDirty();
                }
            }
        }
    }
}