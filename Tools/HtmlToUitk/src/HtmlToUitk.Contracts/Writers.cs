using System.Collections.Generic;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert.Config;

namespace HtmlToUitk.Convert.Writers
{
    public sealed class WriterContext
    {
        public ConversionConfig Config;
        public CapabilityMatrix Capabilities;
        public JobSpec Job;
        public IDiagnosticSink Diagnostics;
        public IFileSystem Fs;
    }

    public interface IWriter
    {
        string Id { get; }
        IEnumerable<OutputFile> Write(TransformedModel model, WriterContext context);
    }
}
