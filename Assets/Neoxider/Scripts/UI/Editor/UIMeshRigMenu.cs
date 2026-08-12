using System;
using Neo.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UIImage = UnityEngine.UI.Image;

namespace Neo.UI.Editor
{
    public static class UIMeshRigMenu
    {
        private const string DefaultPanelSettingsPath = "Assets/Neoxider UI Mesh Rig Panel Settings.asset";

        [MenuItem("GameObject/UI/Neoxider UI Mesh Rig", false, 2050)]
        private static void Create(MenuCommand command)
        {
            GameObject requestedParent = command.context as GameObject;
            Transform parent = ResolveUiParent(requestedParent);
            GameObject rigObject = CreateRigObject("UI Mesh Rig", parent);
            RectTransform rect = (RectTransform)rigObject.transform;
            rect.sizeDelta = new Vector2(300f, 300f);
            rect.anchoredPosition = Vector2.zero;
            UIMeshRigGraphic rig = rigObject.GetComponent<UIMeshRigGraphic>();
            Sprite defaultSprite = FindDefaultSprite();
            if (defaultSprite != null)
            {
                rig.SetSource(defaultSprite, Color.white);
            }

            UIMeshRigEditorUtility.ApplyLayout(rig, UIMeshRigLayoutPreset.SimpleBounce, true);
            Selection.activeGameObject = rigObject;
        }

        // WHY: from Unity 6.4 world-space UI Toolkit renders through PanelRenderer, so that is what a fresh
        // host gets. UIDocument is created only on editors that do not have PanelRenderer at all.
        [MenuItem("GameObject/UI Toolkit/Neoxider UI Mesh Rig", false, 2050)]
        private static void CreateUIToolkit(MenuCommand command)
        {
            GameObject requestedParent = command.context as GameObject;
            GameObject rigObject = new GameObject("UI Mesh Rig (UI Toolkit)");
            Undo.RegisterCreatedObjectUndo(rigObject, "Create Neoxider UI Mesh Rig UI Toolkit host");
            if (requestedParent != null)
            {
                Undo.SetTransformParent(rigObject.transform, requestedParent.transform,
                    "Parent Neoxider UI Mesh Rig UI Toolkit host");
            }

            PanelSettings panelSettings = FindOrCreatePanelSettings();
            Component panelComponent;
#if UNITY_6000_4_OR_NEWER
            PanelRenderer panelRenderer = rigObject.AddComponent<PanelRenderer>();
            panelRenderer.panelSettings = panelSettings;
            panelComponent = panelRenderer;
#else
            UIDocument document = rigObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            panelComponent = document;
#endif

            UIMeshRigUIToolkitHost host = rigObject.AddComponent<UIMeshRigUIToolkitHost>();
            Sprite defaultSprite = FindDefaultSprite();
            if (defaultSprite != null)
            {
                host.SetSource(defaultSprite, Color.white);
            }

            host.SetLayoutPreset(UIMeshRigLayoutPreset.Character);
            host.Refresh();
            UIMeshRigEditorUtility.MarkChanged(rigObject, panelComponent, host);
            Selection.activeGameObject = rigObject;
        }

        [MenuItem("GameObject/2D Object/Neoxider UI Mesh Rig (Sprite Renderer)", false, 2051)]
        private static void CreateSpriteRenderer(MenuCommand command)
        {
            GameObject requestedParent = command.context as GameObject;
            GameObject rigObject = new GameObject(
                "UI Mesh Rig (Sprite Renderer)",
                typeof(SpriteRenderer),
                typeof(UIMeshRigSpriteRenderer));
            Undo.RegisterCreatedObjectUndo(rigObject, "Create Neoxider UI Mesh Rig sprite renderer");
            if (requestedParent != null)
            {
                Undo.SetTransformParent(rigObject.transform, requestedParent.transform,
                    "Parent Neoxider UI Mesh Rig sprite renderer");
                rigObject.transform.localPosition = Vector3.zero;
            }

            UIMeshRigSpriteRenderer rig = rigObject.GetComponent<UIMeshRigSpriteRenderer>();
            Sprite defaultSprite = FindDefaultSprite();
            if (defaultSprite != null)
            {
                rig.SetSource(defaultSprite, Color.white);
            }

            UIMeshRigEditorUtility.ApplyLayout(rig, UIMeshRigLayoutPreset.SimpleBounce, true);
            UIMeshRigEditorUtility.MarkChanged(rigObject, rig, rigObject.transform);
            Selection.activeGameObject = rigObject;
        }

        [MenuItem("GameObject/2D Object/Neoxider UI Mesh Rig (World)", false, 2050)]
        private static void CreateWorld(MenuCommand command)
        {
            GameObject requestedParent = command.context as GameObject;
            GameObject rigObject = new GameObject(
                "UI Mesh Rig (World)",
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(UIMeshRigWorldRenderer));
            Undo.RegisterCreatedObjectUndo(rigObject, "Create Neoxider UI Mesh Rig world renderer");
            if (requestedParent != null)
            {
                Undo.SetTransformParent(rigObject.transform, requestedParent.transform,
                    "Parent Neoxider UI Mesh Rig world renderer");
                rigObject.transform.localPosition = Vector3.zero;
            }

            UIMeshRigWorldRenderer rig = rigObject.GetComponent<UIMeshRigWorldRenderer>();
            Sprite defaultSprite = FindDefaultSprite();
            if (defaultSprite != null)
            {
                rig.SetSource(defaultSprite, Color.white);
            }

            rig.SetSize(new Vector2(3f, 3f));
            UIMeshRigWorldEditorUtility.ApplyLayout(rig, UIMeshRigLayoutPreset.FlagCloth, true);
            UIMeshRigEditorUtility.MarkChanged(rigObject, rig, rigObject.transform);
            Selection.activeGameObject = rigObject;
        }

        [MenuItem("Assets/Create/Neoxider/UI Mesh Rig (UI Toolkit UXML)", false, 2050)]
        private static void CreateUIToolkitUxml()
        {
            const string contents =
                "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" xmlns:neo=\"Neo.UI\">\n" +
                "    <neo:UIMeshRigElement name=\"ui-mesh-rig\" layout-preset=\"SimpleBounce\" " +
                "style=\"width: 300px; height: 300px;\" />\n" +
                "</ui:UXML>\n";
            Texture2D icon = EditorGUIUtility.IconContent("VisualTreeAsset Icon").image as Texture2D;
#if UNITY_6000_5_OR_NEWER
            // CreateAssetWithContent помечен obsolete УРОВНЯ ОШИБКИ начиная с Unity 6.5: его вызов
            // роняет компиляцию, а не просто предупреждает. Замена принимает Action<EntityId>.
            ProjectWindowUtil.CreateAssetWithTextContent("UIMeshRig.uxml", contents, icon);
#else
            ProjectWindowUtil.CreateAssetWithContent("UIMeshRig.uxml", contents, icon);
#endif
        }

        [MenuItem("CONTEXT/Image/Convert To Neoxider UI Mesh Rig")]
        private static void ConvertImage(MenuCommand command)
        {
            UIImage source = (UIImage)command.context;
            if (source.type != UIImage.Type.Simple)
            {
                // Как и предупреждение о конвертации: в консоль, а не в модальное окно — пункт вызывают
                // и автоматизацией через MCP, где модальный диалог вешает редактор.
                Debug.LogWarning(
                    "[UI Mesh Rig] Конвертация отменена: Image Type = " + source.type + ". Без изменения "
                    + "рендеринга конвертируются только Simple-изображения — переключите Image Type на Simple.",
                    source);
                return;
            }

            // Предупреждение идёт в консоль, а не в модальное окно: пункт вызывают и руками,
            // и автоматизацией через MCP в обычном редакторе, где модальный диалог вешает процесс.
            Debug.LogWarning(
                "[UI Mesh Rig] Image заменён на UI Mesh Rig на том же объекте. Таргетинг кнопок и раскладка "
                + "сохранены, но скрипты и AnimationClip, ссылающиеся на UnityEngine.UI.Image по типу, надо "
                + "проверить вручную; операция поддерживает Undo. Если ссылки должны уцелеть — используйте "
                + "пункт Create Non-Destructive Mesh Rig Child.",
                source);

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

            UIMeshRigEditorUtility.ApplyLayout(rig, UIMeshRigLayoutPreset.SimpleBounce, true);

            UIMeshRigEditorUtility.MarkChanged(targetObject, rig, targetObject.transform);
            Selection.activeGameObject = targetObject;
        }

        [MenuItem("CONTEXT/Image/Convert To Neoxider UI Mesh Rig", true)]
        private static bool ValidateConvertImage(MenuCommand command)
        {
            return command.context is UIImage;
        }

        [MenuItem("CONTEXT/Image/Create Non-Destructive Neoxider Mesh Rig Child")]
        private static void CreateNonDestructiveChild(MenuCommand command)
        {
            UIImage source = (UIImage)command.context;
            if (source.type != UIImage.Type.Simple)
            {
                Debug.LogWarning(
                    "[UI Mesh Rig] Дочерний риг не создан: Image Type = " + source.type
                    + ". Поддерживаются только Simple-изображения.",
                    source);
                return;
            }

            Sprite activeSprite = source.overrideSprite != null ? source.overrideSprite : source.sprite;
            Selectable[] selectables = source.GetComponents<Selectable>();
            bool[] retargetSelectable = new bool[selectables.Length];
            for (int index = 0; index < selectables.Length; index++)
            {
                retargetSelectable[index] = selectables[index].targetGraphic == source;
            }

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
            rig.enabled = source.enabled;
            Undo.RecordObject(source, "Hide source Image for UI mesh rig child");
            source.enabled = false;
            for (int index = 0; index < selectables.Length; index++)
            {
                if (!retargetSelectable[index])
                {
                    continue;
                }

                Undo.RecordObject(selectables[index], "Retarget interactive UI mesh rig child");
                selectables[index].targetGraphic = rig;
                UIMeshRigEditorUtility.MarkChanged(selectables[index]);
            }

            UIMeshRigEditorUtility.ApplyLayout(rig, UIMeshRigLayoutPreset.SimpleBounce, true);

            UIMeshRigEditorUtility.MarkChanged(source, rig, rect);
            Selection.activeGameObject = child;
        }

        [MenuItem("CONTEXT/Image/Create Non-Destructive Neoxider Mesh Rig Child", true)]
        private static bool ValidateCreateNonDestructiveChild(MenuCommand command)
        {
            return command.context is UIImage;
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

        private static Sprite FindDefaultSprite()
        {
            if (NeoxiderModulePackageInfoUtility.TryGetForAssembly(
                    typeof(UIMeshRigMenu).Assembly,
                    out NeoxiderModulePackageInfo info) && !string.IsNullOrEmpty(info.RootPath))
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(info.RootPath + "/NeoLogo.png");
                if (sprite != null)
                {
                    return sprite;
                }
            }

            string[] guids = AssetDatabase.FindAssets("NeoLogo t:Sprite");
            int index;
            for (index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                if (path.Contains("/Neoxider/") || path.StartsWith("Packages/com.neoxider.tools/"))
                {
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }

            return null;
        }

        private static PanelSettings FindOrCreatePanelSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:PanelSettings");
            if (guids.Length > 0)
            {
                string existingPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                PanelSettings existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(existingPath);
                if (existing != null)
                {
                    return existing;
                }
            }

            PanelSettings settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "Neoxider UI Mesh Rig Panel Settings";
            AssetDatabase.CreateAsset(settings, DefaultPanelSettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
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

            Canvas existingCanvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
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
            EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
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
