/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using InfinityCode.UltimateEditorEnhancer.JSON;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace InfinityCode.UltimateEditorEnhancer.ProjectTools
{
    [Serializable]
    public class ProjectFolderRule
    {
        public string folderName;
        public string icon;
        public Color color = Color.white;
        public bool rootOnly = false;
        public bool recursive = true;

        [NonSerialized]
        private bool isDirty = true;
        
        [NonSerialized]
        private Texture _iconTexture;
        
        [NonSerialized]
        private string[] _parts;
        
        public string[] parts
        {
            get
            {
                if (_parts == null) _parts = folderName.ToUpperInvariant().Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
                return _parts;
            }
        }

        public JsonItem json
        {
            get
            { 
                return Json.Serialize(this) as JsonObject;
            }
        }
        
        public Texture iconTexture
        {
            get
            {
                if (isDirty)
                {
                    isDirty = false;

                    if (!string.IsNullOrEmpty(icon))
                    {
                        GUIContent content = EditorGUIUtility.IconContent(icon);
                        if (content != null) _iconTexture = content.image;
                        else _iconTexture = null;
                    }
                    else _iconTexture = null;
                }

                return _iconTexture;
            }
        }

        public void Draw(Rect rect, bool expanded, bool isEmpty)
        {
            Rect r;
            Rect r2 = new Rect();
            bool drawIcon = false;
            Texture folderTexture = Icons.folder;
            
            if (rect.height > 20)
            {
                r = new Rect(rect.x - 1, rect.y - 1, rect.width + 2, rect.width + 2);
                r2 = new Rect(rect.x + rect.width / 2, rect.y + rect.width / 2.2f, rect.width / 3, rect.width / 3);
                drawIcon = true;
            }
            else if (rect.x > 20)
            {
                r = new Rect(rect.x - 1, rect.y - 1, rect.height + 2, rect.height + 2);
                /*r2 = new Rect(rect.x + 3, rect.y + 5, rect.height - 6, rect.height - 6);
                drawIcon = true;*/
                if (isEmpty) folderTexture = Icons.folderEmpty;
                else if (expanded) folderTexture = Icons.folderOpen;
            }
            else
            {
                r = new Rect(rect.x + 2, rect.y - 1, rect.height + 2, rect.height + 2);
                if (isEmpty) folderTexture = Icons.folderEmpty;
                else if (expanded) folderTexture = Icons.folderOpen;
                /*r2 = new Rect(rect.x + 6, rect.y + 5, rect.height - 6, rect.height - 6);
                drawIcon = true;*/
            }

            if (Prefs.projectFolderIconsDrawColors)
            {
                Color clr = GUI.color;
                GUI.color = color;
                GUI.DrawTexture(r, folderTexture);
                GUI.color = clr;
            }

            if (drawIcon && Prefs.projectFolderIconsDrawIcons)
            {
                Texture img = iconTexture;

                if (img != null)
                {
                    GUI.DrawTexture(r2, img);
                }
            }
        }

        public void SetDirty()
        {
            isDirty = true;
            _parts = null;
        }
    }
}