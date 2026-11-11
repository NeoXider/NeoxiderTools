using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Data for one dialogue containing multiple monologs.
    /// </summary>
    [Serializable]
    public class Dialogue
    {
        [Header("Events")] public UnityEvent<int> OnChangeDialog;

        [Header("Content")] public Monolog[] monologues;
    }

    /// <summary>
    ///     Data for one character monolog.
    /// </summary>
    [Serializable]
    public class Monolog
    {
        [Header("Events")] public UnityEvent<int> OnChangeMonolog;

        [Header("Content")] public string characterName;

        public Sentence[] sentences;
    }

    /// <summary>
    ///     Data for a single sentence in the dialogue.
    /// </summary>
    [Serializable]
    public class Sentence
    {
        [Header("Events")] public UnityEvent OnChangeSentence;

        [Header("Content")] public Sprite sprite;

        [TextArea(3, 7)] public string sentence;
    }
}
