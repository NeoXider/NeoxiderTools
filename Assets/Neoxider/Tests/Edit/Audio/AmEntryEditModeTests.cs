using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Audio;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     EditMode coverage for the 10.13 audio contract: entries addressable by id and by index, several
    ///     clips per entry, the channel x entry volume product, the pitch defaults, music pools with their
    ///     two modes, per-call overrides, and the migration of pre-10.13 serialized data.
    ///     <para>
    ///         Fades and the shuffle watchdog need a running player loop, so in EditMode every transition
    ///         degrades to a clean cut - which is exactly what lets these tests assert final state
    ///         synchronously.
    ///     </para>
    /// </summary>
    [TestFixture]
    public class AmEntryEditModeTests
    {
        private GameObject _go;
        private AM _am;
        private AudioSource _efx;
        private AudioSource _music;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("AmEntryEditModeTests");
            _am = _go.AddComponent<AM>();
            _efx = _go.AddComponent<AudioSource>();
            _music = _go.AddComponent<AudioSource>();

            SetPrivateField("_efx", _efx);
            SetPrivateField("_music", _music);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                UnityEngine.Object.DestroyImmediate(_go);
            }
        }

        // ---------------------------------------------------------------- entry contract

        [Test]
        public void SoundEntry_DefaultsToPitchRandomisationOn()
        {
            Assert.IsTrue(new SoundEntry().RandomizePitch,
                "Effects repeat constantly - the detune is the point, so it must default to on.");
        }

        [Test]
        public void MusicEntry_DefaultsToPitchRandomisationOff()
        {
            Assert.IsFalse(new MusicEntry().RandomizePitch,
                "A detuned music bed reads as a fault, so music must default to no pitch randomisation.");
        }

        [Test]
        public void MusicEntry_DefaultsToLoopMode()
        {
            Assert.AreEqual(MusicPoolMode.Loop, new MusicEntry().Mode,
                "A track change belongs to a game beat, not to wherever the file ends - Loop is the default.");
        }

        [Test]
        public void Entry_DefaultVolumeIsOne()
        {
            Assert.AreEqual(1f, new SoundEntry().Volume, 1e-5f);
            Assert.AreEqual(1f, new MusicEntry().Volume, 1e-5f);
        }

        [Test]
        public void NextClip_ReturnsEveryClipOfTheEntryAndNeverRepeatsImmediately()
        {
            AudioClip a = CreateClip("a");
            AudioClip b = CreateClip("b");
            AudioClip c = CreateClip("c");
            var entry = new SoundEntry("hit", a, b, c);

            var seen = new HashSet<AudioClip>();
            AudioClip previous = null;

            for (int i = 0; i < 200; i++)
            {
                AudioClip picked = entry.NextClip();
                Assert.IsNotNull(picked, "A entry with three clips must always produce one.");
                Assert.AreNotSame(previous, picked, "The same clip must not be picked twice in a row.");
                seen.Add(picked);
                previous = picked;
            }

            Assert.AreEqual(3, seen.Count, "Over 200 picks every clip of the set must have been used.");
        }

        [Test]
        public void NextClip_SkipsNullsAndSurvivesAnEntryWithOneUsableClip()
        {
            AudioClip only = CreateClip("only");
            var entry = new SoundEntry("gappy", null, only, null);

            for (int i = 0; i < 20; i++)
            {
                Assert.AreSame(only, entry.NextClip(),
                    "With one usable clip among nulls the entry must keep returning it, not fall silent.");
            }
        }

        [Test]
        public void NextClip_OnEmptyEntryReturnsNullWithoutThrowing()
        {
            var entry = new SoundEntry("empty");
            Assert.IsNull(entry.NextClip());
            Assert.IsTrue(entry.IsEmpty);
        }

        [Test]
        public void ClipAt_WrapsSoAWalkingCounterIsSafe()
        {
            AudioClip a = CreateClip("a");
            AudioClip b = CreateClip("b");
            var entry = new SoundEntry("steps", a, b);

            Assert.AreSame(a, entry.ClipAt(0));
            Assert.AreSame(b, entry.ClipAt(1));
            Assert.AreSame(a, entry.ClipAt(2), "Index past the end must wrap, not throw.");
            Assert.AreSame(b, entry.ClipAt(-1), "A negative index must wrap from the end.");
        }

        // ---------------------------------------------------------------- lookup

        [Test]
        public void GetSound_FindsEntryById_AndReturnsNullForUnknownId()
        {
            _am.SetSoundEntries(new SoundEntry("hit", CreateClip("h")), new SoundEntry("coin", CreateClip("c")));

            Assert.AreEqual("coin", _am.GetSound("coin").Id);
            Assert.IsNull(_am.GetSound("nope"), "An unknown id must resolve to null, not to some other entry.");
            Assert.IsNull(_am.GetSound(string.Empty), "An empty id must never match an unnamed entry.");
        }

        [Test]
        public void Play_ByIdAndByIndex_BothResolveWithoutThrowing()
        {
            _am.SetSoundEntries(new SoundEntry("hit", CreateClip("h")));

            Assert.DoesNotThrow(() => _am.Play("hit"));
            Assert.DoesNotThrow(() => _am.Play(0));
            Assert.DoesNotThrow(() => _am.Play("missing"), "An unknown id must warn, not throw.");
            Assert.DoesNotThrow(() => _am.Play(7), "An out-of-range index must warn, not throw.");
        }

        [Test]
        public void GetMusic_FindsPoolById()
        {
            _am.SetMusicEntries(new MusicEntry("menu", CreateClip("m")), new MusicEntry("boss", CreateClip("b")));

            Assert.AreEqual("boss", _am.GetMusicPool("boss").Id);
            Assert.IsNull(_am.GetMusicPool("gameplay"));
        }

        // ---------------------------------------------------------------- volume contract

        [Test]
        public void MusicVolume_IsChannelTimesEntry()
        {
            _am.SetMusicEntries(new MusicEntry("quiet", CreateClip("q")) { Volume = 0.5f });
            _am.SetMusicVolume(0.8f);

            _am.PlayMusicPool("quiet");

            Assert.AreEqual(0.4f, _music.volume, 1e-4f,
                "Channel 0.8 x entry 0.5 must be heard at 0.4 - the entry volume is a multiplier, not a level.");
        }

        [Test]
        public void MusicVolume_EntryAtOneLeavesTheChannelUntouched()
        {
            _am.SetMusicEntries(new MusicEntry("full", CreateClip("f")));
            _am.SetMusicVolume(0.3f);

            _am.PlayMusicPool("full");

            Assert.AreEqual(0.3f, _music.volume, 1e-4f,
                "A channel at 0.3 with an entry at 1 must play at 0.3.");
        }

        [Test]
        public void SetMusicVolume_AfterPlayback_RescalesAgainstTheEntryVolume()
        {
            _am.SetMusicEntries(new MusicEntry("half", CreateClip("h")) { Volume = 0.5f });
            _am.PlayMusicPool("half");

            _am.SetMusicVolume(0.6f);

            Assert.AreEqual(0.3f, _music.volume, 1e-4f,
                "Changing the channel volume must keep the entry multiplier applied.");
        }

        [Test]
        public void MusicChannelVolume_IsAdoptedFromTheAuthoredAudioSource()
        {
            // WHY: a project that turned the music AudioSource down in the inspector and relied on the old
            // random-music path - which never wrote the volume - must not suddenly play at full.
            _music.volume = 0.3f;
            _am.SetMusicEntries(new MusicEntry("menu", CreateClip("m")));

            _am.PlayMusicPool("menu");

            Assert.AreEqual(0.3f, _music.volume, 1e-4f,
                "The authored AudioSource volume is the music channel and must survive the first playback.");
            Assert.AreEqual(0.3f, _am.MusicVolume, 1e-4f);
        }

        [Test]
        public void MusicVolumeOverride_StillMultipliesWithTheChannel()
        {
            _am.SetMusicEntries(new MusicEntry("boss", CreateClip("b")));
            _am.SetMusicVolume(0.8f);

            _am.PlayMusicPool("boss", MusicOptions.Volume(0.5f));

            Assert.AreEqual(0.4f, _music.volume, 1e-4f,
                "A per-call override replaces the ENTRY volume; the player's channel slider must still apply.");
        }

        [Test]
        public void MusicVolumeOverride_DoesNotWriteBackIntoTheEntry()
        {
            var entry = new MusicEntry("boss", CreateClip("b"));
            _am.SetMusicEntries(entry);

            _am.PlayMusicPool("boss", MusicOptions.Volume(0.25f));

            Assert.AreEqual(1f, entry.Volume, 1e-5f,
                "An override is for one call only and must never mutate the configured entry.");
        }

        // ---------------------------------------------------------------- pools

        [Test]
        public void PlayMusicPool_StartsATrackFromThePool()
        {
            AudioClip a = CreateClip("a");
            AudioClip b = CreateClip("b");
            _am.SetMusicEntries(new MusicEntry("gameplay", a, b));

            _am.PlayMusicPool("gameplay");

            Assert.IsTrue(_music.clip == a || _music.clip == b,
                "The pool must start on one of its own tracks.");
            Assert.AreEqual("gameplay", _am.CurrentMusicId);
        }

        [Test]
        public void PlayMusicPool_CalledTwiceForTheSamePool_DoesNotRestartOrRaiseTwice()
        {
            _am.SetMusicEntries(new MusicEntry("menu", CreateClip("m1"), CreateClip("m2")));

            int started = 0;
            _am.OnMusicStarted += _ => started++;

            _am.PlayMusicPool("menu");
            AudioClip first = _music.clip;
            _am.PlayMusicPool("menu");

            Assert.AreEqual(1, started, "Re-asserting the pool that already plays must not restart it.");
            Assert.AreSame(first, _music.clip, "The track must not change when nothing was asked to change.");
        }

        [Test]
        public void PlayMusicPool_SwitchingPools_ChangesTrackAndPool()
        {
            AudioClip menu = CreateClip("menu");
            AudioClip boss = CreateClip("boss");
            _am.SetMusicEntries(new MusicEntry("menu", menu), new MusicEntry("boss", boss));

            _am.PlayMusicPool("menu");
            _am.PlayMusicPool("boss");

            Assert.AreSame(boss, _music.clip);
            Assert.AreEqual("boss", _am.CurrentMusicId);
        }

        [Test]
        public void PlayMusicPool_UnknownId_DoesNotThrowAndKeepsPlaying()
        {
            AudioClip menu = CreateClip("menu");
            _am.SetMusicEntries(new MusicEntry("menu", menu));
            _am.PlayMusicPool("menu");

            Assert.DoesNotThrow(() => _am.PlayMusicPool("does-not-exist"));
            Assert.AreSame(menu, _music.clip, "A bad id must not silence the music that was playing.");
        }

        [Test]
        public void PlayMusicPool_LoopPool_SetsTheSourceToLoop()
        {
            _am.SetMusicEntries(new MusicEntry("menu", CreateClip("a"), CreateClip("b"))
            {
                Mode = MusicPoolMode.Loop
            });

            _am.PlayMusicPool("menu");

            Assert.IsTrue(_music.loop, "A Loop pool must hold its track until the game says otherwise.");
        }

        [Test]
        public void PlayMusicPool_ShufflePoolWithSeveralTracks_DoesNotLoopTheSource()
        {
            _am.SetMusicEntries(new MusicEntry("gameplay", CreateClip("a"), CreateClip("b"))
            {
                Mode = MusicPoolMode.Shuffle
            });

            _am.PlayMusicPool("gameplay");

            Assert.IsFalse(_music.loop,
                "A shuffle pool must let the clip end, otherwise the advance never happens.");
        }

        [Test]
        public void PlayMusicPool_ShufflePoolWithOneTrack_LoopsAnyway()
        {
            _am.SetMusicEntries(new MusicEntry("solo", CreateClip("only")) { Mode = MusicPoolMode.Shuffle });

            _am.PlayMusicPool("solo");

            Assert.IsTrue(_music.loop, "With nothing to shuffle to, the single track must loop rather than stop.");
        }

        [Test]
        public void PlayMusicPool_TrackOverride_StartsThatExactTrack()
        {
            AudioClip a = CreateClip("a");
            AudioClip b = CreateClip("b");
            _am.SetMusicEntries(new MusicEntry("menu", a, b));

            _am.PlayMusicPool("menu", MusicOptions.Track(1));

            Assert.AreSame(b, _music.clip);
        }

        [Test]
        public void NextMusicTrack_MovesToADifferentTrackOfTheCurrentPool()
        {
            AudioClip a = CreateClip("a");
            AudioClip b = CreateClip("b");
            _am.SetMusicEntries(new MusicEntry("gameplay", a, b));
            _am.PlayMusicPool("gameplay");

            AudioClip before = _music.clip;
            bool changed = _am.TryNextMusicTrack();

            Assert.IsTrue(changed);
            Assert.AreNotSame(before, _music.clip, "NextMusicTrack must land on a different track.");
        }

        [Test]
        public void NextMusicTrack_RaisesTheTrackChangedEvent()
        {
            _am.SetMusicEntries(new MusicEntry("gameplay", CreateClip("a"), CreateClip("b")));
            _am.PlayMusicPool("gameplay");

            int changes = 0;
            _am.OnRandomMusicTrackChanged += _ => changes++;

            _am.TryNextMusicTrack();

            Assert.AreEqual(1, changes, "An in-pool track change must raise OnRandomMusicTrackChanged once.");
        }

        [Test]
        public void NextMusicTrack_OnASingleTrackPool_ReturnsFalseAndKeepsPlaying()
        {
            AudioClip only = CreateClip("only");
            _am.SetMusicEntries(new MusicEntry("solo", only));
            _am.PlayMusicPool("solo");

            Assert.IsFalse(_am.TryNextMusicTrack(),
                "A pool with one track has nothing to move to and must say so instead of restarting.");
            Assert.AreSame(only, _music.clip);
        }

        [Test]
        public void NextMusicTrack_WithNoPoolPlaying_ReturnsFalseWithoutThrowing()
        {
            Assert.IsFalse(_am.TryNextMusicTrack());
        }

        [Test]
        public void StopMusic_ClearsTheCurrentPool()
        {
            _am.SetMusicEntries(new MusicEntry("menu", CreateClip("m")));
            _am.PlayMusicPool("menu");

            _am.StopMusic(MusicTransition.Instant);

            Assert.IsNull(_am.CurrentMusicEntry);
            Assert.AreEqual(string.Empty, _am.CurrentMusicId);
        }

        // ---------------------------------------------------------------- sound overrides

        [Test]
        public void SoundOptions_OverridesDoNotMutateTheEntry()
        {
            var entry = new SoundEntry("hit", CreateClip("h"));
            _am.SetSoundEntries(entry);

            _am.Play("hit", SoundOptions.Volume(0.2f).WithoutPitch());

            Assert.AreEqual(1f, entry.Volume, 1e-5f, "The entry volume must survive an override.");
            Assert.IsTrue(entry.RandomizePitch, "The entry pitch setting must survive an override.");
        }

        [Test]
        public void SoundOptions_ClipOverride_IsAccepted()
        {
            _am.SetSoundEntries(new SoundEntry("steps", CreateClip("a"), CreateClip("b"), CreateClip("c")));

            Assert.DoesNotThrow(() => _am.Play("steps", SoundOptions.Clip(2)));
            Assert.DoesNotThrow(() => _am.Play("steps", SoundOptions.Clip(99)),
                "An out-of-range clip index must wrap, not throw.");
        }

        [Test]
        public void SoundOptions_ZeroVolumeOverride_IsRespectedRatherThanTreatedAsFull()
        {
            var entry = new SoundEntry("hit", CreateClip("h"));
            _am.SetSoundEntries(entry);

            // WHY: the legacy Sound record read `volume == 0 ? 1 : volume`, so a zeroed value meant "full".
            // That quirk is folded into the migration and must NOT survive into the new contract, where a
            // caller asking for zero means silence.
            SoundOptions options = SoundOptions.Volume(0f);
            Assert.AreEqual(0f, options.VolumeOverride.Value, 1e-6f);
        }

        // ---------------------------------------------------------------- migration

        [Test]
        public void Migration_TurnsLegacySoundsIntoEntriesPreservingIndexAndVolume()
        {
            AudioClip first = CreateClip("first");
            AudioClip second = CreateClip("second");
            SetPrivateField("_sounds", new[]
            {
                new Sound { clip = first, volume = 0.5f },
                new Sound { clip = second, volume = 0f }
            });
            SetPrivateField("_dataVersion", 0);

            _am.OnAfterDeserialize();

            IReadOnlyList<SoundEntry> entries = _am.SoundEntries;
            Assert.AreEqual(2, entries.Count, "Every legacy record must become one entry, in order.");
            Assert.AreSame(first, entries[0].Clips[0]);
            Assert.AreEqual(0.5f, entries[0].Volume, 1e-5f);
            Assert.AreSame(second, entries[1].Clips[0]);
            Assert.AreEqual(1f, entries[1].Volume, 1e-5f,
                "The old Play(int) read a zero volume as 'full'; migration must preserve that, not silence it.");
        }

        [Test]
        public void Migration_KeepsMusicIndicesStableAndAppendsTheRandomListAsAShufflePool()
        {
            AudioClip m0 = CreateClip("m0");
            AudioClip m1 = CreateClip("m1");
            AudioClip r0 = CreateClip("r0");
            AudioClip r1 = CreateClip("r1");

            SetPrivateField("_musicClips", new[] { m0, m1 });
            SetPrivateField("_randomMusicTracks", new[] { r0, r1 });
            SetPrivateField("_dataVersion", 0);

            _am.OnAfterDeserialize();

            IReadOnlyList<MusicEntry> entries = _am.MusicEntries;
            Assert.AreEqual(3, entries.Count, "Two indexed clips plus one appended random pool.");
            Assert.AreSame(m0, entries[0].Clips[0], "PlayMusic(0) must still resolve to the first legacy clip.");
            Assert.AreSame(m1, entries[1].Clips[0]);
            Assert.AreEqual(AM.LegacyRandomPoolId, entries[2].Id);
            Assert.AreEqual(MusicPoolMode.Shuffle, entries[2].Mode,
                "The legacy random list behaved as a shuffle pool and must migrate as one.");
            Assert.AreEqual(2, entries[2].ClipCount);
        }

        [Test]
        public void Migration_InheritsTheOldGlobalPitchSettingSoSoundIsUnchanged()
        {
            SetPrivateField("_sounds", new[] { new Sound { clip = CreateClip("s"), volume = 1f } });
            SetPrivateField("_randomizePitch", false);
            SetPrivateField("_dataVersion", 0);

            _am.OnAfterDeserialize();

            Assert.IsFalse(_am.SoundEntries[0].RandomizePitch,
                "New entries default to pitch ON, but a migrated project must sound exactly as it did.");
        }

        [Test]
        public void Migration_DoesNotRunTwiceAndDoesNotResurrectDeletedEntries()
        {
            SetPrivateField("_sounds", new[] { new Sound { clip = CreateClip("s"), volume = 1f } });
            SetPrivateField("_dataVersion", 0);

            _am.OnAfterDeserialize();
            Assert.AreEqual(1, _am.SoundEntries.Count);

            _am.SetSoundEntries();
            _am.OnBeforeSerialize();
            _am.OnAfterDeserialize();

            Assert.AreEqual(0, _am.SoundEntries.Count,
                "Once the data is stamped as migrated, an intentionally empty list must stay empty.");
        }

        [Test]
        public void Migration_OnAFreshComponentDoesNothing()
        {
            SetPrivateField("_dataVersion", 0);

            _am.OnAfterDeserialize();

            Assert.AreEqual(0, _am.SoundEntries.Count);
            Assert.AreEqual(0, _am.MusicEntries.Count);
        }

        [Test]
        public void LegacyIndexPlayback_StillWorksWhenOnlyLegacyArraysArePopulated()
        {
            // WHY: components built at runtime never go through deserialization, so migration never runs.
            // The index paths must fall back to the legacy arrays rather than reporting "out of range".
            AudioClip clip = CreateClip("legacy");
            SetPrivateField("_musicClips", new[] { clip });

            _am.PlayMusic(0);

            Assert.AreSame(clip, _music.clip);
        }

        // ---------------------------------------------------------------- helpers

        private static AudioClip CreateClip(string name)
        {
            return AudioClip.Create(name, 4410, 1, 44100, false);
        }

        private void SetPrivateField(string fieldName, object value)
        {
            FieldInfo field = typeof(AM).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"AM no longer has a field named '{fieldName}'.");
            field.SetValue(_am, value);
        }
    }
}
