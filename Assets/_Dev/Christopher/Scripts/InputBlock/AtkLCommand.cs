using SynthOfRage.Scripts.Utilities;
using UnityEngine;

namespace _Dev.Christopher.Scripts.InputBlock
{
    public class AtkLCommand : ICommand
    {
        private static readonly int LightAttack01 = Animator.StringToHash("LightAttack01");
        private static readonly int LightAttack02 = Animator.StringToHash("LightAttack02");
        
        public CustomEnumLib.Priority Priority { get; } = CustomEnumLib.Priority.Minor;
        private int _comboCounter;
        private Animator _animator;

        public AtkLCommand(int comboCounter, Animator animator)
        {
            _comboCounter = comboCounter;
            _animator = animator;
        }
        
        public void Execute()
        {
            switch (_comboCounter)
            {
                case 0:
                    _animator.SetTrigger(LightAttack01);
                    break;
                case 1:
                    _animator.SetTrigger(LightAttack02);
                    break;
                case 2:
                    
                    break;
                case 3:
                    break;
                default:
                    _comboCounter = 0;
                    return;
            }
        }
    }
}