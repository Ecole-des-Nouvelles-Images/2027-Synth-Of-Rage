using System;
using System.Collections.Generic;
using UnityEngine;
using SynthOfRage.Scripts.Gameplay.Core;
using SynthOfRage.Scripts.Gameplay.Common.Modules;
using SynthOfRage.Scripts.Gameplay.Unit.Player;

namespace _Dev.Christopher.Scripts
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerCore))]
    public class AcCharacterHandler : MonoBehaviour
    {
        private static readonly int Move = Animator.StringToHash("Move");
        private static readonly int LightAttack01 = Animator.StringToHash("LightAttack01");
        private static readonly int LightAttack02 = Animator.StringToHash("LightAttack02");
        private static readonly int LightAttack03 = Animator.StringToHash("LightAttack03");
        private static readonly int HeavyAttack01 = Animator.StringToHash("HeavyAttack01");
        private static readonly int Hurt = Animator.StringToHash("Hurt");
        private static readonly int Death = Animator.StringToHash("Death");

        [Header("References")]
        [SerializeField] private PlayerCore playerCore;
        public Animator MyAnimator;

        [Header("Rhythm - Light Combo")]
        [SerializeField] private bool _useRhythm = true;
        [SerializeField] private bool _enableBeatSnapping = true;
        [Tooltip("1 = beat, 0.5 = half-beat, 0.25 = quarter-beat. Can also be 2, 4, etc.")]
        [SerializeField, Min(0.01f)] private float _snapEveryBeats = 0.5f;
        [Tooltip("Hybrid snap never delays the initial input more than this many seconds.")]
        [SerializeField, Min(0f)] private float _maxSnapDelaySeconds = 0.09f;
        [Tooltip("Snap the frame-10 impact instead of the start of the first attack animation.")]
        [SerializeField] private bool _snapToFirstHit = true;
        [SerializeField, Range(0, 60)] private int _firstHitFrame = 10;
        [Tooltip("Duration of a 60-frame light attack clip in musical beats.")]
        [SerializeField, Min(0.1f)] private float _attackDurationBeats = 2f;
        [Tooltip("Normal attack lockout. Confirmed combo follow-ups ignore this value.")]
        [SerializeField, Min(0f)] private float _attackCooldownBeats = 2f;
        [Tooltip("Maximum time to continue a combo after the last attack started.")]
        [SerializeField, Min(0.1f)] private float _comboResetBeats = 4f;
        [Tooltip("Allows an early normal attack press when cooldown is about to finish.")]
        [SerializeField, Min(0f)] private float _inputBufferBeats = 0.5f;

        [Header("Rhythm - Heavy Attack")]
        [SerializeField] private bool _enableHeavyBeatSnapping = true;
        [Tooltip("1 = beat, 0.5 = half-beat, 0.25 = quarter-beat.")]
        [SerializeField, Min(0.01f)] private float _heavySnapEveryBeats = 1f;
        [Tooltip("Maximum extra wait for the heavy attack to align with the grid, in seconds.")]
        [SerializeField, Min(0f)] private float _heavyMaxSnapDelaySeconds = 0.09f;
        [Tooltip("Align the heavy hit moment (rather than animation start) to the musical grid.")]
        [SerializeField] private bool _heavySnapToFirstHit = true;
        [SerializeField, Range(0, 120)] private int _heavyFirstHitFrame = 10;
        [SerializeField, Min(0.1f)] private float _heavyAttackDurationBeats = 2f;
        [SerializeField, Min(0f)] private float _heavyAttackCooldownBeats = 2f;
        [SerializeField, Min(1)] private int _heavyAttackClipFrames = 60;
        [SerializeField, Min(1f)] private float _heavyAttackClipFPS = 60f;
        [Tooltip("Float used as Speed Multiplier by the HeavyAttack01 Animator state.")]
        [SerializeField] private string _heavyAttackSpeedParameter = "HeavyAttackSpeed";

        [Header("Rhythm - Hit Confirm Combo")]
        [Tooltip("Require attack 01 to hit before 02, and attack 02 to hit before 03.")]
        [SerializeField] private bool _enableHitConfirmCombo = true;
        [Tooltip("Confirmed follow-ups are launched on this musical grid (0.5 = next half-beat).")]
        [SerializeField, Min(0.01f)] private float _comboEveryBeats = 0.5f;
        [Tooltip("How long early combo presses are remembered, in musical beats.")]
        [SerializeField, Min(0f)] private float _comboInputBufferBeats = 2f;
        [Tooltip("Enemy knockback multiplier ONLY for the third light attack. Damage is unchanged.")]
        [SerializeField, Min(1f)] private float _thirdHitKnockbackMultiplier = 3f;

        [Header("Combo - Knockback")]
        [Tooltip("Delay knockback for hits 01 and 02 until the combo tolerance expires. A subsequent confirmed hit cancels it.")]
        [SerializeField] private bool _delayComboKnockback = true;

        [Header("Rhythm - Animator")]
        [Tooltip("Requires a float AttackSpeed on LightAttack01, 02 and 03 state Speed Multiplier.")]
        [SerializeField] private bool _syncAttackAnimationSpeed = true;
        [SerializeField] private string _attackSpeedParameter = "AttackSpeed";
        [SerializeField, Min(1)] private int _attackClipFrames = 60;
        [SerializeField, Min(1f)] private float _attackClipFPS = 60f;
        [SerializeField, Min(1f)] private float _fallbackBPM = 87f;
        [SerializeField] private bool _debugEnabled;

        private CharacterController _characterController;
        private HealthModule _healthModule;
        private readonly List<HurtboxEvent> _ownedHurtboxes = new();
        private readonly Queue<double> _bufferedFollowupInputs = new();

        private int _comboCounter; // 0 = none, 1 = attack 01, 2 = attack 02, 3 = finisher 03.
        private bool _hitConfirmed;
        private bool _pendingAttack;
        private int _pendingStep;
        private double _scheduledBeat;
        private double _nextAllowedBeat;
        private double _comboExpireBeat;
        private double _attackFinishBeat;
        private double _attackStartBeat;
        private double _comboEarliestBeat;
        private bool _pendingHeavyAttack;
        private double _scheduledHeavyBeat;
        private double _nextHeavyAllowedBeat;
        private double _heavyFinishBeat;

        private bool _attackSpeedParameterPresent;
        private bool _warnedAnimatorParameter;
        private bool _warnedAnimatorSpeed;
        private bool _warnedFrozenAttack;
        private bool _warnedThirdTrigger;
        private const float MinimumAttackSpeed = 0.05f;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _healthModule = GetComponent<HealthModule>();
            if (!playerCore) playerCore = GetComponent<PlayerCore>();
            if (!MyAnimator) MyAnimator = GetComponentInChildren<Animator>(true);
        }

        private void OnEnable()
        {
            if (!MyAnimator || !playerCore)
            {
                Debug.LogError("[AcCharacterHandler] Animator or PlayerCore reference is missing.", this);
                return;
            }

            _attackSpeedParameterPresent = HasFloatParameter(_attackSpeedParameter);
            _warnedAnimatorSpeed = false;
            _warnedFrozenAttack = false;
            _warnedThirdTrigger = false;

            // Initialize the multiplier BEFORE the first attack enters its Animator state.
            UpdateAttackAnimationSpeed();
            UpdateHeavyAttackAnimationSpeed();
            ResetComboChain();
            _nextAllowedBeat = 0.0;
            _nextHeavyAllowedBeat = 0.0;
            _heavyFinishBeat = 0.0;
            _pendingHeavyAttack = false;
            playerCore.OnPlayerAtkL += AtkLComboManager;
            playerCore.OnPlayerAtkH += AtkHManager;

            // Listen to every hurtbox owned by the player, including inactive children.
            _ownedHurtboxes.Clear();
            GetComponentsInChildren(true, _ownedHurtboxes);
            foreach (HurtboxEvent hurtbox in _ownedHurtboxes)
                if (hurtbox) hurtbox.OnHitConfirmed += HandleAttackHitConfirmed;
            if (_healthModule) _healthModule.OnStunChanged += HandleStunChanged;
        }

        private void OnDisable()
        {
            if (playerCore)
            {
                playerCore.OnPlayerAtkL -= AtkLComboManager;
                playerCore.OnPlayerAtkH -= AtkHManager;
            }
            if (_healthModule) _healthModule.OnStunChanged -= HandleStunChanged;
            foreach (HurtboxEvent hurtbox in _ownedHurtboxes)
                if (hurtbox) hurtbox.OnHitConfirmed -= HandleAttackHitConfirmed;
            _ownedHurtboxes.Clear();
            ResetComboChain();
            _pendingHeavyAttack = false;
        }

        private void OnDestroy()
        {
            // Event subscriptions are released in OnDisable, including when destroyed.
        }

        private void Start()
        {
            if (!_characterController) _characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            // Recover if another component or controller resets AttackSpeed to zero.
            if (MyAnimator && _attackSpeedParameterPresent &&
                MyAnimator.GetFloat(_attackSpeedParameter) <= 0.0001f)
                UpdateAttackAnimationSpeed();

            if (MyAnimator && MyAnimator.speed <= 0f && !_warnedAnimatorSpeed)
            {
                Debug.LogWarning("[AcCharacterHandler] Animator.speed is zero. Attack animations cannot advance.", this);
                _warnedAnimatorSpeed = true;
            }

            double now = BeatNow();
            DiscardExpiredInputs(now);

            if (_pendingHeavyAttack && now + 1e-6 >= _scheduledHeavyBeat)
            {
                _pendingHeavyAttack = false;
                if (CanLaunchHeavy(now)) LaunchHeavyAttack(now);
            }

            if (_pendingAttack && now + 1e-6 >= _scheduledBeat)
            {
                int step = _pendingStep;
                _pendingAttack = false;
                _pendingStep = 0;

                if (CanLaunchStep(step, now)) LaunchAttack(now, step);
            }

            if (_comboCounter == 0) return;

            if (now > _comboExpireBeat ||
                (_comboCounter == 3 && now >= _attackFinishBeat) ||
                (_enableHitConfirmCombo && !_hitConfirmed && !_pendingAttack &&
                 now > _attackFinishBeat + 0.01 && _comboCounter < 3))
            {
                ResetComboChain();
                return;
            }

            TryScheduleBufferedFollowup(now);
        }

        private double BeatNow()
        {
            RhythmManager clock = RhythmManager.Instance;
            return _useRhythm && clock
                ? clock.CurrentBeat
                : Time.timeAsDouble * _fallbackBPM / 60.0;
        }

        private bool RhythmReady() =>
            _useRhythm && RhythmManager.Instance && RhythmManager.Instance.IsReady;

        private bool CanLaunchHeavy(double now) =>
            MyAnimator && HasTriggerParameter(HeavyAttack01) &&
            (!_healthModule || (!_healthModule.IsStunned && _healthModule.HP > 0)) &&
            now + 1e-6 >= _nextHeavyAllowedBeat;

        private void AtkHManager()
        {
            if (!MyAnimator || (_healthModule && (_healthModule.IsStunned || _healthModule.HP <= 0))) return;
            if (_useRhythm && RhythmManager.Instance && !RhythmManager.Instance.IsReady) return;
            if (_pendingHeavyAttack) return;

            double now = BeatNow();
            double earliest = Math.Max(now, _nextHeavyAllowedBeat);
            if (earliest > now + 1e-6 && earliest - now > _inputBufferBeats) return;

            double scheduled = earliest;
            if (_enableHeavyBeatSnapping && RhythmReady())
            {
                double offset = _heavySnapToFirstHit
                    ? (double)_heavyFirstHitFrame / _heavyAttackClipFrames * _heavyAttackDurationBeats
                    : 0.0;
                scheduled = RhythmManager.Instance.HybridSnapBeat(
                    earliest, _heavySnapEveryBeats, _heavyMaxSnapDelaySeconds, offset);
            }

            if (scheduled <= now + 1e-6) LaunchHeavyAttack(now);
            else
            {
                _scheduledHeavyBeat = scheduled;
                _pendingHeavyAttack = true;
                UnityEngine.Debug.Log($"HeavyAttack01 queued for beat {scheduled:F3}.");
            }
        }

        private void LaunchHeavyAttack(double now)
        {
            if (!CanLaunchHeavy(now)) return;

            // A heavy attack interrupts the light combo and clears its queued inputs.
            ResetComboChain();
            UpdateHeavyAttackAnimationSpeed();
            foreach (HurtboxEvent hurtbox in _ownedHurtboxes)
                if (hurtbox) hurtbox.PrepareAttackStep(1f,
                    (float)(_heavyFirstHitFrame / (double)_heavyAttackClipFrames *
                            _heavyAttackDurationBeats * (RhythmReady()
                                ? RhythmManager.Instance.SecondsPerBeat : 60.0 / _fallbackBPM)),
                    0, 0f);

            MyAnimator.ResetTrigger(LightAttack01);
            MyAnimator.ResetTrigger(LightAttack02);
            if (HasTriggerParameter(LightAttack03)) MyAnimator.ResetTrigger(LightAttack03);
            MyAnimator.ResetTrigger(HeavyAttack01);
            MyAnimator.SetTrigger(HeavyAttack01);

            _nextHeavyAllowedBeat = now + Math.Max(_heavyAttackDurationBeats, _heavyAttackCooldownBeats);
            _heavyFinishBeat = now + _heavyAttackDurationBeats;
            _nextAllowedBeat = Math.Max(_nextAllowedBeat, _heavyFinishBeat);
            UnityEngine.Debug.Log("HeavyAttack01 START.");
        }

        private void UpdateHeavyAttackAnimationSpeed()
        {
            if (!MyAnimator) return;
            if (!HasFloatParameter(_heavyAttackSpeedParameter))
            {
                if (_syncAttackAnimationSpeed)
                    Debug.LogWarning($"[AcCharacterHandler] Animator Float '{_heavyAttackSpeedParameter}' missing. HeavyAttack01 will keep its current speed.", this);
                return;
            }

            float speed = 1f;
            if (_syncAttackAnimationSpeed)
            {
                double beatSeconds = RhythmReady()
                    ? RhythmManager.Instance.SecondsPerBeat : 60.0 / _fallbackBPM;
                speed = (float)((_heavyAttackClipFrames / _heavyAttackClipFPS) /
                    (Math.Max(0.1, _heavyAttackDurationBeats) * beatSeconds));
            }
            if (float.IsNaN(speed) || float.IsInfinity(speed)) speed = 1f;
            MyAnimator.SetFloat(_heavyAttackSpeedParameter, Mathf.Max(MinimumAttackSpeed, speed));
        }

        private void AtkLComboManager()
        {
            if (!MyAnimator || (_healthModule && (_healthModule.IsStunned || _healthModule.HP <= 0))) return;
            if (_useRhythm && RhythmManager.Instance && !RhythmManager.Instance.IsReady) return;

            double now = BeatNow();
            if (_pendingHeavyAttack || now < _heavyFinishBeat) return;
            if (_comboCounter != 0 && now > _comboExpireBeat) ResetComboChain();

            if (_enableHitConfirmCombo)
            {
                if (_comboCounter == 3 || (_pendingAttack && _pendingStep == 3)) return;

                // Queue up to two follow-up presses even during attack 01, so fast
                // input sequences are preserved instead of being swallowed.
                if (_comboCounter > 0 || _pendingAttack)
                {
                    if (_bufferedFollowupInputs.Count < 2)
                    {
                        _bufferedFollowupInputs.Enqueue(now);
                        UnityEngine.Debug.Log($"Combo input buffered ({_bufferedFollowupInputs.Count}/2).");
                    }
                    TryScheduleBufferedFollowup(now);
                    return;
                }
            }
            else if (_pendingAttack) return;

            double earliest = Math.Max(now, _nextAllowedBeat);
            if (earliest > now + 1e-6 && earliest - now > _inputBufferBeats) return;

            double scheduled = earliest;
            if (_enableBeatSnapping && RhythmReady())
            {
                double offset = _snapToFirstHit
                    ? (double)_firstHitFrame / _attackClipFrames * _attackDurationBeats
                    : 0.0;
                scheduled = RhythmManager.Instance.HybridSnapBeat(
                    earliest, _snapEveryBeats, _maxSnapDelaySeconds, offset);
            }

            int nextStep = !_enableHitConfirmCombo && _comboCounter >= 1 && _comboCounter < 3
                ? _comboCounter + 1
                : 1;
            QueueAttack(scheduled, nextStep, now);
        }

        private void HandleAttackHitConfirmed(GameObject target)
        {
            if (!_enableHitConfirmCombo || _pendingAttack ||
                _comboCounter < 1 || _comboCounter >= 3 || _hitConfirmed) return;

            double now = BeatNow();
            if (now > _attackFinishBeat + 0.05) return;

            _hitConfirmed = true;
            double step = Math.Max(0.01, _comboEveryBeats);
            // Strictly AFTER the confirmed hit, not at the grid point just crossed.
            _comboEarliestBeat = (Math.Floor(now / step + 1e-6) + 1.0) * step;

            UnityEngine.Debug.Log($"Attack 0{_comboCounter} hit {target.name}; follow-up 0{_comboCounter + 1} unlocked at beat {_comboEarliestBeat:F3}.");
            TryScheduleBufferedFollowup(now);
        }

        private void TryScheduleBufferedFollowup(double now)
        {
            if (!_enableHitConfirmCombo || _pendingAttack ||
                !_hitConfirmed || _comboCounter < 1 || _comboCounter >= 3 ||
                now > _comboExpireBeat) return;

            DiscardExpiredInputs(now);
            if (_bufferedFollowupInputs.Count == 0) return;
            _bufferedFollowupInputs.Dequeue();

            // Hit-confirm follow-ups bypass _nextAllowedBeat / normal cooldown.
            double scheduled = RhythmReady()
                ? Math.Max(_comboEarliestBeat, RhythmManager.QuantizeBeat(now, _comboEveryBeats))
                : now;
            if (scheduled > _comboExpireBeat + 1e-6) return;

            QueueAttack(scheduled, _comboCounter + 1, now);
        }

        private void DiscardExpiredInputs(double now)
        {
            while (_bufferedFollowupInputs.Count > 0 &&
                   now - _bufferedFollowupInputs.Peek() > _comboInputBufferBeats)
                _bufferedFollowupInputs.Dequeue();
        }

        private bool CanLaunchStep(int step, double now)
        {
            if (!MyAnimator || (_healthModule && (_healthModule.IsStunned || _healthModule.HP <= 0)))
                return false;
            if (step == 1) return now + 1e-6 >= _nextAllowedBeat;
            if (step < 2 || step > 3 || _comboCounter != step - 1 || now > _comboExpireBeat)
                return false;
            return !_enableHitConfirmCombo || _hitConfirmed;
        }

        private void QueueAttack(double scheduled, int step, double now)
        {
            _scheduledBeat = scheduled;
            _pendingStep = step;
            _pendingAttack = true;
            UnityEngine.Debug.Log($"Attack 0{step} queued for beat {scheduled:F3}.");

            if (scheduled <= now + 1e-6)
            {
                _pendingAttack = false;
                _pendingStep = 0;
                if (CanLaunchStep(step, now)) LaunchAttack(now, step);
            }
        }

        private void LaunchAttack(double now, int step)
        {
            if (!MyAnimator) return;
            if (step == 3 && !HasTriggerParameter(LightAttack03))
            {
                if (!_warnedThirdTrigger)
                {
                    Debug.LogError("[AcCharacterHandler] Animator Trigger 'LightAttack03' is missing. Create the third attack state and trigger.", this);
                    _warnedThirdTrigger = true;
                }
                ResetComboChain();
                return;
            }

            UpdateAttackAnimationSpeed();

            int trigger = step switch
            {
                2 => LightAttack02,
                3 => LightAttack03,
                _ => LightAttack01
            };

            // The Animator alone controls Hurtbox Collider enabled/disabled curves.
            // Only the damage gate and enemy knockback multiplier change here.
            double beatSeconds = RhythmReady()
                ? RhythmManager.Instance.SecondsPerBeat
                : 60.0 / _fallbackBPM;
            float firstHitDelaySeconds = (float)(
                ((double)_firstHitFrame / _attackClipFrames) * _attackDurationBeats * beatSeconds);
            float multiplier = step == 3 ? _thirdHitKnockbackMultiplier : 1f;
            foreach (HurtboxEvent hurtbox in _ownedHurtboxes)
                if (hurtbox) hurtbox.PrepareAttackStep(
                    multiplier, firstHitDelaySeconds, step,
                    _delayComboKnockback && _enableHitConfirmCombo ? _comboInputBufferBeats : 0f);

            MyAnimator.ResetTrigger(LightAttack01);
            MyAnimator.ResetTrigger(LightAttack02);
            if (HasTriggerParameter(LightAttack03)) MyAnimator.ResetTrigger(LightAttack03);
            MyAnimator.SetTrigger(trigger);

            _comboCounter = step;
            _hitConfirmed = false;
            _comboEarliestBeat = 0.0;
            _attackStartBeat = now;
            _attackFinishBeat = now + _attackDurationBeats;
            _nextAllowedBeat = now + Math.Max(_attackDurationBeats, _attackCooldownBeats);
            _comboExpireBeat = now + _comboResetBeats;
            UnityEngine.Debug.Log($"Attack 0{step} START | enemy knockback x{multiplier:F1}.");
        }

        private void ResetComboChain()
        {
            _comboCounter = 0;
            _hitConfirmed = false;
            _pendingAttack = false;
            _pendingStep = 0;
            _bufferedFollowupInputs.Clear();
            _comboEarliestBeat = 0.0;
            foreach (HurtboxEvent hurtbox in _ownedHurtboxes)
                if (hurtbox) hurtbox.SetEnemyKnockbackMultiplier(1f);
        }

        private void UpdateAttackAnimationSpeed()
        {
            if (!MyAnimator) return;
            _attackSpeedParameterPresent = HasFloatParameter(_attackSpeedParameter);
            if (!_attackSpeedParameterPresent)
            {
                if (_syncAttackAnimationSpeed && !_warnedAnimatorParameter)
                {
                    Debug.LogWarning("[AcCharacterHandler] Add an Animator float '" + _attackSpeedParameter +
                                     "' and assign it to the three light attack states' Speed Multiplier.", this);
                    _warnedAnimatorParameter = true;
                }
                return;
            }

            // Use normal speed if synchronization is disabled, never zero.
            float speed = 1f;
            if (_syncAttackAnimationSpeed)
            {
                double secondsPerBeat = RhythmReady()
                    ? RhythmManager.Instance.SecondsPerBeat
                    : 60.0 / _fallbackBPM;
                float naturalDuration = _attackClipFrames / _attackClipFPS;
                speed = (float)(naturalDuration / (Math.Max(0.1, _attackDurationBeats) * secondsPerBeat));
            }

            if (float.IsNaN(speed) || float.IsInfinity(speed)) speed = 1f;
            MyAnimator.SetFloat(_attackSpeedParameter, Mathf.Max(MinimumAttackSpeed, speed));
        }

        private void LateUpdate()
        {
            if (!_debugEnabled || !MyAnimator || _warnedFrozenAttack) return;
            AnimatorStateInfo state = MyAnimator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("LightAttack01") && !state.IsName("LightAttack02") &&
                !state.IsName("LightAttack03") && !state.IsName("LightAttack1") &&
                !state.IsName("LightAttack2") && !state.IsName("LightAttack3")) return;
            if (state.speed * state.speedMultiplier * MyAnimator.speed > 0.0001f) return;

            _warnedFrozenAttack = true;
            Debug.LogError("[AcCharacterHandler] Attack animation is frozen. Set state Speed=1 and Multiplier=AttackSpeed (not VerticalVelocity).", this);
        }

        private bool HasFloatParameter(string parameter)
        {
            if (!MyAnimator || string.IsNullOrEmpty(parameter)) return false;
            foreach (AnimatorControllerParameter p in MyAnimator.parameters)
                if (p.name == parameter && p.type == AnimatorControllerParameterType.Float) return true;
            return false;
        }

        private bool HasTriggerParameter(int hash)
        {
            if (!MyAnimator) return false;
            foreach (AnimatorControllerParameter p in MyAnimator.parameters)
                if (p.nameHash == hash && p.type == AnimatorControllerParameterType.Trigger) return true;
            return false;
        }

        private void HandleStunChanged(bool stunned)
        {
            if (stunned)
            {
                ResetComboChain();
                _pendingHeavyAttack = false;
            }
        }

        private void HandleAtkL01Animation()
        {
            if (MyAnimator) MyAnimator.SetTrigger(LightAttack01);
        }

        private void HandleAtkL02Animation()
        {
            if (MyAnimator) MyAnimator.SetTrigger(LightAttack02);
        }

        private void HandleAtkL03Animation()
        {
            if (MyAnimator) MyAnimator.SetTrigger(LightAttack03);
        }

        private void HandleAtkH01Animation()
        {
            if (MyAnimator) MyAnimator.SetTrigger(HeavyAttack01);
        }

        private void HandleHurtAnimation()
        {
            if (MyAnimator) MyAnimator.SetTrigger(Hurt);
        }

        private void HandleDeathAnimation()
        {
            if (MyAnimator) MyAnimator.SetTrigger(Death);
        }

        private void OnValidate()
        {
            _snapEveryBeats = Mathf.Max(0.01f, _snapEveryBeats);
            _heavySnapEveryBeats = Mathf.Max(0.01f, _heavySnapEveryBeats);
            _heavyAttackDurationBeats = Mathf.Max(0.1f, _heavyAttackDurationBeats);
            _heavyAttackClipFPS = Mathf.Max(1f, _heavyAttackClipFPS);
            _comboEveryBeats = Mathf.Max(0.01f, _comboEveryBeats);
            _attackDurationBeats = Mathf.Max(0.1f, _attackDurationBeats);
            _attackClipFPS = Mathf.Max(1f, _attackClipFPS);
            _fallbackBPM = Mathf.Max(1f, _fallbackBPM);
            _thirdHitKnockbackMultiplier = Mathf.Max(1f, _thirdHitKnockbackMultiplier);
        }
    }
}
