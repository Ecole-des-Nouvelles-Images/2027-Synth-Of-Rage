
using System.Collections.Generic;
using SynthOfRage.Scripts.Gameplay.Common.Modules;
using UnityEngine;

namespace _Dev.Christopher.Scripts
{
    [RequireComponent(typeof(BoxCollider))]
    public class HurtboxEvent : MonoBehaviour
    {
        [Header("Damage")]
        public int HurtboxDamage = 1;

        [Header("Knockback By Target Type")]
        [Min(0)] public float LightKnockback = 4f;
        [Min(0)] public float NormalKnockback = 2f;
        [Min(0)] public float HeavyKnockback = 0.5f;
        [Min(0)] public float PlayerKnockback = 2f;

        [Min(0)] public float KnockbackDuration = 0.2f;
        public AnimationCurve KnockbackCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Stun")]
        [Min(0)] public float StunDuration = 0.4f;

        [Header("Damage Blink")]
        [Min(0)] public float BlinkDuration = 0.3f;
        [Min(0.01f)] public float BlinkInterval = 0.05f;
        public Color BlinkColor = Color.red;

        [Header("Debug")]
        [SerializeField] private bool _debugEnabled = true;

        private SpriteRenderer _myParentSpriteRenderer;
        private BoxCollider _collider;
        private bool _flipX;
        private bool _wasColliderEnabled;

        private readonly HashSet<GameObject> _hitTargets = new();

        private void Initialize()
        {
            if (!_collider)
                _collider = GetComponent<BoxCollider>();

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

            if (!_collider) return;

            if (_collider.enabled && !_wasColliderEnabled)
            {
                _hitTargets.Clear();
                Log("Collider enabled externally.");
            }

            _wasColliderEnabled = _collider.enabled;
        }

        private void LateUpdate()
        {
            if (!_myParentSpriteRenderer)
                Initialize();

            if (!_myParentSpriteRenderer) return;

            bool flip = _myParentSpriteRenderer.flipX;
            if (flip == _flipX) return;

            transform.Rotate(0f, flip ? 180f : -180f, 0f);
            _flipX = flip;
        }

        private void OnTriggerEnter(Collider other)
        {
            Log($"OnTriggerEnter: {other.name}");
            TryHit(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryHit(other);
        }

        private void TryHit(Collider other)
        {
            if (!_collider || !_collider.enabled) return;

            EnemyHPPlaceholder enemy =
                other.GetComponentInParent<EnemyHPPlaceholder>();

            HealthModule health =
                other.GetComponentInParent<HealthModule>();

            GameObject target = enemy
                ? enemy.gameObject
                : health ? health.gameObject : null;

            if (!target)
            {
                Log($"Collider detected without target health: {other.name}");
                return;
            }

            if (target == transform.root.gameObject)
                return;

            // Only one hit per target during the current attack.
            if (!_hitTargets.Add(target))
                return;

            Vector3 hitOrigin = _myParentSpriteRenderer
                ? _myParentSpriteRenderer.transform.root.position
                : transform.root.position;

            if (enemy)
            {
                float distance = enemy.Type switch
                {
                    EnemyHPPlaceholder.EnemyType.Light => LightKnockback,
                    EnemyHPPlaceholder.EnemyType.Normal => NormalKnockback,
                    EnemyHPPlaceholder.EnemyType.Heavy => HeavyKnockback,
                    _ => NormalKnockback
                };

                Log($"Enemy hit: {target.name} | Damage: {HurtboxDamage}");

                enemy.TakeDamage(
                    HurtboxDamage,
                    hitOrigin,
                    distance,
                    StunDuration,
                    BlinkDuration,
                    BlinkInterval,
                    BlinkColor,
                    KnockbackDuration,
                    KnockbackCurve
                );
            }
            else if (health)
            {
                int previousHP = health.HP;

                Log($"Player hit: {target.name} | HP before: {previousHP}");

                health.ReceiveHit(
                    HurtboxDamage,
                    hitOrigin,
                    PlayerKnockback,
                    KnockbackDuration,
                    KnockbackCurve,
                    StunDuration,
                    BlinkDuration,
                    BlinkInterval,
                    BlinkColor
                );

                Log($"Player HP after: {health.HP}");
            }
        }

        public void EnableHurtbox()
        {
            Initialize();
            if (!_collider) return;

            // Every new attack can hit the same target again.
            _hitTargets.Clear();

            _collider.enabled = true;
            _wasColliderEnabled = true;

            Log($"ATTACK OPEN | Collider enabled: {_collider.enabled}");
        }

        public void DisableHurtbox()
        {
            Initialize();
            if (!_collider) return;

            _collider.enabled = false;
            _wasColliderEnabled = false;

            Log("ATTACK CLOSED");
        }

        public void ResetHitEnemies()
        {
            _hitTargets.Clear();
        }

        private void Log(string message)
        {
            if (_debugEnabled)
                Debug.Log($"[HurtboxEvent] {message}", this);
        }
    }
}
