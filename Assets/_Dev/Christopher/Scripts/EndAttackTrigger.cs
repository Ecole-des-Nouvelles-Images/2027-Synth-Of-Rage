using System;
using UnityEngine;

namespace _Dev.Christopher.Scripts
{
    public class EndAttackTrigger : MonoBehaviour
    {
        private HurtboxEvent _hurtboxEvent;

        private void Awake()
        {
            _hurtboxEvent = GetComponentInChildren<HurtboxEvent>();
        }

        public void AttackFinished()
        {
            Debug.Log("Attack Finished");
            _hurtboxEvent.EndAttack();
        }
    }
}
