using SynthOfRage.Scripts.Utilities;
using UnityEngine;
using Avatar = SynthOfRage.Scripts.Gameplay.Unit.Player.Avatar;

namespace _Dev.Christopher.Scripts.SystemTest.CombatStateMachine
{
    public class CombatStateAttack : CombatState
    {
        private float timerSim;
        
        public CombatStateAttack(FsmCombat fsmCombat, Avatar avatar) : base(fsmCombat, avatar)
        {
            _fsmCombat = fsmCombat;
            _avatar = avatar;
        }

        public override void OnStart()
        {
            Debug.Log("AttackState");
            timerSim = 2f;
        }

        public override void OnUpdate()
        {
            if (timerSim > 0)
            {
                timerSim -= Time.deltaTime;
            }
            else
            {
                _fsmCombat.ChangeState(_fsmCombat.OtherState);
            }
        }

        public override void OnEnd()
        {
            throw new System.NotImplementedException();
        }
    }
}