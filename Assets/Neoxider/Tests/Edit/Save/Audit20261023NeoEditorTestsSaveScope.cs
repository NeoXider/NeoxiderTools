using System;
using System.IO;
using Neo.Save;

namespace Neo.Editor.Tests
{
    /// <summary>Restores the exact provider and optionally redirects writes to a unique temporary directory.</summary>
    internal sealed class Audit20261023NeoEditorTestsSaveScope : IDisposable
    {
        private readonly ISaveProvider _previousProvider;
        private readonly string _temporaryRoot;
        private bool _disposed;

        public Audit20261023NeoEditorTestsSaveScope(bool useTemporaryProvider = true)
        {
            _previousProvider = SaveProvider.CurrentProvider;
            if (useTemporaryProvider)
            {
                _temporaryRoot = Path.Combine(Path.GetTempPath(),
                    "Audit20261023NeoEditorTests-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_temporaryRoot);
                FileSaveProviderOptions options = new FileSaveProviderOptions { PersistenceRoot = _temporaryRoot };
                SaveProvider.SetProvider(new FileSaveProvider("save.json", options));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                SaveProvider.SetProvider(_previousProvider);
            }
            finally
            {
                if (_temporaryRoot != null && Directory.Exists(_temporaryRoot))
                {
                    Directory.Delete(_temporaryRoot, true);
                }
            }
        }
    }
}
