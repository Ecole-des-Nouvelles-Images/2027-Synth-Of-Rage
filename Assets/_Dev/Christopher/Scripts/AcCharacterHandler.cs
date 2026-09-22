using UnityEngine;

namespace _Dev.Christopher.Scripts
{
    public class AcCharacterHandler : MonoBehaviour
    {
        private static readonly int Move = Animator.StringToHash("Move");
        public Animator MyAnimator;
        private CharacterController _characterController;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _characterController = transform.GetComponent<CharacterController>();
        }

        // Update is called once per frame
        void Update()
        {
            MyAnimator.SetBool(Move,_characterController.velocity != Vector3.zero);
        }
    }
}
