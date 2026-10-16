using System.Collections;
using System.Collections.Generic;
using SynthOfRage.Scripts.Gameplay.Core;
using UnityEngine;
using UnityEngine.AI;

namespace _Dev.Christopher.Scripts
{
    /// <summary>
    /// Keeps pre-finisher enemy knockback pending while an attacker can continue a combo.
    /// Damage, stun and blink remain managed by EnemyHPPlaceholder immediately.
    /// </summary>
    [DisallowMultipleComponent]
    public class DeferredComboKnockback : MonoBehaviour
    {
        private readonly Dictionary<Transform, Coroutine> _pendingByAttacker = new();
        private NavMeshAgent _agent;

        private void Awake() => _agent = GetComponent<NavMeshAgent>();

        /// <summary>Cancel an earlier uncommitted push when the same attacker hits again.</summary>
        public void CancelPending(Transform attacker)
        {
            if (!attacker || !_pendingByAttacker.TryGetValue(attacker, out Coroutine routine)) return;
            if (routine != null) StopCoroutine(routine);
            _pendingByAttacker.Remove(attacker);
        }

        /// <summary>Schedule knockback after the player's combo input tolerance, in beats.</summary>
        public void Schedule(Transform attacker, Vector3 hitOrigin, float distance,
            float durationSeconds, AnimationCurve curve, float delayBeats)
        {
            if (!attacker || distance <= 0f) return;
            CancelPending(attacker);
            _pendingByAttacker[attacker] = StartCoroutine(PushAfterTolerance(
                attacker, hitOrigin, distance, durationSeconds, curve, delayBeats));
        }

        private IEnumerator PushAfterTolerance(Transform attacker, Vector3 hitOrigin,
            float distance, float duration, AnimationCurve curve, float delayBeats)
        {
            RhythmManager clock = RhythmManager.Instance;
            bool useBeatClock = clock && clock.IsReady;
            double deadlineBeat = useBeatClock ? clock.CurrentBeat + delayBeats : 0d;
            float remainingSeconds = useBeatClock ? 0f : delayBeats * 60f / 87f;

            while (useBeatClock ? (clock && clock.IsReady && clock.CurrentBeat < deadlineBeat)
                                : remainingSeconds > 0f)
            {
                if (!useBeatClock) remainingSeconds -= Time.deltaTime;
                yield return null;
            }

            // The combo window expired without another confirmed hit.
            _pendingByAttacker.Remove(attacker);
            if (!_agent || !_agent.enabled || !_agent.isOnNavMesh) yield break;

            Vector3 direction = transform.position - hitOrigin;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) yield break;
            direction.Normalize();

            duration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;
            float previous = 0f;
            while (elapsed < duration && _agent && _agent.enabled && _agent.isOnNavMesh)
            {
                elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
                float t = elapsed / duration;
                float progress = curve != null ? Mathf.Clamp01(curve.Evaluate(t)) : t;
                float delta = Mathf.Max(0f, progress - previous) * distance;
                if (delta > 0f) _agent.Move(direction * delta);
                previous = progress;
                yield return null;
            }
        }

        private void OnDisable()
        {
            foreach (Coroutine routine in _pendingByAttacker.Values)
                if (routine != null) StopCoroutine(routine);
            _pendingByAttacker.Clear();
        }
    }
}
