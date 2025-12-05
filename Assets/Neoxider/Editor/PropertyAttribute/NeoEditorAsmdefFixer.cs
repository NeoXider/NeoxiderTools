using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    /// Утилита для автоматического добавления ссылок на все Neo сборки в Neo.Editor.asmdef
    /// Решает проблему отображения компонентов при установке пакета через Package Manager
    /// </summary>
    public static class NeoEditorAsmdefFixer
    {
        [MenuItem("Tools/Neoxider/Fix Editor Assembly References")]
        public static void FixEditorAssemblyReferences()
        {
            string editorAsmdefPath = FindEditorAsmdefPath();
            
            if (string.IsNullOrEmpty(editorAsmdefPath))
            {
                Debug.LogError("[Neoxider] Не удалось найти Neo.Editor.asmdef");
                return;
            }
            
            // Находим все Neo asmdef файлы
            List<string> neoAsmdefGUIDs = FindAllNeoAsmdefGUIDs();
            
            if (neoAsmdefGUIDs.Count == 0)
            {
                Debug.LogWarning("[Neoxider] Не найдены дополнительные Neo asmdef файлы");
                return;
            }
            
            // Читаем текущий asmdef
            string json = File.ReadAllText(editorAsmdefPath);
            
            // Проверяем, какие GUID уже есть
            int addedCount = 0;
            foreach (string guid in neoAsmdefGUIDs)
            {
                if (!json.Contains(guid))
                {
                    // Находим массив references и добавляем новый GUID
                    int referencesStart = json.IndexOf("\"references\": [");
                    if (referencesStart != -1)
                    {
                        int arrayStart = json.IndexOf('[', referencesStart);
                        int insertPosition = arrayStart + 1;
                        
                        // Добавляем новую ссылку
                        string newReference = $"\n    \"GUID:{guid}\",";
                        json = json.Insert(insertPosition, newReference);
                        addedCount++;
                    }
                }
            }
            
            if (addedCount > 0)
            {
                File.WriteAllText(editorAsmdefPath, json);
                AssetDatabase.Refresh();
                Debug.Log($"[Neoxider] Добавлено {addedCount} ссылок в Neo.Editor.asmdef. Unity перекомпилирует скрипты.");
            }
            else
            {
                Debug.Log("[Neoxider] Все необходимые ссылки уже присутствуют в Neo.Editor.asmdef");
            }
        }
        
        private static string FindEditorAsmdefPath()
        {
            // Ищем в Assets
            string[] assetsPaths = AssetDatabase.FindAssets("Neo.Editor t:asmdef");
            if (assetsPaths.Length > 0)
            {
                return AssetDatabase.GUIDToAssetPath(assetsPaths[0]);
            }
            
            // Ищем в Packages
            string[] files = Directory.GetFiles("Packages", "Neo.Editor.asmdef", SearchOption.AllDirectories);
            return files.Length > 0 ? files[0] : null;
        }
        
        private static List<string> FindAllNeoAsmdefGUIDs()
        {
            List<string> guids = new List<string>();
            
            // Ищем все asmdef файлы, которые начинаются с Neo.
            string[] foundAssets = AssetDatabase.FindAssets("Neo t:asmdef");
            
            foreach (string guid in foundAssets)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                
                // Исключаем сам Neo.Editor.asmdef
                if (fileName != "Neo.Editor" && fileName.StartsWith("Neo"))
                {
                    guids.Add(guid);
                }
            }
            
            return guids;
        }
    }
}

