using System;
using System.Collections.Generic;
using SynthOfRage.Scripts.Gameplay.Core;
using UnityEngine;
using UnityEngine.AI;

namespace _Dev.Christopher.Scripts
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class BrainProto : MonoBehaviour
    {
        private static readonly int Move = Animator.StringToHash("Move");
        private static readonly int LightAttack01 = Animator.StringToHash("LightAttack01");
        private static readonly int LightAttack02 = Animator.StringToHash("LightAttack02");

        private enum AttackState { Chase, WaitingForBeat, Attacking, Cooldown }

        [Header("Target")]
        [SerializeField, Min(0f)] private float _attackRange = 2f;
        [SerializeField, Min(0f)] private float _chaseResumeRange = 2.5f;

        [Header("Rhythm - Enemy Attacks")]
        [SerializeField] private bool _useRhythm = true;
        [SerializeField] private bool _enableBeatSnapping = true;
        [Tooltip("1 = one beat, 0.5 = half-beat, 2 = every two beats.")]
        [SerializeField, Min(0.01f)] private float _snapEveryBeats = 1f;
        [Tooltip("When enabled, frame 10 of the attack lands on the beat grid.")]
        [SerializeField] private bool _snapToFirstHit = true;
        [SerializeField, Min(0.1f)] private float _attackDurationBeats = 2f;
        [Tooltip("Additional recovery time, after the complete attack animation.")]
        [SerializeField, Min(0f)] private float _attackCooldownBeats = 1f;
        [SerializeField, Min(1f)] private float _fallbackBPM = 87f;

        [Header("Rhythm - Hit Confirm Combo")]
        [Tooltip("When LightAttack01 hits the player, automatically chain into LightAttack02.")]
        [SerializeField] private bool _enableHitConfirmCombo = true;
        [Tooltip("Follow-up attack waits for the next grid step (0.5 = half-beat).")]
        [SerializeField, Min(0.01f)] private float _comboEveryBeats = 0.5f;
        [Tooltip("When checked, the target must still be inside Chase Resume Range for the combo to trigger.")]
        [SerializeField] private bool _requireComboRange;

        [Header("Rhythm - Animator")]
        [SerializeField] private bool _syncAttackAnimationSpeed = true;
        [SerializeField] private string _attackSpeedParameter = "AttackSpeed";
        [SerializeField, Min(1)] private int _attackClipFrames = 60;
        [SerializeField, Min(1f)] private float _attackClipFPS = 60f;
        [SerializeField] private Color _attackColor = Color.green;
        [Tooltip("Visual window: attacks are 60 frames; both start at frame 10.")]
        [SerializeField, Range(0, 60)] private int _hitStartFrame = 10;
        [SerializeField, Range(0, 60)] private int _hitEndFrame01 = 25;
        [SerializeField, Range(0, 60)] private int _hitEndFrame02 = 30;

        [Header("References")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;

        [Header("Debug")]
        [SerializeField] private bool _debugEnabled;

        private NavMeshAgent _agent;
        private Transform _player;
        private Color _originalColor;
        private AttackState _state;
        private double _scheduledAttackBeat;
        private double _attackStartBeat;
        private double _attackFinishBeat;
        private double _cooldownFinishBeat;
        private float _nextPlayerSearchTime;
        private bool _ownsNavStop;
        private bool _currentAttackIsFirst;
        private bool _comboQueued;
        private double _comboScheduledBeat;
        private readonly List<HurtboxEvent> _ownedHurtboxes = new();
        private bool _showAttackColor;
        private bool _warnedSpeedParameter;
        private int _lastHitEndFrame;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (!_animator) _animator = GetComponentInChildren<Animator>(true);
            if (!_spriteRenderer) _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (_spriteRenderer) _originalColor = _spriteRenderer.color;
            // Hurtbox Collider is driven ONLY by the Animator animation curves.
        }

        private void OnEnable()
        {
            _state = AttackState.Chase;
            _ownsNavStop = false;
            _currentAttackIsFirst = false;
            _comboQueued = false;
            _ownedHurtboxes.Clear();
            GetComponentsInChildren(true, _ownedHurtboxes);
            foreach (HurtboxEvent hurtbox in _ownedHurtboxes)
                if (hurtbox) hurtbox.OnHitConfirmed += HandleAttackHitConfirmed;
            UpdateAttackAnimationSpeed();
            _nextPlayerSearchTime = 0f;
            _showAttackColor = false;
            if (_spriteRenderer) _spriteRenderer.color = _originalColor;
        }

        private bool RhythmReady()
        {
            RhythmManager rhythm = RhythmManager.Instance;
            return _useRhythm && rhythm && rhythm.IsReady;
        }

        private bool WaitingForMusic()
        {
            RhythmManager rhythm = RhythmManager.Instance;
            return _useRhythm && rhythm && !rhythm.IsReady;
        }

        private double BeatNow()
        {
            return RhythmReady()
                ? RhythmManager.Instance.CurrentBeat
                : Time.timeAsDouble * _fallbackBPM / 60.0;
        }

        private void Update()
        {
            if (!_player && Time.time >= _nextPlayerSearchTime)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                _player = player ? player.transform : null;
                _nextPlayerSearchTime = Time.time + 0.5f;
            }

            if (!_agent || !_agent.enabled || !_agent.isOnNavMesh) return;

            if (!_player)
            {
                _state = AttackState.Chase;
                SetAttackColor(false);
                SetMovementAnimation(false);
                StopOwnNavigation();
                return;
            }

            double now = BeatNow();
            float distance = Vector3.ProjectOnPlane(_player.position - transform.position, Vector3.up).magnitude;
            float leaveRange = Mathf.Max(_attackRange, _chaseResumeRange);

            switch (_state)
            {
                case AttackState.Chase:
                    if (_agent.isStopped && !_ownsNavStop)
                    {
                        SetMovementAnimation(false); // External stun / stop owns the agent.
                        return;
                    }

                    if (_ownsNavStop) ReleaseOwnNavigation();
                    if (distance <= _attackRange && now >= _cooldownFinishBeat && !WaitingForMusic())
                    {
                        WaitForNextAttackBeat(now);
                        return;
                    }

                    _agent.stoppingDistance = _attackRange;
                    if (!_agent.isStopped) _agent.SetDestination(_player.position);
                    SetMovementAnimation(!_agent.isStopped && distance > _attackRange);
                    break;

                case AttackState.WaitingForBeat:
                    if (distance > leaveRange)
                    {
                        CancelWaiting();
                        return;
                    }

                    if (!_ownsNavStop && _agent.isStopped)
                    {
                        CancelWaiting(); // Some other component (stun) stopped movement.
                        return;
                    }

                    if (WaitingForMusic()) return;
                    if (now + 1e-6 >= _scheduledAttackBeat) BeginAttack(now);
                    break;

                case AttackState.Attacking:
                    // A landed first hit can start attack 02 on the next half-beat,
                    // even though the normal attack/cooldown period has not expired.
                    if (_comboQueued && now + 1e-6 >= _comboScheduledBeat &&
                        !WaitingForMusic() && _player &&
                        (!_requireComboRange || distance <= leaveRange))
                    {
                        BeginAttack(now, true);
                        return;
                    }

                    SetAttackColor(IsInsideHitWindow(now));
                    if (now + 1e-6 >= _attackFinishBeat)
                    {
                        SetAttackColor(false);
                        _state = AttackState.Cooldown;
                        _comboQueued = false;
                        ReleaseOwnNavigation();
                        Log("Attack ended. Cooldown started.");
                    }
                    break;

                case AttackState.Cooldown:
                    if (now >= _cooldownFinishBeat)
                    {
                        _state = AttackState.Chase;
                        if (distance <= _attackRange && !WaitingForMusic()) WaitForNextAttackBeat(now);
                    }
                    else if (distance > _attackRange)
                    {
                        // Chasing during recovery is OK; the cooldown gate is preserved.
                        ReleaseOwnNavigation();
                        _agent.stoppingDistance = _attackRange;
                        if (!_agent.isStopped) _agent.SetDestination(_player.position);
                        SetMovementAnimation(!_agent.isStopped);
                    }
                    else
                    {
                        StopOwnNavigation();
                        SetMovementAnimation(false);
                    }
                    break;
            }
        }

        private void WaitForNextAttackBeat(double now)
        {
            double impactOffset = _snapToFirstHit
                ? (double)_hitStartFrame / _attackClipFrames * _attackDurationBeats
                : 0.0;
            _scheduledAttackBeat = _enableBeatSnapping && RhythmReady()
                ? RhythmManager.QuantizeBeat(now + impactOffset, _snapEveryBeats) - impactOffset
                : now;

            _state = AttackState.WaitingForBeat;
            StopOwnNavigation();
            SetMovementAnimation(false);
            Log($"Attack planned for beat {_scheduledAttackBeat:F3}");
        }

        private void BeginAttack(double now, bool combo = false)
        {
            _state = AttackState.Attacking;
            _currentAttackIsFirst = !combo;
            _comboQueued = false;
            _attackStartBeat = now;
            _attackFinishBeat = now + _attackDurationBeats;
            _cooldownFinishBeat = _attackFinishBeat + _attackCooldownBeats;
            _lastHitEndFrame = combo ? _hitEndFrame02 : _hitEndFrame01;

            SetAttackColor(false);
            StopOwnNavigation();
            UpdateAttackAnimationSpeed();
            if (_animator)
            {
                _animator.SetBool(Move, false);
                _animator.ResetTrigger(LightAttack01);
                _animator.ResetTrigger(LightAttack02);
                _animator.SetTrigger(combo ? LightAttack02 : LightAttack01);
            }

            Log($"{(combo ? "LightAttack02 (confirmed combo)" : "LightAttack01")} triggered at beat {now:F3}");
        }

        private void HandleAttackHitConfirmed(GameObject target)
        {
            if (!_enableHitConfirmCombo || _state != AttackState.Attacking ||
                !_currentAttackIsFirst || _comboQueued || !_player ||
                !target || target.transform != _player) return;

            double now = BeatNow();
            if (now > _attackFinishBeat + 0.05) return;

            double step = Math.Max(0.01, _comboEveryBeats);
            // Strictly after the hit, so one attack cannot instantly restart on the same beat.
            _comboScheduledBeat = (Math.Floor(now / step + 1e-6) + 1.0) * step;
            _comboQueued = true;
            Log($"Confirmed hit on {target.name}; LightAttack02 planned for beat {_comboScheduledBeat:F3}");
        }

        private bool IsInsideHitWindow(double now)
        {
            double progress = (now - _attackStartBeat) / Math.Max(0.001, _attackDurationBeats);
            return progress >= (double)_hitStartFrame / _attackClipFrames &&
                   progress < (double)_lastHitEndFrame / _attackClipFrames;
        }

        private void CancelWaiting()
        {
            _state = AttackState.Chase;
            _comboQueued = false;
            ReleaseOwnNavigation();
            SetAttackColor(false);
        }

        private void StopOwnNavigation()
        {
            if (!_agent || !_agent.enabled || !_agent.isOnNavMesh) return;
            if (_agent.isStopped) return; // Do not claim someone else's stop.
            _agent.isStopped = true;
            _ownsNavStop = true;
            _agent.ResetPath();
        }

        private void ReleaseOwnNavigation()
        {
            if (!_ownsNavStop) return;
            _ownsNavStop = false;
            if (_agent && _agent.enabled && _agent.isOnNavMesh)
                _agent.isStopped = false;
        }

        private void UpdateAttackAnimationSpeed()
        {
            if (!_animator) return;
            bool found = false;
            foreach (AnimatorControllerParameter parameter in _animator.parameters)
                if (parameter.name == _attackSpeedParameter && parameter.type == AnimatorControllerParameterType.Float)
                    found = true;

            if (!found)
            {
                if (!_warnedSpeedParameter)
                {
                    Debug.LogWarning($"[BrainProto] Create the Animator float '{_attackSpeedParameter}' and bind it as a Speed Multiplier on both attack states.", this);
                    _warnedSpeedParameter = true;
                }
                return;
            }

            double beatSeconds = RhythmReady() ? RhythmManager.Instance.SecondsPerBeat : 60.0 / _fallbackBPM;
            float nativeSeconds = _attackClipFrames / _attackClipFPS;
            float speed = _syncAttackAnimationSpeed
                ? (float)(nativeSeconds / (Math.Max(0.1, _attackDurationBeats) * beatSeconds))
                : 1f;
            _animator.SetFloat(_attackSpeedParameter, Mathf.Max(0.05f, speed));
        }

        private void SetAttackColor(bool active)
        {
            if (_showAttackColor == active) return;
            _showAttackColor = active;
            if (_spriteRenderer) _spriteRenderer.color = active ? _attackColor : _originalColor;
        }

        private void SetMovementAnimation(bool moving)
        {
            if (_animator) _animator.SetBool(Move, moving);
        }

        private void Log(string message)
        {
            if (_debugEnabled) Debug.Log($"[BrainProto] {message}", this);
        }

        private void OnDisable()
        {
            foreach (HurtboxEvent hurtbox in _ownedHurtboxes)
                if (hurtbox) hurtbox.OnHitConfirmed -= HandleAttackHitConfirmed;
            _ownedHurtboxes.Clear();
            _comboQueued = false;
            SetAttackColor(false);
            ReleaseOwnNavigation();
            SetMovementAnimation(false);
        }

        private void OnValidate()
        {
            _attackRange = Mathf.Max(0f, _attackRange);
            _chaseResumeRange = Mathf.Max(_attackRange, _chaseResumeRange);
            _snapEveryBeats = Mathf.Max(0.01f, _snapEveryBeats);
            _comboEveryBeats = Mathf.Max(0.01f, _comboEveryBeats);
            _attackDurationBeats = Mathf.Max(0.1f, _attackDurationBeats);
            _attackClipFPS = Mathf.Max(1f, _attackClipFPS);
            _fallbackBPM = Mathf.Max(1f, _fallbackBPM);
            _hitEndFrame01 = Mathf.Max(_hitStartFrame, _hitEndFrame01);
            _hitEndFrame02 = Mathf.Max(_hitStartFrame, _hitEndFrame02);
        }
    }
}
