
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SynthOfRage.Scripts.Gameplay.Unit.Player;
using SynthOfRage.Scripts.Rendering;

namespace SynthOfRage.Scripts.Gameplay.Interaction
{
    /// <summary>
    /// Trigger d'ascenseur :
    /// 1) La détection des joueurs actifs est effectuée via PlayerManager.
    /// 2) Selon le Detection Mode :
    ///    - AllPlayersInside : attend que tous les joueurs actifs soient dedans.
    ///    - FirstPlayerEnters : déclenche dès qu'au moins un joueur actif entre.
    ///    Un délai optionnel s'écoule (annulé si la condition n'est plus remplie),
    ///    puis l'animation de l'ascenseur et les événements sont déclenchés.
    /// 3) Au début du déplacement (Animation Event B), chaque joueur présent
    ///    est transporté avec le déplacement de la cabine via son CharacterController.
    /// 4) À l'arrivée (Animation Event C), le transport est arrêté.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class AllPlayersTrigger : MonoBehaviour
    {
        public enum DetectionMode
        {
            [InspectorName("All Players Inside")]
            AllPlayersInside,

            [InspectorName("First Player Enters")]
            FirstPlayerEnters
        }

        [Header("References")]
        [SerializeField] private PlayerManager playerManager;
        [SerializeField] private Collider triggerVolume;

        [Header("Detection")]
        [Tooltip("All Players Inside : attend tous les joueurs actifs. First Player Enters : déclenche dès qu'un joueur actif entre.")]
        [SerializeField] private DetectionMode detectionMode = DetectionMode.AllPlayersInside;

        [Tooltip("Optionnel : offset local appliqué à la position racine de chaque joueur pour le test d'appartenance.")]
        [SerializeField] private Transform detectionPointOverride;

        [Min(0f)]
        [SerializeField] private float refreshInterval = 0.05f;

        [Min(0f)]
        [SerializeField] private float boundsTolerance = 0.02f;

        [Header("Activation Delay")]
        [Tooltip("Délai (secondes) entre la condition remplie et le déclenchement. Le délai est annulé si la condition n'est plus remplie. 0 = immédiat.")]
        [Min(0f)]
        [SerializeField] private float activationDelay = 1f;

        [Tooltip("Si coché, le délai ignore Time.timeScale (temps réel). Sinon il suit le temps du gameplay.")]
        [SerializeField] private bool delayIgnoresTimeScale;

        [Header("Carry (Ascenseur / Plateforme mouvante)")]
        [Tooltip("Si coché, chaque joueur présent au début du déplacement est transporté avec la cabine.")]
        [SerializeField] private bool carryPlayersWhileInside = true;

        [Tooltip("Transform animé de la cabine. Si vide, le transform de ce GameObject est utilisé.")]
        [SerializeField] private Transform playerCarryTarget;

        [Tooltip("Arrête le transport d'un joueur quand il sort du trigger.")]
        [SerializeField] private bool stopCarryingOnExit = true;

        [Header("Behaviour")]
        [SerializeField] private bool repeatWhileAllInside;

        [Min(0.05f)]
        [SerializeField] private float repeatInterval = 1f;

        [Header("Animation (optional)")]
        [SerializeField] private Animator targetAnimator;

        [Tooltip("Animator Trigger déclenché après le délai, quand la condition du Detection Mode est remplie.")]
        [SerializeField] private string animatorTriggerName = "AllPlayersInside";

        [Tooltip("Optionnel : Animator Bool vrai tant que la condition du Detection Mode est remplie (sans délai).")]
        [SerializeField] private string animatorBoolName = "";

        [Header("Events")]
        [Tooltip("Déclenché après le délai d'activation (avec l'Animator Trigger), lorsque la condition du Detection Mode est remplie.")]
        [SerializeField] private UnityEvent onAllPlayersInside;

        [Tooltip("Déclenché quand la condition du Detection Mode n'est plus remplie (annule aussi le délai en cours).")]
        [SerializeField] private UnityEvent onNotAllPlayersInside;

        [SerializeField] private UnityEvent<int, int> onOccupancyChanged;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs;
        [SerializeField] private bool drawGizmos = true;

        [SerializeField] private Color gizmoWaitingColor = new Color(0.1f, 0.9f, 1f, 0.9f);
        [SerializeField] private Color gizmoPendingColor = new Color(1f, 0.85f, 0.2f, 0.9f);
        [SerializeField] private Color gizmoReadyColor = new Color(0.2f, 1f, 0.25f, 0.9f);

        // Liste fournie par PlayerManager.
        private readonly List<PlayerMovement> _activePlayers = new List<PlayerMovement>();

        // Liste des joueurs réellement actifs et valides.
        private readonly List<PlayerMovement> _validPlayers = new List<PlayerMovement>();

        // Présence détectée directement par référence de PlayerMovement.
        private readonly HashSet<PlayerMovement> _insidePlayers = new HashSet<PlayerMovement>();

        // Joueurs actuellement transportés par la cabine.
        private readonly HashSet<PlayerMovement> _carriedPlayers =
            new HashSet<PlayerMovement>();

        // Empêche de réattacher un joueur libéré à C tant qu'il n'a pas quitté le trigger.
        private readonly HashSet<PlayerMovement> _releasedUntilExit =
            new HashSet<PlayerMovement>();

        // Position monde de la cabine à la frame précédente.
        private Vector3 _previousCarryPosition;
        private bool _hasPreviousCarryPosition;

        private bool _transportInProgress;

        private bool _allPlayersInside;
        private bool _activationPending;
        private float _activationTimer;
        private bool _hasActivated;

        private float _nextRefreshTime;
        private float _nextRepeatTime;

        private int _lastActiveCount = -1;
        private int _lastInsideCount = -1;

        private int _triggerHash;
        private int _boolHash;

        private bool _hasTrigger;
        private bool _hasBool;
        
        [SerializeField] private VolumeWeightInterpolator _volumeInterpolator;

        public bool AllPlayersInside => _allPlayersInside;
        public bool ActivationPending => _activationPending;
        public int ActivePlayerCount => _validPlayers.Count;
        public int InsidePlayerCount => _insidePlayers.Count;

        private void Reset()
        {
            triggerVolume = GetComponent<Collider>();

            if (triggerVolume != null)
                triggerVolume.isTrigger = true;
        }

        private void Awake()
        {
            if (triggerVolume == null)
                triggerVolume = GetComponent<Collider>();

            if (triggerVolume != null && !triggerVolume.isTrigger)
            {
                UnityEngine.Debug.LogWarning(
                    $"[{nameof(AllPlayersTrigger)}] {name}: enabling Is Trigger on {triggerVolume.name}.",
                    this);

                triggerVolume.isTrigger = true;
            }

            if (playerCarryTarget == null)
                playerCarryTarget = transform;

            CacheAnimatorParameters();
        }

        private void OnEnable()
        {
            _allPlayersInside = false;
            _activationPending = false;
            _activationTimer = 0f;

            _hasActivated = false;
            _transportInProgress = false;
            _hasPreviousCarryPosition = false;

            _nextRefreshTime = 0f;
            _nextRepeatTime = 0f;

            _lastActiveCount = -1;
            _lastInsideCount = -1;

            _insidePlayers.Clear();
            _validPlayers.Clear();

            RefreshOccupancy(true);
        }

        private void Update()
        {
            // 1) Détection périodique.
            if (refreshInterval <= 0f || Time.unscaledTime >= _nextRefreshTime)
            {
                _nextRefreshTime = Time.unscaledTime + refreshInterval;
                RefreshOccupancy(false);
            }

            // 2) Compte à rebours du délai d'activation.
            if (_activationPending)
            {
                _activationTimer -= delayIgnoresTimeScale
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

                if (_activationTimer <= 0f)
                {
                    _activationPending = false;

                    // Re-vérification : la condition doit toujours être remplie.
                    if (_allPlayersInside)
                        Activate();
                }
            }

            // 3) Répétition optionnelle.
            // Elle ne commence qu'après une première activation effective.
            if (_allPlayersInside &&
                _hasActivated &&
                !_activationPending &&
                repeatWhileAllInside &&
                Time.unscaledTime >= _nextRepeatTime)
            {
                _nextRepeatTime = Time.unscaledTime + repeatInterval;
                InvokeReady();
            }
        }

        /// <summary>
        /// Applique aux joueurs le déplacement monde exact de la cabine.
        /// Fonctionne avec les déplacements diagonaux.
        /// </summary>
        private void LateUpdate()
        {
            if (!_transportInProgress ||
                _carriedPlayers.Count == 0 ||
                playerCarryTarget == null)
            {
                return;
            }

            Vector3 currentCarryPosition = playerCarryTarget.position;

            if (!_hasPreviousCarryPosition)
            {
                _previousCarryPosition = currentCarryPosition;
                _hasPreviousCarryPosition = true;
                return;
            }

            // Déplacement réel de la cabine depuis la frame précédente.
            Vector3 cabinDisplacement = currentCarryPosition - _previousCarryPosition;

            if (cabinDisplacement.sqrMagnitude > 0.0000001f)
            {
                List<PlayerMovement> invalidPlayers = null;

                foreach (PlayerMovement player in _carriedPlayers)
                {
                    if (player == null ||
                        !player.isActiveAndEnabled ||
                        !player.gameObject.activeInHierarchy)
                    {
                        invalidPlayers ??= new List<PlayerMovement>();
                        invalidPlayers.Add(player);
                        continue;
                    }

                    player.ApplyElevatorTransportMovement(cabinDisplacement);
                }

                if (invalidPlayers != null)
                {
                    for (int i = 0; i < invalidPlayers.Count; i++)
                        StopCarryingPlayer(invalidPlayers[i], true);
                }
            }

            // Mise à jour de la position de référence.
            _previousCarryPosition = currentCarryPosition;
        }

        private void OnDisable()
        {
            // Sécurité : on libère systématiquement tous les joueurs.
            ReleaseAllCarriedPlayers();

            _releasedUntilExit.Clear();
            _insidePlayers.Clear();
            _validPlayers.Clear();

            _activationPending = false;
            _hasActivated = false;
            _transportInProgress = false;
            _hasPreviousCarryPosition = false;

            if (targetAnimator != null && _hasBool)
                targetAnimator.SetBool(_boolHash, false);
        }

        public void ForceRefresh()
        {
            RefreshOccupancy(false);
        }

        // =========================================================
        // DETECTION
        // =========================================================

        /// <summary>
        /// Actualise la liste des joueurs actifs et leur présence dans le trigger.
        /// </summary>
        private void RefreshOccupancy(bool forceEvents)
        {
            if (playerManager == null || triggerVolume == null)
            {
                if (showDebugLogs)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[{nameof(AllPlayersTrigger)}] {name}: assign PlayerManager and Trigger Collider.",
                        this);
                }

                ReleaseAllCarriedPlayers();

                _insidePlayers.Clear();
                _validPlayers.Clear();
                _activePlayers.Clear();

                SetReady(false, forceEvents);
                return;
            }

            _activePlayers.Clear();
            _validPlayers.Clear();
            _insidePlayers.Clear();

            playerManager.GetActivePlayers(_activePlayers);

            // Déduplication : un joueur ne doit jamais être compté deux fois.
            HashSet<PlayerMovement> uniquePlayers = new HashSet<PlayerMovement>();

            for (int i = 0; i < _activePlayers.Count; i++)
            {
                PlayerMovement player = _activePlayers[i];

                if (player == null ||
                    !player.isActiveAndEnabled ||
                    !player.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!uniquePlayers.Add(player))
                    continue;

                _validPlayers.Add(player);

                bool isInside = IsPlayerInsideTrigger(player);

                if (isInside)
                {
                    _insidePlayers.Add(player);
                }
                else
                {
                    // Une sortie autorise un prochain transport.
                    _releasedUntilExit.Remove(player);

                    StopCarryingPlayer(player);
                }
            }

            // Joueurs retirés/désactivés pendant qu'ils étaient transportés : on les libère.
            List<PlayerMovement> stalePlayers = null;

            foreach (PlayerMovement player in _carriedPlayers)
            {
                if (player == null || !uniquePlayers.Contains(player))
                {
                    stalePlayers ??= new List<PlayerMovement>();
                    stalePlayers.Add(player);
                }
            }

            if (stalePlayers != null)
            {
                for (int i = 0; i < stalePlayers.Count; i++)
                    StopCarryingPlayer(stalePlayers[i], true);
            }

            int total = _validPlayers.Count;
            int inside = _insidePlayers.Count;

            if (total != _lastActiveCount || inside != _lastInsideCount)
            {
                onOccupancyChanged?.Invoke(inside, total);

                if (showDebugLogs)
                {
                    UnityEngine.Debug.Log(
                        $"[{nameof(AllPlayersTrigger)}] {name}: {inside}/{total} active players inside (mode: {detectionMode}).",
                        this);
                }

                _lastActiveCount = total;
                _lastInsideCount = inside;
            }

            SetReady(IsActivationConditionMet(total, inside), forceEvents);
        }

        /// <summary>
        /// Détermine si un joueur est physiquement présent dans le volume.
        ///
        /// Si un Detection Point Override est assigné, il est prioritaire.
        /// Sinon, les colliders actifs du personnage sont utilisés.
        /// Cela évite de dépendre uniquement du pivot racine du joueur.
        /// </summary>
        private bool IsPlayerInsideTrigger(PlayerMovement player)
        {
            if (player == null || triggerVolume == null)
                return false;

            // Mode personnalisé : test d'un point précis du joueur.
            if (detectionPointOverride != null)
            {
                Vector3 point = player.transform.TransformPoint(
                    detectionPointOverride.localPosition);

                Vector3 closest = triggerVolume.ClosestPoint(point);

                return (closest - point).sqrMagnitude <=
                       boundsTolerance * boundsTolerance;
            }

            // Récupération de tous les colliders du joueur.
            Collider[] playerColliders =
                player.GetComponentsInChildren<Collider>(false);

            bool hasUsableCollider = false;

            for (int i = 0; i < playerColliders.Length; i++)
            {
                Collider playerCollider = playerColliders[i];

                if (playerCollider == null ||
                    !playerCollider.enabled ||
                    !playerCollider.gameObject.activeInHierarchy ||
                    playerCollider == triggerVolume ||
                    playerCollider.isTrigger)
                {
                    continue;
                }

                hasUsableCollider = true;

                // Premier filtre rapide : les volumes englobants doivent se croiser.
                if (!triggerVolume.bounds.Intersects(playerCollider.bounds))
                    continue;

                // Test géométrique précis pour les formes prises en charge.
                bool penetrationDetected = Physics.ComputePenetration(
                    triggerVolume,
                    triggerVolume.transform.position,
                    triggerVolume.transform.rotation,
                    playerCollider,
                    playerCollider.transform.position,
                    playerCollider.transform.rotation,
                    out _,
                    out _
                );

                if (penetrationDetected)
                    return true;

                // Tolérance pour les contacts très proches.
                // Vérifie les points caractéristiques du collider du joueur.
                Bounds bounds = playerCollider.bounds;

                Vector3[] samplePoints =
                {
                    bounds.center,
                    bounds.min,
                    bounds.max,
                    new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                    new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
                    new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
                    new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                    new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
                    new Vector3(bounds.max.x, bounds.min.y, bounds.max.z)
                };

                for (int p = 0; p < samplePoints.Length; p++)
                {
                    Vector3 point = samplePoints[p];
                    Vector3 closest = triggerVolume.ClosestPoint(point);

                    if ((closest - point).sqrMagnitude <=
                        boundsTolerance * boundsTolerance)
                    {
                        return true;
                    }
                }
            }

            // Fallback si le joueur ne possède aucun collider exploitable.
            if (!hasUsableCollider)
            {
                Vector3 point = player.transform.position;
                Vector3 closest = triggerVolume.ClosestPoint(point);

                return (closest - point).sqrMagnitude <=
                       boundsTolerance * boundsTolerance;
            }

            return false;
        }

        /// <summary>
        /// Évalue la condition d'activation selon le mode choisi dans l'Inspector.
        /// </summary>
        private bool IsActivationConditionMet(int total, int inside)
        {
            if (total <= 0 || inside <= 0)
                return false;

            switch (detectionMode)
            {
                case DetectionMode.FirstPlayerEnters:
                    // Un seul joueur actif dans la zone suffit.
                    return inside >= 1;

                case DetectionMode.AllPlayersInside:
                default:
                    return inside >= total;
            }
        }

        /// <summary>
        /// Met à jour l'état de disponibilité du trigger.
        /// </summary>
        private void SetReady(bool ready, bool forceEvents)
        {
            bool changed = _allPlayersInside != ready;
            _allPlayersInside = ready;

            if (targetAnimator != null && _hasBool)
                targetAnimator.SetBool(_boolHash, ready);

            if (ready)
            {
                // Ne jamais redémarrer un délai simplement parce qu'un refresh
                // ou un ForceRefresh a été exécuté.
                if (!changed)
                    return;

                if (activationDelay <= 0f)
                {
                    Activate();
                }
                else
                {
                    _activationPending = true;
                    _activationTimer = activationDelay;

                    if (showDebugLogs)
                    {
                        UnityEngine.Debug.Log(
                            $"[{nameof(AllPlayersTrigger)}] {name}: activation dans {activationDelay:F2}s.",
                            this);
                    }
                }

                return;
            }

            // La condition n'est plus remplie : annulation du délai.
            _activationPending = false;
            _activationTimer = 0f;
            _hasActivated = false;

            if (changed || forceEvents)
                onNotAllPlayersInside?.Invoke();
        }

        private void Activate()
        {
            if (!_allPlayersInside)
                return;

            _activationPending = false;
            _hasActivated = true;
            _nextRepeatTime = Time.unscaledTime + repeatInterval;

            InvokeReady();
        }

        private void InvokeReady()
        {
            if (targetAnimator != null && _hasTrigger)
                targetAnimator.SetTrigger(_triggerHash);

            onAllPlayersInside?.Invoke();

            if (showDebugLogs)
            {
                UnityEngine.Debug.Log(
                    $"[{nameof(AllPlayersTrigger)}] {name}: condition '{detectionMode}' remplie, animation déclenchée.",
                    this);
            }
        }

        // =========================================================
        // CARRY (CHARACTER CONTROLLER)
        // =========================================================

        /// <summary>
        /// Appelé par un Animation Event au point B.
        /// Commence à transporter les joueurs présents avec le mouvement de la cabine.
        /// </summary>
        public void BeginElevatorTransport()
        {
            if (!carryPlayersWhileInside || _transportInProgress)
                return;

            if (playerCarryTarget == null)
            {
                UnityEngine.Debug.LogError(
                    $"[{nameof(AllPlayersTrigger)}] {name}: impossible de démarrer le transport, Player Carry Target manquant.",
                    this);

                return;
            }

            // Actualise la liste avant de prendre la décision.
            RefreshOccupancy(false);

            if (!_allPlayersInside)
            {
                if (showDebugLogs)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[{nameof(AllPlayersTrigger)}] {name}: transport annulé, la condition du Detection Mode n'est plus remplie.",
                        this);
                }

                return;
            }

            _transportInProgress = true;
            _previousCarryPosition = playerCarryTarget.position;
            _hasPreviousCarryPosition = true;

            int attachedCount = 0;
            bool failed = false;

            for (int i = 0; i < _validPlayers.Count; i++)
            {
                PlayerMovement player = _validPlayers[i];

                if (player == null ||
                    !player.isActiveAndEnabled ||
                    !player.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!_insidePlayers.Contains(player))
                    continue;

                // Un joueur libéré à l'arrivée précédente doit sortir du trigger
                // avant de pouvoir être transporté à nouveau.
                if (_releasedUntilExit.Contains(player))
                    continue;

                if (!CarryPlayerIfNeeded(player))
                {
                    failed = true;
                    break;
                }

                attachedCount++;
            }

            if (failed)
            {
                ReleaseAllCarriedPlayers();

                _transportInProgress = false;
                _hasPreviousCarryPosition = false;

                UnityEngine.Debug.LogError(
                    $"[{nameof(AllPlayersTrigger)}] {name}: transport annulé, impossible de transporter tous les joueurs.",
                    this);

                return;
            }

            if (showDebugLogs)
            {
                UnityEngine.Debug.Log(
                    $"[{nameof(AllPlayersTrigger)}] {name}: transport démarré avec {attachedCount} joueur(s).",
                    this);
            }
        }

        /// <summary>
        /// Enregistre un joueur pour le transporter avec la cabine.
        /// </summary>
        private bool CarryPlayerIfNeeded(PlayerMovement player)
        {
            if (!carryPlayersWhileInside || player == null)
                return false;

            if (_carriedPlayers.Contains(player))
                return true;

            if (!player.BeginElevatorTransport())
            {
                UnityEngine.Debug.LogError(
                    $"[{nameof(AllPlayersTrigger)}] {player.name}: impossible de démarrer le transport. CharacterController manquant ou désactivé.",
                    player);

                return false;
            }

            _carriedPlayers.Add(player);

            if (showDebugLogs)
            {
                UnityEngine.Debug.Log(
                    $"[{nameof(AllPlayersTrigger)}] {player.name} transporté avec la cabine via CharacterController.",
                    player);
            }

            return true;
        }

        /// <summary>
        /// Arrête le transport d'un joueur.
        /// </summary>
        private void StopCarryingPlayer(PlayerMovement player, bool force = false)
        {
            if (!force && !stopCarryingOnExit)
                return;

            if (ReferenceEquals(player, null))
                return;

            if (!_carriedPlayers.Remove(player))
                return;

            if (player != null)
                player.EndElevatorTransport();

            if (showDebugLogs && player != null)
            {
                UnityEngine.Debug.Log(
                    $"[{nameof(AllPlayersTrigger)}] {player.name} libéré de la cabine.",
                    this);
            }
        }

        /// <summary>
        /// Libère tous les joueurs actuellement transportés.
        /// </summary>
        private void ReleaseAllCarriedPlayers()
        {
            if (_carriedPlayers.Count == 0)
                return;

            List<PlayerMovement> players =
                new List<PlayerMovement>(_carriedPlayers);

            _carriedPlayers.Clear();

            for (int i = 0; i < players.Count; i++)
            {
                PlayerMovement player = players[i];

                if (player != null)
                    player.EndElevatorTransport();
            }
        }

        /// <summary>
        /// Appelé par un Animation Event au point C.
        /// Arrête le transport et empêche un nouveau transport avant la sortie du trigger.
        /// </summary>
        public void ReleaseAllCarriedPlayersAtDestination()
        {
            foreach (PlayerMovement player in _carriedPlayers)
            {
                if (player != null)
                    _releasedUntilExit.Add(player);
            }

            ReleaseAllCarriedPlayers();

            _transportInProgress = false;
            _hasPreviousCarryPosition = false;

            if (showDebugLogs)
            {
                UnityEngine.Debug.Log(
                    $"[{nameof(AllPlayersTrigger)}] {name}: arrivée, tous les joueurs ont été libérés.",
                    this);
            }
        }

        // =========================================================
        // MISC
        // =========================================================

        private void CacheAnimatorParameters()
        {
            _hasTrigger =
                targetAnimator != null &&
                !string.IsNullOrWhiteSpace(animatorTriggerName);

            _hasBool =
                targetAnimator != null &&
                !string.IsNullOrWhiteSpace(animatorBoolName);

            if (_hasTrigger)
                _triggerHash = Animator.StringToHash(animatorTriggerName);

            if (_hasBool)
                _boolHash = Animator.StringToHash(animatorBoolName);
        }

        private void OnValidate()
        {
            if (triggerVolume == null)
                triggerVolume = GetComponent<Collider>();

            refreshInterval = Mathf.Max(0f, refreshInterval);
            boundsTolerance = Mathf.Max(0f, boundsTolerance);
            activationDelay = Mathf.Max(0f, activationDelay);
            repeatInterval = Mathf.Max(0.05f, repeatInterval);

            CacheAnimatorParameters();
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos)
                return;

            Collider volume =
                triggerVolume != null ? triggerVolume : GetComponent<Collider>();

            if (volume == null)
                return;

            Gizmos.color = _activationPending
                ? gizmoPendingColor
                : _allPlayersInside ? gizmoReadyColor : gizmoWaitingColor;

            if (volume is BoxCollider box)
            {
                Matrix4x4 previous = Gizmos.matrix;

                Gizmos.matrix = box.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);

                Gizmos.matrix = previous;
            }
            else if (volume is SphereCollider sphere)
            {
                Matrix4x4 previous = Gizmos.matrix;

                Gizmos.matrix = sphere.transform.localToWorldMatrix;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);

                Gizmos.matrix = previous;
            }
            else
            {
                Gizmos.DrawWireCube(volume.bounds.center, volume.bounds.size);
            }
        }

        public void InterpolatePpVolumes()
        {
            _volumeInterpolator.TransitionToB();
        }
    }
}
