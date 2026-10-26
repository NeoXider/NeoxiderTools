using System;
using UnityEngine;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoQuestConditionProbe : MonoBehaviour
    {
        public Action OnRead { get; set; }
        public bool Result { get; set; } = true;
        public int ReadCount { get; private set; }

        public bool Value
        {
            get
            {
                ReadCount++;
                Action callback = OnRead;
                OnRead = null;
                callback?.Invoke();
                return Result;
            }
        }
    }
}
