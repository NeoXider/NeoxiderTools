/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using InfinityCode.UltimateEditorEnhancer.HierarchyTools;
using InfinityCode.UltimateEditorEnhancer.JSON;
using InfinityCode.UltimateEditorEnhancer.Windows;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace InfinityCode.UltimateEditorEnhancer
{
    public static partial class Prefs
    {
        private const string PrefsKey = "UEE";
        public const string Prefix = PrefsKey + ".";

        private static Action AfterFirstLoad;

        private static bool migrationReplace;

        private static PrefManager[] _managers;
        private static string[] _keywords;
        private static PrefManager[] _generalManagers;
        private static string[] escapeChars = {"%", "%25", ";", "%3B", "(", "%28", ")", "%29"};
        private static bool forceSave;
        private static Vector2 scrollPosition;
        private static bool loaded;

        internal static PrefManager[] managers
        {
            get
            {
                if (_managers == null)
                {
                    List<PrefManager> items = Reflection.GetInheritedItems<PrefManager>();
                    _managers = items.OrderBy(d => d.order).ToArray();
                }
                return _managers;
            }
        }


        internal static PrefManager[] generalManagers
        {
            get
            {
                if (_generalManagers == null)
                {
                    _generalManagers = managers.Where(i => !i.GetType().IsSubclassOf(typeof(StandalonePrefManager))).ToArray();
                }
                return _generalManagers;
            }
        }

        static Prefs()
        {
            Load();
        }

        private static void CreateIgnore(string filename, bool entireAsset)
        {
            string path = new DirectoryInfo(Resources.assetFolder).Parent.FullName + "/." + filename;
            string content = "";
            if (entireAsset)
            {
                content = @"
/Ultimate Editor Enhancer/*
!/Ultimate Editor Enhancer/Scripts
/Ultimate Editor Enhancer/Scripts/Editor/
";
            }

            content += "/Ultimate Editor Enhancer Settings/";

            File.WriteAllText(path, content, Encoding.UTF8);
        }

        private static void DisableEverything()
        {
            foreach (PrefManager m in managers)
            {
                IStateablePref p = m as IStateablePref;
                if (p != null) p.SetState(false);
            }
            Save();
        }

        private static void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayoutUtils.ToolbarButton("File"))
            {
                GenericMenuEx menu = GenericMenuEx.Start();
                menu.Add("Export/Settings", ExportSettings);
                menu.Add("Export/Items/Everything", ExportItems, (int)ExportItemIndex.everything );
                menu.AddSeparator("Export/Items/");
                menu.Add("Export/Items/Bookmarks", ExportItems, (int)ExportItemIndex.bookmarks );
                menu.Add("Export/Items/Empty Inspector", ExportItems, (int)ExportItemIndex.emptyInspector );
                menu.Add("Export/Items/Favorite Windows", ExportItems, (int)ExportItemIndex.favoriteWindows );
                menu.Add("Export/Items/Hierarchy Headers", ExportItems, (int)ExportItemIndex.hierarchyHeaders );
                menu.Add("Export/Items/Quick Access Bar", ExportItems, (int)ExportItemIndex.quickAccessBar );
                menu.Add("Export/Items/Project Icons", ExportItems, (int)ExportItemIndex.projectIcons );

                menu.Add("Import/Settings", ImportSettings);
                menu.Add("Import/Items", ImportItems);

                menu.Show();
            }
            
            if (GUILayoutUtils.ToolbarButton("Bulk Operations"))
            {
                GenericMenuEx menu = GenericMenuEx.Start();
                menu.Add("Restore Default Settings", RestoreDefaultSettings);
                menu.AddSeparator();
                menu.Add("Disable/Everything", DisableEverything);
                menu.AddSeparator("Disable/");
                
                menu.Add("Enable/Everything", EnableEverything);
                menu.AddSeparator("Enable/");
                
                Dictionary<string, Action<bool>> actions = new Dictionary<string, Action<bool>>();

                foreach (PrefManager m in managers)
                {
                    IStateablePref p = m as IStateablePref;
                    if (p == null) continue;

                    actions.Add(p.GetMenuName(), p.SetState);
                }
                
                foreach (KeyValuePair<string, Action<bool>> pair in actions.OrderBy(d => d.Key))
                {
                    menu.Add("Enable/" + pair.Key, () => pair.Value(true));
                    menu.Add("Disable/" + pair.Key, () => pair.Value(false));
                }

                menu.Show();
            }

            if (GUILayoutUtils.ToolbarButton("Version Control"))
            {
                GenericMenuEx menu = GenericMenuEx.Start();
                menu.Add(".gitignore/Exclude Settings", () => { CreateIgnore("gitignore", false); });
                menu.Add(".gitignore/Exclude Entire Asset", () => { CreateIgnore("gitignore", true); });
                menu.Add(".collabignore/Exclude Settings", () => { CreateIgnore("collabignore", false); });
                menu.Add(".collabignore/Exclude Entire Asset", () => { CreateIgnore("collabignore", true); });
                menu.Show();
            }

            GUILayout.Label("", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));
            if (GUILayoutUtils.ToolbarButton("Help"))
            {
                GenericMenuEx menu = GenericMenuEx.Start();
                menu.Add("Welcome", Welcome.OpenWindow);
                menu.Add("Getting Started", GettingStarted.OpenWindow);
                menu.Add("Shortcuts", Shortcuts.OpenWindow);
                menu.AddSeparator();
                menu.Add("Product Page", Links.OpenHomepage);
                menu.Add("Documentation", Links.OpenDocumentation);
                menu.Add("Videos", Links.OpenYouTube);
                menu.AddSeparator();
                menu.Add("Support", Links.OpenSupport);
                menu.Add("Forum", Links.OpenForum);
                menu.Add("Check Updates", Updater.OpenWindow);
                menu.AddSeparator();
                menu.Add("Rate and Review", Welcome.RateAndReview);
                menu.Add("About", About.OpenWindow);

                menu.Show();
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void EnableEverything()
        {
            foreach (PrefManager m in managers)
            {
                IStateablePref p = m as IStateablePref;
                if (p != null) p.SetState(true);
            }
            Save();
        }

        private static void ExportItems(object data)
        {
            ExportItemIndex target = (ExportItemIndex)(int)data;
            string name = "UEE-Items-";
            if (target == ExportItemIndex.everything) name += "Everything";
            else if (target == ExportItemIndex.bookmarks) name += "Bookmarks";
            else if (target == ExportItemIndex.favoriteWindows) name += "Favorite-Windows";
            else if (target == ExportItemIndex.miniLayouts) name += "Mini-Layouts";
            else if (target == ExportItemIndex.quickAccessBar) name += "Quick-Access-Bar";
            else if (target == ExportItemIndex.hierarchyHeaders) name += "Hierarchy-Headers";
            else if (target == ExportItemIndex.emptyInspector) name += "Empty-Inspector";
            else if (target == ExportItemIndex.projectIcons) name += "Project-Icons";

            string filename = EditorUtility.SaveFilePanel("Export Items", EditorApplication.applicationPath, name, "json");
            if (string.IsNullOrEmpty(filename)) return;

            JsonObject obj = GetExportJson(target);

            File.WriteAllText(filename, obj.ToString(), Encoding.UTF8);
        }

        private static void ExportSettings()
        {
            string filename = EditorUtility.SaveFilePanel("Export Settings", EditorApplication.applicationPath, "UEE-Settings", "ucs");
            if (string.IsNullOrEmpty(filename)) return;

            File.WriteAllText(filename, GetSettings(), Encoding.UTF8);
        }

        private static JsonObject GetExportJson(ExportItemIndex target)
        {
            JsonObject obj = new JsonObject();

            if (target == ExportItemIndex.everything || target == ExportItemIndex.bookmarks) obj.Add("bookmarks", Bookmarks.json);
            if (target == ExportItemIndex.everything || target == ExportItemIndex.favoriteWindows) obj.Add("favorite-windows", FavoriteWindowsManager.json);
            if (target == ExportItemIndex.everything || target == ExportItemIndex.miniLayouts) obj.Add("mini-layouts", MiniLayoutsManager.json);
            if (target == ExportItemIndex.everything || target == ExportItemIndex.quickAccessBar) obj.Add("quick-access-bar", QuickAccessBarManager.json);
            if (target == ExportItemIndex.everything || target == ExportItemIndex.hierarchyHeaders) obj.Add("hierarchy-headers", Header.json);
            if (target == ExportItemIndex.everything || target == ExportItemIndex.emptyInspector) obj.Add("empty-inspector", EmptyInspectorManager.json);
            if (target == ExportItemIndex.everything || target == ExportItemIndex.projectIcons) obj.Add("project-icons", ProjectFolderIconManager.json);
            
            return obj;
        }

        private static FieldInfo GetField(FieldInfo[] fields, string key)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].Name == key) return fields[i];
            }

            return null;
        }

        public static IEnumerable<string> GetKeywords()
        {
            if (_keywords == null) _keywords = generalManagers.SelectMany(m => m.keywords).ToArray();
            return _keywords;
        }

        private static string GetSettings()
        {
            FieldInfo[] fields = typeof(Prefs).GetFields(BindingFlags.Static | BindingFlags.Public);
            StringBuilder builder = StaticStringBuilder.Start();

            try
            {
                SaveFields(fields, null, builder);
                return builder.ToString();
            }
            catch (Exception e)
            {
                Log.Add(e);
            }

            return string.Empty;
        }

        private static void ImportItems()
        {
            string filename = EditorUtility.OpenFilePanel("Import Items", EditorApplication.applicationPath, "json");
            if (string.IsNullOrEmpty(filename)) return;

            string text = File.ReadAllText(filename, Encoding.UTF8);
            JsonItem json = Json.Parse(text);
            JsonItem bookmarksItem = json["bookmarks"];

            migrationReplace = true;

            if (bookmarksItem != null) Bookmarks.json = bookmarksItem as JsonArray;

            JsonItem fwItem = json["favorite-windows"];
            if (fwItem != null) FavoriteWindowsManager.json = fwItem as JsonArray;

            JsonItem qabItem = json["quick-access-bar"];
            if (qabItem != null) QuickAccessBarManager.json = qabItem as JsonArray;

            JsonItem hrItem = json["hierarchy-headers"];
            if (hrItem != null) Header.json = hrItem as JsonArray;
            
            JsonItem mlItem = json["mini-layouts"];
            if (mlItem != null) MiniLayoutsManager.json = mlItem as JsonArray;

            JsonItem eiItem = json["empty-inspector"];
            if (eiItem != null) EmptyInspectorManager.json = eiItem as JsonArray;
            
            JsonItem piItem = json["project-icons"];
            if (piItem != null) ProjectFolderIconManager.json = piItem as JsonArray;

            migrationReplace = false;

            ReferenceManager.Save();
        }

        private static void ImportSettings()
        {
            string filename = EditorUtility.OpenFilePanel("Import Settings", EditorApplication.applicationPath, "ucs");
            if (string.IsNullOrEmpty(filename)) return;

            string prefs = File.ReadAllText(filename, Encoding.UTF8);
            LoadSettings(prefs);
        }

        public static void InvokeAfterFirstLoad(Action action)
        {
            if (loaded) action();
            else AfterFirstLoad += action;
        }

        private static void Load()
        {
            string prefStr = EditorPrefs.GetString(PrefsKey, String.Empty);
            LoadSettings(prefStr);

            loaded = true;

            if (AfterFirstLoad != null)
            {
                Delegate[] invocationList = AfterFirstLoad.GetInvocationList();
                for (int i = 0; i < invocationList.Length; i++)
                {
                    try
                    {
                        Delegate d = invocationList[i];
                        d.DynamicInvoke(null);
                    }
                    catch
                    {
                        
                    }
                }

                AfterFirstLoad = null;
            }
        }

        private static void LoadSettings(string str)
        {
            if (string.IsNullOrEmpty(str)) return;

            Type prefType = typeof(Prefs);
            FieldInfo[] fields = prefType.GetFields(BindingFlags.Static | BindingFlags.Public);

            int i = 0;
            try
            {
                LoadFields(str, fields, ref i, null);
            }
            catch (Exception e)
            {
                Log.Add(e);
            }
        }

        private static void LoadFields(string prefStr, FieldInfo[] fields, ref int i, object target)
        {
            StringBuilder builder = StaticStringBuilder.Start();
            bool isKey = true;
            string key = null;

            while (i < prefStr.Length)
            {
                char c = prefStr[i];
                i++;
                if (c == ':' && isKey)
                {
                    key = builder.ToString();
                    builder.Clear();
                    isKey = false;
                }
                else if (c == ';')
                {
                    string value = builder.ToString();
                    builder.Clear();
                    isKey = true;
                    SetValue(target, fields, key, value);
                }
                else if (c == '(')
                {
                    FieldInfo field = GetField(fields, key);
                    if (field == null || (field.FieldType.IsValueType && field.FieldType.IsPrimitive) || field.FieldType == typeof(string))
                    {
                        int indent = 1;
                        i++;
                        while (indent > 0 && i < prefStr.Length)
                        {
                            c = prefStr[i];
                            if (c == ')') indent--;
                            else if (c == '(') indent++;
                            i++;
                        }

                        isKey = true;
                    }
                    else
                    {
                        Type type = field.FieldType; 
                        object newTarget = Activator.CreateInstance(type, false); 

                        BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
                        if (type == typeof(Vector2Int)) bindingFlags |= BindingFlags.NonPublic;

                        FieldInfo[] objFields = type.GetFields(bindingFlags);

                        LoadFields(prefStr, objFields, ref i, newTarget);
                        field.SetValue(target, newTarget);
                        i++;
                        isKey = true;
                    }
                }
                else if (c == ')')
                {
                    string value = builder.ToString();
                    builder.Clear();
                    SetValue(target, fields, key, value);
                    return;
                }
                else builder.Append(c);
            }
        }

        public static string ModifierToString(EventModifiers modifiers)
        {
            StringBuilder builder = StaticStringBuilder.Start();
            if ((modifiers & EventModifiers.Control) == EventModifiers.Control) builder.Append("CTRL");
            if ((modifiers & EventModifiers.Command) == EventModifiers.Command)
            {
                if (builder.Length > 0) builder.Append(" + ");
                builder.Append("CMD");
            }
            if ((modifiers & EventModifiers.Shift) == EventModifiers.Shift)
            {
                if (builder.Length > 0) builder.Append(" + ");
                builder.Append("SHIFT");
            }
            if ((modifiers & EventModifiers.Alt) == EventModifiers.Alt)
            {
                if (builder.Length > 0) builder.Append(" + ");
                builder.Append("ALT");
            }
            if ((modifiers & EventModifiers.FunctionKey) == EventModifiers.FunctionKey)
            {
                if (builder.Length > 0) builder.Append(" + ");
                builder.Append("FN");
            }

            return builder.ToString();
        }

        public static string ModifierToString(EventModifiers modifiers, string extra)
        {
            string v = ModifierToString(modifiers);
            if (!string.IsNullOrEmpty(v)) v += " + ";
            v += extra;
            return v;
        }

        public static string ModifierToString(EventModifiers modifiers, KeyCode keycode)
        {
            return ModifierToString(modifiers, keycode.ToString());
        }

        public static void OnGUI(string searchContext)
        {
            DrawToolbar();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (PrefManager manager in generalManagers)
            {
                try
                {
                    EditorGUI.BeginChangeCheck();
                    manager.Draw();
                    EditorGUILayout.Space();
                    if (EditorGUI.EndChangeCheck() || forceSave)
                    {
                        Save();
                        forceSave = false;
                    }
                }
                catch (ExitGUIException)
                {
                    throw;
                }
                catch
                {
                    
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static void RestoreDefaultSettings()
        {
            if (!EditorUtility.DisplayDialog(
                    "Restore default settings",
                    "Are you sure you want to restore the default settings?",
                    "Restore", "Cancel"))
            {
                return;
            }
            
            if (EditorPrefs.HasKey(PrefsKey)) EditorPrefs.DeleteKey(PrefsKey);

            ReferenceManager.ResetContent();
            LocalSettings.ResetContent();

            AssetDatabase.ImportAsset(Resources.assetFolder + "Scripts/Editor/Prefs/Methods.Prefs.cs", ImportAssetOptions.ForceUpdate);
        }

        public static void Save() 
        {
            string value = GetSettings();
            EditorPrefs.SetString(PrefsKey, value);
        }

        private static void SaveFields(FieldInfo[] fields, object target, StringBuilder builder)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field.IsLiteral || field.IsInitOnly) continue; 
                object value = field.GetValue(target); 

                if (value == null) continue; 

                if (i > 0) builder.Append(";");
                builder.Append(field.Name).Append(":");

                Type type = value.GetType();
                if (type == typeof(string)) StaticStringBuilder.AppendEscaped(builder, value as string, escapeChars);
                else if (type.IsEnum) builder.Append(value);
                else if (type.IsValueType && type.IsPrimitive) builder.Append(value);
                else
                {
                    BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;
                    if (type == typeof(Vector2Int)) bindingFlags |= BindingFlags.NonPublic;
                    FieldInfo[] objFields = type.GetFields(bindingFlags);
                    if (objFields.Length == 0) continue;

                    builder.Append("(");

                    SaveFields(objFields, value, builder);

                    builder.Append(")");
                }
            }
        }

        private static void SetValue(object target, FieldInfo[] fields, string key, object value)
        {
            FieldInfo field = GetField(fields, key);
            if (field == null) return;

            Type type = field.FieldType;
            if (type == typeof(string))
            {
                field.SetValue(target, Unescape(value as string, escapeChars));
            }
            else if (field.FieldType.IsEnum)
            {
                string strVal = value as string;
                if (strVal != null)
                {
                    try
                    {
                        value = Enum.Parse(type, strVal);
                        field.SetValue(target, value);
                    }
                    catch
                    {
                        Debug.Log("Some exception");
                    }
                }
            }
            else if (type.IsValueType)
            {
                try
                {
                    MethodInfo method = type.GetMethod("Parse", new[] { typeof(string) });
                    if (method == null)
                    {
                        Debug.Log("No parse for " + key); 
                        return;
                    }
                    value = method.Invoke(null, new[] { value });
                    if (value != null) field.SetValue(target, value); 
                }
                catch
                {

                }
            }
        }

        private static string Unescape(string value, string[] escapeCodes)
        {
            if (escapeChars == null || escapeChars.Length % 2 != 0) throw new Exception("Length of escapeCodes must be N * 2");

            StringBuilder builder = StaticStringBuilder.Start();

            for (int i = 0; i < value.Length; i++)
            {
                bool success = false;
                for (int j = 0; j < escapeCodes.Length; j += 2)
                {
                    string code = escapeCodes[j + 1];
                    if (value.Length - i - code.Length <= 0) continue;

                    success = true;

                    for (int k = 0; k < code.Length; k++)
                    {
                        if (code[k] != value[i + k])
                        {
                            success = false;
                            break;
                        }
                    }

                    if (success)
                    {
                        builder.Append(escapeCodes[j]);
                        i += code.Length - 1;
                        break;
                    }
                }

                if (!success) builder.Append(value[i]);
            }

            return builder.ToString();
        }
        
        public enum ExportItemIndex
        {
            everything = -1,
            bookmarks = 0,
            favoriteWindows = 1,
            quickAccessBar = 2,
            hierarchyHeaders = 3,
            emptyInspector = 4,
            projectIcons = 5,
            miniLayouts = 6
        }
    }
}