using _Dev.Christopher.Scripts.SystemTest.InputBlock;
using UnityEngine;
using Avatar = SynthOfRage.Scripts.Gameplay.Unit.Player.Avatar;

namespace _Dev.Christopher.Scripts.SystemTest.CombatStateMachine
{
    public class CombatStateOther : CombatState
    {
        public CombatStateOther(FsmCombat fsmCombat, Avatar avatar) : base(fsmCombat, avatar)
        {
            _fsmCombat = fsmCombat;
            _avatar = avatar;
        }

        public override void OnStart()
        {
            Debug.Log("OtherState");
        }

        public override void OnUpdate()
        {
            if(_fsmCombat.ReadLastCommand()!= null && _fsmCombat.ReadLastCommand().GetType() == typeof(AtkLCommand))
                _fsmCombat.ChangeState(_fsmCombat.AttackState);
        }

        public override void OnEnd()
        {
            throw new System.NotImplementedException();
        }
    }
}