using NUnit.Framework;
using UnityEditor;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Guards the two ways the Scene Saver used to hurt: it re-saved a scene that never changed for as
    ///     long as the editor stayed open, and its settings could not be turned off durably.
    /// </summary>
    public class SceneSaverAutoSaveTests
    {
        private const string ScenePath = "Assets/Scenes/SceneSaverProbe.unity";
        private const double IntervalSeconds = 180d;

        private bool _hadEnabled;
        private bool _enabled;
        private bool _hadInterval;
        private float _interval;
        private bool _hadSaveEvenIfNotDirty;
        private bool _saveEvenIfNotDirty;

        // WHY: EditorPrefs is the real, machine-wide store of the developer running the suite. Capture
        // whatever was there and put it back, key by key.
        [SetUp]
        public void CaptureEditorPrefs()
        {
            _hadEnabled = EditorPrefs.HasKey(SceneSaverSettings.EnabledKey);
            _enabled = EditorPrefs.GetBool(SceneSaverSettings.EnabledKey, SceneSaverSettings.DefaultEnabled);
            _hadInterval = EditorPrefs.HasKey(SceneSaverSettings.IntervalMinutesKey);
            _interval = EditorPrefs.GetFloat(SceneSaverSettings.IntervalMinutesKey,
                SceneSaverSettings.DefaultIntervalMinutes);
            _hadSaveEvenIfNotDirty = EditorPrefs.HasKey(SceneSaverSettings.SaveEvenIfNotDirtyKey);
            _saveEvenIfNotDirty = EditorPrefs.GetBool(SceneSaverSettings.SaveEvenIfNotDirtyKey, false);
        }

        [TearDown]
        public void RestoreEditorPrefs()
        {
            RestoreBool(SceneSaverSettings.EnabledKey, _hadEnabled, _enabled);
            RestoreBool(SceneSaverSettings.SaveEvenIfNotDirtyKey, _hadSaveEvenIfNotDirty, _saveEvenIfNotDirty);

            if (_hadInterval)
            {
                EditorPrefs.SetFloat(SceneSaverSettings.IntervalMinutesKey, _interval);
            }
            else
            {
                EditorPrefs.DeleteKey(SceneSaverSettings.IntervalMinutesKey);
            }

            // WHY: the live editor session keeps a cached instance; leave it agreeing with the store.
            SceneSaverSettings.Shared.Reload();
        }

        private static void RestoreBool(string key, bool existed, bool value)
        {
            if (existed)
            {
                EditorPrefs.SetBool(key, value);
            }
            else
            {
                EditorPrefs.DeleteKey(key);
            }
        }

        private static SceneSaverCheckContext Check(double now, long revision, bool isDirty,
            bool saveEvenIfNotDirty = false)
        {
            return new SceneSaverCheckContext(now, IntervalSeconds, ScenePath, revision, isDirty,
                saveEvenIfNotDirty);
        }

        [Test]
        public void ShouldSave_RepeatedCheckOnUnchangedScene_DoesNotRequestASecondSave()
        {
            SceneSaverAutoSaveScheduler scheduler = new SceneSaverAutoSaveScheduler();
            scheduler.ResetTimer(0d);

            SceneSaverCheckContext first = Check(IntervalSeconds + 1d, 7L, true);
            Assert.That(scheduler.ShouldSave(first), Is.True,
                "The first check after the interval must back up a dirty scene.");
            scheduler.MarkHandled(first.ScenePath, first.Revision, first.Now);

            // WHY: SaveScene(..., saveAsCopy: true) never clears the dirty flag, so the trigger the old
            // code relied on is still true here even though nothing in the scene changed. Reading it as
            // "not backed up yet" is exactly what re-saved the same scene every interval, forever.
            Assert.That(scheduler.ShouldSave(Check(first.Now + IntervalSeconds + 1d, 7L, true)), Is.False,
                "An unchanged scene must not be written a second time, however dirty it stays.");
            Assert.That(scheduler.ShouldSave(Check(first.Now + 100d * IntervalSeconds, 7L, true)), Is.False,
                "No amount of elapsed time revives the same revision.");
        }

        [Test]
        public void ShouldSave_SaveEvenIfNotDirty_StillNeverRepeatsTheSameRevision()
        {
            SceneSaverAutoSaveScheduler scheduler = new SceneSaverAutoSaveScheduler();
            scheduler.ResetTimer(0d);

            SceneSaverCheckContext first = Check(IntervalSeconds + 1d, 3L, false, true);
            Assert.That(scheduler.ShouldSave(first), Is.True);
            scheduler.MarkHandled(first.ScenePath, first.Revision, first.Now);

            Assert.That(scheduler.ShouldSave(Check(first.Now + IntervalSeconds + 1d, 3L, false, true)), Is.False,
                "'Save even if not dirty' asks for backups of changes, not for identical copies on a timer.");
        }

        [Test]
        public void ShouldSave_AfterTheSceneChanges_RequestsANewSave()
        {
            SceneSaverAutoSaveScheduler scheduler = new SceneSaverAutoSaveScheduler();
            scheduler.ResetTimer(0d);

            SceneSaverCheckContext first = Check(IntervalSeconds + 1d, 7L, true);
            Assert.That(scheduler.ShouldSave(first), Is.True);
            scheduler.MarkHandled(first.ScenePath, first.Revision, first.Now);

            SceneSaverCheckContext changed = Check(first.Now + IntervalSeconds + 1d, 8L, true);
            Assert.That(scheduler.ShouldSave(changed), Is.True,
                "A new revision must still be backed up — the gate must not disable auto-save outright.");
        }

        [Test]
        public void ShouldSave_SceneDirtiedAgainWithoutUndoEntry_EarnsExactlyOneMoreSave()
        {
            SceneSaverAutoSaveScheduler scheduler = new SceneSaverAutoSaveScheduler();
            scheduler.ResetTimer(0d);

            SceneSaverCheckContext first = Check(IntervalSeconds + 1d, 5L, true);
            Assert.That(scheduler.ShouldSave(first), Is.True);
            scheduler.MarkHandled(first.ScenePath, first.Revision, first.Now);

            // WHY: a normal Save Scene cleans the flag; a later script-driven change dirties it again
            // without creating an undo group, so the change token alone would miss it.
            scheduler.ShouldSave(Check(first.Now + 1d, 5L, false));

            SceneSaverCheckContext dirtiedAgain = Check(first.Now + IntervalSeconds + 2d, 5L, true);
            Assert.That(scheduler.ShouldSave(dirtiedAgain), Is.True,
                "The clean-to-dirty transition is a new revision and must be backed up.");
            scheduler.MarkHandled(dirtiedAgain.ScenePath, dirtiedAgain.Revision, dirtiedAgain.Now);

            Assert.That(scheduler.ShouldSave(Check(dirtiedAgain.Now + 10d * IntervalSeconds, 5L, true)), Is.False,
                "Staying dirty afterwards must not produce a stream of identical copies.");
        }

        [Test]
        public void ShouldSave_UnsavedScene_IsNeverWritten()
        {
            SceneSaverAutoSaveScheduler scheduler = new SceneSaverAutoSaveScheduler();
            scheduler.ResetTimer(0d);

            SceneSaverCheckContext untitled = new SceneSaverCheckContext(IntervalSeconds + 1d, IntervalSeconds,
                string.Empty, 1L, true, true);
            Assert.That(scheduler.ShouldSave(untitled), Is.False);
        }

        [Test]
        public void Settings_DisabledState_SurvivesAnEditorRestart()
        {
            SceneSaverSettings settings = new SceneSaverSettings();
            settings.IsEnabled = false;

            // WHY: a brand-new instance is what the next editor session gets — it can only know what was
            // persisted. Before 10.12.0 nothing was, so auto-save came back on after every restart.
            SceneSaverSettings afterRestart = new SceneSaverSettings();
            Assert.That(afterRestart.IsEnabled, Is.False,
                "A user who switched auto-save off must not find it running again after a restart.");
        }

        [Test]
        public void Settings_IntervalAndNotDirtyOption_SurviveAnEditorRestart()
        {
            SceneSaverSettings settings = new SceneSaverSettings();
            settings.IntervalMinutes = 7.5f;
            settings.SaveEvenIfNotDirty = true;

            SceneSaverSettings afterRestart = new SceneSaverSettings();
            Assert.That(afterRestart.IntervalMinutes, Is.EqualTo(7.5f).Within(0.0001f));
            Assert.That(afterRestart.SaveEvenIfNotDirty, Is.True);
        }

        [Test]
        public void Settings_NonPositiveInterval_IsClampedInsteadOfSavingEveryTick()
        {
            SceneSaverSettings settings = new SceneSaverSettings();
            settings.IntervalMinutes = 0f;

            Assert.That(settings.IntervalMinutes, Is.EqualTo(SceneSaverSettings.MinIntervalMinutes).Within(0.0001f));
        }

        [Test]
        public void Settings_ResetToDefaults_RestoresTheShippedBehaviour()
        {
            SceneSaverSettings settings = new SceneSaverSettings();
            settings.IsEnabled = false;
            settings.IntervalMinutes = 42f;

            settings.ResetToDefaults();

            Assert.That(settings.IsEnabled, Is.EqualTo(SceneSaverSettings.DefaultEnabled));
            Assert.That(settings.IntervalMinutes,
                Is.EqualTo(SceneSaverSettings.DefaultIntervalMinutes).Within(0.0001f));
            Assert.That(new SceneSaverSettings().IsEnabled, Is.EqualTo(SceneSaverSettings.DefaultEnabled),
                "Reset must clear the persisted keys, not only the in-memory copy.");
        }
    }
}
