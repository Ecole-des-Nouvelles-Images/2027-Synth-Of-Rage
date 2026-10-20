using _Dev.Christopher.Scripts.InputBlock;
using SynthOfRage.Scripts.Utilities;

namespace _Dev.Christopher.Scripts.SystemTest.InputBlock
{
    public class MoveCommand : ICommand
    {
        public CustomEnumLib.Priority Priority { get; } = CustomEnumLib.Priority.MinorLow;


        public void Execute()
        {
            throw new System.NotImplementedException();
        }
    }
}
