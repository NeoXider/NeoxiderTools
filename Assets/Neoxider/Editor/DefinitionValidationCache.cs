using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor
{
    /// <summary>
    ///     Keeps the result of an asset's validation pass (for example <c>ValidateDefinition()</c>) until the
    ///     asset changes, so an inspector does not re-run a full validation and rebuild every message string on
    ///     each IMGUI event.
    ///     <para>
    ///         A result is considered stale when the asset's <see cref="EditorUtility.GetDirtyCount(Object)" />
    ///         moves (any serialized edit, including undo and redo) or when <see cref="Invalidate" /> is called.
    ///         A stale result is only replaced during the Layout event, so Layout and Repaint always draw the same
    ///         number of help boxes.
    ///     </para>
    /// </summary>
    internal sealed class DefinitionValidationCache
    {
        private Object _source;
        private int _dirtyCount = -1;
        private IReadOnlyList<string> _issues = Array.Empty<string>();

        /// <summary>The last stored issues; empty until <see cref="Store" /> runs.</summary>
        internal IReadOnlyList<string> Issues => _issues;

        /// <summary>Number of times <see cref="Store" /> accepted a fresh result. Used by tests.</summary>
        internal int RefreshCount { get; private set; }

        /// <summary>
        ///     True when <paramref name="source" /> has no stored result yet, or changed since the last
        ///     <see cref="Store" /> and the current event may rebuild the layout.
        /// </summary>
        internal bool NeedsRefresh(Object source)
        {
            if (source == null)
            {
                return false;
            }

            if (!ReferenceEquals(source, _source) || _dirtyCount < 0)
            {
                return true;
            }

            if (EditorUtility.GetDirtyCount(source) == _dirtyCount)
            {
                return false;
            }

            Event current = Event.current;
            return current == null || current.type == EventType.Layout;
        }

        /// <summary>Stores <paramref name="issues" /> as the current result for <paramref name="source" />.</summary>
        internal void Store(Object source, IReadOnlyList<string> issues)
        {
            _source = source;
            _dirtyCount = source != null ? EditorUtility.GetDirtyCount(source) : -1;
            _issues = issues ?? Array.Empty<string>();
            RefreshCount++;
        }

        /// <summary>Forces the next <see cref="NeedsRefresh" /> to report a refresh, e.g. after an external change.</summary>
        internal void Invalidate()
        {
            _dirtyCount = -1;
        }
    }
}
