using System;

namespace Neo.Editor
{
    /// <summary>
    ///     Former auto-registration hook for Neo component editors. Kept only so code that names the type still
    ///     compiles; it does nothing.
    /// </summary>
    /// <remarks>
    ///     WHY: the [InitializeOnLoad] hook enumerated every type of every loaded assembly after each domain reload
    ///     and discarded the result. Editor registration never depended on it: <see cref="NeoCustomEditor" /> is a
    ///     fallback editor for all MonoBehaviours and Unity discovers it by its attribute.
    /// </remarks>
    [Obsolete("NeoEditorAutoRegister no longer does anything; NeoCustomEditor is discovered by Unity directly.")]
    public static class NeoEditorAutoRegister
    {
    }
}
