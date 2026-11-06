
using System.Collections.Generic;
using UnityEngine;

namespace SynthOfRage.Scripts.Gameplay.Unit.Player
{
    public class PlayerManager : MonoBehaviour
    {
        [Header("Players")]
        [Tooltip("Liste de tous les joueurs de la scène. Un joueur désactivé est automatiquement ignoré.")]
        [SerializeField] private List<PlayerMovement> players = new();

        private readonly List<PlayerMovement> activePlayers = new();

        public IReadOnlyList<PlayerMovement> Players => players;

        public int ActivePlayerCount
        {
            get
            {
                RefreshActivePlayers();
                return activePlayers.Count;
            }
        }

        /// <summary>
        /// Rebuilds the active player list.
        /// A player is active only if its GameObject is active in the hierarchy
        /// and its PlayerMovement component is enabled.
        /// </summary>
        public void RefreshActivePlayers()
        {
            activePlayers.Clear();

            if (players == null)
                return;

            foreach (PlayerMovement player in players)
            {
                if (player == null)
                    continue;

                if (!player.gameObject.activeInHierarchy || !player.enabled)
                    continue;

                if (!activePlayers.Contains(player))
                    activePlayers.Add(player);
            }
        }

        /// <summary>
        /// Copies active players into an external reusable list.
        /// </summary>
        public void GetActivePlayers(List<PlayerMovement> results)
        {
            if (results == null)
                return;

            RefreshActivePlayers();

            results.Clear();
            results.AddRange(activePlayers);
        }

        /// <summary>
        /// Returns the active player at the requested index.
        /// </summary>
        public PlayerMovement GetActivePlayer(int index)
        {
            RefreshActivePlayers();

            if (index < 0 || index >= activePlayers.Count)
                return null;

            return activePlayers[index];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (players == null)
                players = new List<PlayerMovement>();

            // Remove null references and duplicate entries.
            HashSet<PlayerMovement> uniquePlayers = new();

            for (int i = players.Count - 1; i >= 0; i--)
            {
                PlayerMovement player = players[i];

                if (player == null || !uniquePlayers.Add(player))
                    players.RemoveAt(i);
            }
        }
#endif
    }
}
