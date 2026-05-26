using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Editor.Binding;
using Neo.Network;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Network
{
    /// <summary>
    ///     Friendly inspector for <see cref="NetworkContextActionRelay"/>.
    ///     Builds component / method dropdowns via reflection (reusing
    ///     <see cref="ComponentBindingInspectorShared"/> — the same helpers that drive NeoCondition pickers),
    ///     and only shows argument fields relevant to the chosen action and method signature.
    ///     Inherits from <see cref="CustomEditorBase"/> so it sits inside the Neoxider-branded inspector frame
    ///     just like <c>NeoConditionEditor</c>.
    /// </summary>
    [CustomEditor(typeof(NetworkContextActionRelay))]
    [CanEditMultipleObjects]
    public sealed class NetworkContextActionRelayEditor : CustomEditorBase
    {
        /// <summary>Enables the Neoxider frame and lets us render the body via <see cref="DrawCustomNeoxiderInspectorGUI"/>.</summary>
        protected override bool UseCustomNeoxiderInspectorGUI => true;

        /// <summary>
        ///     Required by <see cref="CustomEditorBase"/>. The relay does not use NeoCustom attribute drawers
        ///     (no <c>[Component]</c> / <c>[Resource]</c> auto-assignment fields), so this stays empty —
        ///     same as components whose fields are wired directly in the Inspector.
        /// </summary>
        protected override void ProcessAttributeAssignments()
        {
        }

        private static readonly Color SourceAccent = new(0.28f, 0.62f, 0.98f, 1f);
        private static readonly Color TargetAccent = new(0.26f, 0.82f, 0.52f, 1f);
        private static readonly Color ActionAccent = new(1f, 0.64f, 0.22f, 1f);
        private static readonly Color NetworkingAccent = new(0.70f, 0.42f, 0.98f, 1f);

        private static readonly string MethodNoneLabel = "<none>";

        // Cached dropdown data for the currently inspected target. Rebuilt every OnInspectorGUI.
        private readonly List<string> _componentDisplayNames = new();
        private readonly List<string> _componentFullNames = new();
        private readonly List<string> _methodLabels = new();
        private readonly List<MethodInfo> _methodInfos = new();
        private readonly List<MethodKind> _methodKinds = new();

        private enum MethodKind
        {
            NoArg,
            Bool,
            Float,
            String,
            GameObject
        }

        /// <summary>Renders the entire body inside the Neoxider frame (CustomEditorBase handles label + rainbow line + Actions).</summary>
        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            DrawSection("Context", SourceAccent, DrawContextSection);
            EditorGUILayout.Space(4);

            DrawSection("Target", TargetAccent, DrawTargetSection);
            EditorGUILayout.Space(4);

            DrawSection("Action", ActionAccent, DrawActionSection);
            EditorGUILayout.Space(4);

            DrawSection("Networking", NetworkingAccent, DrawNetworkingSection);
            EditorGUILayout.Space(4);

            DrawSection("Diagnostics", new Color(0.86f, 0.86f, 0.26f, 1f), DrawDiagnosticsSection);
            EditorGUILayout.Space(4);

            DrawSection("Editor Helpers", new Color(0.62f, 0.62f, 0.62f, 1f), DrawEditorHelpersSection);

            // Reuse the same Events foldout that NeoConditionEditor uses (auto-discovers UnityEvents,
            // collapsible, listener counts, broken-listener warnings). Matches the rest of the project's look.
            DrawCollapsibleUnityEvents();

            serializedObject.ApplyModifiedProperties();
        }

        // ────────────────────── Context ──────────────────────

        private void DrawContextSection()
        {
            SerializedProperty isNetworked = serializedObject.FindProperty("isNetworked");
            SerializedProperty contextSource = serializedObject.FindProperty("_contextSource");
            SerializedProperty rootMode = serializedObject.FindProperty("_rootMode");
            SerializedProperty explicitContext = serializedObject.FindProperty("_explicitContext");

            if (isNetworked != null)
            {
                EditorGUILayout.PropertyField(isNetworked,
                    new GUIContent("Is Networked",
                        "When ON, the action is dispatched through Mirror so every client sees it. When OFF, the action runs only locally."));
            }

            EditorGUILayout.PropertyField(contextSource,
                new GUIContent("Source",
                    "Where the context object comes from. EventArgument uses the Collider/GameObject passed by the event (e.g. OnTriggerEnter)."));

            if (contextSource.enumValueIndex == (int)NetworkContextSourceMode.ExplicitObject)
            {
                EditorGUILayout.PropertyField(explicitContext,
                    new GUIContent("Explicit Context",
                        "The fixed GameObject used as the context root. Must contain a NetworkIdentity when running networked."));
            }

            EditorGUILayout.PropertyField(rootMode,
                new GUIContent("Root Mode",
                    "How to climb from the raw context object up to a stable root (usually NetworkIdentity in parents)."));
        }

        // ────────────────────── Target ──────────────────────

        private void DrawTargetSection()
        {
            SerializedProperty targetMode = serializedObject.FindProperty("_targetMode");
            SerializedProperty targetName = serializedObject.FindProperty("_targetName");
            SerializedProperty targetPath = serializedObject.FindProperty("_targetPath");
            SerializedProperty targetComponentType = serializedObject.FindProperty("_targetComponentType");
            SerializedProperty includeInactive = serializedObject.FindProperty("_includeInactive");

            EditorGUILayout.PropertyField(targetMode,
                new GUIContent("Target Mode",
                    "How to select the GameObject the action will run on, relative to the resolved root."));

            switch (targetMode.enumValueIndex)
            {
                case (int)NetworkContextTargetMode.Root:
                    EditorGUILayout.HelpBox("Action runs on the root GameObject (the resolved context).",
                        MessageType.Info);
                    break;
                case (int)NetworkContextTargetMode.ChildByName:
                    EditorGUILayout.PropertyField(targetName, new GUIContent("Child Name",
                        "Recursively searches for a child with this exact name. Use the editor preview target to verify."));
                    break;
                case (int)NetworkContextTargetMode.ChildByPath:
                    EditorGUILayout.PropertyField(targetPath, new GUIContent("Child Path",
                        "Transform.Find path from the root (e.g. \"Visual/Sphere\")."));
                    break;
                case (int)NetworkContextTargetMode.ChildByComponent:
                    DrawComponentTypeDropdown(
                        new GUIContent("Child by Component",
                            "Pick a component type — the action runs on the first GameObject under root that owns it."),
                        targetComponentType);
                    break;
            }

            EditorGUILayout.PropertyField(includeInactive,
                new GUIContent("Include Inactive",
                    "When searching by name/path/component, include inactive children."));
        }

        // ────────────────────── Action ──────────────────────

        private void DrawActionSection()
        {
            SerializedProperty action = serializedObject.FindProperty("_action");
            SerializedProperty boolValue = serializedObject.FindProperty("_boolValue");
            SerializedProperty floatValue = serializedObject.FindProperty("_floatValue");
            SerializedProperty stringValue = serializedObject.FindProperty("_stringValue");
            SerializedProperty messageName = serializedObject.FindProperty("_messageName");
            SerializedProperty methodComponentType = serializedObject.FindProperty("_methodComponentType");
            SerializedProperty methodName = serializedObject.FindProperty("_methodName");
            SerializedProperty methodArgumentMode = serializedObject.FindProperty("_methodArgumentMode");

            EditorGUILayout.PropertyField(action,
                new GUIContent("Action",
                    "What the relay does after resolving the target. Use Invoke Component Method for full reflection access."));

            switch (action.enumValueIndex)
            {
                case (int)NetworkContextActionType.InvokeEventsOnly:
                    EditorGUILayout.HelpBox(
                        "Fires UnityEvents only — wire any logic via OnNetworkTriggered / OnTargetResolved.",
                        MessageType.Info);
                    break;

                case (int)NetworkContextActionType.SetActive:
                    EditorGUILayout.PropertyField(boolValue,
                        new GUIContent("Set Active",
                            "Active state to apply to the target GameObject (calls GameObject.SetActive)."));
                    break;

                case (int)NetworkContextActionType.SendMessage:
                    EditorGUILayout.PropertyField(messageName,
                        new GUIContent("Message Name",
                            "Method name to call via UnityEngine.SendMessage on the target. The method must take no parameters or one of the supported types."));
                    break;

                case (int)NetworkContextActionType.InvokeComponentMethod:
                    DrawInvokeComponentMethod(methodComponentType, methodName, methodArgumentMode,
                        boolValue, floatValue, stringValue);
                    break;
            }
        }

        private void DrawInvokeComponentMethod(SerializedProperty componentType, SerializedProperty methodName,
            SerializedProperty argumentMode, SerializedProperty boolValue, SerializedProperty floatValue,
            SerializedProperty stringValue)
        {
            GameObject preview = ResolvePreviewTarget();
            DrawComponentTypeDropdown(
                new GUIContent("Component", "Component on the target that owns the method to invoke."),
                componentType);

            Component previewComponent = preview != null
                ? ComponentBindingInspectorShared.FindComponentByTypeName(preview, componentType.stringValue)
                : null;

            if (previewComponent == null)
            {
                EditorGUILayout.PropertyField(methodName,
                    new GUIContent("Method",
                        "Method name on the component. Drag an Editor Preview Target to enable the dropdown."));
            }
            else
            {
                BuildMethodList(previewComponent);
                int idx = IndexOfMethod(methodName.stringValue, argumentMode.enumValueIndex);
                string[] labels = BuildMethodLabelsArray();

                EditorGUI.BeginChangeCheck();
                int newIdx = EditorGUILayout.Popup(
                    new GUIContent("Method",
                        "Picks a method from the chosen component. The list shows public instance methods with no arg or a single primitive/GameObject arg."),
                    idx, labels);
                if (EditorGUI.EndChangeCheck())
                {
                    ApplyMethodSelection(newIdx, methodName, argumentMode);
                }
            }

            DrawArgumentField(argumentMode, boolValue, floatValue, stringValue);
        }

        private void DrawArgumentField(SerializedProperty argumentMode, SerializedProperty boolValue,
            SerializedProperty floatValue, SerializedProperty stringValue)
        {
            EditorGUILayout.PropertyField(argumentMode,
                new GUIContent("Argument Mode",
                    "How the argument passed to the method is produced. Auto-populated when you pick a method from the dropdown."));

            switch (argumentMode.enumValueIndex)
            {
                case (int)NetworkContextMethodArgumentMode.None:
                    break;
                case (int)NetworkContextMethodArgumentMode.Bool:
                    EditorGUILayout.PropertyField(boolValue, new GUIContent("Bool Value"));
                    break;
                case (int)NetworkContextMethodArgumentMode.Float:
                    EditorGUILayout.PropertyField(floatValue, new GUIContent("Float Value"));
                    break;
                case (int)NetworkContextMethodArgumentMode.String:
                    EditorGUILayout.PropertyField(stringValue, new GUIContent("String Value"));
                    break;
                case (int)NetworkContextMethodArgumentMode.TargetGameObject:
                    EditorGUILayout.HelpBox("Passes the resolved target GameObject as the argument.",
                        MessageType.Info);
                    break;
                case (int)NetworkContextMethodArgumentMode.ContextGameObject:
                    EditorGUILayout.HelpBox("Passes the resolved context (root) GameObject as the argument.",
                        MessageType.Info);
                    break;
            }
        }

        // ────────────────────── Networking ──────────────────────

        private void DrawNetworkingSection()
        {
            SerializedProperty scope = serializedObject.FindProperty("_scope");
            SerializedProperty authority = serializedObject.FindProperty("_authorityMode");
            SerializedProperty localOnly = serializedObject.FindProperty("_triggerOnlyForLocalContext");

            EditorGUILayout.PropertyField(scope, new GUIContent("Scope",
                "AllClients: every client (including host) applies. ServerOnly: only the server. OthersOnly: every client except the originator."));
            EditorGUILayout.PropertyField(authority, new GUIContent("Authority Mode",
                "Who is allowed to send Commands for this relay. None lets any client trigger; OwnerOnly restricts to the context owner; ServerOnly disables client-originated triggers."));

            if (localOnly != null)
            {
                EditorGUILayout.PropertyField(localOnly, new GUIContent("Trigger Only For Local Context",
                    "Recommended ON for physics triggers. Each client runs physics on every replicated collider, " +
                    "so a single player entering the trigger fires OnTriggerEnter on every client and produces duplicate messages. " +
                    "With this ON, only the client that actually owns the entering player dispatches; remote clients wait for the server's broadcast."));

                if (!localOnly.boolValue)
                {
                    EditorGUILayout.HelpBox(
                        "Trigger Only For Local Context is OFF — expect duplicate dispatches (one per connected client) for physics triggers.",
                        MessageType.Warning);
                }
            }
        }

        // ────────────────────── Diagnostics ──────────────────────

        private void DrawDiagnosticsSection()
        {
            SerializedProperty verboseProp = serializedObject.FindProperty("_verboseLogging");
            EditorGUILayout.PropertyField(verboseProp, new GUIContent("Verbose Logging",
                "Trace every step of dispatch and receipt: Trigger → Send → OnServer → Broadcast → OnClient → Apply. " +
                "Turn on while reproducing a multiplayer issue, then check the console on host AND each client."));

            if (verboseProp.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "Verbose logging is ON. Each host & client will print [NetworkContextActionRelay] entries while triggering.",
                    MessageType.Info);
            }
        }

        // ────────────────────── Editor Helpers ──────────────────────

        private void DrawEditorHelpersSection()
        {
            SerializedProperty preview = serializedObject.FindProperty("_editorPreviewTarget");
            EditorGUILayout.PropertyField(preview, new GUIContent("Preview Target",
                "Optional reference GameObject used ONLY by this inspector to populate component/method dropdowns. " +
                "Drop a representative player/template/prefab here."));

            EditorGUILayout.HelpBox(
                "Preview Target is editor-only — runtime always resolves the target via Context Source + Root Mode + Target Mode.",
                MessageType.None);
        }

        // ────────────────────── Dropdown helpers ──────────────────────

        private void DrawComponentTypeDropdown(GUIContent label, SerializedProperty fullTypeNameProp)
        {
            GameObject preview = ResolvePreviewTarget();
            ComponentBindingInspectorShared.BuildComponentPickLists(preview, _componentDisplayNames,
                _componentFullNames);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (_componentFullNames.Count == 0)
                {
                    EditorGUILayout.PropertyField(fullTypeNameProp, label);
                    if (preview == null)
                    {
                        EditorGUILayout.LabelField("(drop Preview Target to enable dropdown)",
                            EditorStyles.miniLabel, GUILayout.Width(220));
                    }

                    return;
                }

                int currentIndex = ComponentBindingInspectorShared.IndexOfFullName(_componentFullNames,
                    fullTypeNameProp.stringValue);

                List<string> displayed = new(_componentDisplayNames) { "<custom string>" };
                if (currentIndex < 0)
                {
                    currentIndex = displayed.Count - 1;
                }

                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUILayout.Popup(label, currentIndex, displayed.ToArray());
                if (EditorGUI.EndChangeCheck())
                {
                    fullTypeNameProp.stringValue = newIndex < _componentFullNames.Count
                        ? _componentFullNames[newIndex]
                        : fullTypeNameProp.stringValue;
                }
            }

            // Always show the raw text field so it's clear what's stored and the user can type a full name manually.
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(fullTypeNameProp, new GUIContent("Stored Type Name",
                "Full type name actually serialized (Type.FullName). Type manually if your target isn't represented in the Preview Target."));
            EditorGUI.indentLevel--;
        }

        private GameObject ResolvePreviewTarget()
        {
            SerializedProperty previewProp = serializedObject.FindProperty("_editorPreviewTarget");
            if (previewProp != null && previewProp.objectReferenceValue is GameObject go)
            {
                return go;
            }

            if (target is MonoBehaviour mb)
            {
                return mb.gameObject;
            }

            return null;
        }

        private void BuildMethodList(Component component)
        {
            _methodLabels.Clear();
            _methodInfos.Clear();
            _methodKinds.Clear();

            if (component == null)
            {
                _methodLabels.Add(MethodNoneLabel);
                _methodInfos.Add(null);
                _methodKinds.Add(MethodKind.NoArg);
                return;
            }

            _methodLabels.Add(MethodNoneLabel);
            _methodInfos.Add(null);
            _methodKinds.Add(MethodKind.NoArg);

            Type type = component.GetType();
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.DeclaringType == typeof(object) || method.DeclaringType == typeof(Component) ||
                    method.DeclaringType == typeof(Behaviour) || method.DeclaringType == typeof(MonoBehaviour))
                {
                    continue;
                }

                if (method.IsSpecialName || method.IsGenericMethod || method.ReturnType != typeof(void))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length > 1)
                {
                    continue;
                }

                MethodKind? kind = parameters.Length == 0
                    ? MethodKind.NoArg
                    : ClassifyParameter(parameters[0].ParameterType);

                if (kind == null)
                {
                    continue;
                }

                string label = parameters.Length == 0
                    ? $"{method.Name} ()"
                    : $"{method.Name} ({FormatKind(kind.Value)})";
                _methodLabels.Add(label);
                _methodInfos.Add(method);
                _methodKinds.Add(kind.Value);
            }
        }

        private static MethodKind? ClassifyParameter(Type parameterType)
        {
            if (parameterType == typeof(bool))
            {
                return MethodKind.Bool;
            }

            if (parameterType == typeof(float) || parameterType == typeof(double) ||
                parameterType == typeof(int) || parameterType == typeof(long))
            {
                return MethodKind.Float;
            }

            if (parameterType == typeof(string))
            {
                return MethodKind.String;
            }

            if (parameterType == typeof(GameObject))
            {
                return MethodKind.GameObject;
            }

            return null;
        }

        private static string FormatKind(MethodKind kind)
        {
            return kind switch
            {
                MethodKind.NoArg => "",
                MethodKind.Bool => "bool",
                MethodKind.Float => "float",
                MethodKind.String => "string",
                MethodKind.GameObject => "GameObject",
                _ => "?"
            };
        }

        private string[] BuildMethodLabelsArray()
        {
            return _methodLabels.ToArray();
        }

        private int IndexOfMethod(string storedName, int storedArgMode)
        {
            if (string.IsNullOrEmpty(storedName))
            {
                return 0;
            }

            MethodKind storedKind = ArgModeToKind((NetworkContextMethodArgumentMode)storedArgMode);
            for (int i = 1; i < _methodInfos.Count; i++)
            {
                MethodInfo m = _methodInfos[i];
                if (m == null)
                {
                    continue;
                }

                if (m.Name != storedName)
                {
                    continue;
                }

                if (_methodKinds[i] != storedKind)
                {
                    continue;
                }

                return i;
            }

            for (int i = 1; i < _methodInfos.Count; i++)
            {
                MethodInfo m = _methodInfos[i];
                if (m != null && m.Name == storedName)
                {
                    return i;
                }
            }

            return 0;
        }

        private void ApplyMethodSelection(int index, SerializedProperty methodName, SerializedProperty argumentMode)
        {
            if (index <= 0 || index >= _methodInfos.Count)
            {
                methodName.stringValue = string.Empty;
                argumentMode.enumValueIndex = (int)NetworkContextMethodArgumentMode.None;
                return;
            }

            MethodInfo method = _methodInfos[index];
            if (method == null)
            {
                return;
            }

            methodName.stringValue = method.Name;
            argumentMode.enumValueIndex = (int)KindToArgMode(_methodKinds[index]);
        }

        private static MethodKind ArgModeToKind(NetworkContextMethodArgumentMode mode)
        {
            return mode switch
            {
                NetworkContextMethodArgumentMode.Bool => MethodKind.Bool,
                NetworkContextMethodArgumentMode.Float => MethodKind.Float,
                NetworkContextMethodArgumentMode.String => MethodKind.String,
                NetworkContextMethodArgumentMode.TargetGameObject => MethodKind.GameObject,
                NetworkContextMethodArgumentMode.ContextGameObject => MethodKind.GameObject,
                _ => MethodKind.NoArg
            };
        }

        private static NetworkContextMethodArgumentMode KindToArgMode(MethodKind kind)
        {
            return kind switch
            {
                MethodKind.Bool => NetworkContextMethodArgumentMode.Bool,
                MethodKind.Float => NetworkContextMethodArgumentMode.Float,
                MethodKind.String => NetworkContextMethodArgumentMode.String,
                MethodKind.GameObject => NetworkContextMethodArgumentMode.TargetGameObject,
                _ => NetworkContextMethodArgumentMode.None
            };
        }

        // ────────────────────── Section helpers ──────────────────────

        private static void DrawSection(string title, Color accent, Action body)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                GUIStyle titleStyle = new(EditorStyles.boldLabel) { fontSize = 12 };
                EditorGUI.LabelField(
                    new Rect(headerRect.x + 6f, headerRect.y, headerRect.width - 6f, headerRect.height),
                    title, titleStyle);
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(new Rect(headerRect.x - 4f, headerRect.y, 3f, headerRect.height), accent);
                }

                EditorGUILayout.Space(2);
                body();
            }
        }
    }
}
