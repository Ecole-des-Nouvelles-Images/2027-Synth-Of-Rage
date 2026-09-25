using System;
using SynthOfRage.Scripts.Core;
using SynthOfRage.Scripts.Helper;
using UnityEngine;

namespace _Dev.Christopher.Scripts
{ 
    public class SpawnerManager : SingletonMonoBehaviour<SpawnerManager> 
    {
        public GameObject[] Spawners;
        
        private int _waveClearCounter = 0;

        private void OnEnable()
        {
            GameManager.Instance.OnArenaEnter += SpawnerActivator;
            GameManager.Instance.OnArenaExit += SpawnerDeactivator;
        }

        private void OnDisable()
        {
            GameManager.Instance.OnArenaEnter -= SpawnerActivator;
            GameManager.Instance.OnArenaExit -= SpawnerDeactivator;
        }

        private void SpawnerActivator()
        {
            if (Spawners == null || Spawners.Length == 0 || _waveClearCounter >= Spawners.Length)
            {
                return;
            }
            Spawners[_waveClearCounter].SetActive(true);
            Spawners[_waveClearCounter].GetComponent<Spawner>()._SquadEnable = true;
        }
        private void SpawnerDeactivator()
        {
            if (Spawners == null || Spawners.Length == 0 || _waveClearCounter >= Spawners.Length)
            {
                return;
            }
            Spawners[_waveClearCounter].GetComponent<Spawner>()._SquadEnable = false;
            Spawners[_waveClearCounter].SetActive(false);
            _waveClearCounter++;
        }
    }
}
