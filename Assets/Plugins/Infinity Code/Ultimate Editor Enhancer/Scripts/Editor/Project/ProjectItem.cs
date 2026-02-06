/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.IO;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.ProjectTools
{
    public class ProjectItem
    {
        public string guid;
        public string path;
        public Rect rect;
        public bool hovered;
        public bool isFolder;

        public Object asset
        {
            get
            {
                return ProjectAssetCache.Get<Object>(path);
            }
        }

        public void Set(string guid, Rect rect)
        {
            this.guid = guid;
            this.rect = rect;

            path = AssetDatabase.GUIDToAssetPath(guid);

            if (!string.IsNullOrEmpty(path))
            {
                FileAttributes attributes = File.GetAttributes(path);
                isFolder = (attributes & FileAttributes.Directory) == FileAttributes.Directory;
            }
            else isFolder = false;

            Vector2 p = Event.current.mousePosition;
            hovered = rect.Contains(p);
        }
    }
}