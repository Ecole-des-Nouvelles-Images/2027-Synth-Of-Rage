using _Old.Gameplay.Unit.Player;

namespace _Dev.Christopher.Scripts.SystemTest.CombatStateMachine
{
    public abstract class CombatState
    {
        protected FsmCombat _fsmCombat;
        protected Avatar _avatar;

        protected CombatState(FsmCombat fsmCombat, Avatar avatar)
        {
            _fsmCombat = fsmCombat;
            _avatar = avatar;
        }

        public abstract void OnStart();
        public abstract void OnUpdate();
        public abstract void OnEnd();
    }
}
