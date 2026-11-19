using SynthOfRage.Scripts.Gameplay.Common.StateMachine;

namespace SynthOfRage.Scripts.Gameplay.Unit.Avatar.States
{
    /// <summary>
    /// Every <see cref="BaseState"/> an <see cref="Avatar"/> can run.<br/>
    /// To add a state, create its class then add one field here — the <see cref="StateCollection"/>
    /// base registers it automatically, no other wiring needed.
    /// </summary>
    public class AvatarStateCollection : StateCollection
    {
        public AvatarStateIdle Idle = new();
        public AvatarStateMove Move = new();
        public AvatarStateJump Jump = new();
        public AvatarStateDash Dash = new();
        public AvatarStateGuard Guard = new();
        public AvatarStateLightAttack LightAttack = new();
        public AvatarStateHeavyAttack HeavyAttack = new();
        public AvatarStateAirLightAttack AirLightAttack = new();
        public AvatarStateAirHeavyAttack AirHeavyAttack = new();
        public AvatarStateSpecial Special = new();
        public AvatarStateHitStun HitStun = new();
    }
}
