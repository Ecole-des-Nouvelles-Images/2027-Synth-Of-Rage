using System;
using System.Collections;
using SynthOfRage.Scripts.Gameplay.Unit.Player;
using UnityEngine;

namespace SynthOfRage.Scripts.Gameplay.Common.Modules
{
    /// <summary>
    /// Simple module that represent and manages the health of any unit.<br/>
    /// Raise an event when the unit's <c>HP</c> reach 0.
    /// </summary>
    public class HealthModule : MonoBehaviour
    {
        [Min(0)] [SerializeField] private int _maxHP = 100;
        [SerializeField] private int _hp;

        [Header("Hit Reaction")]
        [SerializeField] private CharacterController _controller;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private PlayerMovement _playerMovement;
        private Color _originalColor;
        private Coroutine _reactionCoroutine;
        private bool _isStunned;

        public event Action<int, int> OnHealthChanged;
        public event Action OnEmptyHealth;
        public event Action<int> OnDamageTaken;
        public event Action<bool> OnStunChanged;

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
                    UnityEngine.Debug.Log($"[HealthModule] {name} took {damageTaken} damage. HP: {_hp}/{MaxHP}", this);
                }

                if (previousHP > 0 && _hp == 0)
                {
                    StopReaction();
                    OnEmptyHealth?.Invoke();
                }
            }
        }

        private void Awake()
        {
            _maxHP = Mathf.Max(1, _maxHP);
            _hp = MaxHP;

            if (!_controller) _controller = GetComponent<CharacterController>();
            if (!_spriteRenderer) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _playerMovement = GetComponent<PlayerMovement>();
            if (_spriteRenderer) _originalColor = _spriteRenderer.color;
        }

        /// <summary>
        /// Simply set a health parameter to a fixed value.
        /// </summary>
        /// <param name="value">The fixed value to set the <c>HP</c> property to.</param>
        /// <param name="clamped">Should the value of <c>HP</c> be clamped to <c>MaxHP</c> (<i>true by default</i>). If <c>MaxHP</c> is reduced, this parameter will clamp down the <c>HP</c> too.</param>
        /// <param name="setMaxHP">Should the <c>maxHP</c> be set rather than current <c>HP</c> (<i>false by default</i>).</param>
        /// <remarks><c>HP</c> and <c>MaxHP</c> can't ever be negative.<br/><c>MaxHP</c> can't be updated below 1.</remarks>
        public void Set(int value, bool clamped = true, bool setMaxHP = false)
        {
            if (setMaxHP)
            {
                MaxHP = Mathf.Max(1, value);
                if (clamped) HP = Mathf.Clamp(HP, 0, MaxHP);
            }
            else
            {
                HP = clamped ? Mathf.Clamp(value, 0, MaxHP) : Mathf.Max(0, value);
            }
        }

        /// <summary>
        /// Variant method that set a health parameter value relative to an incoming value. This method will always clamp the <c>HP</c>.
        /// </summary>
        /// <param name="delta">The relative value of <c>HP</c> to change.<br/> A positive value will heal and a negative value will hurt</param>
        /// <param name="updateMaxHP">Should the change affect the <c>MaxHP</c> instead of current <c>HP</c>.</param>
        /// <param name="reflectMaxHPChangeToCurrent">Should the <c>MaxHP</c> change reflects in current <c>HP</c>.<br/>
        /// Increased <c>MaxHP</c> will also be given to current <c>HP</c>. Current <c>HP</c> will be clamped to the new max if needed.</param>
        /// <remarks><c>HP</c> and <c>MaxHP</c> can't ever be negative.<br/><c>MaxHP</c> can't be updated below 1.</remarks>
        public void Apply(int delta, bool updateMaxHP = false, bool reflectMaxHPChangeToCurrent = false)
        {
            if (delta == 0) return;

            if (updateMaxHP)
            {
                MaxHP = Mathf.Max(1, MaxHP + delta);
                if (!reflectMaxHPChangeToCurrent) return;
                if (delta > 0) HP += delta;
                HP = Mathf.Clamp(HP, 0, MaxHP);
            }
            else
            {
                HP = Mathf.Clamp(HP + delta, 0, MaxHP);
            }
        }

        /// <summary>
        /// Applies attack damage and configures stun, blink and knockback using attack properties.
        /// </summary>
        public void ReceiveHit(int damage, Vector3 hitOrigin, float knockbackDistance,
            float knockbackDuration, AnimationCurve knockbackCurve, float stunDuration,
            float blinkDuration, float blinkInterval, Color blinkColor)
        {
            if (HP <= 0) return;
            Apply(-Mathf.Max(0, damage));
            if (HP <= 0) return;

            Vector3 direction = transform.position - hitOrigin;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = transform.forward;
                direction.y = 0f;
            }
            direction.Normalize();

            StopReaction();
            _reactionCoroutine = StartCoroutine(ReactToHit(direction, Mathf.Max(0f, knockbackDistance),
                Mathf.Max(0f, knockbackDuration), knockbackCurve, Mathf.Max(0f, stunDuration),
                Mathf.Max(0f, blinkDuration), Mathf.Max(0.01f, blinkInterval), blinkColor));
        }

        private IEnumerator ReactToHit(Vector3 direction, float distance, float knockbackDuration,
            AnimationCurve curve, float stunDuration, float blinkDuration, float blinkInterval, Color blinkColor)
        {
            float elapsed = 0f;
            float previousProgress = 0f;
            float totalDuration = Mathf.Max(knockbackDuration, stunDuration, blinkDuration);
            SetStunned(stunDuration > 0f);

            while (elapsed < totalDuration)
            {
                elapsed = Mathf.Min(elapsed + Time.deltaTime, totalDuration);

                if (knockbackDuration > 0f && previousProgress < 1f)
                {
                    float t = Mathf.Clamp01(elapsed / knockbackDuration);
                    float progress = Mathf.Clamp01(curve != null ? curve.Evaluate(t) : t);
                    float movement = Mathf.Max(0f, progress - previousProgress) * distance;

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
                    bool blink = elapsed < blinkDuration &&
                        Mathf.FloorToInt(elapsed / blinkInterval) % 2 == 0;
                    _spriteRenderer.color = blink ? blinkColor : _originalColor;
                }

                if (_isStunned && elapsed >= stunDuration)
                    SetStunned(false);

                yield return null;
            }

            ResetReaction();
            _reactionCoroutine = null;
        }

        private void SetStunned(bool stunned)
        {
            if (_isStunned == stunned) return;
            _isStunned = stunned;
            OnStunChanged?.Invoke(stunned);
        }

        private void ResetReaction()
        {
            if (_spriteRenderer) _spriteRenderer.color = _originalColor;
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

        private void OnDisable() => StopReaction();

        [ContextMenu("DIE MOTHERFUCKER")]
        public void ForceDIE()
        {
            Set(0);
        }
    }
}
