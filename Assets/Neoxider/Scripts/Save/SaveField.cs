using System;

namespace Neo.Save
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SaveField : Attribute
    {
        public SaveField(string key, bool autoSaveOnQuit = true, bool autoLoadOnAwake = true)
        {
            Key = key;
            AutoSaveOnQuit = autoSaveOnQuit;
            AutoLoadOnAwake = autoLoadOnAwake;
        }

        public string Key { get; private set; }
        public bool AutoSaveOnQuit { get; private set; } = true;
        public bool AutoLoadOnAwake { get; private set; } = true;
    }
}