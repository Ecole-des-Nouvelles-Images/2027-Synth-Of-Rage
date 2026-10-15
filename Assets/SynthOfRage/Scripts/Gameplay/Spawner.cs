
using System.Collections.Generic;
using UnityEngine;

namespace SynthOfRage.Scripts.Gameplay
{
    public class Spawner : MonoBehaviour
    {
        public List<GameObject> Enemy;
        public float SpawnRate;
        public bool _SquadEnable;

        private float _currentTime;
        private readonly List<GameObject> _livingEnemies = new();
        private int _enemySpawnIndex;
        private Transform _dynamicContainer;

        public bool IsCompleted =>
            _SquadEnable &&
            _enemySpawnIndex >= (Enemy?.Count ?? 0) &&
            _livingEnemies.Count == 0;

        private void Awake()
        {
            _dynamicContainer = GameObject.FindGameObjectWithTag("DynamicInstances")?.transform;

            if (!_dynamicContainer)
                UnityEngine.Debug.LogWarning("[Spawner] DynamicInstances container missing.");

            if (Enemy == null || Enemy.Count == 0)
                UnityEngine.Debug.LogWarning($"[Spawner] No enemies configured on {name}.");

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_SquadEnable) return;

            if (_enemySpawnIndex < (Enemy?.Count ?? 0))
            {
                _currentTime -= Time.deltaTime;

                if (_currentTime <= 0f)
                    SpawnEnemy();
            }

            UpdateLivingEnemies();
        }

        private void SpawnEnemy()
        {
            GameObject prefab = Enemy[_enemySpawnIndex++];

            if (prefab)
            {
                GameObject enemy = Instantiate(
                    prefab,
                    transform.position,
                    transform.rotation,
                    _dynamicContainer
                );

                _livingEnemies.Add(enemy);
            }

            _currentTime = SpawnRate;
        }

        private void UpdateLivingEnemies()
        {
            _livingEnemies.RemoveAll(enemy =>
                !enemy || !enemy.activeInHierarchy);
        }

        public void ResetSpawner()
        {
            _enemySpawnIndex = 0;
            _currentTime = 0f;
            _livingEnemies.Clear();
            _SquadEnable = false;
        }
    }
}
