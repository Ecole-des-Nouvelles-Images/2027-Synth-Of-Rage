
using System.Collections;
using System.Collections.Generic;
using SynthOfRage.Scripts.Gameplay.Common.Modules;
using SynthOfRage.Scripts.Gameplay.Unit.Player;
using UnityEngine;

namespace SynthOfRage.Scripts.Gameplay.Interaction
{
    /// <summary>
    /// FallingPlayerTrigger
    ///
    /// Lorsqu'un joueur entre dans le trigger :
    /// 1) Annule immédiatement tous ses déplacements en cours.
    /// 2) Immobilise le joueur.
    /// 3) Lui inflige les dégâts configurés.
    /// 4) Le téléporte immédiatement au Respawn Point.
    /// 5) Le maintient immobile dans les airs.
    /// 6) Le fait clignoter pendant le délai de respawn.
    /// 7) Lui applique une invulnérabilité complète pendant cette période.
    /// 8) Restaure son état initial à la fin du délai.
    ///
    /// Chaque joueur est traité indépendamment.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class FallingPlayerTrigger : MonoBehaviour
    {
        // ============================================================
        // RESPAWN
        // ============================================================

        [Header("Respawn")]

        [Tooltip("Point de réapparition. Placez un Transform enfant du trigger.")]
        [SerializeField] private Transform respawnPoint;

        [Tooltip("Durée pendant laquelle le joueur reste immobilisé et invulnérable.")]
        [Min(0f)]
        [SerializeField] private float respawnDuration = 1.5f;

        [Tooltip("Conserver la rotation actuelle du joueur au respawn.")]
        [SerializeField] private bool preservePlayerRotation;


        // ============================================================
        // DAMAGE
        // ============================================================

        [Header("Damage")]

        [Tooltip("Dégâts infligés au joueur lors de la chute.")]
        [Min(0)]
        [SerializeField] private int damage = 10;

        [Tooltip("Durée du stun appliqué par HealthModule.ReceiveHit.")]
        [Min(0f)]
        [SerializeField] private float damageStunDuration = 0f;


        // ============================================================
        // BLINK
        // ============================================================

        [Header("Blink")]

        [Tooltip("Active le clignotement du joueur pendant le respawn.")]
        [SerializeField] private bool blinkPlayer = true;

        [Tooltip("Intervalle entre chaque alternance visible/invisible.")]
        [Min(0.01f)]
        [SerializeField] private float blinkInterval = 0.1f;

        [Tooltip("Le clignotement ignore Time.timeScale.")]
        [SerializeField] private bool blinkIgnoresTimeScale = true;


        // ============================================================
        // DETECTION
        // ============================================================

        [Header("Detection")]

        [Tooltip("Collider utilisé pour détecter les joueurs.")]
        [SerializeField] private Collider triggerVolume;

        [Tooltip("Autorise un nouveau déclenchement après être sorti du trigger.")]
        [SerializeField] private bool allowRetriggerAfterExit = true;


        // ============================================================
        // DEBUG
        // ============================================================

        [Header("Debug")]

        [SerializeField] private bool showDebugLogs = false;


        // ============================================================
        // PLAYER STATE
        // ============================================================

        private sealed class PlayerRespawnState
        {
            public PlayerMovement Player;
            public CharacterController Controller;
            public HealthModule Health;

            public bool MovementWasEnabled;
            public bool ControllerWasEnabled;

            public SpriteRenderer[] Renderers;
            public bool[] RendererWasEnabled;

            public bool EnabledInvincibilityByTrigger;

            public Quaternion OriginalRotation;
        }

        private readonly HashSet<PlayerMovement> _playersInside =
            new HashSet<PlayerMovement>();

        private readonly HashSet<PlayerMovement> _playersBeingProcessed =
            new HashSet<PlayerMovement>();

        private readonly Dictionary<PlayerMovement, PlayerRespawnState> _activePlayers =
            new Dictionary<PlayerMovement, PlayerRespawnState>();


        // ============================================================
        // INITIALIZATION
        // ============================================================

        private void Reset()
        {
            triggerVolume = GetComponent<Collider>();

            if (triggerVolume != null)
                triggerVolume.isTrigger = true;

            FindRespawnPoint();
        }

        private void Awake()
        {
            if (triggerVolume == null)
                triggerVolume = GetComponent<Collider>();

            if (respawnPoint == null)
                FindRespawnPoint();

            if (triggerVolume != null)
                triggerVolume.isTrigger = true;

            if (respawnPoint == null)
            {
                UnityEngine.Debug.LogError(
                    $"[{nameof(FallingPlayerTrigger)}] {name}: RespawnPoint manquant.",
                    this);
            }
        }

        private void FindRespawnPoint()
        {
            Transform found = transform.Find("RespawnPoint");

            if (found != null)
                respawnPoint = found;
        }


        // ============================================================
        // TRIGGER DETECTION
        // ============================================================

        private void OnTriggerEnter(Collider other)
        {
            TryProcessPlayer(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!allowRetriggerAfterExit)
                return;

            PlayerMovement player =
                other.GetComponentInParent<PlayerMovement>();

            if (player == null)
                return;

            // Plusieurs colliders peuvent appartenir au même joueur.
            // On attend que tous aient quitté la zone.

            if (!HasOtherColliderInside(player, other))
            {
                _playersInside.Remove(player);

                if (showDebugLogs)
                {
                    UnityEngine.Debug.Log(
                        $"[{nameof(FallingPlayerTrigger)}] {player.name}: sortie du trigger.",
                        this);
                }
            }
        }

        private void TryProcessPlayer(Collider other)
        {
            PlayerMovement player =
                other.GetComponentInParent<PlayerMovement>();

            if (player == null || !player.isActiveAndEnabled)
                return;

            // Empêche plusieurs traitements simultanés.
            if (_playersBeingProcessed.Contains(player))
                return;

            // Empêche les déclenchements multiples à l'intérieur.
            if (!_playersInside.Add(player))
                return;

            if (respawnPoint == null)
            {
                UnityEngine.Debug.LogError(
                    $"[{nameof(FallingPlayerTrigger)}] {name}: RespawnPoint manquant.",
                    this);

                _playersInside.Remove(player);
                return;
            }

            StartCoroutine(ProcessFallingPlayer(player));
        }


        // ============================================================
        // MAIN RESPAWN ROUTINE
        // ============================================================

        private IEnumerator ProcessFallingPlayer(PlayerMovement player)
        {
            if (player == null)
                yield break;

            _playersBeingProcessed.Add(player);

            // --------------------------------------------------------
            // COMPONENT REFERENCES
            // --------------------------------------------------------

            CharacterController controller =
                player.GetComponent<CharacterController>();

            if (controller == null)
                controller = player.GetComponentInChildren<CharacterController>();

            HealthModule health =
                player.GetComponent<HealthModule>();

            if (health == null)
                health = player.GetComponentInChildren<HealthModule>();

            SpriteRenderer[] renderers =
                player.GetComponentsInChildren<SpriteRenderer>(true);


            // --------------------------------------------------------
            // SAVE INITIAL STATE
            // --------------------------------------------------------

            PlayerRespawnState state = new PlayerRespawnState
            {
                Player = player,
                Controller = controller,
                Health = health,

                MovementWasEnabled = player.enabled,

                ControllerWasEnabled =
                    controller != null && controller.enabled,

                Renderers = renderers,

                RendererWasEnabled = new bool[renderers.Length],

                OriginalRotation = player.transform.rotation
            };

            for (int i = 0; i < renderers.Length; i++)
            {
                state.RendererWasEnabled[i] =
                    renderers[i] != null && renderers[i].enabled;
            }

            _activePlayers[player] = state;


            // --------------------------------------------------------
            // STEP 1 - CANCEL ALL ACTIVE MOVEMENT
            // --------------------------------------------------------

            // Annule les déplacements résiduels avant toute désactivation.
            // Cela comprend le mouvement horizontal, le saut, la gravité,
            // le dash et le transport par ascenseur.

            player.CancelMovementImmediately();


            // --------------------------------------------------------
            // STEP 2 - IMMOBILIZE PLAYER
            // --------------------------------------------------------

            // Désactive les déplacements, la gravité et les actions
            // gérés par PlayerMovement.

            player.enabled = false;

            // Désactive les mouvements physiques du CharacterController.

            if (controller != null)
                controller.enabled = false;


            // --------------------------------------------------------
            // STEP 3 - APPLY DAMAGE
            // --------------------------------------------------------

            // Les dégâts sont appliqués une seule fois, au début
            // du traitement, avant l'activation de l'invulnérabilité.

            if (health != null && damage > 0)
            {
                health.ReceiveHit(
                    damage,
                    transform.position,
                    0f,
                    0f,
                    AnimationCurve.Linear(0f, 0f, 1f, 1f),
                    damageStunDuration,
                    0f,
                    blinkInterval,
                    Color.white);
            }
            else if (health == null && showDebugLogs)
            {
                UnityEngine.Debug.LogWarning(
                    $"[{nameof(FallingPlayerTrigger)}] {player.name}: HealthModule introuvable.",
                    player);
            }


            // --------------------------------------------------------
            // STEP 4 - ENABLE DAMAGE IMMUNITY
            // --------------------------------------------------------

            // On conserve une éventuelle invulnérabilité préexistante.
            // On ne désactive que celle que ce trigger a lui-même activée.

            if (health != null && !health.IsAttackInvincible)
            {
                health.EnableAttackInvincibility();

                state.EnabledInvincibilityByTrigger = true;
            }


            // --------------------------------------------------------
            // STEP 5 - TELEPORT IMMEDIATELY
            // --------------------------------------------------------

            player.transform.position = respawnPoint.position;

            player.transform.rotation = preservePlayerRotation
                ? state.OriginalRotation
                : respawnPoint.rotation;

            Physics.SyncTransforms();


            if (showDebugLogs)
            {
                UnityEngine.Debug.Log(
                    $"[{nameof(FallingPlayerTrigger)}] {player.name}: " +
                    $"déplacement annulé, dégâts appliqués ({damage}), " +
                    $"téléportation effectuée. Respawn : {respawnDuration:F2}s.",
                    this);
            }


            // --------------------------------------------------------
            // STEP 6 - BLINK DURING RESPAWN
            // --------------------------------------------------------

            // Le joueur reste à sa position de respawn.
            // PlayerMovement et CharacterController restent désactivés.
            // HealthModule conserve son invulnérabilité.

            float elapsed = 0f;
            float nextBlink = blinkInterval;

            bool visible = true;

            while (elapsed < respawnDuration)
            {
                float deltaTime = blinkIgnoresTimeScale
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

                elapsed += deltaTime;

                if (blinkPlayer && elapsed >= nextBlink)
                {
                    visible = !visible;

                    nextBlink = elapsed + blinkInterval;

                    for (int i = 0; i < renderers.Length; i++)
                    {
                        if (renderers[i] != null)
                        {
                            renderers[i].enabled =
                                visible && state.RendererWasEnabled[i];
                        }
                    }
                }

                yield return null;
            }


            // --------------------------------------------------------
            // STEP 7 - RESTORE PLAYER
            // --------------------------------------------------------

            RestorePlayerState(state);

            _activePlayers.Remove(player);
            _playersBeingProcessed.Remove(player);


            // --------------------------------------------------------
            // STEP 8 - UPDATE TRIGGER STATE
            // --------------------------------------------------------

            // Synchronisation physique pour éviter de dépendre
            // exclusivement des événements OnTriggerExit.

            Physics.SyncTransforms();

            yield return new WaitForFixedUpdate();

            if (player != null && allowRetriggerAfterExit)
            {
                // Si le joueur est sorti de la zone pendant son respawn,
                // il peut immédiatement déclencher un nouveau traitement.

                if (!HasOtherColliderInside(player, null))
                {
                    _playersInside.Remove(player);
                }
            }

            if (showDebugLogs && player != null)
            {
                UnityEngine.Debug.Log(
                    $"[{nameof(FallingPlayerTrigger)}] {player.name}: respawn terminé.",
                    this);
            }
        }


        // ============================================================
        // RESTORE PLAYER STATE
        // ============================================================

        private void RestorePlayerState(PlayerRespawnState state)
        {
            if (state == null)
                return;

            // Restore sprite visibility.

            if (state.Renderers != null)
            {
                for (int i = 0; i < state.Renderers.Length; i++)
                {
                    if (state.Renderers[i] != null)
                    {
                        state.Renderers[i].enabled =
                            state.RendererWasEnabled[i];
                    }
                }
            }

            // Restore CharacterController.

            if (state.Controller != null)
            {
                state.Controller.enabled =
                    state.ControllerWasEnabled;
            }

            // Restore PlayerMovement.

            if (state.Player != null)
            {
                state.Player.enabled =
                    state.MovementWasEnabled;
            }

            // Remove only the invulnerability granted by this trigger.

            if (state.EnabledInvincibilityByTrigger &&
                state.Health != null)
            {
                state.Health.DisableAttackInvincibility();
            }
        }


        // ============================================================
        // COLLIDER DETECTION
        // ============================================================

        private bool HasOtherColliderInside(
            PlayerMovement player,
            Collider exitingCollider)
        {
            if (player == null || triggerVolume == null)
                return false;

            Collider[] playerColliders =
                player.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < playerColliders.Length; i++)
            {
                Collider candidate = playerColliders[i];

                if (candidate == null ||
                    candidate == exitingCollider ||
                    !candidate.enabled ||
                    !candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (triggerVolume.bounds.Intersects(candidate.bounds))
                    return true;
            }

            return false;
        }


        // ============================================================
        // CLEANUP
        // ============================================================

        private void OnDisable()
        {
            // Si le trigger est désactivé pendant un respawn,
            // on restaure les joueurs pour éviter de les bloquer.

            StopAllCoroutines();

            foreach (var entry in _activePlayers)
            {
                RestorePlayerState(entry.Value);
            }

            _activePlayers.Clear();
            _playersBeingProcessed.Clear();
            _playersInside.Clear();
        }


        // ============================================================
        // VALIDATION
        // ============================================================

        private void OnValidate()
        {
            if (triggerVolume == null)
                triggerVolume = GetComponent<Collider>();

            respawnDuration = Mathf.Max(0f, respawnDuration);

            damage = Mathf.Max(0, damage);

            damageStunDuration = Mathf.Max(0f, damageStunDuration);

            blinkInterval = Mathf.Max(0.01f, blinkInterval);
        }


        // ============================================================
        // GIZMOS
        // ============================================================

        private void OnDrawGizmosSelected()
        {
            Transform point = respawnPoint;

            if (point == null)
                point = transform.Find("RespawnPoint");

            if (point == null)
                return;

            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(point.position, 0.25f);

            Gizmos.DrawLine(
                point.position,
                point.position + point.forward * 0.75f);
        }
    }
}
