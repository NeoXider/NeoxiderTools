using System;
using UnityEngine;

namespace Neo.GridSystem
{
    /// <summary>
    ///     Scene-facing helper that assembles a grid object from small optional GridSystem modules.
    /// </summary>
    [NeoDoc("GridSystem/GridGameBuilder.md")]
    [RequireComponent(typeof(Grid))]
    [RequireComponent(typeof(FieldGenerator))]
    [CreateFromMenu("Neoxider/GridSystem/GridGameBuilder")]
    [AddComponentMenu("Neoxider/GridSystem/GridGameBuilder")]
    public class GridGameBuilder : MonoBehaviour
    {
        [SerializeField] private GridGameFeatures _features = GridGameFeatures.DebugDrawer;
        [SerializeField] private bool _generateFieldOnAwake = true;

        public GridGameFeatures Features
        {
            get => _features;
            set => _features = value;
        }

        public FieldGenerator Generator { get; private set; }

        private void Awake()
        {
            EnsureConfigured();
            if (_generateFieldOnAwake && Generator != null)
            {
                Generator.GenerateField();
            }
        }

        [Button("Ensure Grid Components")]
        public void EnsureConfigured()
        {
            Generator = GetComponent<FieldGenerator>();
            if (Generator == null)
            {
                Generator = gameObject.AddComponent<FieldGenerator>();
            }

            EnsureFeature<FieldDebugDrawer>(GridGameFeatures.DebugDrawer);
            EnsureFeature<FieldSpawner>(GridGameFeatures.FieldSpawner);
            EnsureFeature<FieldObjectSpawner>(GridGameFeatures.FieldObjectSpawner);
            EnsureFeature<Match3.Match3BoardService>(GridGameFeatures.Match3);
            EnsureFeature<TicTacToe.TicTacToeBoardService>(GridGameFeatures.TicTacToe);
            EnsureFeature<SlidingMerge.SlidingMergeBoardService>(GridGameFeatures.SlidingMerge);
        }

        private void EnsureFeature<T>(GridGameFeatures feature) where T : Component
        {
            if ((_features & feature) == 0 || GetComponent<T>() != null)
            {
                return;
            }

            gameObject.AddComponent<T>();
        }
    }

    [Flags]
    public enum GridGameFeatures
    {
        None = 0,
        DebugDrawer = 1 << 0,
        FieldSpawner = 1 << 1,
        FieldObjectSpawner = 1 << 2,
        Match3 = 1 << 3,
        TicTacToe = 1 << 4,
        SlidingMerge = 1 << 5
    }
}
