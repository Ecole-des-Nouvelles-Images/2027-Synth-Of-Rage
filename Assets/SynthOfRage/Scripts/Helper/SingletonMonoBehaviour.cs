using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace SynthOfRage.Scripts.Helper
{
    public partial class SingletonMonoBehaviour<T> : MonoBehaviour where T : Component
    {
        [AutoStaticsCleanup]
        private static T _instance;

        [AutoStaticsCleanup]
        private static bool _autoDiscard = true; // Each closed type will get its own static field

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    T[] objs = FindObjectsByType<T>(FindObjectsInactive.Include);

                    if (objs.Length > 0)
                        _instance = objs[0];

                    if (objs.Length > 1)
                        throw new Exception($"[{typeof(T).Name}] There is more than one instance in the scene !");
                }

                if (!_instance)
                {
                    throw new Exception($"[{typeof(T).Name}] No singleton instance found in the scene !");
                }

                return _instance;
            }
        }
        
        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
            }
            else if (_instance != this as T)
            {
                if (_autoDiscard)
                {
                    UnityEngine.Debug.LogWarning($"⚠️ [{typeof(T).Name}] Singleton object auto-discarded being a duplicate");
                    Destroy(gameObject);
                }
                else
                {
                    throw new Exception($"⚠️ [{typeof(T).Name}] Attempted to create a second instance!");
                }
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this as T)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Force the object to not be destroyed automatically if another singleton instance exist.<br/>
        /// ⚠️ Be advised → setting the _autoDiscard to false will just skip to raise an exception and mark a hard error.
        /// </summary>
        public static void DisableAutoDiscard()
        {
            UnityEngine.Debug.LogWarning($"⚠️ [{typeof(T).Name}] auto-discard disabled\nThis WILL lead to errors instead of warnings upon further instantiations !");
            _autoDiscard = false;
        }
    }
}
