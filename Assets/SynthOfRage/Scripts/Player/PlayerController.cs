using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = System.Object;

namespace SynthOfRage.Scripts.Player
{
    [Serializable]
    public class PlayerController
    {
        public InputActionAsset InputActionAsset { get; private set; } = InputSystem.actions;

        private InputActionMap _mapGameplay;
        private InputActionMap _mapUI;

        private InputAction _move;
        private InputAction _jump;
        private InputAction _dash;
        private InputAction _guard;
        private InputAction _atkLight;
        private InputAction _atkHeavy;
        private InputAction _atkSP;
        private InputAction _interaction;
        
        private void BindInputActionMaps()
        {
            _mapGameplay = InputActionAsset.FindActionMap("Gameplay");
            if (_mapGameplay == null) UnityEngine.Debug.LogError($"[PlayerController] The project-wide Input Action Asset « {InputActionAsset.name} » doesn't have a « Gameplay » map.");
            
            _mapUI = InputActionAsset.FindActionMap("UI");
            if (_mapUI == null) UnityEngine.Debug.LogError($"[PlayerController] The project-wide Input Action Asset « {InputActionAsset.name} » doesn't have a « UI » map.");
        }

        private void BindInputActions()
        {
            
        }
    }
}
