using SynthOfRage.Scripts.Gameplay.Common.Modules;
using UnityEngine;

namespace SynthOfRage.Scripts.Gameplay.Unit.Player
{
    /// <summary>
    /// Attach this StateMachineBehaviour to each light attack Animator state.
    /// Invincibility follows the animation's normalized frame progress, even when
    /// AttackSpeed changes. Leaving the state (dash, hit, combo transition) clears it.
    /// </summary>
    public sealed class AttackInvincibilityState : StateMachineBehaviour
    {
        [Header("Active Frames - 60 Frame Clip")]
        [SerializeField, Min(1)] private int _clipFrames = 60;
        [SerializeField, Min(0)] private int _startFrame = 10;
        [SerializeField, Min(0)] private int _endFrame = 25;

        [Tooltip("If checked, these frames grant attack invincibility.")]
        [SerializeField] private bool _enableInvincibility = true;

        private HealthModule _health;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _health = animator.GetComponentInParent<HealthModule>();
            if (_health) _health.SetAttackInvincibility(false);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!_health) _health = animator.GetComponentInParent<HealthModule>();
            if (!_health) return;

            float progress = Mathf.Repeat(stateInfo.normalizedTime, 1f);
            // End time is exclusive, matching the frame on which the hitbox shuts off.
            float start = (float)_startFrame / Mathf.Max(1, _clipFrames);
            float end = (float)_endFrame / Mathf.Max(1, _clipFrames);
            bool active = _enableInvincibility && _endFrame > _startFrame &&
                          stateInfo.normalizedTime >= 0f && stateInfo.normalizedTime < 1f &&
                          progress >= start && progress < end;
            _health.SetAttackInvincibility(active);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (_health) _health.SetAttackInvincibility(false);
            _health = null;
        }
    }
}
