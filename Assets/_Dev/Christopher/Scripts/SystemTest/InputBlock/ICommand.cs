using SynthOfRage.Scripts.Utilities;

namespace _Dev.Christopher.Scripts.SystemTest.InputBlock
{
    public interface ICommand
    {
        public CustomEnumLib.Priority Priority { get; }
        public void Execute();
    }
}
