
using System;
using SynthOfRage.Scripts.Gameplay.Core;
using SynthOfRage.Scripts.Utilities;
using UnityEngine;

namespace SynthOfRage.Scripts.Gameplay
{
    public class SpawnerManager : SingletonMonoBehaviour<SpawnerManager>
    {
        [Serializable]
        public class SpawnerGroup
        {
            public GameObject[] Spawners;
        }

        [Header("Combat Spawners")]
        [SerializeField] private SpawnerGroup[] _combatGroups;

        private int _waveClearCounter;
        private bool _combatActive;
        private bool _combatCompleted;
        private GameManager _gameManager;

        private void OnEnable()
        {
            _gameManager = GameManager.Instance;

            if (!_gameManager) return;

            _gameManager.OnArenaEnter += SpawnerActivator;
            _gameManager.OnArenaExit += SpawnerDeactivator;
        }

        private void OnDisable()
        {
            if (!_gameManager) return;

            _gameManager.OnArenaEnter -= SpawnerActivator;
            _gameManager.OnArenaExit -= SpawnerDeactivator;
        }

        private void Update()
        {
            if (!_combatActive || _combatCompleted) return;
            if (!IsCombatCompleted()) return;

            _combatCompleted = true;

            if (_gameManager)
                _gameManager.OnArenaExit?.Invoke();
        }

        private void SpawnerActivator()
        {
            if (_combatGroups == null ||
                _waveClearCounter >= _combatGroups.Length)
                return;

            _combatActive = false;
            _combatCompleted = false;

            SpawnerGroup group = _combatGroups[_waveClearCounter];

            if (group?.Spawners == null || group.Spawners.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[SpawnerManager] Empty combat group.");
                return;
            }

            foreach (GameObject obj in group.Spawners)
            {
                if (!obj) continue;
                if (!obj.TryGetComponent(out Spawner spawner)) continue;

                spawner.ResetSpawner();
                obj.SetActive(true);
                spawner._SquadEnable = true;
            }

            _combatActive = true;
        }

        private void SpawnerDeactivator()
        {
            if (!_combatActive && !_combatCompleted) return;

            _combatActive = false;
            _combatCompleted = true;

            if (_combatGroups != null &&
                _waveClearCounter < _combatGroups.Length)
            {
                SpawnerGroup group = _combatGroups[_waveClearCounter];

                if (group?.Spawners != null)
                {
                    foreach (GameObject obj in group.Spawners)
                    {
                        if (!obj) continue;

                        if (obj.TryGetComponent(out Spawner spawner))
                            spawner._SquadEnable = false;

                        obj.SetActive(false);
                    }
                }
            }

            _waveClearCounter++;
        }

        private bool IsCombatCompleted()
        {
            if (_combatGroups == null ||
                _waveClearCounter >= _combatGroups.Length)
                return false;

            SpawnerGroup group = _combatGroups[_waveClearCounter];

            if (group?.Spawners == null || group.Spawners.Length == 0)
                return false;

            bool hasSpawner = false;

            foreach (GameObject obj in group.Spawners)
            {
                if (!obj || !obj.TryGetComponent(out Spawner spawner))
                    return false;

                hasSpawner = true;

                if (!spawner.IsCompleted)
                    return false;
            }

            return hasSpawner;
        }
    }
}
