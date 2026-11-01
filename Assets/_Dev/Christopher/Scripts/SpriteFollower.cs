using UnityEngine;
using UnityEngine.AI;

namespace _Dev.Christopher.Scripts
{
    public class SpriteFollower : MonoBehaviour
    {
        private Quaternion initialWorldRotation;
        private NavMeshAgent _navMeshAgent;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            initialWorldRotation = transform.rotation;
            _navMeshAgent = GetComponentInParent<NavMeshAgent>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            transform.rotation = initialWorldRotation;

            if (!_navMeshAgent || !_spriteRenderer || !_navMeshAgent.hasPath)
                return;

            Vector3 direction = _navMeshAgent.destination - _navMeshAgent.transform.position;
            float dot = Vector3.Dot(initialWorldRotation * Vector3.right, direction);

            if (Mathf.Abs(dot) > 0.01f)
                _spriteRenderer.flipX = dot < 0f;
        }
    }
}