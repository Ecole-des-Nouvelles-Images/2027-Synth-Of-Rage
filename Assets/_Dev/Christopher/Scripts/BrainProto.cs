using UnityEngine;
using UnityEngine.AI;

namespace _Dev.Christopher.Script
{
    public class BrainProto : MonoBehaviour
    {
        private static readonly int Move = Animator.StringToHash("Move");
        private NavMeshAgent _myNavMeshAgent;
        private GameObject _player;
        private GameObject _target;

        private Animator _animator;

        private void Start()
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            _myNavMeshAgent = GetComponent<NavMeshAgent>();
            _animator = GetComponentInChildren<Animator>();

            _animator.SetBool(Move, true);
        }

        private void Update()
        {
            if (!_player)
            {
                UnityEngine.Debug.Log("[BrainProto] Cannot find a Player to track");
                return;
            };
            _target = _player;
            TargetPath();
        }

        private void TargetPath()
        {
            _myNavMeshAgent.SetDestination(_target.transform.position);
        }
    }
}
