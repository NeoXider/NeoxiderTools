using UnityEngine;

namespace Neo.Save.Examples
{
    public class PlayerData : MonoBehaviour, ISaveableComponent
    {
        [SaveField(nameof(playerScore))] [SerializeField]
        private int playerScore;

        [SaveField(nameof(playerPosition))] [SerializeField]
        private Vector3 playerPosition;

        [SaveField("Money")] [SerializeField] private float _money;
        public bool IsLoad { get; private set; }

        // The Start method is no longer needed for setting position
        public void OnDataLoaded()
        {
            transform.position = playerPosition;
            Debug.Log($"PlayerData for {gameObject.name} loaded. Position set to {playerPosition}");
            IsLoad = true;
        }
    }
}