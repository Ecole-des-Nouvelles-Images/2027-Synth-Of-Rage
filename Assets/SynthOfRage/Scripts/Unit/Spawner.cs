using System.Collections.Generic;
using SynthOfRage.Scripts.Core;
using UnityEngine;

namespace SynthOfRage.Scripts.Unit
{
    public class Spawner : MonoBehaviour
    {

        public List<GameObject> Enemy;
        public float SpawnRate;
        public bool _SquadEnable;
        
        private float _currentTime = 0;
        private List<GameObject> _livingEnemies = new List<GameObject>();
        private int _enemySpawnIndex = 0;

        private Transform _dynamicContainer;

        private void Awake()
        {
            _dynamicContainer = GameObject.FindGameObjectWithTag("DynamicInstances")?.transform;
            if (!_dynamicContainer)
                UnityEngine.Debug.Log("[Spawner] An object with the DynamicInstances tag is missing in the scene !");
            
            
            transform.gameObject.SetActive(false);
            if (Enemy == null || Enemy.Count == 0)
            {
                UnityEngine.Debug.LogError("No enemy to spawn !!!");
            }
        }

        private void Update()
        {
            if (_SquadEnable && _enemySpawnIndex < Enemy.Count)
            {
                if (_currentTime > 0)
                {
                    _currentTime -= Time.deltaTime;
                
                }
                else
                {
                    SpawnEnemy();
                }
            }
            UpdateLivingEnemies();
        }

        private void SpawnEnemy()
        {
            GameObject enemy = Instantiate(Enemy[_enemySpawnIndex], transform.position, transform.rotation);
            enemy.transform.parent = _dynamicContainer;
            _livingEnemies.Add(enemy);
            _currentTime = SpawnRate;
            _enemySpawnIndex++;
        }

        private void UpdateLivingEnemies()
        {
            for (int i = 0; i < _livingEnemies.Count; i++)
            {
                if (!_livingEnemies[i] || !_livingEnemies[i].activeInHierarchy)
                {
                    _livingEnemies.Remove(_livingEnemies[i]);
                }
            }

            if (_livingEnemies.Count == 0)
            {
                GameManager.Instance.OnArenaExit.Invoke();
            }
        }
    }
}