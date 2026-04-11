using System.Collections;
using Neo.Core.Level;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg.DemoUtils
{
    /// <summary>
    /// Spawns waves of NPCs with increasing difficulty/level. 
    /// Dispatches XP to the targeted player logic upon kills.
    /// </summary>
    [AddComponentMenu("Neoxider/RPG/Demo/RpgWaveSpawner")]
    public sealed class RpgWaveSpawner : MonoBehaviour
    {
        [Header("References")]
        public GameObject NpcPrefab;
        public LevelComponent PlayerLevelContext;
        public Transform[] SpawnPoints;

        [Header("Wave Config")]
        public int BaseWaveCount = 3;
        public int CountPerWave = 1;
        public float SpawnDelay = 1.5f;
        public int XpPerLevel = 25;

        [Header("Events")]
        public UnityEvent<string> OnWaveStarted = new();
        public UnityEvent<string> OnWaveCleared = new();

        private int _currentWave = 1;
        private int _aliveNpcs = 0;

        private void Start()
        {
            StartNextWave();
        }

        private void StartNextWave()
        {
            OnWaveStarted?.Invoke($"WAVE {_currentWave}");
            StartCoroutine(SpawnWaveRoutine());
        }

        private IEnumerator SpawnWaveRoutine()
        {
            int toSpawn = BaseWaveCount + (_currentWave * CountPerWave);
            for (int i = 0; i < toSpawn; i++)
            {
                if (SpawnPoints == null || SpawnPoints.Length == 0) yield break;

                Transform point = SpawnPoints[Random.Range(0, SpawnPoints.Length)];
                GameObject npc = Instantiate(NpcPrefab, point.position, point.rotation);
                
                if (npc.TryGetComponent(out RpgCombatant combatant))
                {
                    combatant.SetLevel(_currentWave);
                    combatant.OnDeath.AddListener(() => OnNpcDied(npc, _currentWave));
                }
                else if (npc.TryGetComponent(out RpgStatsManager statsManager))
                {
                    statsManager.LevelState.Value = _currentWave;
                    statsManager.OnDeath.AddListener(() => OnNpcDied(npc, _currentWave));
                }

                _aliveNpcs++;
                yield return new WaitForSeconds(SpawnDelay);
            }
        }

        private void OnNpcDied(GameObject npc, int level)
        {
            _aliveNpcs--;
            
            if (PlayerLevelContext != null)
            {
                PlayerLevelContext.AddXp(XpPerLevel * level);
            }

            if (_aliveNpcs <= 0)
            {
                OnWaveCleared?.Invoke($"WAVE {_currentWave} CLEARED!");
                _currentWave++;
                Invoke(nameof(StartNextWave), 3f);
            }

            Destroy(npc, 0.1f);
        }
    }
}
