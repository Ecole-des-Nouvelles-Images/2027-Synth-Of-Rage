using System;
using System.Collections;
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

        // ============================================================
        // RHYTHM - IDLE ANIMATION
        // ============================================================

        [Header("Rhythm - Idle Animation")]
        [Tooltip("Synchronizes the Idle animation loop with the current BPM.")]
        [SerializeField] private bool _syncIdleAnimationToBpm = true;

        [Tooltip("Synchronizes the beginning of the Idle animation with beat 0.")]
        [SerializeField] private bool _snapIdleStartToBeat = true;

        [Tooltip("Full Animator state path of the Idle state. Example: Base Layer.Idle")]
        [SerializeField] private string _idleStateFullPath = "Base Layer.Idle";

        [Tooltip("Animator layer containing the Idle state.")]
        [SerializeField] private int _idleAnimatorLayer = 0;

        [Tooltip("Float parameter used as the Idle state Speed Multiplier.")]
        [SerializeField] private string _idleSpeedParameter = "IdleSpeed";

        [Tooltip("Number of frames in the Idle animation clip.")]
        [SerializeField, Min(1)] private int _idleClipFrames = 45;

        [Tooltip("Native FPS of the Idle animation clip.")]
        [SerializeField, Min(1f)] private float _idleClipFPS = 60f;

        [Tooltip("Musical duration of one complete Idle loop. 1 = one beat.")]
        [SerializeField, Min(0.01f)] private float _idleLoopDurationBeats = 1f;

        // ============================================================
        // RHYTHM - LIGHT COMBO
        // ============================================================

        [Header("Rhythm - Light Combo")]
        [SerializeField] private bool _enableBeatSnapping = true;

        [Tooltip("Musical grid used for light attacks. 1 = beat, 0.5 = half-beat, 0.25 = quarter-beat.")]
        [SerializeField, Min(0.01f)] private float _snapEveryBeats = 0.5f;

        [Tooltip("Maximum distance between the theoretical sync frame and the next beat. Expressed in beats.")]
        [SerializeField, Min(0f)] private float _maxSnapDelayBeats = 0.25f;

        [Tooltip("Align the configured synchronization frame instead of the beginning of the animation.")]
        [SerializeField] private bool _snapToSyncFrame = true;

        // ============================================================
        // LIGHT ATTACK SYNC FRAMES
        // ============================================================

        [Header("Light Attack Sync Frames")]
        [Tooltip("Animation frame that must land exactly on the musical beat.")]
        [SerializeField, Min(0)] private int _lightAttack01SyncFrame = 10;

        [Tooltip("Animation frame that must land exactly on the musical beat.")]
        [SerializeField, Min(0)] private int _lightAttack02SyncFrame = 10;

        [Tooltip("Animation frame that must land exactly on the musical beat.")]
        [SerializeField, Min(0)] private int _lightAttack03SyncFrame = 10;

        // ============================================================
        // LIGHT ATTACK HIT FRAMES
        // ============================================================

        [Header("Light Attack Impact Frames")]
        [Tooltip("Frame at which LightAttack01 hitbox becomes active.")]
        [SerializeField, Min(0)] private int _lightAttack01HitFrame = 10;

        [Tooltip("Frame at which LightAttack02 hitbox becomes active.")]
        [SerializeField, Min(0)] private int _lightAttack02HitFrame = 10;

        [Tooltip("Frame at which LightAttack03 hitbox becomes active.")]
        [SerializeField, Min(0)] private int _lightAttack03HitFrame = 10;

        [Tooltip("Musical duration of a light attack animation.")]
        [SerializeField, Min(0.1f)] private float _attackDurationBeats = 2f;

        [Tooltip("Normal attack lockout. Confirmed combo follow-ups ignore this value.")]
        [SerializeField, Min(0f)] private float _attackCooldownBeats = 2f;

        [Tooltip("Maximum time to continue a combo after the last attack started.")]
        [SerializeField, Min(0.1f)] private float _comboResetBeats = 4f;

        [Tooltip("Allows an early normal attack press when cooldown is about to finish.")]
        [SerializeField, Min(0f)] private float _inputBufferBeats = 0.5f;

        // ============================================================
        // RHYTHM - HEAVY ATTACK
        // ============================================================

        [Header("Rhythm - Heavy Attack")]
        [SerializeField] private bool _enableHeavyBeatSnapping = true;

        [Tooltip("Musical grid used for heavy attacks. 1 = beat, 0.5 = half-beat, 0.25 = quarter-beat.")]
        [SerializeField, Min(0.01f)] private float _heavySnapEveryBeats = 1f;

        [Tooltip("Maximum distance between the theoretical sync frame and the next beat. Expressed in beats.")]
        [SerializeField, Min(0f)] private float _heavyMaxSnapDelayBeats = 0.25f;

        [Tooltip("Align the configured synchronization frame instead of the beginning of the animation.")]
        [SerializeField] private bool _heavySnapToSyncFrame = true;

        // ============================================================
        // HEAVY ATTACK SYNC AND HIT FRAMES
        // ============================================================

        [Header("Heavy Attack Sync Frame")]
        [Tooltip("Animation frame that must land exactly on the musical beat.")]
        [SerializeField, Min(0)] private int _heavyAttack01SyncFrame = 10;

        [Header("Heavy Attack Impact Frame")]
        [Tooltip("Frame at which HeavyAttack01 hitbox becomes active.")]
        [SerializeField, Min(0)] private int _heavyAttackHitFrame = 10;

        [SerializeField, Min(0.1f)] private float _heavyAttackDurationBeats = 2f;
        [SerializeField, Min(0f)] private float _heavyAttackCooldownBeats = 2f;
        [SerializeField, Min(1)] private int _heavyAttackClipFrames = 60;
        [SerializeField, Min(1f)] private float _heavyAttackClipFPS = 60f;

        [Tooltip("Float used as Speed Multiplier by the HeavyAttack01 Animator state.")]
        [SerializeField] private string _heavyAttackSpeedParameter = "HeavyAttackSpeed";

        // ============================================================
        // RHYTHM - HIT CONFIRM COMBO
        // ============================================================

        [Header("Rhythm - Hit Confirm Combo")]
        [Tooltip("Require attack 01 to hit before 02, and attack 02 to hit before 03.")]
        [SerializeField] private bool _enableHitConfirmCombo = true;

        [Tooltip("Confirmed follow-ups use this musical grid when they are within the rhythm threshold.")]
        [SerializeField, Min(0.01f)] private float _comboEveryBeats = 0.5f;

        [Tooltip("How long early combo presses are remembered, in musical beats.")]
        [SerializeField, Min(0f)] private float _comboInputBufferBeats = 2f;

        [Tooltip("Enemy knockback multiplier ONLY for the third light attack. Damage is unchanged.")]
        [SerializeField, Min(1f)] private float _thirdHitKnockbackMultiplier = 3f;

        // ============================================================
        // COMBO - KNOCKBACK
        // ============================================================

        [Header("Combo - Knockback")]
        [Tooltip("Delay knockback for hits 01 and 02 until the combo tolerance expires. A subsequent confirmed hit cancels it.")]
        [SerializeField] private bool _delayComboKnockback = true;

        // ============================================================
        // RHYTHM - ON BEAT REWARD
        // ============================================================

        [Header("Rhythm - On Beat Reward")]
        [Tooltip("Damage multiplier applied when the attack was successfully rhythm-snapped.")]
        [SerializeField, Min(1f)] private float _onBeatDamageMultiplier = 1.5f;

        [Tooltip("VFX instantiated when an On-Beat attack successfully damages a target.")]
        [SerializeField] private GameObject _onBeatHitVfx;

        [Tooltip("Position offset applied to the hit VFX, in world space.")]
        [SerializeField] private Vector3 _onBeatHitVfxOffset;

        [Tooltip("Use the target rotation when instantiating the hit VFX.")]
        [SerializeField] private bool _onBeatVfxUseTargetRotation = true;

        // ============================================================
        // RHYTHM - ANIMATOR
        // ============================================================

        [Header("Rhythm - Animator")]
        [Tooltip("Requires a float AttackSpeed on LightAttack01, 02 and 03 state Speed Multiplier.")]
        [SerializeField] private bool _syncAttackAnimationSpeed = true;

        [SerializeField] private string _attackSpeedParameter = "AttackSpeed";
        [SerializeField, Min(1)] private int _attackClipFrames = 60;
        [SerializeField, Min(1f)] private float _attackClipFPS = 60f;
        [SerializeField] private bool _debugEnabled;

        // ============================================================
        // AUDIO - ATTACK STEMS
        // ============================================================

        [Header("Audio - Attack Stems")]
        [Tooltip("Stem ID for successful light attacks.")]
        [SerializeField] private string _lightAttackStemId = "Guitar_S";

        [Tooltip("Stem ID for successful heavy attacks.")]
        [SerializeField] private string _heavyAttackStemId = "Guitar_L";

        [Tooltip("Duration of the light attack stem after the finisher or an abandoned combo, in beats.")]
        [SerializeField, Min(0.01f)] private float _lightStemDurationBeats = 1f;

        [Tooltip("Duration of the heavy attack stem, in beats.")]
        [SerializeField, Min(0.01f)] private float _heavyStemDurationBeats = 2f;

        // ============================================================
        // INTERNAL REFERENCES AND STATE
        // ============================================================

        private CharacterController _characterController;
        private HealthModule _healthModule;

        private readonly List<HurtboxEvent> _ownedHurtboxes = new();
        private readonly Queue<double> _bufferedFollowupInputs = new();

        private int _comboCounter;
        private bool _hitConfirmed;

        private bool _pendingAttack;
        private int _pendingStep;
        private double _scheduledBeat;
        private bool _pendingAttackWasRhythmSnapped;

        private double _nextAllowedBeat;
        private double _comboExpireBeat;
        private double _attackFinishBeat;
        private double _attackStartBeat;

        private bool _pendingHeavyAttack;
        private double _scheduledHeavyBeat;
        private bool _pendingHeavyWasRhythmSnapped;

        private double _nextHeavyAllowedBeat;
        private double _heavyFinishBeat;

        private bool _currentAttackIsHeavy;
        private bool _currentAttackWasRhythmSnapped;

        private Coroutine _activeStemOffRoutine;
        private int _stemActivationVersion;
        private bool _lightStemHeldForCombo;

        private bool _attackSpeedParameterPresent;
        private bool _warnedAnimatorParameter;
        private bool _warnedAnimatorSpeed;
        private bool _warnedFrozenAttack;
        private bool _warnedThirdTrigger;

        // Idle animation synchronization.
        private bool _idleSpeedParameterPresent;
        private bool _warnedIdleSpeedParameter;
        private float _lastIdleSpeed = -1f;

        // Idle beat snapping.
        private int _idleStateHash;
        private bool _wasInIdleState;
        private bool _idleSnapPending;
        private double _scheduledIdleBeat;

        private const float MinimumAttackSpeed = 0.05f;
        private const float AnimatorSpeedChangeThreshold = 0.0001f;

        // ============================================================
        // UNITY LIFECYCLE
        // ============================================================

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _healthModule = GetComponent<HealthModule>();

            if (!playerCore)
                playerCore = GetComponent<PlayerCore>();

            if (!MyAnimator)
                MyAnimator = GetComponentInChildren<Animator>(true);

            _idleStateHash = Animator.StringToHash(_idleStateFullPath);
        }

        private void OnEnable()
        {
            if (!MyAnimator || !playerCore)
            {
                Debug.LogError("[AcCharacterHandler] Animator or PlayerCore reference is missing.", this);
                return;
            }

            _idleStateHash = Animator.StringToHash(_idleStateFullPath);

            _attackSpeedParameterPresent = HasFloatParameter(_attackSpeedParameter);
            _idleSpeedParameterPresent = HasFloatParameter(_idleSpeedParameter);

            _warnedAnimatorParameter = false;
            _warnedAnimatorSpeed = false;
            _warnedFrozenAttack = false;
            _warnedThirdTrigger = false;
            _warnedIdleSpeedParameter = false;

            _lastIdleSpeed = -1f;

            _wasInIdleState = false;
            _idleSnapPending = false;

            UpdateIdleAnimationSpeed();
            UpdateAttackAnimationSpeed();
            UpdateHeavyAttackAnimationSpeed();

            ResetComboChain();

            _nextAllowedBeat = 0.0;
            _nextHeavyAllowedBeat = 0.0;
            _heavyFinishBeat = 0.0;

            _pendingHeavyAttack = false;
            _pendingAttack = false;

            _currentAttackIsHeavy = false;
            _currentAttackWasRhythmSnapped = false;

            playerCore.OnPlayerAtkL += AtkLComboManager;
            playerCore.OnPlayerAtkH += AtkHManager;

            _ownedHurtboxes.Clear();
            GetComponentsInChildren(true, _ownedHurtboxes);

            foreach (HurtboxEvent hurtbox in _ownedHurtboxes)
                if (hurtbox)
                    hurtbox.OnHitConfirmed += HandleAttackHitConfirmed;

            if (_healthModule)
                _healthModule.OnStunChanged += HandleStunChanged;
        }

        private void OnDisable()
        {
            if (playerCore)
            {
                playerCore.OnPlayerAtkL -= AtkLComboManager;
                playerCore.OnPlayerAtkH -= AtkHManager;
            }

            if (_healthModule)
                _healthModule.OnStunChanged -= HandleStunChanged;

            foreach (HurtboxEvent hurtbox in _ownedHurtboxes)
                if (hurtbox)
                    hurtbox.OnHitConfirmed -= HandleAttackHitConfirmed;

            _ownedHurtboxes.Clear();

            ResetComboChain();

            _pendingHeavyAttack = false;
            _pendingAttack = false;

            _currentAttackIsHeavy = false;
            _currentAttackWasRhythmSnapped = false;

            _wasInIdleState = false;
            _idleSnapPending = false;

            StopAttackStemRoutine();
            _lightStemHeldForCombo = false;

            RhythmManager rhythm = RhythmManager.Instance;

            if (rhythm &&
                rhythm.CurrentAudioMode == RhythmManager.AudioMode.StemSystem)
            {
                rhythm.SetStems(
                    (_lightAttackStemId, false),
                    (_heavyAttackStemId, false)
                );
            }
        }

        private void OnDestroy()
        {
            // Event subscriptions are released in OnDisable, including when destroyed.
        }

        private void Start()
        {
            if (!_characterController)
                _characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            // Keep the Idle speed updated in case the BPM changes during gameplay.
            UpdateIdleAnimationSpeed();

            if (MyAnimator && _attackSpeedParameterPresent &&
                MyAnimator.GetFloat(_attackSpeedParameter) <= 0.0001f)
                UpdateAttackAnimationSpeed();

            if (MyAnimator && MyAnimator.speed <= 0f && !_warnedAnimatorSpeed)
            {
                Debug.LogWarning("[AcCharacterHandler] Animator.speed is zero. Attack animations cannot advance.", this);
                _warnedAnimatorSpeed = true;
            }

            // All beat-based gameplay waits for the central DSP clock.
            // There is deliberately no local BPM or elapsed-time fallback.
            if (!RhythmReady())
                return;

            double now = BeatNow();

            // Synchronize the beginning of each Idle entry with beat 0.
            UpdateIdleBeatSnapping(now);

            DiscardExpiredInputs(now);

            if (_pendingHeavyAttack && now + 1e-6 >= _scheduledHeavyBeat)
            {
                bool wasSnapped = _pendingHeavyWasRhythmSnapped;

                _pendingHeavyAttack = false;
                _pendingHeavyWasRhythmSnapped = false;

                if (CanLaunchHeavy(now))
                    LaunchHeavyAttack(now, wasSnapped);
            }

            if (_pendingAttack && now + 1e-6 >= _scheduledBeat)
            {
                int step = _pendingStep;
                bool wasSnapped = _pendingAttackWasRhythmSnapped;

                _pendingAttack = false;
                _pendingStep = 0;
                _pendingAttackWasRhythmSnapped = false;

                if (CanLaunchStep(step, now))
                    LaunchAttack(now, step, wasSnapped);
            }

            if (_comboCounter == 0)
                return;

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

        // ============================================================
        // RHYTHM CLOCK
        // ============================================================

        /// <summary>
        /// RhythmManager is the only source of musical time.
        /// This method is only used after RhythmReady() has been checked.
        /// </summary>
        private double BeatNow()
        {
            RhythmManager clock = RhythmManager.Instance;
            return clock && clock.IsReady ? clock.CurrentBeat : 0.0;
        }

        private bool RhythmReady()
        {
            RhythmManager clock = RhythmManager.Instance;
            return clock && clock.IsReady;
        }

        /// <summary>
        /// Returns the duration of one beat from the central RhythmManager.
        /// Returns zero while the clock is unavailable; callers must check readiness.
        /// </summary>
        private double GetSecondsPerBeat()
        {
            RhythmManager clock = RhythmManager.Instance;
            return clock && clock.IsReady ? clock.SecondsPerBeat : 0.0;
        }

        // ============================================================
        // IDLE ANIMATION BPM SYNCHRONIZATION
        // ============================================================

        /// <summary>
        /// Calculates the Idle animation speed so that one complete loop
        /// lasts exactly the configured number of musical beats.
        /// </summary>
        private void UpdateIdleAnimationSpeed()
        {
            if (!MyAnimator)
                return;

            if (!HasFloatParameter(_idleSpeedParameter))
            {
                _idleSpeedParameterPresent = false;

                if (_syncIdleAnimationToBpm && !_warnedIdleSpeedParameter)
                {
                    Debug.LogWarning(
                        $"[AcCharacterHandler] Animator Float '{_idleSpeedParameter}' is missing. " +
                        "Add it and assign it to the Idle state's Speed Multiplier.",
                        this);

                    _warnedIdleSpeedParameter = true;
                }

                return;
            }

            _idleSpeedParameterPresent = true;

            float speed = 1f;

            if (_syncIdleAnimationToBpm)
            {
                if (!RhythmReady())
                    return;

                double secondsPerBeat = GetSecondsPerBeat();

                if (secondsPerBeat <= 0.0001 || MyAnimator.speed <= 0.0001f)
                    return;

                float naturalDuration = _idleClipFrames / _idleClipFPS;

                float targetDuration = (float)(
                    _idleLoopDurationBeats * secondsPerBeat);

                if (targetDuration <= 0.0001f)
                    return;

                // Account for Animator.speed as well as the state multiplier.
                speed = naturalDuration / (targetDuration * MyAnimator.speed);
            }

            if (float.IsNaN(speed) || float.IsInfinity(speed))
                speed = 1f;

            speed = Mathf.Max(MinimumAttackSpeed, speed);

            if (Mathf.Abs(speed - _lastIdleSpeed) <= AnimatorSpeedChangeThreshold)
                return;

            MyAnimator.SetFloat(_idleSpeedParameter, speed);
            _lastIdleSpeed = speed;

            if (_debugEnabled)
            {
                double secondsPerBeat = GetSecondsPerBeat();

                Debug.Log(
                    $"[AcCharacterHandler] Idle synchronized | " +
                    $"BPM: {60.0 / secondsPerBeat:F1} | " +
                    $"Clip: {_idleClipFrames} frames at {_idleClipFPS:F1} FPS | " +
                    $"Loop: {_idleLoopDurationBeats:F2} beat(s) | " +
                    $"Speed Multiplier: {speed:F3}.",
                    this);
            }
        }

        // ============================================================
        // IDLE BEAT SNAPPING
        // ============================================================

        /// <summary>
        /// Detects when the Animator enters Idle.
        /// Schedules a restart of the Idle animation at the next full beat.
        ///
        /// If the character leaves Idle before that beat, the scheduled
        /// restart is cancelled.
        /// </summary>
        private void UpdateIdleBeatSnapping(double now)
        {
            if (!MyAnimator || !_snapIdleStartToBeat)
            {
                _wasInIdleState = false;
                _idleSnapPending = false;
                return;
            }

            if (string.IsNullOrEmpty(_idleStateFullPath))
                return;

            AnimatorStateInfo currentState = MyAnimator.GetCurrentAnimatorStateInfo(
                _idleAnimatorLayer);

            bool isIdle = currentState.IsName(_idleStateFullPath);

            // Also consider the destination state during a transition.
            if (MyAnimator.IsInTransition(_idleAnimatorLayer))
            {
                AnimatorStateInfo nextState = MyAnimator.GetNextAnimatorStateInfo(
                    _idleAnimatorLayer);

                if (nextState.IsName(_idleStateFullPath))
                    isIdle = true;
            }

            // We have left Idle: cancel any pending restart.
            if (!isIdle)
            {
                _wasInIdleState = false;
                _idleSnapPending = false;
                return;
            }

            // Detect a new entry into Idle.
            if (!_wasInIdleState)
            {
                _wasInIdleState = true;

                // Find the next integer beat.
                double nextBeat = Math.Ceiling(now - 1e-6);

                _scheduledIdleBeat = Math.Max(now, nextBeat);
                _idleSnapPending = true;

                if (_debugEnabled)
                {
                    Debug.Log(
                        $"[AcCharacterHandler] Idle entered | " +
                        $"Waiting for beat {_scheduledIdleBeat:F3} to start the animation.",
                        this);
                }
            }

            if (!_idleSnapPending)
                return;

            if (now + 1e-6 < _scheduledIdleBeat)
                return;

            // Restart Idle at normalized time 0 on the scheduled beat.
            MyAnimator.Play(
                _idleStateHash,
                _idleAnimatorLayer,
                0f);

            _idleSnapPending = false;

            if (_debugEnabled)
            {
                Debug.Log(
                    $"[AcCharacterHandler] Idle Beat Snap executed | " +
                    $"Beat: {now:F3} | Animation restarted at frame 0.",
                    this);
            }
        }

        // ============================================================
        // ATTACK FRAME CONFIGURATION
        // ============================================================

        private int GetAttackHitFrame(int step, bool heavy)
        {
            if (heavy)
                return _heavyAttackHitFrame;

            return step switch
            {
                1 => _lightAttack01HitFrame,
                2 => _lightAttack02HitFrame,
                3 => _lightAttack03HitFrame,
                _ => _lightAttack01HitFrame
            };
        }

        private int GetAttackSyncFrame(int step, bool heavy)
        {
            if (heavy)
                return _heavyAttack01SyncFrame;

            return step switch
            {
                1 => _lightAttack01SyncFrame,
                2 => _lightAttack02SyncFrame,
                3 => _lightAttack03SyncFrame,
                _ => _lightAttack01SyncFrame
            };
        }

        private double GetAttackSyncOffsetBeats(int step, bool heavy)
        {
            int syncFrame = GetAttackSyncFrame(step, heavy);

            if (heavy)
            {
                return (double)syncFrame /
                       Math.Max(1, _heavyAttackClipFrames) *
                       _heavyAttackDurationBeats;
            }

            return (double)syncFrame /
                   Math.Max(1, _attackClipFrames) *
                   _attackDurationBeats;
        }

        private double GetAttackHitOffsetBeats(int step, bool heavy)
        {
            if (heavy)
            {
                return (double)_heavyAttackHitFrame /
                       Math.Max(1, _heavyAttackClipFrames) *
                       _heavyAttackDurationBeats;
            }

            return (double)GetAttackHitFrame(step, false) /
                   Math.Max(1, _attackClipFrames) *
                   _attackDurationBeats;
        }

        // ============================================================
        // BEAT SNAPPING
        // ============================================================

        private bool TryScheduleRhythmSnap(
            double earliestStartBeat,
            float everyBeats,
            float thresholdBeats,
            double syncOffsetBeats,
            out double scheduledStartBeat)
        {
            scheduledStartBeat = earliestStartBeat;

            if (!RhythmReady())
                return false;

            if (everyBeats <= 0f || thresholdBeats < 0f)
                return false;

            double theoreticalSyncBeat = earliestStartBeat + syncOffsetBeats;

            double gridIndex = Math.Ceiling(
                theoreticalSyncBeat / everyBeats - 1e-6);

            double targetSyncBeat = gridIndex * everyBeats;
            double delayBeats = targetSyncBeat - theoreticalSyncBeat;

            if (delayBeats > thresholdBeats + 1e-6)
            {
                if (_debugEnabled)
                {
                    Debug.Log(
                        $"[AcCharacterHandler] Snap rejected | " +
                        $"Distance: {delayBeats:F3} beats | " +
                        $"Threshold: {thresholdBeats:F3} beats.",
                        this);
                }

                return false;
            }

            scheduledStartBeat = targetSyncBeat - syncOffsetBeats;
            scheduledStartBeat = Math.Max(earliestStartBeat, scheduledStartBeat);

            if (_debugEnabled)
            {
                Debug.Log(
                    $"[AcCharacterHandler] Snap accepted | " +
                    $"Sync frame: {targetSyncBeat:F3} | " +
                    $"Distance: {delayBeats:F3} beats | " +
                    $"Threshold: {thresholdBeats:F3} beats | " +
                    $"Animation start: {scheduledStartBeat:F3}.",
                    this);
            }

            return true;
        }

        // ============================================================
        // HEAVY ATTACK
        // ============================================================

        private bool CanLaunchHeavy(double now) =>
            RhythmReady() &&
            MyAnimator &&
            HasTriggerParameter(HeavyAttack01) &&
            (!_healthModule || (!_healthModule.IsStunned && _healthModule.HP > 0)) &&
            now + 1e-6 >= _nextHeavyAllowedBeat;

        private void AtkHManager()
        {
            if (!RhythmReady())
                return;

            if (!MyAnimator || (_healthModule && (_healthModule.IsStunned || _healthModule.HP <= 0)))
                return;

            if (_pendingHeavyAttack)
                return;

            double now = BeatNow();
            double earliest = Math.Max(now, _nextHeavyAllowedBeat);

            if (earliest > now + 1e-6 && earliest - now > _inputBufferBeats)
                return;

            double scheduled = earliest;
            bool wasSnapped = false;

            if (_enableHeavyBeatSnapping)
            {
                double offset = _heavySnapToSyncFrame
                    ? GetAttackSyncOffsetBeats(1, true)
                    : 0.0;

                wasSnapped = TryScheduleRhythmSnap(
                    earliest,
                    _heavySnapEveryBeats,
                    _heavyMaxSnapDelayBeats,
                    offset,
                    out scheduled);
            }

            if (_debugEnabled)
            {
                Debug.Log(
                    $"[AcCharacterHandler] Heavy input | " +
                    $"{(wasSnapped ? "IN RHYTHM - SNAPPED" : "OFF RHYTHM - NO SNAP")} | " +
                    $"Scheduled beat: {scheduled:F3}.",
                    this);
            }

            if (scheduled <= now + 1e-6)
            {
                LaunchHeavyAttack(now, wasSnapped);
            }
            else
            {
                _scheduledHeavyBeat = scheduled;
                _pendingHeavyAttack = true;
                _pendingHeavyWasRhythmSnapped = wasSnapped;

                if (_debugEnabled)
                    Debug.Log($"[AcCharacterHandler] HeavyAttack01 queued for beat {scheduled:F3}.", this);
            }
        }

        private void LaunchHeavyAttack(double now, bool wasRhythmSnapped)
        {
            if (!RhythmReady() || !CanLaunchHeavy(now))
                return;

            ResetComboChain();

            _currentAttackIsHeavy = true;
            _currentAttackWasRhythmSnapped = wasRhythmSnapped;

            UpdateHeavyAttackAnimationSpeed();

            double beatSeconds = GetSecondsPerBeat();

            if (beatSeconds <= 0.0001)
                return;

            float firstHitDelaySeconds = (float)(
                GetAttackHitOffsetBeats(1, true) * beatSeconds);

            float damageMultiplier = wasRhythmSnapped
                ? _onBeatDamageMultiplier
                : 1f;

            foreach (HurtboxEvent hurtbox in _ownedHurtboxes)
            {
                if (!hurtbox)
                    continue;

                hurtbox.PrepareAttackStep(
                    1f,
                    firstHitDelaySeconds,
                    0,
                    0f,
                    damageMultiplier);
            }

            MyAnimator.ResetTrigger(LightAttack01);
            MyAnimator.ResetTrigger(LightAttack02);

            if (HasTriggerParameter(LightAttack03))
                MyAnimator.ResetTrigger(LightAttack03);

            MyAnimator.ResetTrigger(HeavyAttack01);
            MyAnimator.SetTrigger(HeavyAttack01);

            _nextHeavyAllowedBeat = now + Math.Max(
                _heavyAttackDurationBeats,
                _heavyAttackCooldownBeats);

            _heavyFinishBeat = now + _heavyAttackDurationBeats;
            _nextAllowedBeat = Math.Max(_nextAllowedBeat, _heavyFinishBeat);

            if (_debugEnabled)
            {
                Debug.Log(
                    $"[AcCharacterHandler] HeavyAttack01 START | " +
                    $"{(wasRhythmSnapped ? "IN RHYTHM" : "OFF RHYTHM")} | " +
                    $"Sync frame: {_heavyAttack01SyncFrame} | " +
                    $"Impact frame: {_heavyAttackHitFrame} | " +
                    $"Damage multiplier: x{damageMultiplier:F2}.",
                    this);
            }
        }

        private void UpdateHeavyAttackAnimationSpeed()
        {
            if (!MyAnimator)
                return;

            if (!HasFloatParameter(_heavyAttackSpeedParameter))
            {
                if (_syncAttackAnimationSpeed)
                    Debug.LogWarning(
                        $"[AcCharacterHandler] Animator Float '{_heavyAttackSpeedParameter}' missing. HeavyAttack01 will keep its current speed.",
                        this);

                return;
            }

            float speed = 1f;

            if (_syncAttackAnimationSpeed)
            {
                if (!RhythmReady())
                    return;

                double beatSeconds = GetSecondsPerBeat();

                if (beatSeconds <= 0.0001)
                    return;

                speed = (float)(
                    (_heavyAttackClipFrames / _heavyAttackClipFPS) /
                    (Math.Max(0.1, _heavyAttackDurationBeats) * beatSeconds));
            }

            if (float.IsNaN(speed) || float.IsInfinity(speed))
                speed = 1f;

            MyAnimator.SetFloat(
                _heavyAttackSpeedParameter,
                Mathf.Max(MinimumAttackSpeed, speed));
        }

        // ============================================================
        // LIGHT ATTACK INPUT
        // ============================================================

        private void AtkLComboManager()
        {
            if (!RhythmReady())
                return;

            if (!MyAnimator || (_healthModule && (_healthModule.IsStunned || _healthModule.HP <= 0)))
                return;

            double now = BeatNow();

            if (_pendingHeavyAttack || now < _heavyFinishBeat)
                return;

            if (_comboCounter != 0 && now > _comboExpireBeat)
                ResetComboChain();

            if (_enableHitConfirmCombo)
            {
                if (_comboCounter == 3 || (_pendingAttack && _pendingStep == 3))
                    return;

                if (_comboCounter > 0 || _pendingAttack)
                {
                    if (_bufferedFollowupInputs.Count < 2)
                    {
                        _bufferedFollowupInputs.Enqueue(now);

                        if (_debugEnabled)
                        {
                            Debug.Log(
                                $"[AcCharacterHandler] Combo input buffered ({_bufferedFollowupInputs.Count}/2).",
                                this);
                        }
                    }

                    TryScheduleBufferedFollowup(now);
                    return;
                }
            }
            else if (_pendingAttack)
            {
                return;
            }

            int nextStep = !_enableHitConfirmCombo &&
                           _comboCounter >= 1 &&
                           _comboCounter < 3
                ? _comboCounter + 1
                : 1;

            double earliest = Math.Max(now, _nextAllowedBeat);

            if (earliest > now + 1e-6 && earliest - now > _inputBufferBeats)
                return;

            double scheduled = earliest;
            bool wasSnapped = false;

            if (_enableBeatSnapping)
            {
                double offset = _snapToSyncFrame
                    ? GetAttackSyncOffsetBeats(nextStep, false)
                    : 0.0;

                wasSnapped = TryScheduleRhythmSnap(
                    earliest,
                    _snapEveryBeats,
                    _maxSnapDelayBeats,
                    offset,
                    out scheduled);
            }

            if (_debugEnabled)
            {
                Debug.Log(
                    $"[AcCharacterHandler] Light 0{nextStep} input | " +
                    $"{(wasSnapped ? "IN RHYTHM - SNAPPED" : "OFF RHYTHM - NO SNAP")} | " +
                    $"Scheduled beat: {scheduled:F3}.",
                    this);
            }

            QueueAttack(scheduled, nextStep, now, wasSnapped);
        }

        // ============================================================
        // HIT CONFIRM
        // ============================================================

        private void HandleAttackHitConfirmed(GameObject target)
        {
            // The VFX is spawned here, not in HurtboxEvent.
            // OnHitConfirmed is only raised after actual damage was applied.
            if (_currentAttackWasRhythmSnapped &&
                _onBeatHitVfx &&
                target)
            {
                Quaternion rotation = _onBeatVfxUseTargetRotation
                    ? target.transform.rotation
                    : Quaternion.identity;

                Instantiate(
                    _onBeatHitVfx,
                    target.transform.position + _onBeatHitVfxOffset,
                    rotation);
            }

            // Activate the corresponding stem on every successful hit,
            // regardless of whether the attack was rhythm-snapped.
            ActivateAttackStem(_currentAttackIsHeavy);

            if (!RhythmReady())
                return;

            if (!_enableHitConfirmCombo || _pendingAttack ||
                _comboCounter < 1 || _comboCounter >= 3 || _hitConfirmed)
                return;

            double now = BeatNow();

            if (now > _attackFinishBeat + 0.05)
                return;

            _hitConfirmed = true;

            if (_debugEnabled)
            {
                Debug.Log(
                    $"[AcCharacterHandler] Attack 0{_comboCounter} hit " +
                    $"{(target ? target.name : "target")}. Follow-up unlocked.",
                    this);
            }

            TryScheduleBufferedFollowup(now);
        }

        // ============================================================
        // ATTACK STEMS
        // ============================================================

        private void ActivateAttackStem(bool heavy)
        {
            RhythmManager rhythm = RhythmManager.Instance;

            if (!rhythm ||
                rhythm.CurrentAudioMode != RhythmManager.AudioMode.StemSystem ||
                !rhythm.IsReady)
                return;

            string activeStem = heavy ? _heavyAttackStemId : _lightAttackStemId;
            string inactiveStem = heavy ? _lightAttackStemId : _heavyAttackStemId;

            // Heavy attacks always use their own independent activation.
            if (heavy)
            {
                StopAttackStemRoutine();

                rhythm.SetStems(
                    (inactiveStem, false),
                    (activeStem, true)
                );

                StartStemOffTimer(
                    rhythm,
                    activeStem,
                    _heavyStemDurationBeats
                );

                if (_debugEnabled)
                    Debug.Log(
                        $"[AcCharacterHandler] HEAVY hit confirmed. " +
                        $"{activeStem} activated for {_heavyStemDurationBeats:F2} beat(s).",
                        this);

                return;
            }

            // Light attacks 01 and 02 keep Guitar_S active during the combo.
            bool holdForCombo =
                _enableHitConfirmCombo &&
                (_comboCounter == 1 || _comboCounter == 2);

            // The stem is already active and held: do not restart it.
            if (_lightStemHeldForCombo)
            {
                if (holdForCombo)
                {
                    if (_debugEnabled)
                        Debug.Log(
                            $"[AcCharacterHandler] {activeStem} remains active during combo step {_comboCounter}.",
                            this);

                    return;
                }

                // Combo finisher: release the stem after its configured duration.
                _lightStemHeldForCombo = false;

                StopAttackStemRoutine();

                StartStemOffTimer(
                    rhythm,
                    activeStem,
                    _lightStemDurationBeats
                );

                if (_debugEnabled)
                    Debug.Log(
                        $"[AcCharacterHandler] Combo finisher. {activeStem} will deactivate after {_lightStemDurationBeats:F2} beat(s).",
                        this);

                return;
            }

            // First activation of the Light combo.
            StopAttackStemRoutine();

            rhythm.SetStems(
                (inactiveStem, false),
                (activeStem, true)
            );

            if (holdForCombo)
            {
                _lightStemHeldForCombo = true;

                if (_debugEnabled)
                    Debug.Log(
                        $"[AcCharacterHandler] {activeStem} activated and held for combo.",
                        this);

                return;
            }

            // Standalone Light hit or combo without hit-confirm logic.
            StartStemOffTimer(
                rhythm,
                activeStem,
                _lightStemDurationBeats
            );

            if (_debugEnabled)
                Debug.Log(
                    $"[AcCharacterHandler] LIGHT hit confirmed. {activeStem} activated for {_lightStemDurationBeats:F2} beat(s).",
                    this);
        }

        private void StartStemOffTimer(
            RhythmManager rhythm,
            string stemId,
            double durationBeats)
        {
            if (!rhythm || !rhythm.IsReady)
                return;

            _stemActivationVersion++;

            _activeStemOffRoutine = StartCoroutine(
                DisableStemAfterBeats(
                    rhythm,
                    stemId,
                    durationBeats,
                    _stemActivationVersion
                )
            );
        }

        private void ReleaseComboStem()
        {
            if (!_lightStemHeldForCombo)
                return;

            _lightStemHeldForCombo = false;

            if (!isActiveAndEnabled)
                return;

            RhythmManager rhythm = RhythmManager.Instance;

            if (!rhythm ||
                rhythm.CurrentAudioMode != RhythmManager.AudioMode.StemSystem ||
                !rhythm.IsReady)
                return;

            StopAttackStemRoutine();

            StartStemOffTimer(
                rhythm,
                _lightAttackStemId,
                _lightStemDurationBeats
            );

            if (_debugEnabled)
                Debug.Log(
                    $"[AcCharacterHandler] Combo ended. {_lightAttackStemId} will deactivate after {_lightStemDurationBeats:F2} beat(s).",
                    this);
        }

        private IEnumerator DisableStemAfterBeats(
            RhythmManager rhythm,
            string stemId,
            double durationBeats,
            int activationVersion)
        {
            double endBeat = rhythm.CurrentBeat + durationBeats;

            while (rhythm && activationVersion == _stemActivationVersion)
            {
                if (rhythm.IsReady && rhythm.CurrentBeat >= endBeat)
                    break;

                yield return null;
            }

            if (activationVersion != _stemActivationVersion)
                yield break;

            if (rhythm &&
                rhythm.CurrentAudioMode == RhythmManager.AudioMode.StemSystem)
            {
                rhythm.SetStem(stemId, false);

                if (_debugEnabled)
                    Debug.Log($"[AcCharacterHandler] {stemId} deactivated.", this);
            }

            _activeStemOffRoutine = null;
        }

        private void StopAttackStemRoutine()
        {
            _stemActivationVersion++;

            if (_activeStemOffRoutine != null)
            {
                StopCoroutine(_activeStemOffRoutine);
                _activeStemOffRoutine = null;
            }
        }

        // ============================================================
        // COMBO FOLLOW-UP SCHEDULING
        // ============================================================

        private void TryScheduleBufferedFollowup(double now)
        {
            if (!RhythmReady())
                return;

            if (!_enableHitConfirmCombo || _pendingAttack ||
                !_hitConfirmed || _comboCounter < 1 || _comboCounter >= 3 ||
                now > _comboExpireBeat)
                return;

            DiscardExpiredInputs(now);

            if (_bufferedFollowupInputs.Count == 0)
                return;

            _bufferedFollowupInputs.Dequeue();

            int nextStep = _comboCounter + 1;

            double scheduled = now;
            bool wasSnapped = false;

            if (_enableBeatSnapping)
            {
                double offset = _snapToSyncFrame
                    ? GetAttackSyncOffsetBeats(nextStep, false)
                    : 0.0;

                wasSnapped = TryScheduleRhythmSnap(
                    now,
                    _comboEveryBeats,
                    _maxSnapDelayBeats,
                    offset,
                    out scheduled);
            }

            if (scheduled > _comboExpireBeat + 1e-6)
                return;

            if (_debugEnabled)
            {
                Debug.Log(
                    $"[AcCharacterHandler] Follow-up 0{nextStep} | " +
                    $"{(wasSnapped ? "IN RHYTHM - SNAPPED" : "OFF RHYTHM - NO SNAP")}.",
                    this);
            }

            QueueAttack(scheduled, nextStep, now, wasSnapped);
        }

        private void DiscardExpiredInputs(double now)
        {
            while (_bufferedFollowupInputs.Count > 0 &&
                   now - _bufferedFollowupInputs.Peek() > _comboInputBufferBeats)
                _bufferedFollowupInputs.Dequeue();
        }

        // ============================================================
        // ATTACK LAUNCHING
        // ============================================================

        private bool CanLaunchStep(int step, double now)
        {
            if (!RhythmReady() ||
                !MyAnimator ||
                (_healthModule && (_healthModule.IsStunned || _healthModule.HP <= 0)))
                return false;

            if (step == 1)
                return now + 1e-6 >= _nextAllowedBeat;

            if (step < 2 || step > 3 ||
                _comboCounter != step - 1 ||
                now > _comboExpireBeat)
                return false;

            return !_enableHitConfirmCombo || _hitConfirmed;
        }

        private void QueueAttack(
            double scheduled,
            int step,
            double now,
            bool wasRhythmSnapped)
        {
            if (!RhythmReady())
                return;

            _scheduledBeat = scheduled;
            _pendingStep = step;
            _pendingAttack = true;
            _pendingAttackWasRhythmSnapped = wasRhythmSnapped;

            if (_debugEnabled)
            {
                Debug.Log(
                    $"[AcCharacterHandler] Attack 0{step} queued for beat {scheduled:F3} " +
                    $"| {(wasRhythmSnapped ? "SNAPPED" : "NOT SNAPPED")}.",
                    this);
            }

            if (scheduled <= now + 1e-6)
            {
                _pendingAttack = false;
                _pendingStep = 0;
                _pendingAttackWasRhythmSnapped = false;

                if (CanLaunchStep(step, now))
                    LaunchAttack(now, step, wasRhythmSnapped);
            }
        }

        private void LaunchAttack(double now, int step, bool wasRhythmSnapped)
        {
            if (!RhythmReady() || !MyAnimator)
                return;

            if (step == 3 && !HasTriggerParameter(LightAttack03))
            {
                if (!_warnedThirdTrigger)
                {
                    Debug.LogError(
                        "[AcCharacterHandler] Animator Trigger 'LightAttack03' is missing. Create the third attack state and trigger.",
                        this);

                    _warnedThirdTrigger = true;
                }

                ResetComboChain();
                return;
            }

            _currentAttackIsHeavy = false;
            _currentAttackWasRhythmSnapped = wasRhythmSnapped;

            UpdateAttackAnimationSpeed();

            double beatSeconds = GetSecondsPerBeat();

            if (beatSeconds <= 0.0001)
                return;

            int trigger = step switch
            {
                2 => LightAttack02,
                3 => LightAttack03,
                _ => LightAttack01
            };

            float firstHitDelaySeconds = (float)(
                GetAttackHitOffsetBeats(step, false) * beatSeconds);

            // This multiplier affects enemy knockback only.
            float knockbackMultiplier = step == 3
                ? _thirdHitKnockbackMultiplier
                : 1f;

            // The On-Beat damage reward is separate from the knockback multiplier.
            float damageMultiplier = wasRhythmSnapped
                ? _onBeatDamageMultiplier
                : 1f;

            foreach (HurtboxEvent hurtbox in _ownedHurtboxes)
            {
                if (!hurtbox)
                    continue;

                hurtbox.PrepareAttackStep(
                    knockbackMultiplier,
                    firstHitDelaySeconds,
                    step,
                    _delayComboKnockback && _enableHitConfirmCombo
                        ? _comboInputBufferBeats
                        : 0f,
                    damageMultiplier);
            }

            MyAnimator.ResetTrigger(LightAttack01);
            MyAnimator.ResetTrigger(LightAttack02);

            if (HasTriggerParameter(LightAttack03))
                MyAnimator.ResetTrigger(LightAttack03);

            MyAnimator.SetTrigger(trigger);

            _comboCounter = step;
            _hitConfirmed = false;

            _attackStartBeat = now;
            _attackFinishBeat = now + _attackDurationBeats;

            _nextAllowedBeat = now + Math.Max(
                _attackDurationBeats,
                _attackCooldownBeats);

            _comboExpireBeat = now + _comboResetBeats;

            if (_debugEnabled)
            {
                Debug.Log(
                    $"[AcCharacterHandler] Attack 0{step} START | " +
                    $"{(wasRhythmSnapped ? "IN RHYTHM" : "OFF RHYTHM")} | " +
                    $"Sync frame: {GetAttackSyncFrame(step, false)} | " +
                    $"Impact frame: {GetAttackHitFrame(step, false)} | " +
                    $"Enemy knockback x{knockbackMultiplier:F1} | " +
                    $"Damage x{damageMultiplier:F2}.",
                    this);
            }
        }

        private void ResetComboChain()
        {
            ReleaseComboStem();

            _comboCounter = 0;
            _hitConfirmed = false;

            _pendingAttack = false;
            _pendingStep = 0;
            _pendingAttackWasRhythmSnapped = false;

            _bufferedFollowupInputs.Clear();

            foreach (HurtboxEvent hurtbox in _ownedHurtboxes)
                if (hurtbox)
                    hurtbox.SetEnemyKnockbackMultiplier(1f);
        }

        // ============================================================
        // ANIMATION SPEED
        // ============================================================

        private void UpdateAttackAnimationSpeed()
        {
            if (!MyAnimator)
                return;

            _attackSpeedParameterPresent = HasFloatParameter(_attackSpeedParameter);

            if (!_attackSpeedParameterPresent)
            {
                if (_syncAttackAnimationSpeed && !_warnedAnimatorParameter)
                {
                    Debug.LogWarning(
                        "[AcCharacterHandler] Add an Animator float '" +
                        _attackSpeedParameter +
                        "' and assign it to the three light attack states' Speed Multiplier.",
                        this);

                    _warnedAnimatorParameter = true;
                }

                return;
            }

            float speed = 1f;

            if (_syncAttackAnimationSpeed)
            {
                if (!RhythmReady())
                    return;

                double secondsPerBeat = GetSecondsPerBeat();

                if (secondsPerBeat <= 0.0001)
                    return;

                float naturalDuration = _attackClipFrames / _attackClipFPS;

                speed = (float)(
                    naturalDuration /
                    (Math.Max(0.1, _attackDurationBeats) * secondsPerBeat));
            }

            if (float.IsNaN(speed) || float.IsInfinity(speed))
                speed = 1f;

            MyAnimator.SetFloat(
                _attackSpeedParameter,
                Mathf.Max(MinimumAttackSpeed, speed));
        }

        // ============================================================
        // ANIMATOR VALIDATION
        // ============================================================

        private void LateUpdate()
        {
            if (!_debugEnabled || !MyAnimator || _warnedFrozenAttack)
                return;

            AnimatorStateInfo state = MyAnimator.GetCurrentAnimatorStateInfo(0);

            if (!state.IsName("LightAttack01") &&
                !state.IsName("LightAttack02") &&
                !state.IsName("LightAttack03") &&
                !state.IsName("LightAttack1") &&
                !state.IsName("LightAttack2") &&
                !state.IsName("LightAttack3"))
                return;

            if (state.speed * state.speedMultiplier * MyAnimator.speed > 0.0001f)
                return;

            _warnedFrozenAttack = true;

            Debug.LogError(
                "[AcCharacterHandler] Attack animation is frozen. Set state Speed=1 and Multiplier=AttackSpeed (not VerticalVelocity).",
                this);
        }

        private bool HasFloatParameter(string parameter)
        {
            if (!MyAnimator || string.IsNullOrEmpty(parameter))
                return false;

            foreach (AnimatorControllerParameter p in MyAnimator.parameters)
                if (p.name == parameter &&
                    p.type == AnimatorControllerParameterType.Float)
                    return true;

            return false;
        }

        private bool HasTriggerParameter(int hash)
        {
            if (!MyAnimator)
                return false;

            foreach (AnimatorControllerParameter p in MyAnimator.parameters)
                if (p.nameHash == hash &&
                    p.type == AnimatorControllerParameterType.Trigger)
                    return true;

            return false;
        }

        // ============================================================
        // STUN HANDLING
        // ============================================================

        private void HandleStunChanged(bool stunned)
        {
            if (stunned)
            {
                ResetComboChain();
                _pendingHeavyAttack = false;
                _pendingHeavyWasRhythmSnapped = false;
            }
        }

        // ============================================================
        // ANIMATION EVENT HELPERS
        // ============================================================

        private void HandleAtkL01Animation()
        {
            if (MyAnimator)
                MyAnimator.SetTrigger(LightAttack01);
        }

        private void HandleAtkL02Animation()
        {
            if (MyAnimator)
                MyAnimator.SetTrigger(LightAttack02);
        }

        private void HandleAtkL03Animation()
        {
            if (MyAnimator)
                MyAnimator.SetTrigger(LightAttack03);
        }

        private void HandleAtkH01Animation()
        {
            if (MyAnimator)
                MyAnimator.SetTrigger(HeavyAttack01);
        }

        private void HandleHurtAnimation()
        {
            if (MyAnimator)
                MyAnimator.SetTrigger(Hurt);
        }

        private void HandleDeathAnimation()
        {
            if (MyAnimator)
                MyAnimator.SetTrigger(Death);
        }

        // ============================================================
        // INSPECTOR VALIDATION
        // ============================================================

        private void OnValidate()
        {
            _idleClipFrames = Mathf.Max(1, _idleClipFrames);
            _idleClipFPS = Mathf.Max(1f, _idleClipFPS);
            _idleLoopDurationBeats = Mathf.Max(0.01f, _idleLoopDurationBeats);

            _idleAnimatorLayer = Mathf.Max(0, _idleAnimatorLayer);

            if (string.IsNullOrEmpty(_idleStateFullPath))
                _idleStateFullPath = "Base Layer.Idle";

            _snapEveryBeats = Mathf.Max(0.01f, _snapEveryBeats);
            _heavySnapEveryBeats = Mathf.Max(0.01f, _heavySnapEveryBeats);

            _maxSnapDelayBeats = Mathf.Max(0f, _maxSnapDelayBeats);
            _heavyMaxSnapDelayBeats = Mathf.Max(0f, _heavyMaxSnapDelayBeats);

            _lightAttack01SyncFrame = Mathf.Max(0, _lightAttack01SyncFrame);
            _lightAttack02SyncFrame = Mathf.Max(0, _lightAttack02SyncFrame);
            _lightAttack03SyncFrame = Mathf.Max(0, _lightAttack03SyncFrame);
            _heavyAttack01SyncFrame = Mathf.Max(0, _heavyAttack01SyncFrame);

            _lightAttack01HitFrame = Mathf.Max(0, _lightAttack01HitFrame);
            _lightAttack02HitFrame = Mathf.Max(0, _lightAttack02HitFrame);
            _lightAttack03HitFrame = Mathf.Max(0, _lightAttack03HitFrame);
            _heavyAttackHitFrame = Mathf.Max(0, _heavyAttackHitFrame);

            _attackDurationBeats = Mathf.Max(0.1f, _attackDurationBeats);
            _heavyAttackDurationBeats = Mathf.Max(0.1f, _heavyAttackDurationBeats);

            _attackClipFrames = Mathf.Max(1, _attackClipFrames);
            _heavyAttackClipFrames = Mathf.Max(1, _heavyAttackClipFrames);

            _attackClipFPS = Mathf.Max(1f, _attackClipFPS);
            _heavyAttackClipFPS = Mathf.Max(1f, _heavyAttackClipFPS);

            _comboEveryBeats = Mathf.Max(0.01f, _comboEveryBeats);

            _thirdHitKnockbackMultiplier = Mathf.Max(1f, _thirdHitKnockbackMultiplier);

            _onBeatDamageMultiplier = Mathf.Max(1f, _onBeatDamageMultiplier);

            _lightStemDurationBeats = Mathf.Max(0.01f, _lightStemDurationBeats);
            _heavyStemDurationBeats = Mathf.Max(0.01f, _heavyStemDurationBeats);
        }
    }
}