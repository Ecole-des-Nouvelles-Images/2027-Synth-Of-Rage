using System;
using SynthOfRage.Scripts.HSM;
using UnityEngine;

namespace SynthOfRage.Scripts.StatesSystem
{
    public class Fsm : MonoBehaviour
    {
        public State CurrentState;

        private void Start()
        {
            CurrentState.Start();
        }

        private void Update()
        {
            CurrentState.Update();
        }

        private void SwitchState(State newState)
        {
            CurrentState = newState;
            CurrentState.Start();
        }
    }
}