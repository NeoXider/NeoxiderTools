using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Данные одного диалога, содержащего несколько монологов.
    /// </summary>
    [Serializable]
    public class Dialogue
    {
        [Header("Events")]
        public UnityEvent<int> OnChangeDialog;
        [Header("Content")]
        public Monolog[] monologues;
    }

    /// <summary>
    ///     Данные монолога одного персонажа.
    /// </summary>
    [Serializable]
    public class Monolog
    {
        [Header("Events")]
        public UnityEvent<int> OnChangeMonolog;
        [Header("Content")]
        public string characterName;
        public Sentence[] sentences;
    }

    /// <summary>
    ///     Данные одного предложения в диалоге.
    /// </summary>
    [Serializable]
    public class Sentence
    {
        [Header("Events")]
        public UnityEvent OnChangeSentence;
        [Header("Content")]
        public Sprite sprite;
        [TextArea(3, 7)] public string sentence;
    }
}