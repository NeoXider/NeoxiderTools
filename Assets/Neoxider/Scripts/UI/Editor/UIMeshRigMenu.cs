using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.UI.Editor
{
    public static class UIMeshRigMenu
    {
        [MenuItem("GameObject/UI/Neoxider UI Mesh Rig", false, 2050)]
        private static void Create(MenuCommand command)
        {
            GameObject requestedParent = command.context as GameObject;
            Transform parent = ResolveUiParent(requestedParent);
            GameObject rigObject = CreateRigObject("UI Mesh Rig", parent);
            RectTransform rect = (RectTransform)rigObject.transform;
            rect.sizeDelta = new Vector2(300f, 300f);
            rect.anchoredPosition = Vector2.zero;
            Selection.activeGameObject = rigObject;
        }

        [MenuItem("CONTEXT/Image/Convert To Neoxider UI Mesh Rig")]
        private static void ConvertImage(MenuCommand command)
        {
            Image source = (Image)command.context;
            if (source.type != Image.Type.Simple)
            {
                EditorUtility.DisplayDialog(
                    "UI Mesh Rig",
                    "Only Simple UI Images can be converted without changing their rendering. Change Image Type to Simple first.",
                    "OK");
                return;
            }

            if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                    "Convert Image In Place",
                    "This replaces the Image component on the same GameObject. Button targeting and layout are preserved, but custom scripts or AnimationClips that explicitly reference UnityEngine.UI.Image must be reviewed. The operation supports Undo.\n\nUse 'Create Non-Destructive Mesh Rig Child' when Image references must remain intact.",
                    "Convert In Place",
                    "Cancel"))
            {
                return;
            }

            Sprite activeSprite = source.overrideSprite != null ? source.overrideSprite : source.sprite;
            GameObject targetObject = source.gameObject;
            Color sourceColor = source.color;
            Material sourceMaterial = source.material;
            bool sourceRaycastTarget = source.raycastTarget;
            Vector4 sourceRaycastPadding = source.raycastPadding;
            bool sourceMaskable = source.maskable;
            bool sourcePreserveAspect = source.preserveAspect;
            bool sourceEnabled = source.enabled;
            Selectable[] selectables = targetObject.GetComponents<Selectable>();
            bool[] retargetSelectable = new bool[selectables.Length];
            for (int index = 0; index < selectables.Length; index++)
            {
                retargetSelectable[index] = selectables[index].targetGraphic == source;
            }

            Undo.DestroyObjectImmediate(source);
            UIMeshRigGraphic rig = Undo.AddComponent<UIMeshRigGraphic>(targetObject);
            rig.SetSource(activeSprite, sourceColor, sourceMaterial);
            rig.raycastTarget = sourceRaycastTarget;
            rig.SetInteractionRaycastPadding(sourceRaycastPadding);
            rig.maskable = sourceMaskable;
            rig.SetPreserveAspect(sourcePreserveAspect);
            rig.enabled = sourceEnabled;
            for (int index = 0; index < selectables.Length; index++)
            {
                if (!retargetSelectable[index])
                {
                    continue;
                }

                Undo.RecordObject(selectables[index], "Retarget interactive UI mesh rig graphic");
                selectables[index].targetGraphic = rig;
            }

            UIMeshRigEditorUtility.MarkChanged(targetObject, rig, targetObject.transform);
            Selection.activeGameObject = targetObject;
        }

        [MenuItem("CONTEXT/Image/Convert To Neoxider UI Mesh Rig", true)]
        private static bool ValidateConvertImage(MenuCommand command)
        {
            return command.context is Image;
        }

        [MenuItem("CONTEXT/Image/Create Non-Destructive Neoxider Mesh Rig Child")]
        private static void CreateNonDestructiveChild(MenuCommand command)
        {
            Image source = (Image)command.context;
            if (source.type != Image.Type.Simple)
            {
                EditorUtility.DisplayDialog("UI Mesh Rig", "Only Simple UI Images are supported.", "OK");
                return;
            }

            Sprite activeSprite = source.overrideSprite != null ? source.overrideSprite : source.sprite;
            GameObject child = CreateRigObject(source.gameObject.name + " Rigged", source.rectTransform);
            RectTransform rect = (RectTransform)child.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = source.rectTransform.pivot;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            UIMeshRigGraphic rig = child.GetComponent<UIMeshRigGraphic>();
            rig.SetSource(activeSprite, source.color, source.material);
            rig.raycastTarget = source.raycastTarget;
            rig.SetInteractionRaycastPadding(source.raycastPadding);
            rig.maskable = source.maskable;
            rig.SetPreserveAspect(source.preserveAspect);
            source.enabled = false;
            UIMeshRigEditorUtility.MarkChanged(source, rig, rect);
            Selection.activeGameObject = child;
        }

        [MenuItem("CONTEXT/Image/Create Non-Destructive Neoxider Mesh Rig Child", true)]
        private static bool ValidateCreateNonDestructiveChild(MenuCommand command)
        {
            return command.context is Image;
        }

        private static GameObject CreateRigObject(string objectName, Transform parent)
        {
            GameObject rigObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIMeshRigGraphic));
            Undo.RegisterCreatedObjectUndo(rigObject, "Create Neoxider UI Mesh Rig");
            if (parent != null)
            {
                Undo.SetTransformParent(rigObject.transform, parent, "Parent Neoxider UI Mesh Rig");
                rigObject.layer = parent.gameObject.layer;
            }
            else
            {
                int uiLayer = LayerMask.NameToLayer("UI");
                rigObject.layer = uiLayer >= 0 ? uiLayer : 0;
            }

            RectTransform rect = (RectTransform)rigObject.transform;
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            UIMeshRigEditorUtility.MarkChanged(rigObject, rect, rigObject.GetComponent<UIMeshRigGraphic>());
            return rigObject;
        }

        private static Transform ResolveUiParent(GameObject requestedParent)
        {
            if (requestedParent != null)
            {
                Canvas parentCanvas = requestedParent.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    return requestedParent.transform;
                }

                Canvas ownCanvas = requestedParent.GetComponent<Canvas>();
                if (ownCanvas != null)
                {
                    return requestedParent.transform;
                }
            }

            Canvas existingCanvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (existingCanvas != null)
            {
                return existingCanvas.transform;
            }

            return CreateCanvas().transform;
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Canvas for UI Mesh Rig");
            int uiLayer = LayerMask.NameToLayer("UI");
            canvasObject.layer = uiLayer >= 0 ? uiLayer : 0;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            EnsureEventSystem();
            UIMeshRigEditorUtility.MarkChanged(canvasObject, canvas, canvasObject.GetComponent<CanvasScaler>());
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = UnityEngine.Object.FindObjectOfType<EventSystem>();
            if (eventSystem != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            Type inputSystemModule = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem",
                false);
            if (inputSystemModule != null)
            {
                eventSystemObject.AddComponent(inputSystemModule);
            }
            else
            {
                eventSystemObject.AddComponent<StandaloneInputModule>();
            }
            Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create EventSystem for UI Mesh Rig");
        }
    }
}
