using System.Collections.Generic;
using Neo.Quest;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Quest
{
    [CustomEditor(typeof(QuestFlowConfig))]
    [CanEditMultipleObjects]
    public sealed class QuestFlowConfigEditor : CustomEditorBase
    {
        // WHY: IMGUI draws one inspector at a time, so one cleared set serves every structure pass; a fresh HashSet
        // per pass (two per event before) was pure per-repaint garbage.
        private static readonly HashSet<Object> s_uniqueQuests = new();

        private NeoxiderEditorGUI.Badge[] _summaryBadges;
        private FlowStats _badgeStats;

        protected override bool UseCustomNeoxiderInspectorGUI => true;

        protected override void ProcessAttributeAssignments()
        {
        }

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty chainsProp = serializedObject.FindProperty("_chains");
            SerializedProperty standaloneProp = serializedObject.FindProperty("_standaloneQuests");
            FlowStats stats = CollectStats(chainsProp, standaloneProp);

            NeoxiderEditorGUI.DrawSummaryCard("Quest Flow Config",
                "Config drives chains and standalone quests. Summary above shows structure size and potential duplicates.",
                GetSummaryBadges(stats));
            NeoInspectorStyles.Space(4f);
            DrawValidation(stats);

            NeoxiderEditorGUI.BeginSection("Chains", "Sequential quest chains with optional strict order.");
            EditorGUILayout.PropertyField(chainsProp, true);
            NeoxiderEditorGUI.EndSection();

            NeoInspectorStyles.Space(4f);

            NeoxiderEditorGUI.BeginSection("Standalone Quests", "Quests not tied to any chain.");
            EditorGUILayout.PropertyField(standaloneProp, true);
            NeoxiderEditorGUI.EndSection();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>Structure counts of one config, gathered in a single pass over its serialized lists.</summary>
        internal struct FlowStats
        {
            public int Chains;
            public int StrictChains;
            public int Standalone;
            public int QuestRefs;
            public int DuplicateRefs;
            public bool HasEmptyChain;

            public bool SameAs(in FlowStats other)
            {
                return Chains == other.Chains && StrictChains == other.StrictChains &&
                       Standalone == other.Standalone && QuestRefs == other.QuestRefs &&
                       DuplicateRefs == other.DuplicateRefs && HasEmptyChain == other.HasEmptyChain;
            }
        }

        internal static FlowStats CollectStats(SerializedProperty chainsProp, SerializedProperty standaloneProp)
        {
            FlowStats stats = new()
            {
                Chains = chainsProp.arraySize,
                Standalone = standaloneProp.arraySize
            };

            s_uniqueQuests.Clear();
            try
            {
                for (int i = 0; i < chainsProp.arraySize; i++)
                {
                    SerializedProperty chainProp = chainsProp.GetArrayElementAtIndex(i);
                    if (chainProp.FindPropertyRelative("_strictOrder").boolValue)
                    {
                        stats.StrictChains++;
                    }

                    SerializedProperty questsProp = chainProp.FindPropertyRelative("_quests");
                    if (questsProp.arraySize == 0)
                    {
                        stats.HasEmptyChain = true;
                    }

                    for (int j = 0; j < questsProp.arraySize; j++)
                    {
                        CountQuest(questsProp.GetArrayElementAtIndex(j).objectReferenceValue, ref stats);
                    }
                }

                for (int i = 0; i < standaloneProp.arraySize; i++)
                {
                    CountQuest(standaloneProp.GetArrayElementAtIndex(i).objectReferenceValue, ref stats);
                }
            }
            finally
            {
                // WHY: the set must not keep quest assets alive after the pass.
                s_uniqueQuests.Clear();
            }

            return stats;
        }

        private static void CountQuest(Object quest, ref FlowStats stats)
        {
            stats.QuestRefs++;
            if (quest != null && !s_uniqueQuests.Add(quest))
            {
                stats.DuplicateRefs++;
            }
        }

        /// <summary>
        ///     Summary badges for <paramref name="stats" />. The array and its texts are rebuilt only when a count
        ///     changes, so an unchanged inspector repaints without allocating.
        /// </summary>
        internal NeoxiderEditorGUI.Badge[] GetSummaryBadges(in FlowStats stats)
        {
            if (_summaryBadges != null && _badgeStats.SameAs(stats))
            {
                return _summaryBadges;
            }

            _badgeStats = stats;
            _summaryBadges = new NeoxiderEditorGUI.Badge[stats.DuplicateRefs > 0 ? 5 : 4];
            _summaryBadges[0] = new NeoxiderEditorGUI.Badge("Chains " + stats.Chains,
                new Color(0.20f, 0.50f, 0.78f, 1f));
            _summaryBadges[1] = new NeoxiderEditorGUI.Badge("Strict " + stats.StrictChains,
                new Color(0.42f, 0.34f, 0.82f, 1f));
            _summaryBadges[2] = new NeoxiderEditorGUI.Badge("Standalone " + stats.Standalone,
                new Color(0.24f, 0.60f, 0.60f, 1f));
            _summaryBadges[3] = new NeoxiderEditorGUI.Badge("Quest Refs " + stats.QuestRefs,
                new Color(0.18f, 0.62f, 0.32f, 1f));
            if (stats.DuplicateRefs > 0)
            {
                _summaryBadges[4] = new NeoxiderEditorGUI.Badge("Duplicates " + stats.DuplicateRefs,
                    new Color(0.78f, 0.46f, 0.18f, 1f));
            }

            return _summaryBadges;
        }

        private static void DrawValidation(in FlowStats stats)
        {
            if (stats.Chains == 0 && stats.Standalone == 0)
            {
                EditorGUILayout.HelpBox("Config is empty. Add at least one chain or one standalone quest.",
                    MessageType.Info);
            }

            if (stats.HasEmptyChain)
            {
                EditorGUILayout.HelpBox(
                    "Some chains have no quests. Fill them or remove them to keep the config clean.",
                    MessageType.Warning);
            }

            if (stats.DuplicateRefs > 0)
            {
                EditorGUILayout.HelpBox(
                    "The same QuestConfig appears more than once. That is not always valid and can confuse progression flow.",
                    MessageType.Warning);
            }
        }
    }
}
