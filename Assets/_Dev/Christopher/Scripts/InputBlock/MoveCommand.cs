using SynthOfRage.Scripts.Utilities;

namespace _Dev.Christopher.Scripts.InputBlock
{
    public class MoveCommand : ICommand
    {
        public CustomEnumLib.Priority Priority { get; } = CustomEnumLib.Priority.Minor;


        public void Execute()
        {
            throw new System.NotImplementedException();
        }
    }
}
