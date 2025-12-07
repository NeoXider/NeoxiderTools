using UnityEngine;

namespace Flexalon
{
    /// <summary> A transform updater determines how an object
    /// gets from its current position to the computed layout position. </summary>
    public interface TransformUpdater
    {
        /// <summary> Called before the layout system starts updating any transforms.
        /// Use this to capture the transform position. </summary>
        /// <param name="node"> The node being updated. </param>
        void PreUpdate(FlexalonNode node);

        /// <summary> Called to update the position of the object. </summary>
        /// <param name="node"> The node being updated. </param>
        /// <param name="position"> The computed local position of the object. </param>
        bool UpdatePosition(FlexalonNode node, Vector3 position);

        /// <summary> Called to update the rotation of the object. </summary>
        /// <param name="node"> The node being updated. </param>
        /// <param name="rotation"> The computed local rotation of the object. </param>
        bool UpdateRotation(FlexalonNode node, Quaternion rotation);

        /// <summary> Called to update the scale of the object. </summary>
        /// <param name="node"> The node being updated. </param>
        /// <param name="scale"> The computed local scale of the object. </param>
        bool UpdateScale(FlexalonNode node, Vector3 scale);

        /// <summary> Called to update the rect of the object. </summary>
        /// <param name="node"> The node being updated. </param>
        /// <param name="rect"> The computed rect of the object. </param>
        bool UpdateRectSize(FlexalonNode node, Vector2 rect);
    }

    internal class DefaultTransformUpdater : TransformUpdater
    {
        private void RecordEdit(FlexalonNode node)
        {
#if UNITY_EDITOR
            if (Flexalon.RecordFrameChanges)
            {
                UnityEditor.Undo.RecordObject(node.GameObject.transform, "Flexalon transform change");
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(node.GameObject.transform);
            }
#endif
        }

        public void PreUpdate(FlexalonNode node)
        {
        }

        public bool UpdatePosition(FlexalonNode node, Vector3 position)
        {
            RecordEdit(node);
            node.GameObject.transform.localPosition = position;
            return true;
        }

        public bool UpdateRotation(FlexalonNode node, Quaternion rotation)
        {
            RecordEdit(node);
            node.GameObject.transform.localRotation = rotation;
            return true;
        }

        public bool UpdateScale(FlexalonNode node, Vector3 scale)
        {
            RecordEdit(node);
            node.GameObject.transform.localScale = scale;
            return true;
        }

        public bool UpdateRectSize(FlexalonNode node, Vector2 size)
        {
            RecordEdit(node);
            var rectTransform = node.GameObject.transform as RectTransform;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            return true;
        }
    }
}