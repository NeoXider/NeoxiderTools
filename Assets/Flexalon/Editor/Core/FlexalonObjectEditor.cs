using UnityEditor;
using UnityEngine;

namespace Flexalon.Editor
{
    [CustomEditor(typeof(FlexalonObject)), CanEditMultipleObjects]
    public class FlexalonObjectEditor : FlexalonComponentEditor
    {
        private SerializedProperty _width;
        private SerializedProperty _widthType;
        private SerializedProperty _widthOfParent;
        private SerializedProperty _height;
        private SerializedProperty _heightType;
        private SerializedProperty _heightOfParent;
        private SerializedProperty _depth;
        private SerializedProperty _depthType;
        private SerializedProperty _depthOfParent;
        private SerializedProperty _minWidth;
        private SerializedProperty _minWidthType;
        private SerializedProperty _minWidthOfParent;
        private SerializedProperty _minHeight;
        private SerializedProperty _minHeightType;
        private SerializedProperty _minHeightOfParent;
        private SerializedProperty _minDepth;
        private SerializedProperty _minDepthType;
        private SerializedProperty _minDepthOfParent;
        private SerializedProperty _maxWidth;
        private SerializedProperty _maxWidthType;
        private SerializedProperty _maxWidthOfParent;
        private SerializedProperty _maxHeight;
        private SerializedProperty _maxHeightType;
        private SerializedProperty _maxHeightOfParent;
        private SerializedProperty _maxDepth;
        private SerializedProperty _maxDepthType;
        private SerializedProperty _maxDepthOfParent;
        private SerializedProperty _offset;
        private SerializedProperty _rotation;
        private SerializedProperty _scale;
        private SerializedProperty _marginLeft;
        private SerializedProperty _marginRight;
        private SerializedProperty _marginTop;
        private SerializedProperty _marginBottom;
        private SerializedProperty _marginFront;
        private SerializedProperty _marginBack;
        private SerializedProperty _paddingLeft;
        private SerializedProperty _paddingRight;
        private SerializedProperty _paddingTop;
        private SerializedProperty _paddingBottom;
        private SerializedProperty _paddingFront;
        private SerializedProperty _paddingBack;
        private SerializedProperty _skipLayout;
        private SerializedProperty _useDefaultAdapter;

        private static readonly int ValueWidth = 50;

        [MenuItem("GameObject/Flexalon/Flexalon Object")]
        public static void Create(MenuCommand command)
        {
            FlexalonComponentEditor.Create<FlexalonObject>("Flexalon Object", command.context);
        }

        void OnEnable()
        {
            _width = serializedObject.FindProperty("_width");
            _widthType = serializedObject.FindProperty("_widthType");
            _widthOfParent = serializedObject.FindProperty("_widthOfParent");
            _height = serializedObject.FindProperty("_height");
            _heightType = serializedObject.FindProperty("_heightType");
            _heightOfParent = serializedObject.FindProperty("_heightOfParent");
            _depth = serializedObject.FindProperty("_depth");
            _depthType = serializedObject.FindProperty("_depthType");
            _depthOfParent = serializedObject.FindProperty("_depthOfParent");
            _minWidth = serializedObject.FindProperty("_minWidth");
            _minWidthType = serializedObject.FindProperty("_minWidthType");
            _minWidthOfParent = serializedObject.FindProperty("_minWidthOfParent");
            _minHeight = serializedObject.FindProperty("_minHeight");
            _minHeightType = serializedObject.FindProperty("_minHeightType");
            _minHeightOfParent = serializedObject.FindProperty("_minHeightOfParent");
            _minDepth = serializedObject.FindProperty("_minDepth");
            _minDepthType = serializedObject.FindProperty("_minDepthType");
            _minDepthOfParent = serializedObject.FindProperty("_minDepthOfParent");
            _maxWidth = serializedObject.FindProperty("_maxWidth");
            _maxWidthType = serializedObject.FindProperty("_maxWidthType");
            _maxWidthOfParent = serializedObject.FindProperty("_maxWidthOfParent");
            _maxHeight = serializedObject.FindProperty("_maxHeight");
            _maxHeightType = serializedObject.FindProperty("_maxHeightType");
            _maxHeightOfParent = serializedObject.FindProperty("_maxHeightOfParent");
            _maxDepth = serializedObject.FindProperty("_maxDepth");
            _maxDepthType = serializedObject.FindProperty("_maxDepthType");
            _maxDepthOfParent = serializedObject.FindProperty("_maxDepthOfParent");
            _offset = serializedObject.FindProperty("_offset");
            _rotation = serializedObject.FindProperty("_rotation");
            _scale = serializedObject.FindProperty("_scale");
            _marginLeft = serializedObject.FindProperty("_marginLeft");
            _marginRight = serializedObject.FindProperty("_marginRight");
            _marginTop = serializedObject.FindProperty("_marginTop");
            _marginBottom = serializedObject.FindProperty("_marginBottom");
            _marginFront = serializedObject.FindProperty("_marginFront");
            _marginBack = serializedObject.FindProperty("_marginBack");
            _paddingLeft = serializedObject.FindProperty("_paddingLeft");
            _paddingRight = serializedObject.FindProperty("_paddingRight");
            _paddingTop = serializedObject.FindProperty("_paddingTop");
            _paddingBottom = serializedObject.FindProperty("_paddingBottom");
            _paddingFront = serializedObject.FindProperty("_paddingFront");
            _paddingBack = serializedObject.FindProperty("_paddingBack");
            _skipLayout = serializedObject.FindProperty("_skipLayout");
            _useDefaultAdapter = serializedObject.FindProperty("_useDefaultAdapter");

            _sizeFoldout = EditorPrefs.GetBool("FlexalonSizeFoldout", true);
            _minMaxFoldout = EditorPrefs.GetBool("FlexalonMinMaxFoldout", false);
            _marginFoldout = EditorPrefs.GetBool("FlexalonMarginFoldout", false);
            _paddingFoldout = EditorPrefs.GetBool("FlexalonPaddingFoldout", false);
            _transformFoldout = EditorPrefs.GetBool("FlexalonTransformFoldout", false);
        }

        void OnDisable()
        {
            EditorPrefs.SetBool("FlexalonSizeFoldout", _sizeFoldout);
            EditorPrefs.SetBool("FlexalonMinMaxFoldout", _minMaxFoldout);
            EditorPrefs.SetBool("FlexalonMarginFoldout", _marginFoldout);
            EditorPrefs.SetBool("FlexalonPaddingFoldout", _paddingFoldout);
            EditorPrefs.SetBool("FlexalonTransformFoldout", _transformFoldout);
        }

        private static bool _sizeFoldout;
        private static bool _minMaxFoldout;
        private static bool _marginFoldout;
        private static bool _paddingFoldout;
        private static bool _transformFoldout;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ForceUpdateButton();

            _sizeFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_sizeFoldout, "Size");
            if (_sizeFoldout)
            {
                CreateSizeProperty("Width", _widthType, _width, _widthOfParent);
                CreateSizeProperty("Height", _heightType, _height, _heightOfParent);
                CreateSizeProperty("Depth", _depthType, _depth, _depthOfParent);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();


            _minMaxFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_minMaxFoldout, "Min / Max");
            if (_minMaxFoldout)
            {
                CreateMinMaxSizeProperty("Width", _minWidthType, _minWidth, _minWidthOfParent, _maxWidthType, _maxWidth, _maxWidthOfParent);
                CreateMinMaxSizeProperty("Height", _minHeightType, _minHeight, _minHeightOfParent, _maxHeightType, _maxHeight, _maxHeightOfParent);
                CreateMinMaxSizeProperty("Depth", _minDepthType, _minDepth, _minDepthOfParent, _maxDepthType, _maxDepth, _maxDepthOfParent);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _marginFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_marginFoldout, "Margin");
            if (_marginFoldout)
            {
                CreateSpacingProperty("Left", "Right", _marginLeft, _marginRight);
                CreateSpacingProperty("Top", "Bottom", _marginTop, _marginBottom);
                CreateSpacingProperty("Front", "Back", _marginFront, _marginBack);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _paddingFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_paddingFoldout, "Padding");
            if (_paddingFoldout)
            {
                CreateSpacingProperty("Left", "Right", _paddingLeft, _paddingRight);
                CreateSpacingProperty("Top", "Bottom", _paddingTop, _paddingBottom);
                CreateSpacingProperty("Front", "Back", _paddingFront, _paddingBack);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _transformFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_transformFoldout, "Transform");
            if (_transformFoldout)
            {
                EditorGUILayout.PropertyField(_offset);
                EditorGUILayout.PropertyField(_rotation);
                EditorGUILayout.PropertyField(_scale);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.PropertyField(_skipLayout);
            EditorGUILayout.PropertyField(_useDefaultAdapter);

            ApplyModifiedProperties();
        }

        private void CreateSizeProperty(string label, SerializedProperty typeProperty, SerializedProperty fixedProperty, SerializedProperty ofParentProperty)
        {
            EditorGUILayout.BeginHorizontal();

            bool showTypeLabel = true;
            if (typeProperty.enumValueIndex == (int)SizeType.Fixed)
            {
                EditorGUILayout.PropertyField(fixedProperty, new GUIContent(label));
                showTypeLabel = false;
            }
            else if (typeProperty.enumValueIndex == (int)SizeType.Fill)
            {
                EditorGUILayout.PropertyField(ofParentProperty, new GUIContent(label));
                showTypeLabel = false;
            }

            EditorGUILayout.PropertyField(typeProperty, showTypeLabel ? new GUIContent(label) : GUIContent.none);

            EditorGUILayout.EndHorizontal();
        }

        private void CreateMinMaxSizeProperty(string label, SerializedProperty minTypeProperty, SerializedProperty minFixedProperty, SerializedProperty minOfParentProperty,
            SerializedProperty maxTypeProperty, SerializedProperty maxFixedProperty, SerializedProperty maxOfParentProperty)
        {
            EditorGUILayout.BeginHorizontal();

            var labelWidth = EditorGUIUtility.labelWidth;

            if (minTypeProperty.enumValueIndex == (int)MinMaxSizeType.Fixed)
            {
                EditorGUILayout.PropertyField(minFixedProperty, new GUIContent(label), GUILayout.Width(EditorGUIUtility.labelWidth + ValueWidth + 3));
            }
            else if (minTypeProperty.enumValueIndex == (int)MinMaxSizeType.Fill)
            {
                EditorGUILayout.PropertyField(minOfParentProperty, new GUIContent(label), GUILayout.Width(EditorGUIUtility.labelWidth + ValueWidth + 3));
            }
            else
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("-", GUILayout.Width(ValueWidth));
                EditorGUI.EndDisabledGroup();
            }

            EditorGUIUtility.labelWidth = 20;

            EditorGUILayout.PropertyField(minTypeProperty, GUIContent.none);

            if (maxTypeProperty.enumValueIndex == (int)MinMaxSizeType.Fixed)
            {
                EditorGUILayout.PropertyField(maxFixedProperty, new GUIContent(" "), GUILayout.Width(EditorGUIUtility.labelWidth + ValueWidth + 3));
            }
            else if (maxTypeProperty.enumValueIndex == (int)MinMaxSizeType.Fill)
            {
                EditorGUILayout.PropertyField(maxOfParentProperty, new GUIContent(" "), GUILayout.Width(EditorGUIUtility.labelWidth + ValueWidth + 3));
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.LabelField(" ", GUILayout.Width(EditorGUIUtility.labelWidth));
                EditorGUILayout.TextField("-", GUILayout.Width(ValueWidth));
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.PropertyField(maxTypeProperty, GUIContent.none);

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUILayout.EndHorizontal();
        }

        private void CreateSpacingProperty(string label1, string label2, SerializedProperty property1, SerializedProperty property2)
        {
            EditorGUILayout.BeginHorizontal();
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUILayout.PropertyField(property1, new GUIContent(label1));
            EditorGUIUtility.labelWidth = 50;
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(property2, new GUIContent(label2));
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUILayout.EndHorizontal();
        }

        private void CreateSizeProperty2(SerializedProperty typeProperty, SerializedProperty fixedProperty, SerializedProperty ofParentProperty,
            SerializedProperty minTypeProperty, SerializedProperty minFixedProperty, SerializedProperty minOfParentProperty,
            SerializedProperty maxTypeProperty, SerializedProperty maxFixedProperty, SerializedProperty maxOfParentProperty,
            SerializedProperty margin1, SerializedProperty margin2, SerializedProperty padding1, SerializedProperty padding2)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth - 54);
            EditorGUILayout.LabelField("Min/Max", GUILayout.Width(54));
            if (minTypeProperty.enumValueIndex == (int)MinMaxSizeType.Fixed)
            {
                EditorGUILayout.PropertyField(minFixedProperty, GUIContent.none);
            }
            else if (minTypeProperty.enumValueIndex == (int)MinMaxSizeType.Fill)
            {
                EditorGUILayout.PropertyField(minOfParentProperty, GUIContent.none);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("-");
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.PropertyField(minTypeProperty, GUIContent.none);

            if (maxTypeProperty.enumValueIndex == (int)MinMaxSizeType.Fixed)
            {
                EditorGUILayout.PropertyField(maxFixedProperty, GUIContent.none);
            }
            else if (maxTypeProperty.enumValueIndex == (int)MinMaxSizeType.Fill)
            {
                EditorGUILayout.PropertyField(maxOfParentProperty, GUIContent.none);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("-");
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.PropertyField(maxTypeProperty, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth - 54);
            EditorGUILayout.LabelField("Margin", GUILayout.Width(54));
            EditorGUILayout.PropertyField(margin1, GUIContent.none);
            EditorGUILayout.PropertyField(margin2, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth - 54);
            EditorGUILayout.LabelField("Padding", GUILayout.Width(54));
            EditorGUILayout.PropertyField(padding1, GUIContent.none);
            EditorGUILayout.PropertyField(padding2, GUIContent.none);
            EditorGUILayout.EndHorizontal();
        }
        void OnSceneGUI()
        {
            // Draw a box at the transforms position
            var script = target as FlexalonObject;
            var node = Flexalon.GetNode(script.gameObject);
            if (node == null || node.Result == null)
            {
                return;
            }

            var r = node.Result;

            if (node.Parent != null)
            {
                var layoutBoxScale = node.GetWorldBoxScale(false);
                var layoutRotation = script.transform.parent != null ? script.transform.parent.rotation * r.LayoutRotation : r.LayoutRotation;

                // Box used to layout this object, plus margins.
                Handles.color = new Color(1f, 1f, .2f, 1.0f);
                Handles.matrix = Matrix4x4.TRS(script.transform.position, layoutRotation, layoutBoxScale);
                Handles.DrawWireCube(r.RotatedAndScaledBounds.center + node.Margin.Center, r.RotatedAndScaledBounds.size + node.Margin.Size);

                // Box used to layout this object.
                Handles.color = new Color(.2f, 1f, .5f, 1.0f);
                Handles.matrix = Matrix4x4.TRS(script.transform.position, layoutRotation, layoutBoxScale);
                Handles.DrawWireCube(r.RotatedAndScaledBounds.center, r.RotatedAndScaledBounds.size);
            }

            // Box in which children are layed out. This is the box with handles on it.
            Handles.color = Color.cyan;
            var worldBoxScale = node.GetWorldBoxScale(true);
            Handles.matrix = Matrix4x4.TRS(node.GetWorldBoxPosition(worldBoxScale, false), script.transform.rotation, worldBoxScale);
            Handles.DrawWireCube(Vector3.zero, r.AdapterBounds.size);

            var id = 0;
            float result;
            if (script.WidthType == SizeType.Fixed)
            {
                if (CreateSizeHandles(id++, id++, r.AdapterBounds.size, 0, script, out result))
                {
                    Record(script);
                    script.Width = result;
                    MarkDirty(script);
                }
            }

            if (script.HeightType == SizeType.Fixed)
            {
                if (CreateSizeHandles(id++, id++, r.AdapterBounds.size, 1, script, out result))
                {
                    Record(script);
                    script.Height = result;
                    MarkDirty(script);
                }
            }

            if (script.DepthType == SizeType.Fixed)
            {
                if (CreateSizeHandles(id++, id++, r.AdapterBounds.size, 2, script, out result))
                {
                    Record(script);
                    script.Depth = result;
                    MarkDirty(script);
                }
            }
        }

        private bool CreateSizeHandles(int id1, int id2, Vector3 size, int axis, FlexalonObject script, out float result)
        {
            bool changed = false;
            result = 0;

            if (CreateSizeHandleOnSide(id1, size, axis, 1, script, out float r1))
            {
                result = r1;
                changed = true;
            }

            if (CreateSizeHandleOnSide(id2, size, axis, -1, script, out float r2))
            {
                result = r2;
                changed = true;
            }

            return changed;
        }

        private bool CreateSizeHandleOnSide(int id, Vector3 size, int axis, int positive, FlexalonObject script, out float result)
        {
            var cid = GUIUtility.GetControlID(id, FocusType.Passive);
            var p = new Vector3();
            p[axis] = size[axis] / 2 * positive;
            EditorGUI.BeginChangeCheck();
#if UNITY_2022_1_OR_NEWER
            Vector3 newPos = Handles.FreeMoveHandle(cid, p, HandleUtility.GetHandleSize(p) * 0.2f, Vector3.one * 0.1f, Handles.SphereHandleCap);
#else
            Vector3 newPos = Handles.FreeMoveHandle(cid, p, Quaternion.identity, HandleUtility.GetHandleSize(p) * 0.2f, Vector3.one * 0.1f, Handles.SphereHandleCap);
#endif
            if (EditorGUI.EndChangeCheck())
            {
                result = newPos[axis] * 2 * positive;
                return true;
            }

            result = 0;
            return false;
        }
    }
}