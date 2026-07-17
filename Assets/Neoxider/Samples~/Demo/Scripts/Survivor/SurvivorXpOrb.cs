using UnityEngine;

namespace Neo.Samples.Survivor
{
    /// <summary>
    ///     A pooled XP pickup. Idles until the player is within magnet range, then homes in and is
    ///     collected on contact. Purely presentational; progression lives in <see cref="SurvivorGame" />.
    /// </summary>
    public sealed class SurvivorXpOrb : MonoBehaviour
    {
        private SpriteRenderer _body;
        private SurvivorGame _game;
        private int _value;
        private bool _active;

        private void Awake()
        {
            _body = GetComponentInChildren<SpriteRenderer>();
        }

        public void Spawned(SurvivorGame game, int value)
        {
            _game = game;
            _value = value;
            _active = true;
        }

        private void Update()
        {
            if (!_active || _game == null)
            {
                return;
            }

            SurvivorPlayerController player = _game.Player;
            if (player == null || !player.IsAlive)
            {
                return;
            }

            Vector3 toPlayer = player.transform.position - transform.position;
            float distance = toPlayer.magnitude;

            float pickup = _game.Config.PlayerRadius + 0.25f;
            if (distance <= pickup)
            {
                Collect();
                return;
            }

            if (distance <= _game.Config.PickupRadius)
            {
                float pull = Mathf.Lerp(14f, 4f, distance / _game.Config.PickupRadius);
                transform.position += toPlayer / distance * pull * Time.deltaTime;
            }
        }

        private void Collect()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            _game.HandleOrbCollected(this, _value);
        }
    }
}
