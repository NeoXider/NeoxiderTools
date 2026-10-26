using System;
using Neo.Tools;
using UnityEngine;

namespace Neo.Tests.Play
{
    public class Audit20261023NeoToolsSpawnerPoolProbe : PoolableBehaviour
    {
        public static int CreatedCount;
        public static Action<GameObject> Created;
        public static Action<GameObject> Got;
        public Action<GameObject> Released;

        public override void OnPoolCreate()
        {
            CreatedCount++;
            Created?.Invoke(gameObject);
        }

        public override void OnPoolGet()
        {
            Got?.Invoke(gameObject);
        }

        public override void OnPoolRelease()
        {
            Released?.Invoke(gameObject);
        }
    }
}
