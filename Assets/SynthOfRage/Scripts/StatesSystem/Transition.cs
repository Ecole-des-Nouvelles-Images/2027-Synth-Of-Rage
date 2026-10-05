namespace SynthOfRage.Scripts.HSM
{
    public abstract class Transition
    {
        State _nextState;
        
        public Transition(State nextState)
        {
            _nextState = nextState;
        }

        public abstract bool accesCondition();
    }
}