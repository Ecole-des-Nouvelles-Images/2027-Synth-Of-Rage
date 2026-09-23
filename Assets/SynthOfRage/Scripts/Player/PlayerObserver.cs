using System;
using UnityEngine;

namespace SynthOfRage.Scripts.Player
{
    public class PlayerObserver : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCore playerCore;

        [Header("Dev Settings")]
        [SerializeField] private bool debugEnabled;

        // ─────────────────────────────────────────────
        // Gameplay Events
        // ─────────────────────────────────────────────

        public event Action<Vector2> OnPlayerMove;
        public event Action OnPlayerJump;
        public event Action OnPlayerDash;
        public event Action OnPlayerAtkL;
        public event Action OnPlayerAtkH;
        public event Action<bool> OnPlayerGuard;
        public event Action OnPlayerAtkSp;
        public event Action OnPlayerPause;

        // ─────────────────────────────────────────────
        // UI Events
        // ─────────────────────────────────────────────

        public event Action<Vector2> OnUIMove;
        public event Action OnUIPause;
        public event Action OnUIValidate;
        public event Action OnUICancel;
        public event Action OnUIPanelPrevious;
        public event Action OnUIPanelNext;

        // ─────────────────────────────────────────────
        // Input Map Events
        // ─────────────────────────────────────────────

        public event Action<PlayerCore.PlayerInputMap> OnInputMapChanged;

        // ─────────────────────────────────────────────
        // Properties
        // ─────────────────────────────────────────────

        public PlayerCore.PlayerInputMap CurrentInputMap
        {
            get
            {
                if (playerCore == null)
                    return PlayerCore.PlayerInputMap.Gameplay;

                return playerCore.CurrentInputMap;
            }
        }

        // ─────────────────────────────────────────────
        // Unity
        // ─────────────────────────────────────────────

        private void OnEnable()
        {
            if (playerCore == null)
            {
                UnityEngine.Debug.LogError(
                    $"[{nameof(PlayerObserver)}] PlayerCore reference is missing.",
                    this
                );

                return;
            }

            // Gameplay
            playerCore.OnPlayerMove += HandlePlayerMove;
            playerCore.OnPlayerJump += HandlePlayerJump;
            playerCore.OnPlayerDash += HandlePlayerDash;
            playerCore.OnPlayerAtkL += HandlePlayerAtkL;
            playerCore.OnPlayerAtkH += HandlePlayerAtkH;
            playerCore.OnPlayerGuard += HandlePlayerGuard;
            playerCore.OnPlayerAtkSp += HandlePlayerAtkSp;
            playerCore.OnPlayerPause += HandlePlayerPause;

            // UI
            playerCore.OnUIMove += HandleUIMove;
            playerCore.OnUIPause += HandleUIPause;
            playerCore.OnUIValidate += HandleUIValidate;
            playerCore.OnUICancel += HandleUICancel;
            playerCore.OnUIPanelPrevious += HandleUIPanelPrevious;
            playerCore.OnUIPanelNext += HandleUIPanelNext;

            // Input Map
            playerCore.OnInputMapChanged += HandleInputMapChanged;
        }

        private void OnDisable()
        {
            if (playerCore == null)
                return;

            // Gameplay
            playerCore.OnPlayerMove -= HandlePlayerMove;
            playerCore.OnPlayerJump -= HandlePlayerJump;
            playerCore.OnPlayerDash -= HandlePlayerDash;
            playerCore.OnPlayerAtkL -= HandlePlayerAtkL;
            playerCore.OnPlayerAtkH -= HandlePlayerAtkH;
            playerCore.OnPlayerGuard -= HandlePlayerGuard;
            playerCore.OnPlayerAtkSp -= HandlePlayerAtkSp;
            playerCore.OnPlayerPause -= HandlePlayerPause;

            // UI
            playerCore.OnUIMove -= HandleUIMove;
            playerCore.OnUIPause -= HandleUIPause;
            playerCore.OnUIValidate -= HandleUIValidate;
            playerCore.OnUICancel -= HandleUICancel;
            playerCore.OnUIPanelPrevious -= HandleUIPanelPrevious;
            playerCore.OnUIPanelNext -= HandleUIPanelNext;

            // Input Map
            playerCore.OnInputMapChanged -= HandleInputMapChanged;
        }

        // ─────────────────────────────────────────────
        // Gameplay Callbacks
        // ─────────────────────────────────────────────

        private void HandlePlayerMove(Vector2 direction)
        {
            if (debugEnabled)
                UnityEngine.Debug.Log(
                    $"[PlayerObserver] OnPlayerMove : {direction}"
                );

            OnPlayerMove?.Invoke(direction);
        }

        private void HandlePlayerJump()
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerObserver] OnPlayerJump");

            OnPlayerJump?.Invoke();
        }

        private void HandlePlayerDash()
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerObserver] OnPlayerDash");

            OnPlayerDash?.Invoke();
        }

        private void HandlePlayerAtkL()
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerObserver] OnPlayerAtkL");

            OnPlayerAtkL?.Invoke();
        }

        private void HandlePlayerAtkH()
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerObserver] OnPlayerAtkH");

            OnPlayerAtkH?.Invoke();
        }

        private void HandlePlayerGuard(bool isGuarding)
        {
            if (debugEnabled)
                UnityEngine.Debug.Log(
                    $"[PlayerObserver] OnPlayerGuard : {isGuarding}"
                );

            OnPlayerGuard?.Invoke(isGuarding);
        }

        private void HandlePlayerAtkSp()
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerObserver] OnPlayerAtkSp");

            OnPlayerAtkSp?.Invoke();
        }

        private void HandlePlayerPause()
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerObserver] OnPlayerPause");

            OnPlayerPause?.Invoke();
        }

        // ─────────────────────────────────────────────
        // UI Callbacks
        // ─────────────────────────────────────────────

        private void HandleUIMove(Vector2 direction)
        {
            if (debugEnabled)
                UnityEngine.Debug.Log(
                    $"[PlayerObserver] OnUIMove : {direction}"
                );

            OnUIMove?.Invoke(direction);
        }

        private void HandleUIPause()
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerObserver] OnUIPause");

            OnUIPause?.Invoke();
        }

        private void HandleUIValidate()
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerObserver] OnUIValidate");

            OnUIValidate?.Invoke();
        }

        private void HandleUICancel()
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerObserver] OnUICancel");

            OnUICancel?.Invoke();
        }

        private void HandleUIPanelPrevious()
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerObserver] OnUIPanelPrevious");

            OnUIPanelPrevious?.Invoke();
        }

        private void HandleUIPanelNext()
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerObserver] OnUIPanelNext");

            OnUIPanelNext?.Invoke();
        }

        // ─────────────────────────────────────────────
        // Input Map Callback
        // ─────────────────────────────────────────────

        private void HandleInputMapChanged(
            PlayerCore.PlayerInputMap inputMap
        )
        {
            if (debugEnabled)
            {
                UnityEngine.Debug.Log(
                    $"[PlayerObserver] OnInputMapChanged : {inputMap}"
                );
            }

            OnInputMapChanged?.Invoke(inputMap);
        }

        // ─────────────────────────────────────────────
        // Input Map Control
        // ─────────────────────────────────────────────

        public void SwitchToGameplay()
        {
            if (playerCore == null)
                return;

            playerCore.SwitchToGameplay();
        }

        public void SwitchToUI()
        {
            if (playerCore == null)
                return;

            playerCore.SwitchToUI();
        }

        public void SwitchInputMap(
            PlayerCore.PlayerInputMap inputMap
        )
        {
            if (playerCore == null)
                return;

            playerCore.SwitchInputMap(inputMap);
        }
    }
}
