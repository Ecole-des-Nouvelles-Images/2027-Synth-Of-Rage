using System;
using SynthOfRage.Scripts.Unit.Player;
using UnityEngine;

namespace _Dev.Christopher.Scripts
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerObserver))]
    public class AcCharacterHandler : MonoBehaviour
    {

        private static readonly int Move = Animator.StringToHash("Move");
        private static readonly int LightAttack01 = Animator.StringToHash("LightAttack01");
        private static readonly int LightAttack02 = Animator.StringToHash("LightAttack02");
        private static readonly int HeavyAttack01 = Animator.StringToHash("HeavyAttack01");
        private static readonly int Hurt = Animator.StringToHash("Hurt");
        private static readonly int Death = Animator.StringToHash("Death");

        [Header("References")]
        [SerializeField] private PlayerObserver playerObserver;

        public Animator MyAnimator;

        private CharacterController _characterController;
        private int _comboCounter;
        private float _currentResetTime;
        private float _currentCooldownAtk;

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

            playerObserver.OnPlayerAtkL += AtkLComboManager;
        }

        private void OnDestroy()
        {
            playerObserver.OnPlayerAtkL -= AtkLComboManager;
        }

        void Start()
        {
            _characterController = transform.GetComponent<CharacterController>();
        }

        void Update()
        {
            if (_currentResetTime > 0)
            {
                _currentResetTime -= Time.deltaTime;
                if (_currentResetTime <= 0)
                    _comboCounter = 0;
            }
            if (_currentCooldownAtk > 0)
            {
                _currentCooldownAtk -= Time.deltaTime;
            }
        }

        private void AtkLComboManager()
        {
            if (_comboCounter == 0 && _currentCooldownAtk <= 0)
            {
                HandleAtkL01Animation();
                _comboCounter = 1;
                _currentResetTime = 3.0f;
                _currentCooldownAtk = 0.3f;
                return;
            }
            if (_comboCounter > 0 && _currentCooldownAtk <= 0)
            {
                HandleAtkL02Animation();
                _currentCooldownAtk = 0.3f;
                _comboCounter = 0;
                _currentResetTime = 0;
            }
        }

        private void HandleAtkL01Animation()
        {
            MyAnimator.SetTrigger(LightAttack01);
        }
        private void HandleAtkL02Animation()
        {
            Debug.Log("coup 2 OK");
            MyAnimator.SetTrigger(LightAttack02);
        }
        private void HandleAtkH01Animation()
        {
            MyAnimator.SetTrigger(HeavyAttack01);
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