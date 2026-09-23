#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION

using System;

namespace SynthOfRage.Scripts.Debug
{
    public abstract class DebugCommandBase
    {
        public string ID { get; }
        public string Description { get; }
        public string Format { get; }

        protected DebugCommandBase(string id, string description, string format)
        {
            ID = id;
            Description = description;
            Format = format;
        }

        public abstract void Invoke(string[] args);
    }

    public class DebugCommand : DebugCommandBase
    {
        private Action<string[]> _command;
        
        public DebugCommand(string id, string description, string format, Action<string[]> command) 
            : base(id, description, format)
        {
            _command = command;
        }

        public override void Invoke(string[] args)
        {
            _command.Invoke(args);
        }
    }
}

#endif
