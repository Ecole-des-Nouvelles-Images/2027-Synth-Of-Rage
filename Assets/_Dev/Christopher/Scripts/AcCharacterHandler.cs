using SynthOfRage.Scripts.Player;
using UnityEngine;

namespace _Dev.Christopher.Scripts
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerObserver))]
    public class AcCharacterHandler : MonoBehaviour
    {
        
        private static readonly int Move = Animator.StringToHash("Move");
        private static readonly int Attack = Animator.StringToHash("Attack");
        private static readonly int Hurt = Animator.StringToHash("Hurt");
        private static readonly int Death = Animator.StringToHash("Death");

        [Header("References")]
        [SerializeField] private PlayerObserver playerObserver;
        
        public Animator MyAnimator;
        
        private CharacterController _characterController;
        
        private void OnEnable()
        {
            if (MyAnimator == null)
            {
                Debug.LogError(
                    $"[{nameof(MyAnimator)}] PlayerObserver reference is missing.",
                    this
                );

                return;
            }
            if (playerObserver == null)
            {
                playerObserver = transform.GetComponent<PlayerObserver>();
            }

            playerObserver.OnPlayerAtkL += HandleAtkLAnimation;
        }
        
        void Start()
        {
            _characterController = transform.GetComponent<CharacterController>();
        }

        // void Update()
        // {
        //     MyAnimator.SetBool(Move,_characterController.velocity != Vector3.zero);
        // }

        private void HandleAtkLAnimation()
        {
            MyAnimator.SetTrigger(Attack);
        }

        private void HandleHurtAnimation()
        {
            MyAnimator.SetTrigger(Hurt);
        }

        private void HandleDeathAnimation()
        {
            MyAnimator.SetTrigger(Death);
        }
        
    }
}
