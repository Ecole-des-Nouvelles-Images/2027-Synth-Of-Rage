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
        // Events
        // ─────────────────────────────────────────────

        public event Action<Vector2> OnPlayerMove;
        public event Action OnPlayerJump;
        public event Action OnPlayerDash;
        public event Action OnPlayerAtkL;
        public event Action OnPlayerAtkH;
        public event Action<bool> OnPlayerGuard;
        public event Action OnPlayerAtkSp;

        // ─────────────────────────────────────────────
        // Unity
        // ─────────────────────────────────────────────

        private void OnEnable()
        {
            if (playerCore == null)
            {
                Debug.LogError(
                    $"[{nameof(PlayerObserver)}] PlayerCore reference is missing.",
                    this
                );

                return;
            }

            playerCore.OnPlayerMove += HandlePlayerMove;
            playerCore.OnPlayerJump += HandlePlayerJump;
            playerCore.OnPlayerDash += HandlePlayerDash;
            playerCore.OnPlayerAtkL += HandlePlayerAtkL;
            playerCore.OnPlayerAtkH += HandlePlayerAtkH;
            playerCore.OnPlayerGuard += HandlePlayerGuard;
            playerCore.OnPlayerAtkSp += HandlePlayerAtkSp;
        }

        private void OnDisable()
        {
            if (playerCore == null)
                return;

            playerCore.OnPlayerMove -= HandlePlayerMove;
            playerCore.OnPlayerJump -= HandlePlayerJump;
            playerCore.OnPlayerDash -= HandlePlayerDash;
            playerCore.OnPlayerAtkL -= HandlePlayerAtkL;
            playerCore.OnPlayerAtkH -= HandlePlayerAtkH;
            playerCore.OnPlayerGuard -= HandlePlayerGuard;
            playerCore.OnPlayerAtkSp -= HandlePlayerAtkSp;
        }

        // ─────────────────────────────────────────────
        // PlayerCore Callbacks
        // ─────────────────────────────────────────────

        private void HandlePlayerMove(Vector2 direction)
        {
            if (debugEnabled)
                Debug.Log($"[PlayerObserver] OnPlayerMove : {direction}");

            OnPlayerMove?.Invoke(direction);
        }

        private void HandlePlayerJump()
        {
            if (debugEnabled)
                Debug.Log("[PlayerObserver] OnPlayerJump");

            OnPlayerJump?.Invoke();
        }

        private void HandlePlayerDash()
        {
            if (debugEnabled)
                Debug.Log("[PlayerObserver] OnPlayerDash");

            OnPlayerDash?.Invoke();
        }

        private void HandlePlayerAtkL()
        {
            if (debugEnabled)
                Debug.Log("[PlayerObserver] OnPlayerAtkL");

            OnPlayerAtkL?.Invoke();
        }

        private void HandlePlayerAtkH()
        {
            if (debugEnabled)
                Debug.Log("[PlayerObserver] OnPlayerAtkH");

            OnPlayerAtkH?.Invoke();
        }

        private void HandlePlayerGuard(bool isGuarding)
        {
            if (debugEnabled)
                Debug.Log($"[PlayerObserver] OnPlayerGuard : {isGuarding}");

            OnPlayerGuard?.Invoke(isGuarding);
        }

        private void HandlePlayerAtkSp()
        {
            if (debugEnabled)
                Debug.Log("[PlayerObserver] OnPlayerAtkSp");

            OnPlayerAtkSp?.Invoke();
        }
    }
}