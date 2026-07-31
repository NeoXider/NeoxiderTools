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

        // WHY: position is applied here (from loaded save data) rather than in Start
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
