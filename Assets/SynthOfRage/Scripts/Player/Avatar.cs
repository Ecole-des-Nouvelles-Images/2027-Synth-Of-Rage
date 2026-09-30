using System;
using SynthOfRage.Scripts.Common.Modules;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SynthOfRage.Scripts.Player
{
    public partial class Avatar : MonoBehaviour
    {
        [AutoStaticsCleanup] public static Action<InputAction.CallbackContext> OnMove;
        [AutoStaticsCleanup] public static Action<InputAction.CallbackContext> OnJump;
        [AutoStaticsCleanup] public static Action<InputAction.CallbackContext> OnDash;
        [AutoStaticsCleanup] public static Action<InputAction.CallbackContext> OnGuard;
        [AutoStaticsCleanup] public static Action<InputAction.CallbackContext> OnAttackLight;
        [AutoStaticsCleanup] public static Action<InputAction.CallbackContext> OnAttackHeavy;
        [AutoStaticsCleanup] public static Action<InputAction.CallbackContext> OnSpecial;
        [AutoStaticsCleanup] public static Action<InputAction.CallbackContext> OnInteract;

        public HealthModule HealthModule { get; private set; }
    }
}
