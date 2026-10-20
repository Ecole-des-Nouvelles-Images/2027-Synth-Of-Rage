using System;
using SynthOfRage.Scripts.Utilities;
using UnityEngine;

namespace _Dev.Christopher.Scripts.SystemTest
{
    public class FrameCounter : SingletonMonoBehaviour<FrameCounter>
    {
        public Action OnFrameTick;
        
        private float _accumulator = 0;
        private int _frameCounter = 0;

        /*private void OnEnable()
        {
            OnFrameTick+=FrameCounterDebug;
        }
        
        private void OnDisable()
        {
            OnFrameTick-=FrameCounterDebug;
        }*/

        private void FixedUpdate()
        {
            _accumulator += Time.fixedDeltaTime;
            if (_accumulator >= 1f/60f)
            {
                _accumulator -= 1f/60f;
                OnFrameTick?.Invoke();
            }
        }

        private void FrameCounterDebug()
        {
        }
    }
}