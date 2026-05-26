using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem.SlidingMerge
{
    [Serializable]
    public sealed class SlidingMergeStep
    {
        public Vector3Int From;
        public Vector3Int To;
        public int Value;
        public bool IsMerge;
        public int ResultValue;
    }

    [Serializable]
    public sealed class SlidingMergeResult
    {
        public bool Changed;
        public int MoveCount;
        public int MergeCount;
        public int ScoreDelta;
        public List<SlidingMergeStep> Steps = new();
    }
}
