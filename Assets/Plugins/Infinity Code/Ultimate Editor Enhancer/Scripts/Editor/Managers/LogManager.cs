/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InfinityCode.UltimateEditorEnhancer
{
    [InitializeOnLoad]
    public static class LogManager
    {
        private const double Frequency = 1;
        private const int ErrorMode = 16640;
        private const int ErrorMode2 = 8405248;
        private const int ExceptionMode = 4325632;
        private const int ExceptionMode2 = 12714240;

        private static Dictionary<int, List<Entry>> entries;
        private static double lastUpdatedTime;
        private static bool isDirty;
        private static int lastCount;

        static LogManager()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.logMessageReceived += OnLogMessageReceived;

            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
            Application.logMessageReceivedThreaded += OnLogMessageReceived;

            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;

            entries = new Dictionary<int, List<Entry>>();

            UpdateEntries();
        }

        public static List<Entry> GetEntries(int id)
        {
            List<Entry> localEntries;
            if (entries.TryGetValue(id, out localEntries)) return localEntries;
            return null;
        }

        private static void OnLogMessageReceived(string condition, string stacktrace, LogType type)
        {
            isDirty = true;
        }

        private static void OnUpdate()
        {
            if (!Prefs.hierarchyErrorIcons) return;

            if (EditorApplication.timeSinceStartup - lastUpdatedTime > Frequency)
            {
                if (!isDirty)
                {
                    int currentCount = LogEntriesRef.GetCount();
                    if (lastCount > currentCount) isDirty = true;
                    lastCount = currentCount;
                }
            }

            if (isDirty) UpdateEntries();
        }

        private static void UpdateEntries()
        {
            entries.Clear();

            try
            {
                int count = LogEntriesRef.StartGettingEntries();
                object nativeEntry = Activator.CreateInstance(LogEntryRef.type);

                int maxRecords = Mathf.Min(count, 999);

                for (int i = 0; i < maxRecords; i++) 
                {
                    LogEntriesRef.GetEntryInternal(i, nativeEntry);
                    int mode = LogEntryRef.GetMode(nativeEntry);
                    if (mode != ErrorMode && mode != ErrorMode2 && 
                        mode != ExceptionMode && mode != ExceptionMode2) continue;

                    
                    int instanceID = LogEntryRef.GetInstanceID(nativeEntry);
                    if (instanceID == 0) continue;

                    Entry entry = new Entry(nativeEntry, i);
                    Object reference = EditorUtility.InstanceIDToObject(instanceID);
                    if (reference == null) continue;

                    GameObject target = reference as GameObject;

                    if (target == null)
                    {
                        Component component = reference as Component;
                        if (component == null) continue;
                        target = component.gameObject;
                    }

                    List<Entry> localEntries;
                    int id = target.GetInstanceID();
                    if (entries.TryGetValue(id, out localEntries)) localEntries.Add(entry);
                    else entries.Add(id, new List<Entry> {entry});
                }

                EditorApplication.RepaintHierarchyWindow();
            }
            catch (Exception ex)
            {
                Log.Add(ex);
            }

            LogEntriesRef.EndGettingEntries();
            lastUpdatedTime = EditorApplication.timeSinceStartup;
            isDirty = false;
        }

        public class Entry
        {
            public string message;
            private int index;

            public Entry(object nativeEntry, int index)
            {
                this.index = index;
                message = LogEntryRef.GetMessage(nativeEntry);
            }

            public void Open()
            {
                LogEntriesRef.RowGotDoubleClicked(index);
            }
        }
    }
}