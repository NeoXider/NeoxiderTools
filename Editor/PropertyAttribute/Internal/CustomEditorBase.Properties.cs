using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor
{
    public abstract partial class CustomEditorBase
    {
        /// <summary>
        ///     The top-level layout of one inspected type, resolved once per editor: the visible properties in order,
        ///     their [Header] sections, the UnityEvents and the replication flags.
        /// </summary>
        /// <remarks>
        ///     WHY: the property pass runs on every IMGUI event. Walking the iterator, copying every property and
        ///     reading its name, type and path (each a native string) plus the header lookup cost several allocations
        ///     per field per event. The set of top-level visible fields of a type cannot change while the editor
        ///     lives, and cached SerializedProperty objects stay valid across <c>serializedObject.Update()</c>, the
        ///     same way Unity's own editors cache them in OnEnable.
        /// </remarks>
        private sealed class PropertyPlan
        {
            public SerializedObject Owner;
            public Type TargetType;
            public SerializedProperty Script;
            public readonly List<SerializedProperty> Properties = new();
            public readonly List<string> HeaderTitles = new();
            public readonly List<SerializedProperty> UnityEvents = new();
            public readonly List<HeaderSection> Sections = new();

            // WHY: keyed by the cached property instances themselves; displayName and tooltip are native strings
            // that every boolean row used to read twice per event.
            public readonly Dictionary<SerializedProperty, GUIContent> BoolLabels =
                new(ReferenceComparer.Instance);

            public bool HasAnyHeader;
            public SerializedProperty Networked;
            public ReplicatedMembers Replicated;
        }

        /// <summary>Reference identity for <see cref="SerializedProperty" /> keys of the property plan.</summary>
        private sealed class ReferenceComparer : IEqualityComparer<SerializedProperty>
        {
            public static readonly ReferenceComparer Instance = new();

            public bool Equals(SerializedProperty x, SerializedProperty y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(SerializedProperty obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }

        private PropertyPlan _plan;
        private int _planBuildCount;
        private SerializedProperty _suppressedBodyProperty;
        private Action _drawSuppressedBody;
        private SerializedProperty _noHeaderProperty;
        private Action _drawNoHeaderBody;

        /// <summary>How often this editor resolved its property plan (test hook: once per editor lifetime).</summary>
        internal int PropertyPlanBuildCount => _planBuildCount;

        private static readonly Dictionary<(Type, string), string> s_sectionFoldoutKeys = new();

        private PropertyPlan GetPropertyPlan()
        {
            SerializedObject owner = serializedObject;
            Type targetType = target != null ? target.GetType() : null;
            if (_plan != null && ReferenceEquals(_plan.Owner, owner) && _plan.TargetType == targetType)
            {
                return _plan;
            }

            _plan = BuildPropertyPlan(owner, targetType);
            _planBuildCount++;
            return _plan;
        }

        private PropertyPlan BuildPropertyPlan(SerializedObject owner, Type targetType)
        {
            PropertyPlan plan = new() { Owner = owner, TargetType = targetType };
            SerializedProperty iterator = owner.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.name == "m_Script")
                {
                    plan.Script = iterator.Copy();
                    continue;
                }

                if (IsUnityEventProperty(iterator))
                {
                    plan.UnityEvents.Add(iterator.Copy());
                    continue;
                }

                SerializedProperty copy = iterator.Copy();
                plan.Properties.Add(copy);
                if (copy.propertyType == SerializedPropertyType.Boolean)
                {
                    plan.BoolLabels[copy] = new GUIContent(copy.displayName, copy.tooltip);
                }

                string title = TryGetHeaderTitleForProperty(iterator);
                plan.HeaderTitles.Add(title);
                plan.HasAnyHeader |= !string.IsNullOrEmpty(title);
            }

            if (plan.HasAnyHeader)
            {
                BuildHeaderSections(plan);
            }

            plan.Replicated = GetReplicatedMembers(targetType);
            if (plan.Replicated != null)
            {
                SerializedProperty networked = owner.FindProperty("isNetworked") ?? owner.FindProperty("_isNetworked");
                plan.Networked = networked != null && networked.propertyType == SerializedPropertyType.Boolean
                    ? networked
                    : null;
            }

            return plan;
        }

        private void DrawNeoPropertiesWithCollapsibleUnityEvents()
        {
            if (serializedObject == null || target == null)
            {
                return;
            }

            serializedObject.Update();
            PropertyPlan plan = GetPropertyPlan();

            if (plan.Script != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(plan.Script, true);
                }
            }

            DrawHeaderSections(plan);

            if (plan.UnityEvents.Count > 0)
            {
                NeoInspectorStyles.Space(4);
                DrawUnityEventsFoldout(plan.UnityEvents);
            }

            DrawActionsFoldout();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeaderSections(PropertyPlan plan)
        {
            List<SerializedProperty> properties = plan.Properties;
            if (properties.Count == 0)
            {
                return;
            }

            if (!plan.HasAnyHeader)
            {
                for (int i = 0; i < properties.Count; i++)
                {
                    DrawPropertyFieldResolved(properties[i], false);
                }

                return;
            }

            Color accent = NeoxiderEditorGUI.SectionAccent(true);
            int minFieldsForCategory = Mathf.Max(0, CustomEditorSettings.MinFieldsForHeaderCategory);

            for (int i = 0; i < plan.Sections.Count; i++)
            {
                HeaderSection section = plan.Sections[i];
                if (section.IsWarningHeader)
                {
                    DrawWarningHeader(section.Title);
                    DrawSectionProperties(section);
                    continue;
                }

                bool shouldForceFoldout = ShouldForceFoldoutHeader(section);

                // WHY: the one-field shortcut has to yield to the force-foldout rule, or a section holding a
                // single list renders as a bare label while its neighbour holding a list plus two settings
                // renders as a folder - the same content styled two different ways in one inspector.
                if (!section.ExcludeFromMinFieldsRule && !shouldForceFoldout &&
                    (section.Properties.Count == 1 ||
                     (minFieldsForCategory > 0 && section.Properties.Count < minFieldsForCategory)))
                {
                    DrawPlainHeader(section.Title);
                    DrawSectionProperties(section);
                    continue;
                }

                string key = section.FoldoutKey;
                bool expanded = GetFoldoutState(key, true);
                float amount = GetFoldAmount(key, expanded);
                bool hasOverride = amount < 1f && SectionHasPrefabOverride(section);
                expanded = DrawNeoSectionHeader(expanded, amount, section.Title, section.Properties.Count, accent,
                    "Folder Icon", hasOverride);
                _neoFoldouts[key] = expanded;
                amount = GetFoldAmount(key, expanded);

                if (BeginSectionBody(amount, accent, out bool fading))
                {
                    EditorGUI.indentLevel++;
                    DrawSectionProperties(section);
                    EditorGUI.indentLevel--;
                    EndSectionBody(fading);
                }
            }
        }

        // WHY: a section starts at the one property carrying its [Header] and the rest of it has none, so the header
        // question is answered here instead of being reflected again for every drawn property.
        private void DrawSectionProperties(HeaderSection section)
        {
            for (int p = 0; p < section.Properties.Count; p++)
            {
                SerializedProperty property = section.Properties[p];
                if (property != null)
                {
                    DrawPropertyFieldResolved(property, section.StartsAtHeader && p == 0);
                }
            }
        }

        /// <summary>Foldout persistence key of a [Header] section, built once per (type, title).</summary>
        internal static string GetSectionFoldoutKey(Type targetType, string title)
        {
            (Type, string) cacheKey = (targetType, title);
            if (!s_sectionFoldoutKeys.TryGetValue(cacheKey, out string key))
            {
                key = $"{(targetType != null ? targetType.FullName : "Neo")}.NeoFoldout.Header.{title}";
                if (s_sectionFoldoutKeys.Count >= MaxReflectionCacheEntries)
                {
                    s_sectionFoldoutKeys.Clear();
                }

                s_sectionFoldoutKeys[cacheKey] = key;
            }

            return key;
        }

        private static bool ShouldForceFoldoutHeader(HeaderSection section)
        {
            if (section.Properties == null || section.Properties.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < section.Properties.Count; i++)
            {
                SerializedProperty property = section.Properties[i];
                if (property == null)
                {
                    continue;
                }

                if (property.isArray && property.propertyType != SerializedPropertyType.String)
                {
                    return true;
                }

                if (property.propertyType == SerializedPropertyType.Generic && property.hasVisibleChildren)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     True when a field of a closed section is overridden on this prefab instance; the closed header then
        ///     shows Unity's override marker instead of hiding the change.
        /// </summary>
        private bool SectionHasPrefabOverride(HeaderSection section)
        {
            if (target == null || !PrefabUtility.IsPartOfPrefabInstance(target))
            {
                return false;
            }

            for (int i = 0; i < section.Properties.Count; i++)
            {
                SerializedProperty property = section.Properties[i];
                if (property != null && property.prefabOverride && !property.isDefaultOverride)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class FoldMotion
        {
            public bool Target;
            public double Start;
            public float From;
            public float Value;
        }

        private readonly Dictionary<string, FoldMotion> _foldMotions = new();

        /// <summary>
        ///     Openness (0..1) of the foldout with <paramref name="key" />: eases towards <paramref name="expanded" />
        ///     over <see cref="NeoInspectorAnimation.FoldDuration" /> while transitions are on, snaps otherwise. The
        ///     value depends only on this frame's clock, so Layout and the following event lay out the same height.
        /// </summary>
        internal float GetFoldAmount(string key, bool expanded)
        {
            float target = expanded ? 1f : 0f;
            // WHY: without the per-frame clock (an editor that overrides OnInspectorGUI and never samples it) two
            // events of one frame could read different times and lay out different heights; such editors snap.
            if (string.IsNullOrEmpty(key) || _motionNow < 0.0)
            {
                return target;
            }

            if (!_foldMotions.TryGetValue(key, out FoldMotion motion))
            {
                motion = new FoldMotion { Target = expanded, Start = -1000.0, From = target, Value = target };
                _foldMotions[key] = motion;
                return target;
            }

            if (motion.Target != expanded)
            {
                motion.From = motion.Value;
                motion.Target = expanded;
                motion.Start = MotionNow;
            }

            if (motion.Value == target)
            {
                return target;
            }

            if (!NeoInspectorAnimation.TransitionsActive())
            {
                motion.Value = target;
                return target;
            }

            double duration = NeoInspectorAnimation.FoldDuration * Mathf.Abs(target - motion.From);
            float t = NeoInspectorAnimation.Progress(MotionNow, motion.Start, duration);
            motion.Value = t >= 1f ? target : Mathf.Lerp(motion.From, target, NeoInspectorAnimation.EaseOutCubic(t));
            if (t < 1f)
            {
                // WHY: the open/close lasts a fifth of a second; repaint the inspector every frame for it instead of
                // waiting for the throttled ticker, which would turn it into a few visible steps.
                Repaint();
            }

            return motion.Value;
        }

        /// <summary>
        ///     Opens the body of a section at openness <paramref name="amount" />: nothing at 0, a fade group (height
        ///     and alpha follow the amount) while animating, a plain inset column with the accent guide line at 1.
        ///     Pair a true result with <see cref="EndSectionBody" />.
        /// </summary>
        private static bool BeginSectionBody(float amount, Color accent, out bool fading)
        {
            fading = false;
            if (amount <= 0f)
            {
                return false;
            }

            if (amount < 1f)
            {
                fading = true;
                EditorGUILayout.BeginFadeGroup(amount);
            }

            Rect body = EditorGUILayout.BeginVertical(NeoInspectorStyles.BodyPanel);
            NeoxiderEditorGUI.DrawSectionBodyGuide(body, accent);
            return true;
        }

        private static void EndSectionBody(bool fading)
        {
            EditorGUILayout.EndVertical();
            if (fading)
            {
                EditorGUILayout.EndFadeGroup();
            }
        }

        private static bool IsDebugProperty(SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            string name = property.name ?? string.Empty;
            string path = property.propertyPath ?? string.Empty;

            return name.Contains("debug", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("gizmo", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("editor", StringComparison.OrdinalIgnoreCase) ||
                   path.Contains("debug", StringComparison.OrdinalIgnoreCase) ||
                   path.Contains("gizmo", StringComparison.OrdinalIgnoreCase);
        }

        private void DrawPropertyField(SerializedProperty property, bool suppressUnityHeaderDecorators)
        {
            if (property == null)
            {
                return;
            }

            DrawPropertyFieldResolved(property,
                suppressUnityHeaderDecorators && !string.IsNullOrEmpty(TryGetHeaderTitleForProperty(property)));
        }

        private void DrawPropertyFieldResolved(SerializedProperty property, bool suppressHeaderDecorator)
        {
            DrawNetworkReplicationMarker(property);

            if (property.propertyType == SerializedPropertyType.Boolean)
            {
                DrawBooleanToggle(property);
                return;
            }

            // WHY: we already render the [Header] title as our own section header, so suppress Unity's built-in
            // header decorator around EVERY draw path (collection/nested/plain) to avoid doubling the title —
            // previously arrays/generics returned early and their [Header] rendered a second time.
            if (suppressHeaderDecorator)
            {
                _drawSuppressedBody ??= DrawSuppressedBody;
                SerializedProperty previous = _suppressedBodyProperty;
                _suppressedBodyProperty = property;
                try
                {
                    RunWithHeaderDecoratorsSuppressed(property, _drawSuppressedBody);
                }
                finally
                {
                    _suppressedBodyProperty = previous;
                }

                return;
            }

            DrawPropertyFieldBody(property);
        }

        private void DrawSuppressedBody()
        {
            DrawPropertyFieldBody(_suppressedBodyProperty);
        }

        /// <summary>
        ///     Escape hatch for a derived inspector that has to own the rendering of ONE field while every
        ///     other field keeps the standard pass - a list that also accepts asset drops, for instance.
        ///     Return <c>true</c> once the property has been drawn; the base draw is then skipped.
        /// </summary>
        /// <remarks>
        ///     <paramref name="property" /> is a copy owned by this call: advancing it (<c>Next</c>/<c>NextVisible</c>
        ///     to walk children) is allowed and does not move the field the standard pass draws next.
        /// </remarks>
        protected virtual bool DrawCustomProperty(SerializedProperty property)
        {
            return false;
        }

        // WHY: -1 unknown, 0 no override, 1 the editor overrides DrawCustomProperty; resolved once per editor.
        private int _customPropertyHook = -1;

        private bool HasCustomPropertyHook()
        {
            if (_customPropertyHook < 0)
            {
                MethodInfo method = GetType().GetMethod(nameof(DrawCustomProperty),
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null,
                    new[] { typeof(SerializedProperty) }, null);
                _customPropertyHook = method != null && method.DeclaringType != typeof(CustomEditorBase) ? 1 : 0;
            }

            return _customPropertyHook == 1;
        }

        private void DrawPropertyFieldBody(SerializedProperty property)
        {
            // WHY: inside the body rather than in DrawPropertyField so the override still runs with the
            // [Header] decorator suppressed - the section header already printed that title.
            // WHY: the property pass hands out SerializedProperty objects cached for the editor's lifetime. An
            // override that walked the property with Next/NextVisible (a common idiom) would move the cached entry to
            // another path for good, so an override gets its own copy, as it did before the plan cached them.
            if (HasCustomPropertyHook() && DrawCustomProperty(property.Copy()))
            {
                return;
            }

            DrawStandardProperty(property);
        }

        /// <summary>
        ///     The normal draw for one property. Exposed so an override of <see cref="DrawCustomProperty" />
        ///     can still get the standard look and only add to it.
        /// </summary>
        protected void DrawStandardProperty(SerializedProperty property)
        {
            if (!CustomEditorSettings.UseDefaultListAndArrayDrawing && ShouldUseCollectionPropertyFoldout(property))
            {
                DrawCollectionPropertyFoldout(property);
                return;
            }

            int nestedChildren = GetNestedFoldoutChildCount(property);
            if (nestedChildren > 0)
            {
                DrawNestedPropertyFoldout(property, nestedChildren);
                return;
            }

            EditorGUILayout.PropertyField(property, true);
        }

        private void DrawCollectionPropertyFoldout(SerializedProperty property)
        {
            if (property == null)
            {
                return;
            }

            Color accent = IsDebugProperty(property)
                ? new Color(0.94f, 0.34f, 0.34f, 1f)
                : new Color(0.92f, 0.72f, 0.28f, 1f);

            property.isExpanded = DrawNestedFoldoutHeader(property, property.displayName, property.arraySize,
                property.isExpanded, accent);
            if (!property.isExpanded)
            {
                return;
            }

            DrawDirectChildren(property, accent);
        }

        private void DrawNestedPropertyFoldout(SerializedProperty property, int childCount)
        {
            if (property == null)
            {
                return;
            }

            Color accent = IsDebugProperty(property)
                ? new Color(0.92f, 0.30f, 0.30f, 1f)
                : new Color(0.40f, 0.66f, 0.98f, 1f);

            property.isExpanded =
                DrawNestedFoldoutHeader(property, property.displayName, childCount, property.isExpanded, accent);
            if (!property.isExpanded)
            {
                return;
            }

            DrawDirectChildren(property, accent);
        }

        private void DrawDirectChildren(SerializedProperty property, Color accent)
        {
            Rect body = EditorGUILayout.BeginVertical(NeoInspectorStyles.BodyPanel);
            NeoxiderEditorGUI.DrawSectionBodyGuide(body, accent);
            SerializedProperty iterator = property.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();
            int directChildDepth = property.depth + 1;
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;
                if (iterator.depth != directChildDepth)
                {
                    continue;
                }

                DrawPropertyField(iterator.Copy(), false);
            }

            EditorGUILayout.EndVertical();
        }

        private static bool ShouldUseCollectionPropertyFoldout(SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            if (IsUnityEventProperty(property))
            {
                return false;
            }

            return property.isArray && property.propertyType != SerializedPropertyType.String;
        }

        /// <summary>
        ///     Number of direct visible children when <paramref name="property" /> is drawn as a nested foldout
        ///     (a serializable class without its own drawer); 0 when it is drawn by PropertyField instead.
        /// </summary>
        private static int GetNestedFoldoutChildCount(SerializedProperty property)
        {
            if (property == null)
            {
                return 0;
            }

            if (property.propertyType != SerializedPropertyType.Generic || !property.hasVisibleChildren ||
                property.isArray)
            {
                return 0;
            }

            if (IsUnityEventProperty(property))
            {
                return 0;
            }

            // WHY: Let CustomPropertyDrawer run for the whole serialized class (e.g. ComponentFloatBinding), instead of
            // expanding child fields and bypassing the drawer.
            if (string.Equals(property.type, "ComponentFloatBinding", StringComparison.Ordinal))
            {
                return 0;
            }

            return CountDirectVisibleChildren(property);
        }

        private static int CountDirectVisibleChildren(SerializedProperty property)
        {
            if (property == null || !property.hasVisibleChildren)
            {
                return 0;
            }

            SerializedProperty iterator = property.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();
            int directChildDepth = property.depth + 1;
            int count = 0;
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;
                if (iterator.depth == directChildDepth)
                {
                    count++;
                }
            }

            return count;
        }

        // WHY: BeginProperty reads the label during the call and keeps no reference to it.
        private static readonly GUIContent s_nestedLabel = new();

        /// <summary>
        ///     Header row of a nested class or custom-drawn list. Wrapped in BeginProperty/EndProperty so the row
        ///     carries the prefab-override marker and the Copy/Paste/Revert context menu of the whole property.
        /// </summary>
        private static bool DrawNestedFoldoutHeader(SerializedProperty property, string title, int count,
            bool expanded, Color accent)
        {
            const float height = 20f;

            Rect row = GUILayoutUtility.GetRect(0f, height + 2f, NeoInspectorStyles.ExpandWidth);
            row = EditorGUI.IndentedRect(row);
            Rect rect = NeoInspectorTheme.Snap(new Rect(row.x, row.y + 1f, row.width, height));

            s_nestedLabel.text = title;
            s_nestedLabel.tooltip = null;
            EditorGUI.BeginProperty(rect, s_nestedLabel, property);

            Event current = Event.current;
            if (Event.current.type == EventType.Repaint)
            {
                bool pro = NeoInspectorTheme.IsDark;
                float hover = GUI.enabled
                    ? NeoInspectorAnimation.Hover(NeoInspectorAnimation.ControlKey(rect),
                        rect.Contains(current.mousePosition))
                    : 0f;
                float open = expanded ? 1f : 0f;
                Color background = Color.Lerp(NeoInspectorTheme.SectionBackground, accent, 0.05f + 0.04f * open);
                Color hoverOverlay = NeoInspectorTheme.HoverOverlay;
                background = Color.Lerp(background, new Color(hoverOverlay.r, hoverOverlay.g, hoverOverlay.b, 1f),
                    hoverOverlay.a * hover);
                Color edge = Color.Lerp(NeoInspectorTheme.Separator,
                    new Color(accent.r, accent.g, accent.b, pro ? 0.36f : 0.45f), Mathf.Max(open * 0.7f, hover * 0.5f));
                NeoInspectorTheme.DrawRoundedRect(rect, background, edge, NeoInspectorTheme.RadiusRow, 1f);
                NeoInspectorTheme.DrawAccentRail(rect, accent, 2f, 4f);

                Color accentText = NeoInspectorTheme.AccentText(accent);
                NeoInspectorTheme.DrawChevron(new Rect(rect.x + 6f, rect.y, 12f, rect.height), open,
                    Color.Lerp(NeoInspectorTheme.MutedText, accentText, 0.5f + 0.5f * open), 6f, 1.6f);

                GUIStyle countStyle = NeoInspectorStyles.NestedCount(accentText);
                string countText = NeoInspectorStyles.CountText(count);
                float countW = Mathf.Max(20f, countStyle.CalcSize(NeoInspectorStyles.TempContent(countText)).x + 10f);
                Rect countRect = NeoInspectorTheme.Snap(new Rect(rect.xMax - countW - 6f,
                    rect.y + (rect.height - 14f) * 0.5f, countW, 14f));
                Rect titleRect = new(rect.x + 22f, rect.y, Mathf.Max(0f, countRect.x - rect.x - 28f), rect.height);

                GUI.Label(titleRect, title, NeoInspectorStyles.NestedTitle(NeoInspectorTheme.TitleText));
                NeoInspectorTheme.DrawRoundedRect(countRect, NeoInspectorTheme.AccentFill(accent),
                    NeoInspectorTheme.RadiusPill);
                GUI.Label(countRect, countText, countStyle);
            }

            if (current.type == EventType.MouseDown && current.button == 0 && GUI.enabled &&
                rect.Contains(current.mousePosition))
            {
                expanded = !expanded;
                GUI.FocusControl(null);
                current.Use();
            }

            EditorGUI.EndProperty();
            return expanded;
        }

        /// <summary>
        ///     Draws <paramref name="property" /> WITHOUT its Unity <c>[Header]</c> decorator. Module editors
        ///     that render their OWN section header (a Foldout/label) call this instead of
        ///     <see cref="EditorGUILayout.PropertyField(SerializedProperty, bool)" /> so the title is not shown
        ///     twice (once as the section header, once as the field's built-in header).
        /// </summary>
        protected void DrawPropertyFieldNoHeader(SerializedProperty property)
        {
            if (property == null)
            {
                return;
            }

            // WHY: match the standard property pass — booleans render as the styled switch, not a plain checkbox.
            // DrawBooleanToggle draws no [Header] decorator, so it never doubles the title.
            if (property.propertyType == SerializedPropertyType.Boolean)
            {
                DrawBooleanToggle(property);
                return;
            }

            _drawNoHeaderBody ??= DrawNoHeaderBody;
            SerializedProperty previous = _noHeaderProperty;
            _noHeaderProperty = property;
            try
            {
                RunWithHeaderDecoratorsSuppressed(property, _drawNoHeaderBody);
            }
            finally
            {
                _noHeaderProperty = previous;
            }
        }

        private void DrawNoHeaderBody()
        {
            EditorGUILayout.PropertyField(_noHeaderProperty, true);
        }

        /// <summary>
        ///     Draws a collapsible section in the STANDARD Neoxider style (section bar + count pill, animated
        ///     open/close, body with the accent guide) for module editors that keep manual control over which fields
        ///     they show. Use <see cref="DrawPropertyFieldNoHeader" /> inside <paramref name="drawBody" /> so a field's
        ///     <c>[Header]</c> does not double the section title. Reuses the same look/persistence as the
        ///     auto-generated <c>[Header]</c> sections so custom component editors match the rest of the package.
        /// </summary>
        protected void DrawNeoSection(string title, int count, Action drawBody)
        {
            if (drawBody == null)
            {
                return;
            }

            Color accent = NeoxiderEditorGUI.SectionAccent(true);
            string key = GetSectionFoldoutKey(target != null ? target.GetType() : null, title);
            bool expanded = GetFoldoutState(key, true);
            float amount = GetFoldAmount(key, expanded);
            expanded = DrawNeoSectionHeader(expanded, amount, title, count, accent, "Folder Icon", false);
            _neoFoldouts[key] = expanded;
            amount = GetFoldAmount(key, expanded);

            if (BeginSectionBody(amount, accent, out bool fading))
            {
                EditorGUI.indentLevel++;
                try
                {
                    drawBody();
                }
                finally
                {
                    EditorGUI.indentLevel--;
                }

                // WHY: outside the finally - when drawBody exits the GUI (an object picker opening), Unity unwinds
                // the layout itself and closing the groups again would log a mismatch.
                EndSectionBody(fading);
            }
        }

        // WHY: the handler type is Unity's internal PropertyHandler; look its field up once, not per drawn header.
        private static readonly object[] s_getHandlerArgs = new object[1];
        private static Type s_decoratorsHandlerType;
        private static FieldInfo s_decoratorsField;

        private static FieldInfo GetDecoratorsField(Type handlerType)
        {
            if (handlerType != s_decoratorsHandlerType)
            {
                s_decoratorsHandlerType = handlerType;
                s_decoratorsField =
                    handlerType.GetField("m_DecoratorDrawers", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            return s_decoratorsField;
        }

        // WHY: temporarily strip the built-in HeaderDrawer from the property handler so `draw` renders the
        // field WITHOUT Unity's [Header] label (we already drew that title as our own section header). Wraps
        // ALL draw paths — plain, collection foldout and nested foldout — so array/generic fields with a
        // [Header] no longer double the title.
        internal static void RunWithHeaderDecoratorsSuppressed(SerializedProperty property, Action draw)
        {
            if (property == null)
            {
                return;
            }

            if (_getHandlerMethod == null)
            {
                draw();
                return;
            }

            object handler = null;
            object[] args = s_getHandlerArgs;
            try
            {
                args[0] = property;
                handler = _getHandlerMethod.Invoke(null, args);
            }
            catch
            {
                draw();
                return;
            }
            finally
            {
                args[0] = null;
            }

            if (handler == null)
            {
                draw();
                return;
            }

            FieldInfo decoratorsField = GetDecoratorsField(handler.GetType());
            if (decoratorsField == null)
            {
                draw();
                return;
            }

            object originalDecorators = null;
            try
            {
                originalDecorators = decoratorsField.GetValue(handler);
                if (originalDecorators is IList list)
                {
                    IList filtered = (IList)Activator.CreateInstance(decoratorsField.FieldType);
                    for (int i = 0; i < list.Count; i++)
                    {
                        object d = list[i];
                        if (d == null)
                        {
                            continue;
                        }

                        if (d.GetType().Name == "HeaderDrawer")
                        {
                            continue;
                        }

                        filtered.Add(d);
                    }

                    decoratorsField.SetValue(handler, filtered);
                }
            }
            catch
            {
                // WHY: decorator suppression is optional; the draw callback must still run exactly once.
            }

            try
            {
                draw();
            }
            finally
            {
                if (originalDecorators != null)
                {
                    try
                    {
                        decoratorsField.SetValue(handler, originalDecorators);
                    }
                    catch
                    {
                    }
                }
            }
        }

        // WHY delegated: the switch is the package's boolean, and a PropertyDrawer (which cannot inherit this
        // class) needs to draw the same thing inside nested serialized types. One definition, in
        // NeoxiderEditorGUI; this stays as the layout-based entry point the property pass uses.
        // WHY: BeginProperty/PrefixLabel read the label immediately and keep no reference, so one scratch content
        // serves every boolean row instead of a new GUIContent per row per event.
        private static readonly GUIContent s_boolLabel = new();

        private void DrawBooleanToggle(SerializedProperty property)
        {
            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            if (_plan == null || !_plan.BoolLabels.TryGetValue(property, out GUIContent label))
            {
                s_boolLabel.text = property.displayName;
                s_boolLabel.tooltip = property.tooltip;
                label = s_boolLabel;
            }

            NeoxiderEditorGUI.DrawPillToggleField(rect, property, label);
        }

        private static void BuildHeaderSections(PropertyPlan plan)
        {
            List<SerializedProperty> properties = plan.Properties;
            List<string> headerTitles = plan.HeaderTitles;
            List<HeaderSection> sections = plan.Sections;
            sections.Clear();
            int i = 0;
            while (i < properties.Count)
            {
                string headerTitle = headerTitles[i];

                if (string.IsNullOrEmpty(headerTitle))
                {
                    List<SerializedProperty> general = new();
                    while (i < properties.Count && string.IsNullOrEmpty(headerTitles[i]))
                    {
                        general.Add(properties[i]);
                        i++;
                    }

                    if (general.Count > 0)
                    {
                        sections.Add(new HeaderSection("General", general, false, false, plan.TargetType));
                    }

                    continue;
                }

                List<SerializedProperty> sectionProps = new() { properties[i] };
                i++;

                while (i < properties.Count && string.IsNullOrEmpty(headerTitles[i]))
                {
                    sectionProps.Add(properties[i]);
                    i++;
                }

                bool looksLikeWarning =
                    headerTitle.StartsWith("⚠", StringComparison.Ordinal) ||
                    headerTitle.StartsWith("!", StringComparison.Ordinal) ||
                    headerTitle.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
                    headerTitle.Contains("error", StringComparison.OrdinalIgnoreCase);

                sections.Add(new HeaderSection(headerTitle, sectionProps, looksLikeWarning, true, plan.TargetType));
            }
        }

        private void DrawWarningHeader(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Rect row = GUILayoutUtility.GetRect(0f, 26f, NeoInspectorStyles.ExpandWidth);
            row = EditorGUI.IndentedRect(row);
            Rect rect = NeoInspectorTheme.Snap(new Rect(row.x, row.y + 2f, row.width, 22f));

            if (Event.current.type == EventType.Repaint)
            {
                bool pro = NeoInspectorTheme.IsDark;
                Color accent = new(1f, 0.38f, 0.36f, 1f);
                Color background = pro
                    ? new Color(0.30f, 0.11f, 0.11f, 0.90f)
                    : new Color(0.99f, 0.91f, 0.90f, 1f);

                NeoInspectorTheme.DrawRoundedRect(rect, background,
                    new Color(accent.r, accent.g, accent.b, 0.40f), NeoInspectorTheme.RadiusRow, 1f);
                NeoInspectorTheme.DrawAccentRail(rect, accent, 3f, 5f);

                GUIStyle style = NeoInspectorStyles.WarningHeader(pro
                    ? new Color(1f, 0.86f, 0.85f, 1f)
                    : new Color(0.55f, 0.11f, 0.10f, 1f));

                GUI.Label(new Rect(rect.x + 12f, rect.y, rect.width - 18f, rect.height), text, style);
            }

            NeoInspectorStyles.Space(2);
        }

        private static bool IsDebugHeaderTitle(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return text.Contains("debug", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("gizmo", StringComparison.OrdinalIgnoreCase);
        }

        private void DrawPlainHeader(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Rect row = GUILayoutUtility.GetRect(0f, 22f, NeoInspectorStyles.ExpandWidth);
            row = EditorGUI.IndentedRect(row);
            Rect rect = NeoInspectorTheme.Snap(new Rect(row.x, row.y + 3f, row.width, 18f));

            if (Event.current.type == EventType.Repaint)
            {
                bool isDebug = IsDebugHeaderTitle(text);
                Color accent = isDebug
                    ? new Color(0.94f, 0.34f, 0.34f, 1f)
                    : NeoxiderEditorGUI.SectionAccent(true);
                NeoInspectorTheme.DrawAccentRail(rect, accent, 3f, 3f);
                EditorGUI.DrawRect(new Rect(rect.x + 10f, rect.yMax - 1f, rect.width - 10f, 1f),
                    NeoInspectorTheme.Separator);

                GUIStyle style = NeoInspectorStyles.PlainHeader(isDebug
                    ? Color.Lerp(NeoInspectorTheme.TitleText, accent, 0.35f)
                    : NeoInspectorTheme.TitleText);

                GUI.Label(new Rect(rect.x + 10f, rect.y, rect.width - 14f, rect.height), text, style);
            }

            NeoInspectorStyles.Space(2);
        }

        // WHY: Header titles and field lookups depend only on (type, property path) and are requested several
        // times per property on every IMGUI event; reflect once and reuse the answer. The cap keeps huge arrays
        // (one path per element) from growing the cache without bound.
        private const int MaxReflectionCacheEntries = 4096;
        private static readonly Dictionary<(Type, string), string> s_headerTitleCache = new();
        private static readonly Dictionary<(Type, string), FieldInfo> s_fieldPathCache = new();

        internal string TryGetHeaderTitleForProperty(SerializedProperty property)
        {
            if (property == null || target == null)
            {
                return null;
            }

            (Type, string) key = (target.GetType(), property.propertyPath);
            if (s_headerTitleCache.TryGetValue(key, out string cached))
            {
                return cached;
            }

            string title = ResolveHeaderTitle(key.Item1, key.Item2);
            if (s_headerTitleCache.Count >= MaxReflectionCacheEntries)
            {
                s_headerTitleCache.Clear();
            }

            s_headerTitleCache[key] = title;
            return title;
        }

        private static string ResolveHeaderTitle(Type rootType, string propertyPath)
        {
            if (!TryGetFieldInfoForPropertyPath(rootType, propertyPath, out FieldInfo fieldInfo))
            {
                return null;
            }

            try
            {
                object[] attrs = fieldInfo.GetCustomAttributes(typeof(HeaderAttribute), true);
                if (attrs == null || attrs.Length == 0)
                {
                    return null;
                }

                HeaderAttribute header = attrs[0] as HeaderAttribute;
                return header?.header;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryGetFieldInfoForPropertyPath(Type rootType, string propertyPath, out FieldInfo fieldInfo)
        {
            fieldInfo = null;
            if (rootType == null || string.IsNullOrEmpty(propertyPath))
            {
                return false;
            }

            (Type, string) key = (rootType, propertyPath);
            if (s_fieldPathCache.TryGetValue(key, out FieldInfo cached))
            {
                fieldInfo = cached;
                return cached != null;
            }

            bool found = ResolveFieldInfoForPropertyPath(rootType, propertyPath, out fieldInfo);
            if (s_fieldPathCache.Count >= MaxReflectionCacheEntries)
            {
                s_fieldPathCache.Clear();
            }

            s_fieldPathCache[key] = fieldInfo;
            return found;
        }

        private static bool ResolveFieldInfoForPropertyPath(Type rootType, string propertyPath,
            out FieldInfo fieldInfo)
        {
            fieldInfo = null;
            string[] parts = propertyPath.Split('.');
            Type currentType = rootType;
            FieldInfo currentField = null;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];

                if (part == "Array")
                {
                    continue;
                }

                if (part.StartsWith("data[", StringComparison.Ordinal))
                {
                    continue;
                }

                currentField = FindFieldInHierarchy(currentType, part);

                if (currentField == null)
                {
                    return false;
                }

                currentType = currentField.FieldType;

                if (currentType.IsArray)
                {
                    currentType = currentType.GetElementType();
                }
                else if (currentType.IsGenericType &&
                         currentType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    currentType = currentType.GetGenericArguments()[0];
                }
            }

            fieldInfo = currentField;
            return fieldInfo != null;
        }

        // WHY: Type.GetField does not return private fields declared on a base class, so a [Header] on a private
        // field of an inherited component was invisible and its section silently merged into the one above.
        private static FieldInfo FindFieldInHierarchy(Type type, string name)
        {
            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                FieldInfo field = current.GetField(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }

        private bool GetFoldoutState(string key, bool defaultValue)
        {
            if (string.IsNullOrEmpty(key))
            {
                return defaultValue;
            }

            if (_neoFoldouts.TryGetValue(key, out bool value))
            {
                return value;
            }

            _neoFoldouts[key] = defaultValue;
            return defaultValue;
        }

        private static bool IsUnityEventProperty(SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            string typeName = property.type;
            if (!string.IsNullOrEmpty(typeName) && typeName.Contains("UnityEvent"))
            {
                return true;
            }

            if (property.propertyType != SerializedPropertyType.Generic)
            {
                return false;
            }

            SerializedProperty calls = property.FindPropertyRelative("m_PersistentCalls.m_Calls");
            return calls != null && calls.isArray;
        }

        private void DrawNetworkReplicationMarker(SerializedProperty property)
        {
            if (!IsNetworkReplicationHighlighted(property))
            {
                return;
            }

            Rect rect = GUILayoutUtility.GetRect(0f, 16f, NeoInspectorStyles.ExpandWidth);
            rect = NeoInspectorTheme.Snap(EditorGUI.IndentedRect(rect));

            if (Event.current.type == EventType.Repaint)
            {
                bool pro = NeoInspectorTheme.IsDark;
                Color accent = new(0.22f, 0.78f, 1f, 1f);
                Color background = pro ? new Color(0.06f, 0.16f, 0.23f, 0.92f) : new Color(0.86f, 0.94f, 0.99f, 1f);
                NeoInspectorTheme.DrawRoundedRect(rect, background, NeoInspectorTheme.RadiusRow);
                NeoInspectorTheme.DrawAccentRail(rect, accent, 3f, 3f);

                GUIStyle style = NeoInspectorStyles.NetworkMarker(pro
                    ? new Color(0.68f, 0.92f, 1f, 1f)
                    : new Color(0.10f, 0.36f, 0.52f, 1f));

                GUI.Label(new Rect(rect.x + 10f, rect.y, rect.width - 10f, rect.height),
                    "NETWORK REPLICATED when isNetworked is enabled", style);
            }
        }

        /// <summary>Serialized members a replicating Neo component sends over the network while it is networked.</summary>
        private sealed class ReplicatedMembers
        {
            private readonly string[] _events;
            private readonly string[] _eventPathSuffixes;
            private readonly string[] _reactive;

            public ReplicatedMembers(string[] events, string[] reactive = null, bool matchNestedEventPaths = false)
            {
                _events = events ?? Array.Empty<string>();
                _reactive = reactive ?? Array.Empty<string>();
                _eventPathSuffixes = Array.Empty<string>();
                if (matchNestedEventPaths)
                {
                    _eventPathSuffixes = new string[_events.Length];
                    for (int i = 0; i < _events.Length; i++)
                    {
                        _eventPathSuffixes[i] = "." + _events[i];
                    }
                }
            }

            public bool HasNestedEventPaths => _eventPathSuffixes.Length > 0;

            public bool MatchesEventName(string propertyName)
            {
                return Array.IndexOf(_events, propertyName) >= 0;
            }

            public bool MatchesEventPath(string propertyPath)
            {
                for (int i = 0; i < _eventPathSuffixes.Length; i++)
                {
                    if (propertyPath.EndsWith(_eventPathSuffixes[i], StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool MatchesReactiveName(string propertyName)
            {
                return Array.IndexOf(_reactive, propertyName) >= 0;
            }
        }

        // WHY: the single source for the NETWORK REPLICATED marker. The type gate and the per-property checks both
        // read it, so a component added here can never show up in one and silently miss the other. Keyed by type
        // name because these components live in assemblies this editor assembly does not reference.
        private static readonly Dictionary<string, ReplicatedMembers> s_replicatedMembersByTypeName =
            new(StringComparer.Ordinal)
            {
                {
                    "InteractiveObject",
                    new ReplicatedMembers(new[]
                    {
                        "onInteractDown", "onInteractUp", "onClick", "onDoubleClick", "onRightClick", "onMiddleClick"
                    })
                },
                { "NetworkEventDispatcher", new ReplicatedMembers(new[] { "onNetworkEvent" }) },
                {
                    "NetworkActionRelay",
                    new ReplicatedMembers(new[] { "onTriggered", "onTriggeredFloat", "onTriggeredString" },
                        matchNestedEventPaths: true)
                },
                {
                    "Counter",
                    new ReplicatedMembers(new[] { "OnValueChangedInt", "OnValueChangedFloat", "OnRepeatByCounterValue" },
                        new[] { "Value" })
                },
                { "RandomRange", new ReplicatedMembers(new[] { "OnGeneratedInt", "OnGeneratedFloat" }, new[] { "Value" }) },
                {
                    "Selector",
                    new ReplicatedMembers(new[]
                        { "OnSelectionChanged", "OnSelectionChangedGameObject", "OnCountActiveChanged" })
                },
                { "NeoCondition", new ReplicatedMembers(new[] { "_onTrue", "_onFalse", "_onResult", "_onInvertedResult" }) },
                {
                    "Spawner",
                    new ReplicatedMembers(new[] { "OnObjectSpawned", "OnWaveStarted", "OnWaveObjectSpawned" })
                }
            };

        private static readonly Dictionary<Type, ReplicatedMembers> s_replicatedMembersByType = new();

        private static ReplicatedMembers GetReplicatedMembers(Type targetType)
        {
            if (targetType == null)
            {
                return null;
            }

            if (!s_replicatedMembersByType.TryGetValue(targetType, out ReplicatedMembers members))
            {
                s_replicatedMembersByTypeName.TryGetValue(targetType.Name, out members);
                s_replicatedMembersByType[targetType] = members;
            }

            return members;
        }

        // WHY: checking the type first keeps the FindProperty("isNetworked") lookups, which ran for every drawn
        // property of every inspector, off all components that never replicate anything.
        internal static bool CanShowReplicationMarker(Type targetType)
        {
            return GetReplicatedMembers(targetType) != null;
        }

        /// <summary>True when <paramref name="targetType" /> replicates the UnityEvent at this name/path.</summary>
        internal static bool IsReplicatedEventMember(Type targetType, string propertyName, string propertyPath)
        {
            ReplicatedMembers members = GetReplicatedMembers(targetType);
            return members != null &&
                   (members.MatchesEventName(propertyName ?? string.Empty) ||
                    members.MatchesEventPath(propertyPath ?? string.Empty));
        }

        /// <summary>True when <paramref name="targetType" /> replicates the reactive property with this name.</summary>
        internal static bool IsReplicatedReactiveMember(Type targetType, string propertyName)
        {
            ReplicatedMembers members = GetReplicatedMembers(targetType);
            return members != null && members.MatchesReactiveName(propertyName ?? string.Empty);
        }

        private bool IsNetworkReplicationHighlighted(SerializedProperty property)
        {
            if (property == null || target == null)
            {
                return false;
            }

            ReplicatedMembers members = GetReplicatedMembers(target.GetType());
            if (members == null || !IsNetworkedEnabled())
            {
                return false;
            }

            string propertyName = property.name ?? string.Empty;
            if (IsUnityEventProperty(property))
            {
                return members.MatchesEventName(propertyName) ||
                       (members.HasNestedEventPaths && members.MatchesEventPath(property.propertyPath ?? string.Empty));
            }

            string propertyType = property.type ?? string.Empty;
            return propertyType.Contains("ReactiveProperty", StringComparison.Ordinal) &&
                   members.MatchesReactiveName(propertyName);
        }

        private bool IsNetworkedEnabled()
        {
            if (serializedObject == null || target == null)
            {
                return false;
            }

            SerializedProperty networked = GetPropertyPlan().Networked;
            return networked != null && networked.boolValue;
        }

        private static SerializedProperty GetPersistentCallsArray(SerializedProperty unityEventProperty)
        {
            if (unityEventProperty == null)
            {
                return null;
            }

            SerializedProperty calls = unityEventProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");
            if (calls != null && calls.isArray)
            {
                return calls;
            }

            return null;
        }

        private static int GetPersistentCallCount(SerializedProperty unityEventProperty)
        {
            SerializedProperty calls = GetPersistentCallsArray(unityEventProperty);
            return calls != null ? calls.arraySize : 0;
        }

        private static int GetBrokenPersistentCallCount(SerializedProperty unityEventProperty)
        {
            SerializedProperty calls = GetPersistentCallsArray(unityEventProperty);
            if (calls == null)
            {
                return 0;
            }

            int broken = 0;
            for (int i = 0; i < calls.arraySize; i++)
            {
                SerializedProperty call = calls.GetArrayElementAtIndex(i);
                if (call == null)
                {
                    continue;
                }

                SerializedProperty targetProp = call.FindPropertyRelative("m_Target");
                SerializedProperty methodProp = call.FindPropertyRelative("m_MethodName");

                bool hasTarget = targetProp != null &&
                                 targetProp.propertyType == SerializedPropertyType.ObjectReference &&
                                 targetProp.objectReferenceValue != null;
                bool hasMethod = methodProp != null &&
                                 methodProp.propertyType == SerializedPropertyType.String &&
                                 !string.IsNullOrWhiteSpace(methodProp.stringValue);

                if (!hasTarget || !hasMethod)
                {
                    broken++;
                }
            }

            return broken;
        }

        private static string BuildPersistentCallPreview(SerializedProperty unityEventProperty, int maxItems)
        {
            SerializedProperty calls = GetPersistentCallsArray(unityEventProperty);
            if (calls == null || calls.arraySize == 0 || maxItems <= 0)
            {
                return null;
            }

            int count = Mathf.Min(calls.arraySize, maxItems);
            List<string> parts = new(count);

            for (int i = 0; i < count; i++)
            {
                SerializedProperty call = calls.GetArrayElementAtIndex(i);
                if (call == null)
                {
                    continue;
                }

                SerializedProperty targetProp = call.FindPropertyRelative("m_Target");
                SerializedProperty methodProp = call.FindPropertyRelative("m_MethodName");

                string targetName = targetProp != null &&
                                    targetProp.propertyType == SerializedPropertyType.ObjectReference &&
                                    targetProp.objectReferenceValue != null
                    ? targetProp.objectReferenceValue.name
                    : "<Missing Target>";

                string methodName = methodProp != null &&
                                    methodProp.propertyType == SerializedPropertyType.String &&
                                    !string.IsNullOrWhiteSpace(methodProp.stringValue)
                    ? methodProp.stringValue
                    : "<Missing Method>";

                parts.Add($"{targetName}.{methodName}");
            }

            if (parts.Count == 0)
            {
                return null;
            }

            string preview = "→ " + string.Join(", ", parts);
            if (calls.arraySize > maxItems)
            {
                preview += $" … (+{calls.arraySize - maxItems})";
            }

            return preview;
        }

        private static readonly Color NeoPropAccent = new(0.49f, 0.60f, 0.94f, 1f);

        private static GUIStyle _neoPropertyPanelStyle;
        private Rect _neoPanelRect;

        /// <summary>Layout-only style for the property card; the visual is drawn by
        /// <see cref="DrawNeoPropertyPanelBackground" /> so the corners can actually be rounded.</summary>
        private static GUIStyle GetNeoPropertyPanelStyle()
        {
            return _neoPropertyPanelStyle ??= new GUIStyle
            {
                padding = new RectOffset(12, 8, 8, 8),
                margin = new RectOffset(0, 0, 0, 4)
            };
        }

        /// <summary>Rounded card behind the default property fields with a faint accent tint.</summary>
        private static void DrawNeoPropertyPanelBackground(Rect rect)
        {
            Color fill = Color.Lerp(NeoInspectorTheme.PanelBackground, NeoPropAccent, 0.035f);
            Color edge = Color.Lerp(NeoInspectorTheme.Separator,
                new Color(NeoPropAccent.r, NeoPropAccent.g, NeoPropAccent.b, 0.16f), 0.5f);
            NeoInspectorTheme.DrawRoundedRect(NeoInspectorTheme.Snap(rect), fill, edge, NeoInspectorTheme.RadiusCard, 1f);
        }

        private static bool DrawNeoSectionHeader(bool expanded, float openness, string title, int count, Color accent,
            string iconName, bool hasOverride)
        {
            // WHY delegated: this bar IS the Neoxider section, and nested drawers need to draw the same one.
            // The single definition lives in NeoxiderEditorGUI; this keeps the layout/indent handling.
            Rect row = GUILayoutUtility.GetRect(0f, NeoxiderEditorGUI.SectionHeaderHeight + 4f,
                NeoInspectorStyles.ExpandWidth);
            row = EditorGUI.IndentedRect(row);
            Rect rect = new(row.x, row.y + 2f, row.width, NeoxiderEditorGUI.SectionHeaderHeight);

            return NeoxiderEditorGUI.DrawSectionHeader(rect, expanded, openness, title, count, accent, iconName,
                hasOverride);
        }

        private sealed class HeaderSection
        {
            public readonly string Title;
            public readonly List<SerializedProperty> Properties;
            public readonly bool IsWarningHeader;
            public readonly bool StartsAtHeader;
            public readonly bool ExcludeFromMinFieldsRule;
            public readonly string FoldoutKey;

            public HeaderSection(string title, List<SerializedProperty> properties, bool isWarningHeader,
                bool startsAtHeader, Type targetType)
            {
                Title = title;
                Properties = properties;
                IsWarningHeader = isWarningHeader;
                StartsAtHeader = startsAtHeader;
                ExcludeFromMinFieldsRule =
                    string.Equals(title, "Actions", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(title, "Events", StringComparison.OrdinalIgnoreCase);
                FoldoutKey = GetSectionFoldoutKey(targetType, title);
            }
        }
    }
}
