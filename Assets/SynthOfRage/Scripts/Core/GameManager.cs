using System;
using SynthOfRage.Scripts.Helper;
using UnityEngine;

namespace SynthOfRage.Scripts.Core
{
    public class GameManager : SingletonMonoBehaviour<GameManager>
    {
        public Action OnArenaEnter;
        public Action OnArenaExit;

        protected override void Awake()
        {
            base.Awake();
            
            OnArenaEnter += () => UnityEngine.Debug.Log("[GameManager] Entering combat zone");
            OnArenaExit += () => UnityEngine.Debug.Log("[GameManager] Combat ended");
        }

        protected override void OnDestroy()
        {
            OnArenaEnter = null;
            OnArenaExit = null;
            
            base.OnDestroy();
        }
    }
}
