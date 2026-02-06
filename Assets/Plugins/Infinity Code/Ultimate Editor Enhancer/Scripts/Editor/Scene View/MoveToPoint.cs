/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.SceneTools
{
    [InitializeOnLoad]
    public static class MoveToPoint
    {
        static MoveToPoint()
        {
            SceneViewManager.AddListener(OnSceneGUI, SceneViewOrder.Early);
        }

        private static void OnSceneGUI(SceneView view)
        {
            if (!Prefs.moveToPoint) return;

            Event e = Event.current;
            if (e.type != EventType.KeyDown) return;
            if (e.keyCode != Prefs.moveToPointKeyCode || e.modifiers != Prefs.moveToPointModifiers) return;
            if (!(EditorWindow.mouseOverWindow is SceneView)) return;
            if (Selection.objects.Any(o =>
            {
                GameObject go = o as GameObject;
                return go == null || go.scene.name == null;
            })) return;

            e.Use();

            Bounds bounds = SelectionBoundsManager.bounds;
            if (bounds.extents == Vector3.zero) return;

            Undo.SetCurrentGroupName("Move To Point");
            int group = Undo.GetCurrentGroup();

            Vector3 worldPosition;
            Vector3 normal;

            SceneViewManager.GetWorldPositionAndNormal(out worldPosition, out normal, Selection.gameObjects);

            bool isRectTransform = SelectionBoundsManager.isRectTransform;
            if (!isRectTransform)
            {
                Vector3 cubeSide = MathHelper.NormalToCubeSide(normal);
                Vector3 extents = bounds.extents;
                extents.Scale(cubeSide);
                    
                Vector3 delta = worldPosition - bounds.center + new Vector3(extents.x, extents.y, extents.z);

                for (int i = 0; i < Selection.gameObjects.Length; i++)
                {
                    GameObject go = Selection.gameObjects[i];
                    Undo.RecordObject(go.transform, "Move To Point");
                    go.transform.Translate(delta, Space.World);
                }
            }
            else
            {
                Vector3 delta = worldPosition - bounds.center;

                for (int i = 0; i < Selection.gameObjects.Length; i++)
                {
                    GameObject go = Selection.gameObjects[i];
                    Undo.RecordObject(go.GetComponent<RectTransform>(), "Move To Point");
                    go.transform.Translate(delta, Space.World);
                }
            }

            Undo.CollapseUndoOperations(group);
        }
    }
}