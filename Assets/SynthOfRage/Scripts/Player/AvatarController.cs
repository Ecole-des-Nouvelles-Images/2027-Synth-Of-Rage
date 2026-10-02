using System;
using UnityEngine.InputSystem;

namespace SynthOfRage.Scripts.Player
{
    [Serializable]
    public class AvatarController
    {
        #region Static Debug Events

        public static void ToggleInputEventsDebug()
        {
            throw new NotImplementedException();
        }

        #endregion

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
