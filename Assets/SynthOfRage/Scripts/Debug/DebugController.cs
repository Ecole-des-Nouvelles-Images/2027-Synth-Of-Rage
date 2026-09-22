using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

using SynthOfRage.Scripts.Utils;

namespace SynthOfRage.Scripts.Debug
{
    public class DebugController : MonoBehaviour
    {
        [SerializeField] private InputAction _triggerKey;
        [SerializeField] private InputAction _returnKey;
         
        private bool _showConsole;
        private string _input;

        private List<DebugCommand> _commandList = new();
        private DebugCommand _cmdExitGame = new("exit-game", "Quit the application", "exit", CommandLibrary.RestartLevel);
        private DebugCommand _cmdRestartLevel = new("restart-level", "Reload the current active scene.", "restart", CommandLibrary.RestartLevel);
        private DebugCommand<int> _cmdLoadLevel = new("load-level", "Load any scene with the provided index.", "load", CommandLibrary.LoadLevel);
        
        private void Awake()
        {
            _commandList.Add(_cmdRestartLevel);
            _commandList.Add(_cmdExitGame);
        }

        private void Start()
        {
            _triggerKey.performed += OnToggleConsole;
            _returnKey.performed += OnCommandReturned;
            
            _triggerKey.Enable();
            _returnKey.Enable();
        }

        private void OnDestroy()
        {
            _triggerKey.performed -= OnToggleConsole;
            _returnKey.performed -= OnCommandReturned;
            
            _triggerKey.Dispose();
            _returnKey.Dispose();
        }

        #if UNITY_INCLUDE_INSTRUMENTATION // Player settings > Managed Code Variant > "Instrumented" or below
        private void OnGUI()
        {
            if (!_showConsole) return;

            float y = 0;

            GUI.Box(new Rect(0, y, Screen.width, 30), "");
            GUI.backgroundColor = new Color(0, 0, 0, 0);
            _input = GUI.TextField(new Rect(10f, y + 5f, Screen.width - 20f, 20f), _input);
        }
        #endif

        private void OnToggleConsole(InputAction.CallbackContext ctx)
        {
            UnityEngine.Debug.Log("[`] Key pressed !");
            _showConsole = !_showConsole;
        }

        private void OnCommandReturned(InputAction.CallbackContext ctx)
        {
            UnityEngine.Debug.Log("[Enter] key pressed");

            if (!_showConsole) return;
            
            HandleInput();
            _input = "";
        }

        private void HandleInput()
        {
            string[] properties = _input.Split(' ');

            for (int i = 0; i < _commandList.Count; i++)
            {
                DebugCommandBase cmdBase = _commandList[0] as DebugCommandBase;

                if (_input.Contains(_commandList[i].ID))
                {
                    if (cmdBase as DebugCommandBase != null)
                    {
                        _commandList[i].Invoke();
                    }
                    else if (cmdBase as DebugCommand<int> != null)
                    {
                        (_commandList[i] as DebugCommand<int>).Invoke(int.Parse(properties[1]));
                    }
                }
            }
        }
    }
}
