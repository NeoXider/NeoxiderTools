using System;

namespace Neo
{
    /// <summary>
    ///     Assigns a documentation .md file to a MonoBehaviour. Path is relative to the package Docs folder.
    ///     Used by CustomEditorBase to show a "Documentation" foldout and "Open in window" in the inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class NeoDocAttribute : Attribute
    {
        public NeoDocAttribute(string docPathRelativeToDocs)
        {
            DocPath = docPathRelativeToDocs?.Trim().Replace('\\', '/').TrimStart('/') ?? string.Empty;
        }

        /// <summary>Path to .md file relative to PackageRoot/Docs/ (e.g. "Bonus/TimeReward/README.md").</summary>
        public string DocPath { get; }
    }
}