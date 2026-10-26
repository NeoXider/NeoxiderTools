using System;
using Neo.Tools;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoToolsTimeTimerProbe : TimerObject
    {
        private string _testSaveKey;

        public Func<DateTime> UtcNowProvider { get; set; }

        public void ConfigureRealTimeSave(string key)
        {
            ConfigureSave(key, TimerSaveMode.RealTime);
        }

        public void ConfigureSave(string key, TimerSaveMode mode)
        {
            _testSaveKey = key;
            saveMode = mode;
            saveProgress = true;
        }

        public void SaveForTest()
        {
            SaveState();
        }

        public void RestoreForTest()
        {
            Init();
        }

        public void DisableSaving()
        {
            saveProgress = false;
        }

        protected override string GetSaveKey()
        {
            return _testSaveKey;
        }

        protected override DateTime GetUtcNow()
        {
            return UtcNowProvider == null ? base.GetUtcNow() : UtcNowProvider();
        }
    }
}
