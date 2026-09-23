#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION // Player settings > Managed Code Variant > "Instrumented" or below

using System;
using System.Collections.Generic;
using SynthOfRage.Scripts.Helper;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SynthOfRage.Scripts.Debug
{
    public class DebugController : SingletonMonoBehaviour<DebugController>
    {
        [SerializeField] private InputAction _triggerKey;
        [SerializeField] private InputAction _returnKey;
        [SerializeField] private InputAction _rewindKeys;

        private bool _showConsole;
        private bool _showHelp;

        private string _input;
        private Vector2 _helpScroll;

        private List<string> _rewindHistory = new();
        private int _rewindIndex;

        protected override void Awake()
        {
            base.Awake();

            DebugCommandLibrary.CmdList.Insert(0, new DebugCommand(
                "help",
                "Shows the list of available commands",
                "help",
                _ =>
                {
                    _showHelp = true;
                })
            );

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _triggerKey.Enable();
            _returnKey.Enable();
            _rewindKeys.Enable();

            _triggerKey.performed += OnToggleConsole;
            _returnKey.performed += OnCommandReturned;
            _rewindKeys.performed += OnRewindCommand;
        }

        protected override void OnDestroy()
        {
            _triggerKey.performed -= OnToggleConsole;
            _returnKey.performed -= OnCommandReturned;
            _rewindKeys.performed -= OnRewindCommand;

            _triggerKey.Dispose();
            _returnKey.Dispose();
            _rewindKeys.Dispose();

            base.OnDestroy();
        }

        private void OnGUI()
        {
            if (!_showConsole) return;

            float posY = 0;
            int helpItem = 0;

            if (_showHelp)
            {
                GUI.Box(new Rect(0, posY, Screen.width, 100), "");
                Rect viewport = new Rect(0, 0, Screen.width - 30, 20 * DebugCommandLibrary.CmdList.Count);
                _helpScroll = GUI.BeginScrollView(new Rect(0, posY + 5f, Screen.width, 90), _helpScroll, viewport);

                foreach (DebugCommandBase cmd in DebugCommandLibrary.CmdList)
                {
                    string label = $"{cmd.Format} - {cmd.Description}";
                    Rect labelRect = new Rect(5, 20 * helpItem, viewport.width - 100, 20);
                    GUI.Label(labelRect, label);
                    helpItem++;
                }
                
                GUI.EndScrollView();
                posY += 100;
            }

            GUI.Box(new Rect(0, posY, Screen.width, 30), "");
            GUI.backgroundColor = new Color(0, 0, 0, 0);
            GUI.SetNextControlName("InputField");
            _input = GUI.TextField(new Rect(10f, posY + 5f, Screen.width - 20f, 20f), _input);
            GUI.FocusControl("InputField");
        }

        private void OnToggleConsole(InputAction.CallbackContext ctx)
        {
            _showConsole = !_showConsole;
            _input = "";
        }

        private void OnCommandReturned(InputAction.CallbackContext ctx)
        {
            if (!_showConsole) return;

            if (string.IsNullOrWhiteSpace(_input))
            {
                _showConsole = false;
                if (_showHelp) _showHelp = false;
            }

            HandleInput();
            _input = "";
        }

        private void OnRewindCommand(InputAction.CallbackContext obj)
        {
            bool isRewindUp = obj.ReadValue<float>() > 1;

            if (_rewindHistory.Count == 0) return;

            if (isRewindUp)
            {
                _rewindIndex = Mathf.Clamp(_rewindIndex--, 0, _rewindHistory.Count - 1);
                _input = _rewindHistory[_rewindIndex];
            }
            else
            {
                _rewindIndex = Mathf.Clamp(_rewindIndex++, 0, _rewindHistory.Count);
                _input = _rewindIndex < _rewindHistory.Count ? _rewindHistory[_rewindIndex] : "";
            }
        }

        private void HandleInput()
        {
            string[] tokens = _input.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return;

            string commandId = tokens[0];
            string[] args = new string[tokens.Length - 1];
            Array.Copy(tokens, 1, args, 0, tokens.Length - 1);

            foreach (var cmd in DebugCommandLibrary.CmdList)
            {
                if (!cmd.ID.Equals(commandId, StringComparison.OrdinalIgnoreCase)) continue;

                if (!cmd.ID.Equals("help", StringComparison.OrdinalIgnoreCase)) { _showHelp = false; }

                cmd.Invoke(args);
                _rewindHistory.Add(_input);
                _rewindIndex = _rewindHistory.Count;

                if (!cmd.ID.Equals("help", StringComparison.OrdinalIgnoreCase)) { _showConsole = false; }

                return;
            }

            UnityEngine.Debug.LogWarning($"Unknown command: {commandId}");
        }
    }
}

#endif
