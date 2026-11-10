
using System;
using System.Collections;
using SynthOfRage.Scripts.Gameplay.Unit.Player;
using SynthOfRage.Scripts.Gameplay.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SynthOfRage.Scripts.Gameplay.Common.Modules
{
    /// <summary>
    /// Simple module that represent and manages the health of any unit.
    /// Raise an event when the unit's HP reach 0.
    /// </summary>
    public class HealthModule : MonoBehaviour
    {
        [Min(0)] [SerializeField] private int _maxHP = 100;
        private int _hp;

        [Header("Hit Reaction")]
        [SerializeField] private CharacterController _controller;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private PlayerMovement _playerMovement;
        private Color _originalColor;
        private Coroutine _reactionCoroutine;
        private bool _isStunned;

        [SerializeField] private Slider _healthBar;

        public event Action<int, int> OnHealthChanged;
        public event Action OnEmptyHealth;
        public event Action<int> OnDamageTaken;
        public event Action<bool> OnStunChanged;
        public event Action OnDeath;

        /// <summary>
        /// True only during the active frames of a player attack
        /// or while explicitly enabled by another gameplay system.
        /// </summary>
        public bool IsAttackInvincible { get; private set; }

        /// <summary>
        /// Called by AttackInvincibilityState on animator attack states.
        /// This does not change the HP Set/Apply APIs.
        /// </summary>
        public void SetAttackInvincibility(bool enabled)
        {
            IsAttackInvincible = enabled && isActiveAndEnabled && HP > 0;
        }

        /// <summary>
        /// Enables damage immunity.
        /// Incoming ReceiveHit calls will be completely ignored.
        /// Scripted damage through Apply or Set remains possible.
        /// </summary>
        public void EnableAttackInvincibility()
        {
            SetAttackInvincibility(true);
        }

        /// <summary>
        /// Disables damage immunity.
        /// </summary>
        public void DisableAttackInvincibility()
        {
            SetAttackInvincibility(false);
        }

        public bool IsStunned => _isStunned;

        public int MaxHP
        {
            get => _maxHP;
            set => _maxHP = Mathf.Max(1, value);
        }

        public int HP
        {
            get => _hp;
            set
            {
                int newHP = Mathf.Max(0, value);
                if (_hp == newHP) return;

                int previousHP = _hp;
                _hp = newHP;

                OnHealthChanged?.Invoke(previousHP, _hp);

                if (_hp < previousHP)
                {
                    int damageTaken = previousHP - _hp;
                    OnDamageTaken?.Invoke(damageTaken);

                    UnityEngine.Debug.Log(
                        $"[HealthModule] {name} took {damageTaken} damage. HP: {_hp}/{MaxHP}",
                        this);
                }

                if (previousHP > 0 && _hp == 0)
                {
                    SetAttackInvincibility(false);
                    StopReaction();
                    OnEmptyHealth?.Invoke();
                }
            }
        }

        private void Awake()
        {
            _maxHP = Mathf.Max(1, _maxHP);
            _hp = MaxHP;

            if (!_controller)
                _controller = GetComponent<CharacterController>();

            if (!_spriteRenderer)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            _playerMovement = GetComponent<PlayerMovement>();

            if (_spriteRenderer)
                _originalColor = _spriteRenderer.color;
        }

        /// <summary>
        /// Simply set a health parameter to a fixed value.
        /// </summary>
        public void Set(int value, bool clamped = true, bool setMaxHP = false)
        {
            if (setMaxHP)
            {
                MaxHP = Mathf.Max(1, value);

                if (clamped)
                    HP = Mathf.Clamp(HP, 0, MaxHP);
            }
            else
            {
                HP = clamped
                    ? Mathf.Clamp(value, 0, MaxHP)
                    : Mathf.Max(0, value);
            }
        }

        /// <summary>
        /// Variant method that changes health relative to an incoming value.
        /// Positive values heal, negative values hurt.
        /// </summary>
        public void Apply(
            int delta,
            bool updateMaxHP = false,
            bool reflectMaxHPChangeToCurrent = false)
        {
            if (delta == 0) return;

            if (updateMaxHP)
            {
                MaxHP = Mathf.Max(1, MaxHP + delta);

                if (!reflectMaxHPChangeToCurrent)
                    return;

                if (delta > 0)
                    HP += delta;

                HP = Mathf.Clamp(HP, 0, MaxHP);
            }
            else
            {
                HP = Mathf.Clamp(HP + delta, 0, MaxHP);
            }

            if (_healthBar != null)
                _healthBar.value = HP;

            if (HP <= 0)
            {
                OnDeath?.Invoke();
            }
        }

        /// <summary>
        /// Applies attack damage and configures stun, blink and knockback.
        /// Durations are expressed in seconds.
        /// </summary>
        public void ReceiveHit(
            int damage,
            Vector3 hitOrigin,
            float knockbackDistance,
            float knockbackDuration,
            AnimationCurve knockbackCurve,
            float stunDuration,
            float blinkDuration,
            float blinkInterval,
            Color blinkColor)
        {
            StartHitReaction(
                damage,
                hitOrigin,
                knockbackDistance,
                knockbackDuration,
                knockbackCurve,
                stunDuration,
                blinkDuration,
                blinkInterval,
                blinkColor,
                false);
        }

        /// <summary>
        /// Equivalent of ReceiveHit with durations expressed in musical beats.
        /// </summary>
        public void ReceiveHitBeats(
            int damage,
            Vector3 hitOrigin,
            float knockbackDistance,
            float knockbackBeats,
            AnimationCurve knockbackCurve,
            float stunBeats,
            float blinkBeats,
            float blinkIntervalBeats,
            Color blinkColor)
        {
            StartHitReaction(
                damage,
                hitOrigin,
                knockbackDistance,
                knockbackBeats,
                knockbackCurve,
                stunBeats,
                blinkBeats,
                blinkIntervalBeats,
                blinkColor,
                true);
        }

        private void StartHitReaction(
            int damage,
            Vector3 hitOrigin,
            float knockbackDistance,
            float knockbackDuration,
            AnimationCurve knockbackCurve,
            float stunDuration,
            float blinkDuration,
            float blinkInterval,
            Color blinkColor,
            bool useBeats)
        {
            // Attack i-frames cancel the entire hit,
            // including damage, stun and knockback.

            if (HP <= 0 || IsAttackInvincible)
                return;

            Apply(-Mathf.Max(0, damage));

            if (HP <= 0)
                return;

            Vector3 direction = transform.position - hitOrigin;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = transform.forward;
                direction.y = 0f;
            }

            direction.Normalize();

            StopReaction();

            _reactionCoroutine = StartCoroutine(
                ReactToHit(
                    direction,
                    Mathf.Max(0f, knockbackDistance),
                    Mathf.Max(0f, knockbackDuration),
                    knockbackCurve,
                    Mathf.Max(0f, stunDuration),
                    Mathf.Max(0f, blinkDuration),
                    Mathf.Max(0.001f, blinkInterval),
                    blinkColor,
                    useBeats));
        }

        private IEnumerator ReactToHit(
            Vector3 direction,
            float distance,
            float knockbackDuration,
            AnimationCurve curve,
            float stunDuration,
            float blinkDuration,
            float blinkInterval,
            Color blinkColor,
            bool useBeats)
        {
            float elapsed = 0f;
            float previousProgress = 0f;

            float totalDuration = Mathf.Max(
                knockbackDuration,
                stunDuration,
                blinkDuration);

            RhythmManager clock = useBeats
                ? RhythmManager.Instance
                : null;

            double beginBeat = clock && clock.IsReady
                ? clock.CurrentBeat
                : 0.0;

            SetStunned(stunDuration > 0f);

            while (elapsed < totalDuration)
            {
                if (useBeats && clock && clock.IsReady)
                {
                    elapsed = Mathf.Max(
                        elapsed,
                        (float)(clock.CurrentBeat - beginBeat));
                }
                else
                {
                    elapsed += Time.deltaTime * (useBeats ? 87f / 60f : 1f);
                }

                float frameElapsed = Mathf.Min(elapsed, totalDuration);

                if (knockbackDuration > 0f && previousProgress < 1f)
                {
                    float t = Mathf.Clamp01(frameElapsed / knockbackDuration);

                    float progress = Mathf.Clamp01(
                        curve != null ? curve.Evaluate(t) : t);

                    float movement = Mathf.Max(
                        0f,
                        progress - previousProgress) * distance;

                    if (_playerMovement && _playerMovement.enabled)
                        _playerMovement.ApplyExternalMovement(direction * movement);
                    else if (_controller && _controller.enabled)
                        _controller.Move(direction * movement);
                    else
                        transform.position += direction * movement;

                    previousProgress = Mathf.Max(previousProgress, progress);
                }

                if (_spriteRenderer)
                {
                    bool blink = frameElapsed < blinkDuration &&
                        Mathf.FloorToInt(frameElapsed / blinkInterval) % 2 == 0;

                    _spriteRenderer.color = blink
                        ? blinkColor
                        : _originalColor;
                }

                if (_isStunned && frameElapsed >= stunDuration)
                    SetStunned(false);

                yield return null;
            }

            ResetReaction();
            _reactionCoroutine = null;
        }

        private void SetStunned(bool stunned)
        {
            if (_isStunned == stunned)
                return;

            _isStunned = stunned;
            OnStunChanged?.Invoke(stunned);
        }

        private void ResetReaction()
        {
            if (_spriteRenderer)
                _spriteRenderer.color = _originalColor;

            SetStunned(false);
        }

        private void StopReaction()
        {
            if (_reactionCoroutine != null)
            {
                StopCoroutine(_reactionCoroutine);
                _reactionCoroutine = null;
            }

            ResetReaction();
        }

        private void OnDisable()
        {
            IsAttackInvincible = false;
            StopReaction();
        }

        [ContextMenu("DIE MOTHERFUCKER")]
        public void ForceDIE()
        {
            Set(0);
        }
    }
}
