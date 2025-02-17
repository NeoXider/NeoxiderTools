using UnityEngine;

namespace Neo.Bonus
{
    [CreateAssetMenu(fileName = "SpritesDafualt", menuName = "Neoxider/Slot/Sprites")]
    public class SpritesData : ScriptableObject
    {
        [SerializeField] private Sprite[] _sprites;
        public Sprite[] sprites => _sprites;
    }
}