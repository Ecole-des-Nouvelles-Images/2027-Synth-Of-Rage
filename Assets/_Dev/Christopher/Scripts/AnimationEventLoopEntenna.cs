using System;
using SynthOfRage.Scripts.Utilities;
using UnityEngine;

namespace _Dev.Christopher.Scripts
{
    public class AnimationEventLoopAntenna : MonoBehaviour
    {
        [SerializeField]private Animator _animator;
        public CustomEnumLib.AnimationState AnimState { get; private set; } = CustomEnumLib.AnimationState.None;

        // private void OnEnable()
        // {
        //     AnimState = CustomEnumLib.AnimationState.None;
        // }

        public void TriggeredOnStart()
        {
            AnimState = CustomEnumLib.AnimationState.Start;
            
            Debug.Log("animation state = " + AnimState);
        }

        public void TriggeredOnActive()
        {
            AnimState = CustomEnumLib.AnimationState.Active;
            Debug.Log("animation state = " + AnimState);
        }

        public void TriggeredOnRecover()
        {
            AnimState = CustomEnumLib.AnimationState.Recover;
            Debug.Log("animation state = " + AnimState);
        }

        public void TriggeredOnEnd()
        {
            AnimState = CustomEnumLib.AnimationState.End;
            Debug.Log("animation state = " + AnimState);
        }
    }
}