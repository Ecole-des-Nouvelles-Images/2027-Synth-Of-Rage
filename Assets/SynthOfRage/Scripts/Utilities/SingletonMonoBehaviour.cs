using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace SynthOfRage.Scripts.Utilities
{
    public partial class SingletonMonoBehaviour<T> : MonoBehaviour where T : Component
    {
        [AutoStaticsCleanup]
        private static T _instance;

        /// <summary>Tell if the <c>SingletonMonoBehaviour</c> should discard any new instances on the spot if an instance already exists.</summary>
        /// <remarks>While the parameter is static, it's applied for each different closed type, not for all singletons.</remarks>
        [AutoStaticsCleanup] private static bool _autoDiscard = true;

        /// <summary>Should the <c>SingletonMonoBehaviour</c> throw a critical exception if a <c>get</c> fails and would return a <see langword="null"/> reference.</summary>
        /// <remarks>When disabled (<see langword="false"/>), additional <see langword="null"/>-checks should be done on each <c>get</c>.</remarks>
        [AutoStaticsCleanup] private static bool _forceNullInstanceException = false;

        public static T Instance
        {
            get
            {
                if (!_instance)
                {
                    T[] objs = FindObjectsByType<T>(FindObjectsInactive.Include);

                    if (objs.Length > 0)
                        _instance = objs[0];

                    if (objs.Length > 1)
                        throw new Exception($"[{typeof(T).Name}] There is more than one instance in the scene !");
                }

                if (_instance) return _instance;

#if UNITY_EDITOR || UNITY_USE_INSTRUMENTATION
                UnityEngine.Debug.LogWarning($"[{typeof(T).Name}] Attempting to get a singleton instance that doesn't exist anymore.");
#endif
                return _forceNullInstanceException ? throw new Exception($"[{typeof(T).Name}] No singleton instance found in the scene !") : null;

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
        /// ⚠️ Be advised → setting <c>_autoDiscard</c> to false will just skip any warnings to raise an exception instead and mark a hard error.
        /// </summary>
        public static void DisableAutoDiscard()
        {
            UnityEngine.Debug.LogWarning($"⚠️ [{typeof(T).Name}] auto-discard disabled.\nThis WILL lead to exceptions instead of warnings upon further instantiations !");
            _autoDiscard = false;
        }
    }
}
