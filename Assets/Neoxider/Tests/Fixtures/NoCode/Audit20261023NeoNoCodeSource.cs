using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Reactive;

namespace Neo.Editor.Tests.Edit
{
    public sealed class Audit20261023NeoNoCodeSource : MonoBehaviour
    {
        public float Score;
        public ReactivePropertyFloat ReactiveScore = new(0.25f);
        public ReactivePropertyInt ReactiveLevel = new(2);
        public ReactivePropertyBool ReactiveFlag = new(false);

        public float this[int index] => Score;
        public float this[string key] => Score;
    }
}
