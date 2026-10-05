
namespace SynthOfRage.Scripts.HSM
{
    public abstract class State
    {
        private State _parentState;

        public State()
        {
            _parentState = null;
        }
        public State(State parentState)
        {
            _parentState = parentState;
        }

        public abstract void Start();
        public abstract void Update();
        //access condition?
    }
}