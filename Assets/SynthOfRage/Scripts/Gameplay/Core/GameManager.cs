using System;
using Unity.Scripting.LifecycleManagement;

namespace SynthOfRage.Scripts.Gameplay.Core
{
    public partial class GameManager : Utilities.SingletonMonoBehaviour<GameManager>
    {
        [AutoStaticsCleanup]
        public static Action OnPause;

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
