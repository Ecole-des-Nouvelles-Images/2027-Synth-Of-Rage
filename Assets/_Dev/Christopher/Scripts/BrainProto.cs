
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

        private enum AttackState { Chase, Preparation, Active, Cooldown }

        [Header("Target")]
        [SerializeField, Min(0f)] private float _attackRange = 2f;
        [SerializeField, Min(0f)] private float _chaseResumeRange = 2.5f;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float _attackPreparation = 0.3f;
        [SerializeField, Min(0.01f)] private float _attackDuration = 0.2f;
        [SerializeField, Min(0f)] private float _attackCooldown = 1f;
        [SerializeField] private Color _attackColor = Color.green;

        [Header("References")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;

        [Header("Debug")]
        [SerializeField] private bool _debugEnabled;

        private NavMeshAgent _agent;
        private Transform _player;
        private Color _originalColor;
        private AttackState _state;

        private float _phaseEndTime;
        private float _nextSearchTime;
        private bool _stoppedByAttack;
        private bool _useFirstAttack = true;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();

            if (!_animator)
                _animator = GetComponentInChildren<Animator>(true);

            if (!_spriteRenderer)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

            if (_spriteRenderer)
                _originalColor = _spriteRenderer.color;
        }

        private void OnEnable()
        {
            _state = AttackState.Chase;
            _phaseEndTime = 0f;
            _nextSearchTime = 0f;
            _stoppedByAttack = false;
            _useFirstAttack = true;
        }

        private void Update()
        {
            if (!_player && Time.time >= _nextSearchTime)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                _player = playerObject ? playerObject.transform : null;
                _nextSearchTime = Time.time + 0.5f;
            }

            if (!_agent || !_agent.enabled || !_agent.isOnNavMesh)
                return;

            if (!_player)
            {
                CancelAttack();
                SetMovementAnimation(false);
                return;
            }

            float distance = Vector3.ProjectOnPlane(
                _player.position - transform.position,
                Vector3.up
            ).magnitude;

            float resumeRange = Mathf.Max(_attackRange, _chaseResumeRange);

            switch (_state)
            {
                case AttackState.Chase:
                    if (_agent.isStopped && !_stoppedByAttack)
                    {
                        SetMovementAnimation(false);
                        return;
                    }

                    ReleaseOwnNavigation();

                    if (distance <= _attackRange)
                    {
                        BeginAttack();
                        return;
                    }

                    _agent.stoppingDistance = _attackRange;
                    _agent.SetDestination(_player.position);
                    SetMovementAnimation(true);
                    break;

                case AttackState.Preparation:
                    if (Time.time >= _phaseEndTime)
                        BeginActiveAttack();
                    break;

                case AttackState.Active:
                    if (Time.time >= _phaseEndTime)
                        BeginCooldown();
                    break;

                case AttackState.Cooldown:
                    if (Time.time < _phaseEndTime) return;

                    CancelAttack();

                    if (distance <= _attackRange)
                        BeginAttack();
                    break;
            }
        }

        private void BeginAttack()
        {
            _state = AttackState.Preparation;
            _phaseEndTime = Time.time + _attackPreparation;

            StopOwnNavigation();
            SetMovementAnimation(false);

            if (_animator)
            {
                int trigger = _useFirstAttack ? LightAttack01 : LightAttack02;

                _animator.ResetTrigger(LightAttack01);
                _animator.ResetTrigger(LightAttack02);
                _animator.SetTrigger(trigger);

                Log(_useFirstAttack
                    ? "LightAttack01 triggered."
                    : "LightAttack02 triggered.");

                _useFirstAttack = !_useFirstAttack;
            }
        }

        private void BeginActiveAttack()
        {
            _state = AttackState.Active;
            _phaseEndTime = Time.time + _attackDuration;

            if (_spriteRenderer)
                _spriteRenderer.color = _attackColor;
        }

        private void BeginCooldown()
        {
            _state = AttackState.Cooldown;
            _phaseEndTime = Time.time + _attackCooldown;

            RestoreColor();
        }

        private void CancelAttack()
        {
            _state = AttackState.Chase;
            RestoreColor();
            ReleaseOwnNavigation();
        }

        private void StopOwnNavigation()
        {
            if (!_agent || !_agent.enabled || !_agent.isOnNavMesh)
                return;

            if (_agent.isStopped) return;

            _agent.isStopped = true;
            _stoppedByAttack = true;
            _agent.ResetPath();
        }

        private void ReleaseOwnNavigation()
        {
            if (!_stoppedByAttack) return;

            _stoppedByAttack = false;

            if (_agent && _agent.enabled && _agent.isOnNavMesh)
                _agent.isStopped = false;
        }

        private void RestoreColor()
        {
            if (_spriteRenderer)
                _spriteRenderer.color = _originalColor;
        }

        private void SetMovementAnimation(bool moving)
        {
            if (_animator)
                _animator.SetBool(Move, moving);
        }

        private void Log(string message)
        {
            if (_debugEnabled)
                Debug.Log($"[BrainProto] {message}", this);
        }

        private void OnDisable()
        {
            RestoreColor();
            ReleaseOwnNavigation();
            SetMovementAnimation(false);
        }

        private void OnValidate()
        {
            _chaseResumeRange = Mathf.Max(_attackRange, _chaseResumeRange);
        }
    }
}
