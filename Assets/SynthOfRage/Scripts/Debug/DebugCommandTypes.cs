using System;
using System.Collections;
using UnityEngine;

namespace SynthOfRage.Scripts.Debug
{
    public abstract class DebugCommandBase
    {
        private string _id;
        private string _description;
        private string _format;

        public string ID => _id;
        public string Description => _description;
        public string Format => _format;

        protected DebugCommandBase(string id, string description, string format)
        {
            _id = id;
            _description = description;
            _format = format;
        }
    }

    public class DebugCommand : DebugCommandBase
    {
        private Action _command;
        
        public DebugCommand(string id, string description, string format, Action command) : base(id, description, format)
        {
            _command = command;
        }

        public void Invoke()
        {
            _command.Invoke();
        }
    }

    public class DebugCommand<T> : DebugCommandBase
    {
        private Action<T> _command;
        
        public DebugCommand(string id, string description, string format, Action<T> cmdWithArg) : base(id, description, format)
        {
            _command = cmdWithArg;
        }

        public void Invoke(T arg)
        {
            _command.Invoke(arg);
        }
    }
}
