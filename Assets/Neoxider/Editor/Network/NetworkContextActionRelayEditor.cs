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
        private static readonly Color DiagnosticsAccent = new(0.86f, 0.86f, 0.26f, 1f);
        private static readonly Color HelpersAccent = new(0.62f, 0.62f, 0.62f, 1f);

        private static readonly string MethodNoneLabel = "<none>";
        private const string CustomStringLabel = "<custom string>";

        // WHY: this inspector used to build every label and tooltip below as a new GUIContent on each IMGUI
        // event, plus one Action per section and a copy of the component list for the popup.
        private static readonly GUIContent IsNetworkedContent = new("Is Networked",
            "When ON, the action is dispatched through Mirror so every client sees it. When OFF, the action runs only locally.");

        private static readonly GUIContent SourceContent = new("Source",
            "Where the context object comes from. EventArgument uses the Collider/GameObject passed by the event (e.g. OnTriggerEnter).");

        private static readonly GUIContent ExplicitContextContent = new("Explicit Context",
            "The fixed GameObject used as the context root. Must contain a NetworkIdentity when running networked.");

        private static readonly GUIContent RootModeContent = new("Root Mode",
            "How to climb from the raw context object up to a stable root (usually NetworkIdentity in parents).");

        private static readonly GUIContent TargetModeContent = new("Target Mode",
            "How to select the GameObject the action will run on, relative to the resolved root.");

        private static readonly GUIContent ChildNameContent = new("Child Name",
            "Recursively searches for a child with this exact name. Use the editor preview target to verify.");

        private static readonly GUIContent ChildPathContent = new("Child Path",
            "Transform.Find path from the root (e.g. \"Visual/Sphere\").");

        private static readonly GUIContent ChildByComponentContent = new("Child by Component",
            "Pick a component type — the action runs on the first GameObject under root that owns it.");

        private static readonly GUIContent IncludeInactiveContent = new("Include Inactive",
            "When searching by name/path/component, include inactive children.");

        private static readonly GUIContent ActionContent = new("Action",
            "What the relay does after resolving the target. Use Invoke Component Method for full reflection access.");

        private static readonly GUIContent SetActiveContent = new("Set Active",
            "Active state to apply to the target GameObject (calls GameObject.SetActive).");

        private static readonly GUIContent MessageNameContent = new("Message Name",
            "Method name to call via UnityEngine.SendMessage on the target. The method must take no parameters or one of the supported types.");

        private static readonly GUIContent ComponentContent = new("Component",
            "Component on the target that owns the method to invoke.");

        private static readonly GUIContent MethodFieldContent = new("Method",
            "Method name on the component. Drag an Editor Preview Target to enable the dropdown.");

        private static readonly GUIContent MethodPopupContent = new("Method",
            "Picks a method from the chosen component. The list shows public instance methods with no arg or a single primitive/GameObject arg.");

        private static readonly GUIContent ArgumentModeContent = new("Argument Mode",
            "How the argument passed to the method is produced. Auto-populated when you pick a method from the dropdown.");

        private static readonly GUIContent BoolValueContent = new("Bool Value");
        private static readonly GUIContent FloatValueContent = new("Float Value");
        private static readonly GUIContent StringValueContent = new("String Value");

        private static readonly GUIContent ScopeContent = new("Scope",
            "AllClients: every client (including host) applies. ServerOnly: only the server. OthersOnly: every client except the originator.");

        private static readonly GUIContent AuthorityModeContent = new("Authority Mode",
            "Who is allowed to send Commands for this relay. None lets any client trigger; OwnerOnly restricts to the context owner; ServerOnly disables client-originated triggers.");

        private static readonly GUIContent LocalOnlyContent = new("Trigger Only For Local Context",
            "Recommended ON for physics triggers. Each client runs physics on every replicated collider, " +
            "so a single player entering the trigger fires OnTriggerEnter on every client and produces duplicate messages. " +
            "With this ON, only the client that actually owns the entering player dispatches; remote clients wait for the server's broadcast.");

        private static readonly GUIContent VerboseLoggingContent = new("Verbose Logging",
            "Trace every step of dispatch and receipt: Trigger → Send → OnServer → Broadcast → OnClient → Apply. " +
            "Turn on while reproducing a multiplayer issue, then check the console on host AND each client.");

        private static readonly GUIContent PreviewTargetContent = new("Preview Target",
            "Optional reference GameObject used ONLY by this inspector to populate component/method dropdowns. " +
            "Drop a representative player/template/prefab here.");

        private static readonly GUIContent StoredTypeNameContent = new("Stored Type Name",
            "Full type name actually serialized (Type.FullName). Type manually if your target isn't represented in the Preview Target.");

        private static GUIStyle s_sectionTitleStyle;
        private static bool s_sectionTitleStyleProSkin;

        private readonly List<string> _componentPopupLabels = new();

        // WHY: the popup arrays are cached per inspector instance. Keying a static cache by this editor kept
        // destroyed inspectors and their lists alive.
        private string[] _componentPopupOptions;
        private string[] _methodPopupOptions;

        // WHY: Cached dropdown data for the currently inspected target. Rebuilt every OnInspectorGUI.
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

            BeginRelaySection("Context", SourceAccent);
            DrawContextSection();
            EndRelaySection();
            EditorGUILayout.Space(4);

            BeginRelaySection("Target", TargetAccent);
            DrawTargetSection();
            EndRelaySection();
            EditorGUILayout.Space(4);

            BeginRelaySection("Action", ActionAccent);
            DrawActionSection();
            EndRelaySection();
            EditorGUILayout.Space(4);

            BeginRelaySection("Networking", NetworkingAccent);
            DrawNetworkingSection();
            EndRelaySection();
            EditorGUILayout.Space(4);

            BeginRelaySection("Diagnostics", DiagnosticsAccent);
            DrawDiagnosticsSection();
            EndRelaySection();
            EditorGUILayout.Space(4);

            BeginRelaySection("Editor Helpers", HelpersAccent);
            DrawEditorHelpersSection();
            EndRelaySection();

            // WHY: Reuse the same Events foldout that NeoConditionEditor uses (auto-discovers UnityEvents,
            // collapsible, listener counts, broken-listener warnings). Matches the rest of the project's look.
            DrawCollapsibleUnityEvents();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawContextSection()
        {
            SerializedProperty isNetworked = serializedObject.FindProperty("isNetworked");
            SerializedProperty contextSource = serializedObject.FindProperty("_contextSource");
            SerializedProperty rootMode = serializedObject.FindProperty("_rootMode");
            SerializedProperty explicitContext = serializedObject.FindProperty("_explicitContext");

            if (isNetworked != null)
            {
                EditorGUILayout.PropertyField(isNetworked, IsNetworkedContent);
            }

            EditorGUILayout.PropertyField(contextSource, SourceContent);

            if (contextSource.hasMultipleDifferentValues ||
                contextSource.enumValueIndex == (int)NetworkContextSourceMode.ExplicitObject)
            {
                EditorGUILayout.PropertyField(explicitContext, ExplicitContextContent);
            }

            EditorGUILayout.PropertyField(rootMode, RootModeContent);
        }

        private void DrawTargetSection()
        {
            SerializedProperty targetMode = serializedObject.FindProperty("_targetMode");
            SerializedProperty targetName = serializedObject.FindProperty("_targetName");
            SerializedProperty targetPath = serializedObject.FindProperty("_targetPath");
            SerializedProperty targetComponentType = serializedObject.FindProperty("_targetComponentType");
            SerializedProperty includeInactive = serializedObject.FindProperty("_includeInactive");

            EditorGUILayout.PropertyField(targetMode, TargetModeContent);

            if (targetMode.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("The selected relays use different target modes.", MessageType.None);
            }
            else
            {
                switch (targetMode.enumValueIndex)
                {
                    case (int)NetworkContextTargetMode.Root:
                        EditorGUILayout.HelpBox("Action runs on the root GameObject (the resolved context).",
                            MessageType.Info);
                        break;
                    case (int)NetworkContextTargetMode.ChildByName:
                        EditorGUILayout.PropertyField(targetName, ChildNameContent);
                        break;
                    case (int)NetworkContextTargetMode.ChildByPath:
                        EditorGUILayout.PropertyField(targetPath, ChildPathContent);
                        break;
                    case (int)NetworkContextTargetMode.ChildByComponent:
                        DrawComponentTypeDropdown(ChildByComponentContent, targetComponentType);
                        break;
                }
            }

            EditorGUILayout.PropertyField(includeInactive, IncludeInactiveContent);
        }

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

            EditorGUILayout.PropertyField(action, ActionContent);

            if (action.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("The selected relays use different actions.", MessageType.None);
                return;
            }

            switch (action.enumValueIndex)
            {
                case (int)NetworkContextActionType.InvokeEventsOnly:
                    EditorGUILayout.HelpBox(
                        "Fires UnityEvents only — wire any logic via OnNetworkTriggered / OnTargetResolved.",
                        MessageType.Info);
                    break;

                case (int)NetworkContextActionType.SetActive:
                    EditorGUILayout.PropertyField(boolValue, SetActiveContent);
                    break;

                case (int)NetworkContextActionType.SendMessage:
                    EditorGUILayout.PropertyField(messageName, MessageNameContent);
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
            DrawComponentTypeDropdown(ComponentContent, componentType);

            Component previewComponent = preview != null
                ? ComponentBindingInspectorShared.FindComponentByTypeName(preview, componentType.stringValue)
                : null;

            if (previewComponent == null || methodName.hasMultipleDifferentValues)
            {
                EditorGUILayout.PropertyField(methodName, MethodFieldContent);
            }
            else
            {
                BuildMethodList(previewComponent);
                int idx = IndexOfMethod(methodName.stringValue, argumentMode.enumValueIndex);
                string[] labels = BuildMethodLabelsArray();

                EditorGUI.BeginChangeCheck();
                int newIdx = EditorGUILayout.Popup(MethodPopupContent, idx, labels);
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
            EditorGUILayout.PropertyField(argumentMode, ArgumentModeContent);

            if (argumentMode.hasMultipleDifferentValues)
            {
                return;
            }

            switch (argumentMode.enumValueIndex)
            {
                case (int)NetworkContextMethodArgumentMode.None:
                    break;
                case (int)NetworkContextMethodArgumentMode.Bool:
                    EditorGUILayout.PropertyField(boolValue, BoolValueContent);
                    break;
                case (int)NetworkContextMethodArgumentMode.Float:
                    EditorGUILayout.PropertyField(floatValue, FloatValueContent);
                    break;
                case (int)NetworkContextMethodArgumentMode.String:
                    EditorGUILayout.PropertyField(stringValue, StringValueContent);
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

        private void DrawNetworkingSection()
        {
            SerializedProperty scope = serializedObject.FindProperty("_scope");
            SerializedProperty authority = serializedObject.FindProperty("_authorityMode");
            SerializedProperty localOnly = serializedObject.FindProperty("_triggerOnlyForLocalContext");

            EditorGUILayout.PropertyField(scope, ScopeContent);
            EditorGUILayout.PropertyField(authority, AuthorityModeContent);

            if (localOnly != null)
            {
                EditorGUILayout.PropertyField(localOnly, LocalOnlyContent);

                if (!localOnly.hasMultipleDifferentValues && !localOnly.boolValue)
                {
                    EditorGUILayout.HelpBox(
                        "Trigger Only For Local Context is OFF — expect duplicate dispatches (one per connected client) for physics triggers.",
                        MessageType.Warning);
                }
            }
        }

        private void DrawDiagnosticsSection()
        {
            SerializedProperty verboseProp = serializedObject.FindProperty("_verboseLogging");
            EditorGUILayout.PropertyField(verboseProp, VerboseLoggingContent);

            if (verboseProp.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "Verbose logging is ON. Each host & client will print [NetworkContextActionRelay] entries while triggering.",
                    MessageType.Info);
            }
        }

        private void DrawEditorHelpersSection()
        {
            SerializedProperty preview = serializedObject.FindProperty("_editorPreviewTarget");
            EditorGUILayout.PropertyField(preview, PreviewTargetContent);

            EditorGUILayout.HelpBox(
                "Preview Target is editor-only — runtime always resolves the target via Context Source + Root Mode + Target Mode.",
                MessageType.None);
        }

        private void DrawComponentTypeDropdown(GUIContent label, SerializedProperty fullTypeNameProp)
        {
            GameObject preview = ResolvePreviewTarget();
            ComponentBindingInspectorShared.BuildComponentPickLists(preview, _componentDisplayNames,
                _componentFullNames);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (_componentFullNames.Count == 0 || fullTypeNameProp.hasMultipleDifferentValues)
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

                _componentPopupLabels.Clear();
                _componentPopupLabels.AddRange(_componentDisplayNames);
                _componentPopupLabels.Add(CustomStringLabel);
                if (currentIndex < 0)
                {
                    currentIndex = _componentPopupLabels.Count - 1;
                }

                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUILayout.Popup(label, currentIndex,
                    ComponentBindingInspectorShared.ReuseOptions(ref _componentPopupOptions, _componentPopupLabels));
                if (EditorGUI.EndChangeCheck())
                {
                    fullTypeNameProp.stringValue = newIndex < _componentFullNames.Count
                        ? _componentFullNames[newIndex]
                        : fullTypeNameProp.stringValue;
                }
            }

            // WHY: Always show the raw text field so it's clear what's stored and the user can type a full name manually.
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(fullTypeNameProp, StoredTypeNameContent);
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

        internal IReadOnlyList<MethodInfo> BuildMethodList(Component component)
        {
            _methodLabels.Clear();
            _methodInfos.Clear();
            _methodKinds.Clear();

            if (component == null)
            {
                _methodLabels.Add(MethodNoneLabel);
                _methodInfos.Add(null);
                _methodKinds.Add(MethodKind.NoArg);
                return _methodInfos;
            }

            _methodLabels.Add(MethodNoneLabel);
            _methodInfos.Add(null);
            _methodKinds.Add(MethodKind.NoArg);

            RelayMethod[] methods = GetRelayMethods(component.GetType());
            for (int i = 0; i < methods.Length; i++)
            {
                _methodLabels.Add(methods[i].Label);
                _methodInfos.Add(methods[i].Method);
                _methodKinds.Add(methods[i].Kind);
            }

            return _methodInfos;
        }

        private readonly struct RelayMethod
        {
            public readonly MethodInfo Method;
            public readonly MethodKind Kind;
            public readonly string Label;

            public RelayMethod(MethodInfo method, MethodKind kind, string label)
            {
                Method = method;
                Kind = kind;
                Label = label;
            }
        }

        // WHY: the method popup is rebuilt on every IMGUI event; a type's public methods only change with a domain
        // reload, so reflect and format them once per type.
        private static readonly Dictionary<Type, RelayMethod[]> s_relayMethods = new();

        private static RelayMethod[] GetRelayMethods(Type type)
        {
            if (s_relayMethods.TryGetValue(type, out RelayMethod[] cached))
            {
                return cached;
            }

            List<RelayMethod> methods = new();
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
                methods.Add(new RelayMethod(method, kind.Value, label));
            }

            cached = methods.ToArray();
            s_relayMethods[type] = cached;
            return cached;
        }

        private static MethodKind? ClassifyParameter(Type parameterType)
        {
            if (parameterType == typeof(bool))
            {
                return MethodKind.Bool;
            }

            // Float input can widen to double, but cannot bind an integral parameter by narrowing.
            if (parameterType == typeof(float) || parameterType == typeof(double))
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
            return ComponentBindingInspectorShared.ReuseOptions(ref _methodPopupOptions, _methodLabels);
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

        private static void BeginRelaySection(string title, Color accent)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            // WHY: the copied boldLabel bakes in the text colour of the skin it was made for, so it is rebuilt
            // after a light/dark skin switch.
            if (s_sectionTitleStyle == null || s_sectionTitleStyleProSkin != EditorGUIUtility.isProSkin)
            {
                s_sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
                s_sectionTitleStyleProSkin = EditorGUIUtility.isProSkin;
            }

            EditorGUI.LabelField(
                new Rect(headerRect.x + 6f, headerRect.y, headerRect.width - 6f, headerRect.height),
                title, s_sectionTitleStyle);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(headerRect.x - 4f, headerRect.y, 3f, headerRect.height), accent);
            }

            EditorGUILayout.Space(2);
        }

        private static void EndRelaySection()
        {
            EditorGUILayout.EndVertical();
        }
    }
}
