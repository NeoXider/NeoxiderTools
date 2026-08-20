using System;

namespace Neo.Editor
{
    /// <summary>
    ///     Decides when the Scene Saver may write the next backup copy. Pure logic: no UnityEditor calls,
    ///     no file system access, so it is cheap enough to run from the editor tick and can be driven
    ///     directly by the EditMode suite.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The saver writes with <c>EditorSceneManager.SaveScene(scene, path, saveAsCopy: true)</c>,
    ///         and a copy save deliberately does <b>not</b> clear the scene's dirty flag. Triggering on
    ///         "the scene is dirty" alone therefore repeats forever: a scene that stays dirty — which is
    ///         the normal state while you work, and the permanent state when some tool keeps dirtying it —
    ///         was re-serialized in full every interval for as long as the editor stayed open.
    ///     </para>
    ///     <para>
    ///         The cycle is broken by remembering which scene revision was already handled instead of
    ///         reading the dirty flag as "not handled yet". A revision is the scene path plus the caller's
    ///         change token plus the number of clean-to-dirty transitions observed here; the same revision
    ///         is never handled twice, in any mode. A save can only happen again after the scene actually
    ///         changes, so no amount of dirtiness produces a second write.
    ///     </para>
    /// </remarks>
    public sealed class SceneSaverAutoSaveScheduler
    {
        private string _handledScenePath;
        private long _handledRevision;
        private long _handledDirtyEpoch;
        private bool _hasHandled;

        private long _dirtyEpoch;
        private bool _wasDirty;
        private double _lastHandledTime;

        /// <summary>
        ///     Number of clean-to-dirty transitions observed so far. Part of the revision identity, so a
        ///     change that dirties the scene without touching the caller's change token still earns one
        ///     backup — and only one.
        /// </summary>
        public long DirtyEpoch => _dirtyEpoch;

        /// <summary>Restarts the interval countdown, e.g. after a different scene was opened.</summary>
        /// <param name="now">Current editor time, in seconds.</param>
        public void ResetTimer(double now)
        {
            _lastHandledTime = now;
        }

        /// <summary>
        ///     Cheap check that answers whether a backup copy is due right now. Safe to call every editor
        ///     tick: it only compares numbers and strings.
        /// </summary>
        /// <param name="context">Snapshot of the current editor and scene state.</param>
        /// <returns><c>true</c> when the caller should write a backup copy of the active scene.</returns>
        public bool ShouldSave(SceneSaverCheckContext context)
        {
            TrackDirtyTransition(context.IsDirty);

            if (string.IsNullOrEmpty(context.ScenePath))
            {
                return false;
            }

            if (!context.IsDirty && !context.SaveEvenIfNotDirty)
            {
                return false;
            }

            // WHY: editor time never runs backwards inside one session, but a stale marker from a
            // previous one would otherwise block auto-save until the difference elapsed.
            if (context.Now < _lastHandledTime)
            {
                _lastHandledTime = context.Now;
            }

            if (context.Now - _lastHandledTime < context.IntervalSeconds)
            {
                return false;
            }

            return !IsAlreadyHandled(context.ScenePath, context.Revision);
        }

        /// <summary>
        ///     Whether this exact scene revision was already handled and must not be written again.
        /// </summary>
        /// <param name="scenePath">Asset path of the scene.</param>
        /// <param name="revision">Change token of the scene.</param>
        /// <returns><c>true</c> when the same revision of the same scene was handled before.</returns>
        public bool IsAlreadyHandled(string scenePath, long revision)
        {
            return _hasHandled
                   && _handledRevision == revision
                   && _handledDirtyEpoch == _dirtyEpoch
                   && string.Equals(_handledScenePath, scenePath, StringComparison.Ordinal);
        }

        /// <summary>
        ///     Records that this revision has been dealt with and restarts the interval countdown. Call it
        ///     after every attempt, successful or not: an attempt that is retried immediately is the same
        ///     cycle in a different disguise.
        /// </summary>
        /// <param name="scenePath">Asset path of the scene that was handled.</param>
        /// <param name="revision">Change token of the scene at the time it was handled.</param>
        /// <param name="now">Current editor time, in seconds.</param>
        public void MarkHandled(string scenePath, long revision, double now)
        {
            _handledScenePath = scenePath;
            _handledRevision = revision;
            _handledDirtyEpoch = _dirtyEpoch;
            _hasHandled = true;
            _lastHandledTime = now;
        }

        private void TrackDirtyTransition(bool isDirty)
        {
            if (!isDirty)
            {
                _wasDirty = false;
                return;
            }

            if (_wasDirty)
            {
                return;
            }

            _wasDirty = true;
            _dirtyEpoch++;
        }
    }
}
