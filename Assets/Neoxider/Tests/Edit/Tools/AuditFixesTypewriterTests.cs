using System;
using System.Reflection;
using System.Threading;
using Neo.Tools;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the TypewriterEffect run guard: a run that was cancelled by a restart must never
    ///     dispose the CancellationTokenSource installed by the run that replaced it.
    /// </summary>
    [TestFixture]
    public class AuditFixesTypewriterTests
    {
        // WHY: PlayAsync returns UniTask and the test assembly does not reference UniTask, so runs are
        // started through reflection (same approach as CardSystemTests).
        private static void StartPlay(TypewriterEffect effect, string text, Action<string> onTextChanged)
        {
            MethodInfo play = typeof(TypewriterEffect).GetMethod("PlayAsync");
            Assert.That(play, Is.Not.Null, "TypewriterEffect.PlayAsync must exist");
            play.Invoke(effect, new object[] { text, onTextChanged, CancellationToken.None });
        }

        [Test]
        public void PlayAsync_RestartedWhileTyping_KeepsTheNewRunAlive()
        {
            // WHY: 0.1 chars/second => 10 s per character, so a run suspends on its first delay and
            // stays "typing" for the whole test.
            var effect = new TypewriterEffect(0.1f);
            effect.UsePunctuationPauses = false;

            bool restarted = false;

            void OnFirstRunText(string _)
            {
                if (restarted)
                {
                    return;
                }

                restarted = true;
                StartPlay(effect, "second", __ => { });
            }

            StartPlay(effect, "first", OnFirstRunText);

            Assert.That(restarted, Is.True, "the first run must reach its text callback");
            Assert.That(effect.FullText, Is.EqualTo("second"), "the second run owns the effect state");
            Assert.That(effect.IsTyping, Is.True,
                "the cancelled first run must not dispose the CancellationTokenSource of the second run");

            effect.Stop();

            Assert.That(effect.IsTyping, Is.False, "Stop must still control the running effect");
        }

        [Test]
        public void PlayAsync_CompletedRun_ReleasesItsOwnCancellationSource()
        {
            // WHY: 5000 chars/second rounds the per-character delay to 0 ms, so the run never suspends
            // and finishes inside the call.
            var effect = new TypewriterEffect(5000f);
            effect.UsePunctuationPauses = false;

            StartPlay(effect, "done", _ => { });

            Assert.That(effect.IsTyping, Is.False, "a finished run must release its own source");
            Assert.That(effect.CurrentText, Is.EqualTo("done"));
        }
    }
}
