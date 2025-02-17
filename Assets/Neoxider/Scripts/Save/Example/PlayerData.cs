using Neo.Save;
using UnityEngine;

namespace Neo.Save.Examples
{
    public class PlayerData : MonoBehaviour, ISaveableComponent
    {
        [SaveField(nameof(playerScore))]
        [SerializeField]
        private int playerScore;

        [SaveField(nameof(playerPosition))]
        [SerializeField]
        private Vector3 playerPosition;

        private void Start()
        {
            playerPosition = transform.position;
        }
    }

    public interface ISaveableComponent { }
}