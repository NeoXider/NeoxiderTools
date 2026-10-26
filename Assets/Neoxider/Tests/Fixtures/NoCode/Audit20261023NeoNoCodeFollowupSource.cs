using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Reactive;

namespace Neo.Editor.Tests.Edit
{
    public sealed class Audit20261023NeoNoCodeFollowupSource : MonoBehaviour
    {
        public float Score = 0.25f;
        public ReactivePropertyFloat ReactiveScore = new(0.25f);
        public int ReadCount { get; private set; }

        public float CountedScore
        {
            get
            {
                ReadCount++;
                return Score;
            }
        }
    }
}
