using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Edit
{
    public sealed class DialogueControllerEditModeTests
    {
        [Test]
        public void StartDialogue_SetsIndicesAndStartedFlag()
        {
            var go = new GameObject("Dlg");
            try
            {
                var dc = go.AddComponent<DialogueController>();
                dc.useTypewriterEffect = false;
                dc.dialogues = new[]
                {
                    new Dialogue
                    {
                        monologues = new[]
                        {
                            new Monolog
                            {
                                characterName = "X",
                                sentences = new[]
                                {
                                    new Sentence { sentence = "Hello" }
                                }
                            }
                        }
                    }
                };

                dc.StartDialogue(0, 0, 0);

                Assert.That(dc.DialogueStarted, Is.True);
                Assert.That(dc.CurrentDialogueId, Is.EqualTo(0));
                Assert.That(dc.CurrentMonologId, Is.EqualTo(0));
                Assert.That(dc.CurrentSentenceId, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
