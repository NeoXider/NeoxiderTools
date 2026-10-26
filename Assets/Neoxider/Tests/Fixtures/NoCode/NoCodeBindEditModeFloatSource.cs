using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Reactive;

namespace Neo.Editor.Tests.Edit
{
    public sealed class NoCodeBindEditModeFloatSource : MonoBehaviour
    {
        public float Score = 40f;
        public ReactivePropertyFloat ReactiveScore = new(0.25f);
        public ReactivePropertyInt ReactiveLevel = new(1);
        public float ScoreProperty => Score;

        public float GetScore()
        {
            return Score;
        }
    }
}
