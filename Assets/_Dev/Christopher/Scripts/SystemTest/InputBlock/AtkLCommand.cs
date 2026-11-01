using _Dev.Christopher.Scripts.InputBlock;
using SynthOfRage.Scripts.Utilities;
using UnityEngine;

namespace _Dev.Christopher.Scripts.SystemTest.InputBlock
{
    public class AtkLCommand : ICommand
    {
        private static readonly int LightAttack01 = Animator.StringToHash("LightAttack01");
        private static readonly int LightAttack02 = Animator.StringToHash("LightAttack02");
        private static readonly int LightAttack03 = Animator.StringToHash("LightAttack03");
        private static readonly int LightAttack04 = Animator.StringToHash("LightAttack04");
        
        public CustomEnumLib.Priority Priority { get; } = CustomEnumLib.Priority.MinorHigh;
        private int _comboCounter;

        public AtkLCommand(int comboCounter)
        {
            _comboCounter = comboCounter;
        }
        
        public void Execute()
        {
            switch (_comboCounter)
            {
                case 0:
                    _comboCounter++;
                    break;
                case 1:
                    _comboCounter++;
                    break;
                case 2:
                    _comboCounter++;
                    break;
                case 3:
                    _comboCounter = 0;
                    break;
                default:
                    _comboCounter = 0;
                    return;
            }
        }
    }
}