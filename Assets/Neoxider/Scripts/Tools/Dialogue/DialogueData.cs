using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    /// Данные одного диалога, содержащего несколько монологов.
    /// </summary>
    [Serializable]
    public class Dialogue
    {
        public UnityEvent<int> OnChangeDialog;
        public Monolog[] monologues;
    }

    /// <summary>
    /// Данные монолога одного персонажа.
    /// </summary>
    [Serializable]
    public class Monolog
    {
        public UnityEvent<int> OnChangeMonolog;
        public string characterName;
        public Sentence[] sentences;
    }

    /// <summary>
    /// Данные одного предложения в диалоге.
    /// </summary>
    [Serializable]
    public class Sentence
    {
        public UnityEvent OnChangeSentence;
        public Sprite sprite;
        [TextArea(3, 7)] public string sentence;
    }
}





