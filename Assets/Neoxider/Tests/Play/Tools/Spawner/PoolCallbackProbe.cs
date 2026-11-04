using Neo.Tools;
using UnityEngine;

namespace Neo.Tests.Play
{
    /// <summary>Counts <see cref="IPoolable" /> lifecycle callbacks without allocating, for the pool callback tests.</summary>
    /// <remarks>
    ///     WHY: Unity refuses to attach a MonoBehaviour declared in an Editor-only assembly ("it is an editor script"),
    ///     so this probe lives in the runtime Play test assembly.
    /// </remarks>
    public sealed class PoolCallbackProbe : MonoBehaviour, IPoolable
    {
        public int Created { get; private set; }
        public int Got { get; private set; }
        public int Released { get; private set; }

        public void OnPoolCreate()
        {
            Created++;
        }

        public void OnPoolGet()
        {
            Got++;
        }

        public void OnPoolRelease()
        {
            Released++;
        }
    }
}
