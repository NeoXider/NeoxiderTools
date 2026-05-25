using UnityEngine;

namespace Neo.Save.Examples
{
    [NeoDoc("Save/PlayerData.md")]
    [CreateFromMenu("Neoxider/Save/PlayerData")]
    [AddComponentMenu("Neoxider/" + "Save/" + nameof(PlayerData))]
    public class PlayerData : MonoBehaviour, ISaveableComponent
    {
        [Header("Save Data")] [SaveField(nameof(playerScore))] [SerializeField]
        private int playerScore;

        [SaveField(nameof(playerPosition))] [SerializeField]
        private Vector3 playerPosition;

        [SaveField("Money")] [SerializeField] private float _money;
        [SerializeField] private bool _debugLog;
        public bool IsLoad { get; private set; }

        // The Start method is no longer needed for setting position
        public void OnDataLoaded()
        {
            transform.position = playerPosition;
            if (_debugLog)
            {
                SaveProvider.Log($"PlayerData for {gameObject.name} loaded. Position set to {playerPosition}", this);
            }
            IsLoad = true;
        }
    }
}
