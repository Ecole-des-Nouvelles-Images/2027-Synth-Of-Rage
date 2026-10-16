
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyHPPlaceholder : MonoBehaviour
{
    public enum EnemyType { Light, Normal, Heavy }

    [Header("Enemy Settings")]
    public EnemyType Type = EnemyType.Normal;
    public int Health = 3;

    private SpriteRenderer _spriteRenderer;
    private NavMeshAgent _navMeshAgent;
    private Color _originalColor;
    private Coroutine _reactionCoroutine;

    private void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        if (_spriteRenderer) _originalColor = _spriteRenderer.color;
    }

    public void TakeDamage(
        int damage,
        Vector3 hitOrigin,
        float knockbackDistance,
        float stunDuration,
        float blinkDuration,
        float blinkInterval,
        Color blinkColor,
        float knockbackDuration,
        AnimationCurve knockbackCurve)
    {
        Health -= damage;

        if (Health <= 0)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 direction = transform.position - hitOrigin;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : -transform.forward;

        if (_reactionCoroutine != null)
            StopCoroutine(_reactionCoroutine);

        _reactionCoroutine = StartCoroutine(ReactToHit(
            direction,
            Mathf.Max(0f, knockbackDistance),
            Mathf.Max(0f, stunDuration),
            Mathf.Max(0f, blinkDuration),
            Mathf.Max(0.01f, blinkInterval),
            blinkColor,
            knockbackCurve
        ));
    }

    private IEnumerator ReactToHit(
        Vector3 direction,
        float distance,
        float stunDuration,
        float blinkDuration,
        float blinkInterval,
        Color blinkColor,
        AnimationCurve curve)
    {
        if (_navMeshAgent && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            _navMeshAgent.isStopped = true;

        float elapsed = 0f;
        float knockbackDuration = Mathf.Min(0.25f, stunDuration);
        float totalDuration = Mathf.Max(stunDuration, blinkDuration);
        float previousProgress = 0f;

        while (elapsed < totalDuration)
        {
            elapsed = Mathf.Min(elapsed + Time.deltaTime, totalDuration);

            if (knockbackDuration > 0f && elapsed <= knockbackDuration)
            {
                float t = Mathf.Clamp01(elapsed / knockbackDuration);
                float progress = curve != null
                    ? curve.Evaluate(t)
                    : t;

                float movement = (progress - previousProgress) * distance;

                if (_navMeshAgent && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
                    _navMeshAgent.Move(direction * movement);

                previousProgress = progress;
            }

            if (_spriteRenderer)
            {
                _spriteRenderer.color = elapsed < blinkDuration &&
                    Mathf.FloorToInt(elapsed / blinkInterval) % 2 == 0
                        ? blinkColor
                        : _originalColor;
            }

            if (elapsed >= stunDuration &&
                _navMeshAgent && _navMeshAgent.enabled &&
                _navMeshAgent.isOnNavMesh && _navMeshAgent.isStopped)
                _navMeshAgent.isStopped = false;

            yield return null;
        }

        if (_spriteRenderer)
            _spriteRenderer.color = _originalColor;

        if (_navMeshAgent && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            _navMeshAgent.isStopped = false;

        _reactionCoroutine = null;
    }

    private void OnDisable()
    {
        if (_reactionCoroutine != null)
        {
            StopCoroutine(_reactionCoroutine);
            _reactionCoroutine = null;
        }

        if (_spriteRenderer)
            _spriteRenderer.color = _originalColor;

        if (_navMeshAgent && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            _navMeshAgent.isStopped = false;
    }
}
