
using System;
using System.Collections.Generic;
using SynthOfRage.Scripts.Gameplay.Common.Modules;
using SynthOfRage.Scripts.Gameplay.Core;
using UnityEngine;

namespace _Dev.Christopher.Scripts
{
    [RequireComponent(typeof(BoxCollider), typeof(AudioSource))]
    public class HurtboxEvent : MonoBehaviour
    {
        /// <summary>
        /// Raised once per attack window when actual damage was applied to a valid target.
        /// Allows the attack owner to confirm a combo without depending on animation timing.
        /// </summary>
        public event Action<GameObject> OnHitConfirmed;

        [Header("Damage")]
        public int HurtboxDamage = 1;

        [Header("Knockback By Target Type")]
        [Min(0)] public float LightKnockback = 4f;
        [Min(0)] public float NormalKnockback = 2f;
        [Min(0)] public float HeavyKnockback = 0.5f;
        [Min(0)] public float PlayerKnockback = 2f;

        [Min(0)] public float KnockbackDuration = 0.2f;
        public AnimationCurve KnockbackCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Stun")]
        [Min(0)] public float StunDuration = 0.4f;

        [Header("Damage Blink")]
        [Min(0)] public float BlinkDuration = 0.3f;
        [Min(0.01f)] public float BlinkInterval = 0.05f;
        public Color BlinkColor = Color.red;

        [Header("Rhythm - Hit Reaction")]
        [Tooltip("Use musical beats rather than the legacy second-based durations above.")]
        [SerializeField] private bool _useBeatDurations = true;
        [SerializeField, Min(0f)] private float _knockbackBeats = 0.5f;
        [SerializeField, Min(0f)] private float _stunBeats = 0.5f;
        [SerializeField, Min(0f)] private float _blinkBeats = 0.5f;
        [SerializeField, Min(0.001f)] private float _blinkIntervalBeats = 0.125f;

        [Header("Audio - Hit Impact")]
        [Tooltip("AudioSource used to play hit and whiff sounds. Automatically found if empty.")]
        [SerializeField] private AudioSource _audioSource;

        [Tooltip("Random impact sound played when damage is successfully applied.")]
        [SerializeField] private List<AudioClip> _hitImpactSounds = new();

        [Tooltip("Volume of successful hit impact sounds.")]
        [SerializeField, Range(0f, 1f)] private float _hitImpactVolume = 0.5f;

        [Header("Audio - Whiff")]
        [Tooltip("Random sound played when an attack finishes without hitting anything.")]
        [SerializeField] private List<AudioClip> _whiffSounds = new();

        [Tooltip("Volume of whiff sounds.")]
        [SerializeField, Range(0f, 1f)] private float _whiffVolume = 1f;

        [Header("Debug")]
        [SerializeField] private bool _debugEnabled;

        private SpriteRenderer _myParentSpriteRenderer;
        private BoxCollider _collider;

        private bool _flipX;
        private bool _wasColliderEnabled;

        private float _enemyKnockbackMultiplier = 1f;
        private int _attackStep;
        private float _comboKnockbackDelayBeats;
        private double _damageAllowedAtTime;

        // Attack audio tracking.
        private bool _attackInProgress;
        private bool _attackHasConfirmedHit;

        private readonly HashSet<GameObject> _hitTargets = new();

        private void Initialize()
        {
            if (!_collider)
                _collider = GetComponent<BoxCollider>();

            if (!_audioSource)
                _audioSource = GetComponent<AudioSource>();

            if (!_myParentSpriteRenderer)
                _myParentSpriteRenderer = GetComponentInParent<SpriteRenderer>();

            if (_collider)
                _collider.isTrigger = true;
        }

        private void Awake()
        {
            Initialize();
            _wasColliderEnabled = _collider && _collider.enabled;
        }

        private void OnEnable()
        {
            Initialize();
            _hitTargets.Clear();
            _wasColliderEnabled = _collider && _collider.enabled;
        }

        private void Update()
        {
            if (!_collider)
                Initialize();

            if (!_collider)
                return;

            // Detect BoxCollider.enabled changes performed by Animator animation curves.
            if (_collider.enabled && !_wasColliderEnabled)
            {
                _hitTargets.Clear();
                Log("New animated hurtbox window.");
            }

            _wasColliderEnabled = _collider.enabled;
        }

        private void LateUpdate()
        {
            if (!_myParentSpriteRenderer)
                Initialize();

            if (!_myParentSpriteRenderer)
                return;

            bool flip = _myParentSpriteRenderer.flipX;

            if (flip == _flipX)
                return;

            transform.Rotate(0f, flip ? 180f : -180f, 0f);
            _flipX = flip;
        }

        private void OnTriggerEnter(Collider other) => TryHit(other);
        private void OnTriggerStay(Collider other) => TryHit(other);

        private void TryHit(Collider other)
        {
            if (!_collider ||
                !_collider.enabled ||
                Time.timeAsDouble < _damageAllowedAtTime)
                return;

            // Physics can run before Update observes an animation-driven enable.
            if (!_wasColliderEnabled)
            {
                _hitTargets.Clear();
                _wasColliderEnabled = true;
            }

            EnemyHPPlaceholder enemy =
                other.GetComponentInParent<EnemyHPPlaceholder>();

            HealthModule health =
                other.GetComponentInParent<HealthModule>();

            GameObject target = enemy
                ? enemy.gameObject
                : health ? health.gameObject : null;

            if (!target || target == transform.root.gameObject)
                return;

            // Only one hit per target during the current attack.
            if (!_hitTargets.Add(target))
                return;

            Vector3 hitOrigin = _myParentSpriteRenderer
                ? _myParentSpriteRenderer.transform.root.position
                : transform.root.position;

            bool beatMode = _useBeatDurations &&
                            RhythmManager.Instance &&
                            RhythmManager.Instance.IsReady;

            double secondsPerBeat = beatMode
                ? RhythmManager.Instance.SecondsPerBeat
                : 60.0 / 87.0;

            float knockSeconds = beatMode
                ? (float)(_knockbackBeats * secondsPerBeat)
                : KnockbackDuration;

            float stunSeconds = beatMode
                ? (float)(_stunBeats * secondsPerBeat)
                : StunDuration;

            float blinkSeconds = beatMode
                ? (float)(_blinkBeats * secondsPerBeat)
                : BlinkDuration;

            float intervalSeconds = beatMode
                ? (float)(_blinkIntervalBeats * secondsPerBeat)
                : BlinkInterval;

            if (HurtboxDamage <= 0)
                return;

            if (enemy)
            {
                if (enemy.Health <= 0)
                    return;

                int previousHP = enemy.Health;

                float distance = enemy.Type switch
                {
                    EnemyHPPlaceholder.EnemyType.Light => LightKnockback,
                    EnemyHPPlaceholder.EnemyType.Normal => NormalKnockback,
                    EnemyHPPlaceholder.EnemyType.Heavy => HeavyKnockback,
                    _ => NormalKnockback
                };

                // Applies to enemy knockback only: never multiplies player knockback or HP damage.
                distance *= _enemyKnockbackMultiplier;

                // Damage, hit confirm, stun and blink are immediate. Only the push is delayed.
                bool deferPush = _attackStep is 1 or 2 &&
                                 _comboKnockbackDelayBeats > 0f;

                DeferredComboKnockback pending =
                    target.GetComponent<DeferredComboKnockback>();

                if (pending)
                    pending.CancelPending(transform.root);

                enemy.TakeDamage(
                    HurtboxDamage,
                    hitOrigin,
                    deferPush ? 0f : distance,
                    stunSeconds,
                    blinkSeconds,
                    intervalSeconds,
                    BlinkColor,
                    knockSeconds,
                    KnockbackCurve
                );

                if (deferPush && enemy && enemy.Health > 0 && distance > 0f)
                {
                    if (!pending)
                        pending = target.AddComponent<DeferredComboKnockback>();

                    pending.Schedule(
                        transform.root,
                        hitOrigin,
                        distance,
                        knockSeconds,
                        KnockbackCurve,
                        _comboKnockbackDelayBeats
                    );
                }

                if (enemy.Health < previousHP)
                {
                    ConfirmSuccessfulHit(target);

                    Log($"Enemy hit confirmed: {target.name}, damage {previousHP - enemy.Health}");
                }
            }
            else if (health)
            {
                if (health.HP <= 0)
                    return;

                int previousHP = health.HP;

                if (beatMode)
                {
                    health.ReceiveHitBeats(
                        HurtboxDamage,
                        hitOrigin,
                        PlayerKnockback,
                        _knockbackBeats,
                        KnockbackCurve,
                        _stunBeats,
                        _blinkBeats,
                        _blinkIntervalBeats,
                        BlinkColor
                    );
                }
                else
                {
                    health.ReceiveHit(
                        HurtboxDamage,
                        hitOrigin,
                        PlayerKnockback,
                        knockSeconds,
                        KnockbackCurve,
                        stunSeconds,
                        blinkSeconds,
                        intervalSeconds,
                        BlinkColor
                    );
                }

                if (health.HP < previousHP)
                {
                    ConfirmSuccessfulHit(target);

                    Log($"Player hit confirmed: {target.name}, remaining HP {health.HP}");
                }
            }
        }

        /// <summary>
        /// Called when damage was actually applied to a target.
        /// Plays an impact sound and marks the current attack as successful.
        /// </summary>
        private void ConfirmSuccessfulHit(GameObject target)
        {
            _attackHasConfirmedHit = true;

            PlayRandomSound(_hitImpactSounds, _hitImpactVolume);

            OnHitConfirmed?.Invoke(target);
        }

        /// <summary>
        /// Prepares a new player attack without enabling or disabling the Collider.
        /// The Animator remains the only owner of its enabled keyframes.
        /// A brief damage gate avoids carrying a previous attack window into the next clip.
        /// </summary>
        public void PrepareAttackStep(float enemyKnockbackMultiplier, float firstHitDelaySeconds)
        {
            Initialize();

            _enemyKnockbackMultiplier = Mathf.Max(0f, enemyKnockbackMultiplier);
            _damageAllowedAtTime = Time.timeAsDouble + Mathf.Max(0f, firstHitDelaySeconds);

            _hitTargets.Clear();
            _attackStep = 0;
            _comboKnockbackDelayBeats = 0f;

            // Start tracking a new attack.
            _attackInProgress = true;
            _attackHasConfirmedHit = false;
        }

        /// <summary>
        /// Prepares a player combo hit. First and second hits defer enemy knockback
        /// for the combo input tolerance; the third hit immediately delivers its push.
        /// </summary>
        public void PrepareAttackStep(
            float enemyKnockbackMultiplier,
            float firstHitDelaySeconds,
            int attackStep,
            float knockbackDelayBeats)
        {
            PrepareAttackStep(enemyKnockbackMultiplier, firstHitDelaySeconds);

            _attackStep = attackStep;
            _comboKnockbackDelayBeats = Mathf.Max(0f, knockbackDelayBeats);
        }

        /// <summary>
        /// Called by an Animation Event at the end of an attack animation.
        /// Plays a whiff sound only if the attack did not successfully hit anything.
        /// </summary>
        public void EndAttack()
        {
            if (!_attackInProgress)
                return;

            if (!_attackHasConfirmedHit)
            {
                PlayRandomSound(_whiffSounds, _whiffVolume);
                Log("Attack ended without hitting a target.");
            }
            else
            {
                Log("Attack ended after a successful hit.");
            }

            _attackInProgress = false;
        }

        /// <summary>
        /// Plays one random clip from the provided list.
        /// </summary>
        private void PlayRandomSound(List<AudioClip> clips, float volume)
        {
            if (!_audioSource)
                Initialize();

            if (!_audioSource || clips == null || clips.Count == 0)
                return;

            // Build a list of valid clips so empty Inspector entries are ignored.
            List<AudioClip> validClips = new();

            foreach (AudioClip clip in clips)
            {
                if (clip)
                    validClips.Add(clip);
            }

            if (validClips.Count == 0)
                return;

            AudioClip selectedClip = validClips[UnityEngine.Random.Range(0, validClips.Count)];

            _audioSource.PlayOneShot(selectedClip, Mathf.Clamp01(volume));
        }

        /// <summary>
        /// Restores or sets the enemy-only knockback multiplier.
        /// </summary>
        public void SetEnemyKnockbackMultiplier(float multiplier)
        {
            _enemyKnockbackMultiplier = Mathf.Max(0f, multiplier);
        }

        public void ResetHitEnemies() => _hitTargets.Clear();

        public void EnableHurtbox()
        {
            Initialize();

            if (!_collider)
                return;

            // Every new attack can hit the same target again.
            _hitTargets.Clear();

            _collider.enabled = true;
            _wasColliderEnabled = true;
        }

        public void DisableHurtbox()
        {
            Initialize();

            if (!_collider)
                return;

            _collider.enabled = false;
            _wasColliderEnabled = false;
        }

        private void Log(string message)
        {
            if (_debugEnabled)
                Debug.Log($"[HurtboxEvent] {message}", this);
        }
    }
}
