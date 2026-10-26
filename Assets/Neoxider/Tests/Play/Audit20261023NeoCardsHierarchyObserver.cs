using System;
using UnityEngine;

namespace Neo.Tests.Play
{
    /// <summary>One-shot fixture callbacks delivered only by Unity's native hierarchy messages.</summary>
    public sealed class Audit20261023NeoCardsHierarchyObserver : MonoBehaviour
    {
        private Action _parentChanged;
        private Action _childrenChanged;

        internal void ArmParentChanged(Action callback)
        {
            _parentChanged = callback;
        }

        internal void ArmChildrenChanged(Action callback)
        {
            _childrenChanged = callback;
        }

        internal void Disarm()
        {
            _parentChanged = null;
            _childrenChanged = null;
        }

        private void OnTransformParentChanged()
        {
            Action callback = _parentChanged;
            _parentChanged = null;
            callback?.Invoke();
        }

        private void OnTransformChildrenChanged()
        {
            Action callback = _childrenChanged;
            _childrenChanged = null;
            callback?.Invoke();
        }
    }
}
