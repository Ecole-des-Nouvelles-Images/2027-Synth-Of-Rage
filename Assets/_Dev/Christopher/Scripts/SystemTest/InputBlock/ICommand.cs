using SynthOfRage.Scripts.Utilities;
using UnityEngine;

namespace _Dev.Christopher.Scripts.InputBlock
{
    public interface ICommand
    {
        public CustomEnumLib.Priority Priority { get; }
        public void Execute();
    }
}
