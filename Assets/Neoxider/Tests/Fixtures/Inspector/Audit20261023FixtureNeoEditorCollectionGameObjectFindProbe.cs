using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023FixtureNeoEditorCollectionGameObjectFindProbe : MonoBehaviour
    {
        [global::Neo.FindInScene, RequireInterface(typeof(Audit20261023FixtureEditorFirstRequirement)), RequireInterface(typeof(Audit20261023FixtureEditorSecondRequirement))]
        public GameObject Candidate;
    }
}
