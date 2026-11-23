#if UNITY_EDITOR
#endif
using System.Collections;
using System.Collections.Generic;
using _Dev.Vincent.Camera_Setup.Scripts.VCameraSystem.CameraShakes;
using _Old.BPM_System;
using _Old.Gameplay;
using _Old.Gameplay.Core;
using _Old.Gameplay.Unit.Player;
using _Old.Gameplay.Unit.Player.Movement.Utils;
using SynthOfRage.Scripts.Gameplay.SO.Camera;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;
using UnityDebug = UnityEngine.Debug;

namespace _Dev.Vincent.Camera_Setup.Scripts.VCameraSystem
{
    [DefaultExecutionOrder(100)]
    public class VCameraSetup_Follow : MonoBehaviour
    {
        [Header("Cameras")]
        [SerializeField] private CinemachineCamera autoScrollCamera;
        [Tooltip("Parent dédié à la rotation principale de CM_AutoScroll. Le Pan Tilt reste sur la caméra enfant et utilise Parent Object comme Reference Frame.")]
        [SerializeField] private Transform autoScrollRotationRoot;
        [SerializeField] private CinemachineCamera combatCamera;

        [Header("Camera Rail - CS")]
        [Tooltip("Spline physique suivie par la caméra.")]
        [SerializeField] private CinemachineSplineDolly autoScrollDolly;

        [Header("Gameplay Rail - GS")]
        [Tooltip("Spline représentant le véritable parcours de gameplay des joueurs.")]
        [SerializeField] private SplineContainer gameplaySplineContainer;

        [Tooltip("Nombre d'échantillons précalculés pour la projection des joueurs.")]
        [SerializeField, Range(50, 2000)] private int gameplayProjectionSamples = 750;

        [Tooltip("Fenêtre de recherche autour de la progression précédente, en Knots.")]
        [SerializeField, Min(0.1f)] private float gameplayProjectionSearchWindow = 3f;

        [Tooltip("Fenêtre de recherche pour les extrémités des transitions MovementSpace.")]
        [SerializeField, Min(0.1f)] private float blendProjectionSearchWindow = 12f;

        [Tooltip("Distance maximale entre le joueur et la GameplaySpline avant avertissement.")]
        [SerializeField, Min(0f)] private float gameplayProjectionWarningDistance = 5f;

        [Tooltip("Poids de continuité de projection.")]
        [SerializeField, Min(0f)] private float projectionContinuityWeight = 0.2f;

        [SerializeField] private bool debugGameplaySpline;

        [Header("Player Manager")]
        [SerializeField] private PlayerManager playerManager;

        [Header("Initial Movement Space")]
        [SerializeField] private MovementSpace initialMovementSpace;

        [Header("Player Group Leash")]
        [Tooltip("Active la limitation de distance des joueurs par rapport au centre moyen du groupe.")]
        [SerializeField] private bool playerLeashEnabled = true;

        [Tooltip("Rayon maximal autorisé autour du centre moyen de tous les joueurs actifs.")]
        [SerializeField, Min(0.1f)] private float maximumPlayerSpacing = 10f;

        [Tooltip("Temps de lissage de la correction du leash. 0 applique la correction immédiatement.")]
        [SerializeField, Min(0f)] private float playerLeashSmoothTime = 0.08f;

        [Tooltip("Vitesse maximale de correction du leash. 0 signifie illimitée.")]
        [SerializeField, Min(0f)] private float playerLeashMaxCorrectionSpeed = 30f;

        [SerializeField] private bool debugPlayerLeash;

        [Header("Player 1 Respawn Protection")]
        [SerializeField] private bool protectCameraDuringPlayer1Respawn = true;
        [SerializeField] private float player1FallDetectionHeight = -10f;
        [SerializeField, Min(0f)] private float player1RespawnRecoveryDelay = 0.5f;
        [SerializeField, Min(0f)] private float player1RespawnDetectionDistance = 5f;
        [SerializeField, Min(0f)] private float player1RespawnRecoverySmoothTime = 1f;
        [SerializeField] private bool debugPlayer1Respawn;

        [Header("Multiplayer Camera Constraint")]
        [Tooltip("Maintient tous les joueurs dans les limites relatives à la caméra.")]
        [SerializeField] private bool constrainAllPlayersToCamera = true;

        [Header("Camera Boundary Offsets")]
        [Tooltip("Décalage du mur gauche. Positif = autorise le joueur à sortir de l'écran.")]
        [SerializeField, Range(-1f, 2f)]
        private float cameraBoundaryOffsetLeft = 0.10f;

        [Tooltip("Décalage du mur droit. Positif = autorise le joueur à sortir de l'écran.")]
        [SerializeField, Range(-1f, 2f)]
        private float cameraBoundaryOffsetRight = 0.10f;

        [Tooltip("Décalage du mur inférieur. Positif = autorise le joueur à sortir de l'écran.")]
        [SerializeField, Range(-1f, 2f)]
        private float cameraBoundaryOffsetBottom = 0.10f;

        [Tooltip("Décalage du mur supérieur. Positif = autorise le joueur à sortir de l'écran.")]
        [SerializeField, Range(-1f, 2f)]
        private float cameraBoundaryOffsetTop = 0.10f;

        [Tooltip("Temps de lissage de la correction. 0 applique la correction immédiatement.")]
        [SerializeField, Min(0f)]
        private float cameraConstraintSmoothTime = 0.05f;

        [Tooltip("Vitesse maximale de correction. 0 signifie illimitée.")]
        [SerializeField, Min(0f)]
        private float cameraConstraintMaxCorrectionSpeed = 40f;

        [SerializeField] private bool debugCameraConstraint = true;

        [Header("Camera Constraint Gizmos")]
        [SerializeField] private bool drawCameraConstraintGizmos = true;
        [SerializeField] private bool drawCameraConstraintGizmosAlways = true;
        [SerializeField, Min(0.1f)] private float debugBoundsReferenceDepth = 10f;
        [SerializeField, Min(0.01f)] private float debugBoundsVisualScale = 1f;
        [SerializeField] private bool drawBoundsAtEachPlayerDepth = true;
        [SerializeField] private bool drawDebugBoundsMarkers = true;
        [SerializeField] private bool drawDebugBoundsLabels = true;

        [Header("Position Follow")]
        [SerializeField, Min(0f)] private float followSmoothTime = 0.5f;
        [SerializeField, Min(0f)] private float blendFollowSmoothTime = 0.12f;
        [SerializeField, Min(0f)] private float blendTargetMaxSpeed = 3f;

        [Header("Camera Framing")]
        [SerializeField] private bool framingCorrectionEnabled = true;
        [SerializeField] private Vector3 framingTargetOffset = new(0f, 1f, 0f);
        [SerializeField, Range(0.1f, 0.9f)] private float desiredViewportX = 0.5f;
        [SerializeField, Range(0.02f, 0.45f)] private float horizontalDeadZoneHalfWidth = 0.18f;
        [SerializeField, Range(0f, 20f)] private float maxHorizontalCorrectionAngle = 4f;
        [SerializeField, Min(0f)] private float horizontalCorrectionSmoothTime = 0.18f;
        [SerializeField, Range(0f, 1f)] private float framingMultiplierDuringBlend;

        [SerializeField] private Camera outputCamera;

        [Header("Group Camera Framing")]
        [Tooltip("Active le FOV dynamique uniquement lorsque plusieurs joueurs sont actifs.")]
        [SerializeField] private bool groupFramingEnabled = true;

        [Tooltip("Marge supplémentaire appliquée au calcul du FOV nécessaire.")]
        [SerializeField, Min(1f)] private float groupFramingMargin = 1.2f;

        [Tooltip("FOV minimal en multijoueur. Il correspond au cadrage le plus rapproché.")]
        [SerializeField, Range(30f, 100f)] private float minGroupFieldOfView = 45f;

        [Tooltip("FOV maximal en multijoueur. Il définit la limite de recul de la caméra.")]
        [SerializeField, Range(30f, 120f)] private float maxGroupFieldOfView = 75f;

        [Tooltip("Temps de transition entre les valeurs de FOV.")]
        [SerializeField, Min(0f)] private float groupFovSmoothTime = 0.25f;

        [Header("Combat Knots")]
        [SerializeField] private bool combatEnabled;
        [SerializeField] private List<CombatKnot> combatKnots = new();
        [SerializeField] private float combatTolerance = 0.01f;

        [Header("Audio - Combat Stem")]
        [SerializeField] private bool useCombatStem = true;
        [SerializeField] private string combatStemId = "Drums_C";

        [Header("Debug")]
        [SerializeField] private bool debugEnabled;

        // =========================================================
        // SPLINES
        // =========================================================

        private SplineContainer cameraSplineContainer;

        private float currentSplinePosition;
        private float targetSplinePosition;
        private float splineVelocity;

        private float currentGameplaySplinePosition;
        private PlayerMovement focusedTrackingPlayer;
        private bool gameplayProjectionInitialized;
        private bool splineCorrespondenceValid;

        // =========================================================
        // INDIVIDUAL PLAYER PROJECTION
        // =========================================================

        private sealed class PlayerProjectionState
        {
            public float Knot;
            public int SampleIndex = -1;
            public Vector3 LastWorldPosition;
            public bool Initialized;
        }

        private readonly Dictionary<PlayerMovement, PlayerProjectionState>
            playerProjectionStates = new();

        private readonly List<PlayerMovement> playersToRemove = new();

        private Vector3[] gameplayProjectionWorldSamples;
        private float[] gameplayProjectionKnotSamples;

        private int gameplayProjectionSampleCount;
        private float gameplayProjectionAverageSampleSpacing;

        private Spline cachedGameplaySpline;
        private int cachedGameplayKnotCount;
        private Matrix4x4 cachedGameplayLocalToWorldMatrix;

        // =========================================================
        // FOLLOW / BLEND
        // =========================================================

        private bool previousFrameWasBlend;
        private bool currentBlendActive;

        private bool blendRailCacheValid;
        private MovementSpace cachedBlendSource;
        private MovementSpace cachedBlendTarget;
        private MovementSpace.Boundary cachedBlendSourceBoundary;
        private MovementSpace.Boundary cachedBlendTargetBoundary;
        private Vector3 cachedBlendWorldEnd;
        private float cachedBlendStartSplinePosition;
        private float cachedBlendEndSplinePosition;

        // =========================================================
        // CAMERA ROTATION
        // =========================================================

        private Quaternion cameraRotationOffsetFromMovementSpace = Quaternion.identity;
        private float currentHorizontalCorrection;
        private float horizontalCorrectionVelocity;
        private bool cameraReferenceInitialized;

        // =========================================================
        // GROUP FOV
        // =========================================================

        private float baseAutoScrollFov;
        private float currentGroupFov;
        private float groupFovVelocity;

        // =========================================================
        // COMBAT
        // =========================================================

        private bool combatMode;
        private int currentCombatStep;
        private int debugUpdateCounter;

        private Coroutine pendingCombatShake;
        private Transform movementSpaceBeforeCombat;

        // =========================================================
        // PLAYER COLLECTIONS
        // =========================================================

        private readonly List<PlayerMovement> cachedPlayerMovements = new();

        // Réutilisation des positions pour les calculs de groupe.
        // Évite de créer un tableau à chaque FixedUpdate.
        private readonly List<Vector3> cachedLeashPositions = new();

        private Vector3 cachedGroupCenter;

        // =========================================================
        // LEASH / CAMERA CONSTRAINT STATE
        // =========================================================

        private readonly Dictionary<PlayerMovement, Vector3> leashVelocities = new();
        private readonly Dictionary<PlayerMovement, Vector3> cameraConstraintVelocities = new();

        // =========================================================
        // PLAYER 1 RESPAWN STATE
        // =========================================================

        private bool player1CameraFrozen;
        private bool player1RespawnPositionDetected;
        private float player1RespawnRecoveryTimer;

        private Vector3 player1LastValidPosition;
        private Vector3 player1FallPosition;

        private PlayerMovement trackedPlayer1;

        private float frozenSplinePosition;

        private bool respawnRecoveryActive;
        private float respawnRecoveryVelocity;

        // =========================================================
        // INITIALIZATION
        // =========================================================

        private void Start()
        {
            if (!ValidateReferences())
                return;

            cameraSplineContainer = autoScrollDolly.Spline;

            if (cameraSplineContainer == null)
            {
                UnityDebug.LogError(
                    $"[{nameof(VCameraSetup_Follow)}] Aucun CameraSpline Container assigné.",
                    this
                );
                return;
            }

            if (gameplaySplineContainer == null)
            {
                UnityDebug.LogError(
                    $"[{nameof(VCameraSetup_Follow)}] GameplaySpline non assignée.",
                    this
                );
                return;
            }

            autoScrollDolly.PositionUnits = PathIndexUnit.Knot;

            outputCamera ??= Camera.main;

            baseAutoScrollFov = autoScrollCamera.Lens.FieldOfView;

            minGroupFieldOfView = Mathf.Clamp(minGroupFieldOfView, 30f, 100f);
            maxGroupFieldOfView = Mathf.Clamp(
                maxGroupFieldOfView,
                minGroupFieldOfView,
                120f
            );

            currentGroupFov = Mathf.Clamp(
                baseAutoScrollFov,
                minGroupFieldOfView,
                maxGroupFieldOfView
            );

            currentSplinePosition = autoScrollDolly.CameraPosition;
            targetSplinePosition = currentSplinePosition;

            autoScrollCamera.gameObject.SetActive(true);
            combatCamera.gameObject.SetActive(true);

            BuildGameplayProjectionCache();
            ValidateSplineCorrespondence();

            if (!splineCorrespondenceValid)
                return;

            ApplyCameraState();
            ApplyInitialMovementSpace();

            InitializeCameraReference();
            ApplyMovementSpaceCameraRotationImmediate();

            ValidateCombatKnots();
            InitializePlayer1RespawnTracking();
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnArenaExit += DisableCombat;
        }

        private void OnDisable()
        {
            SetCombatStem(false);

            if (GameManager.Instance != null)
                GameManager.Instance.OnArenaExit -= DisableCombat;
        }

        // =========================================================
        // FIXED UPDATE - PHYSICS AND PLAYER CONSTRAINTS
        // =========================================================

        private void FixedUpdate()
        {
            if (!Application.isPlaying || combatMode)
                return;

            CollectAllPlayers();

            if (cachedPlayerMovements.Count <= 1)
            {
                leashVelocities.Clear();
                cameraConstraintVelocities.Clear();
                cachedLeashPositions.Clear();
                return;
            }

            // La leash collective est appliquée en premier.
            // Elle utilise le centre moyen du groupe, et non Player 1.
            ApplyPlayerSpacingLeash();

            // Les murs caméra restent une contrainte indépendante.
            // Ils s'appliquent à tous les joueurs, y compris Player 1.
            if (constrainAllPlayersToCamera)
                ConstrainAllPlayersToCamera();
        }

        // =========================================================
        // GROUP CENTER
        // =========================================================

        /// <summary>
        /// Calcule le centre géométrique moyen de tous les joueurs actifs.
        /// Aucun joueur n'est utilisé comme ancre.
        /// </summary>


        /// <summary>
        /// Applique le lissage commun aux corrections physiques des joueurs.
        /// Chaque système conserve son propre cache de vélocité.
        /// </summary>
        private static Vector3 SmoothCorrection(
            PlayerMovement movement,
            Vector3 correction,
            Dictionary<PlayerMovement, Vector3> velocities,
            float smoothTime,
            float maxCorrectionSpeed,
            float deltaTime)
        {
            if (smoothTime <= 0f)
            {
                velocities.Remove(movement);
                return correction;
            }

            Vector3 velocity = velocities.TryGetValue(
                movement,
                out Vector3 existingVelocity
            )
                ? existingVelocity
                : Vector3.zero;

            correction = Vector3.SmoothDamp(
                Vector3.zero,
                correction,
                ref velocity,
                smoothTime,
                maxCorrectionSpeed > 0f
                    ? maxCorrectionSpeed
                    : Mathf.Infinity,
                deltaTime
            );

            velocities[movement] = velocity;
            return correction;
        }

        // =========================================================
        // COLLECTIVE PLAYER LEASH
        // =========================================================

        /// <summary>
        /// Maintient chaque joueur dans un rayon maximal autour du centre
        /// moyen de tous les joueurs actifs.
        ///
        /// Contrairement à l'ancienne version, aucun joueur n'est une ancre.
        /// Les corrections sont calculées à partir d'un même instantané
        /// des positions afin d'éviter les dépendances à l'ordre de la liste.
        /// </summary>
        private void ApplyPlayerSpacingLeash()
        {
            if (!playerLeashEnabled ||
                maximumPlayerSpacing <= 0f ||
                cachedPlayerMovements.Count <= 1)
            {
                leashVelocities.Clear();
                return;
            }

            // -----------------------------------------------------
            // 1. Snapshot des positions et calcul du centre moyen.
            // -----------------------------------------------------

            cachedLeashPositions.Clear();

            Vector3 groupCenter = Vector3.zero;
            int validPlayerCount = 0;

            for (int i = 0; i < cachedPlayerMovements.Count; i++)
            {
                PlayerMovement movement = cachedPlayerMovements[i];

                if (movement == null)
                {
                    cachedLeashPositions.Add(Vector3.zero);
                    continue;
                }

                Vector3 position = movement.transform.position;

                cachedLeashPositions.Add(position);

                groupCenter += position;
                validPlayerCount++;
            }

            if (validPlayerCount <= 1)
            {
                leashVelocities.Clear();
                return;
            }

            groupCenter /= validPlayerCount;

            cachedGroupCenter = groupCenter;

            float maxDistanceSquared =
                maximumPlayerSpacing * maximumPlayerSpacing;

            float fixedDeltaTime = Time.fixedDeltaTime;

            // -----------------------------------------------------
            // 2. Correction individuelle autour du centre commun.
            // -----------------------------------------------------

            for (int i = 0; i < cachedPlayerMovements.Count; i++)
            {
                PlayerMovement movement = cachedPlayerMovements[i];

                if (movement == null)
                    continue;

                Vector3 playerPosition = cachedLeashPositions[i];

                Vector3 offset = playerPosition - groupCenter;
                float distanceSquared = offset.sqrMagnitude;

                if (distanceSquared <= maxDistanceSquared ||
                    distanceSquared <= 0.0001f)
                {
                    leashVelocities.Remove(movement);
                    continue;
                }

                float distance = Mathf.Sqrt(distanceSquared);

                // Position maximale autorisée autour du centre moyen.
                Vector3 targetPosition =
                    groupCenter +
                    offset * (maximumPlayerSpacing / distance);

                Vector3 correction =
                    targetPosition - playerPosition;

                correction = SmoothCorrection(
                    movement,
                    correction,
                    leashVelocities,
                    playerLeashSmoothTime,
                    playerLeashMaxCorrectionSpeed,
                    fixedDeltaTime
                );

                if (correction.sqrMagnitude <= 0.000001f)
                    continue;

                movement.ApplyExternalMovement(correction);

                if (debugPlayerLeash && debugEnabled)
                {
                    UnityDebug.Log(
                        $"[{nameof(VCameraSetup_Follow)}] COLLECTIVE LEASH | " +
                        $"Player={movement.name} | " +
                        $"GroupCenter={groupCenter} | " +
                        $"Distance={distance:F2} | " +
                        $"MaximumRadius={maximumPlayerSpacing:F2} | " +
                        $"Correction={correction}",
                        movement
                    );
                }
            }
        }

        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            if (debugEnabled && ++debugUpdateCounter >= 30)
            {
                debugUpdateCounter = 0;

                UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"CS={currentSplinePosition:F3} | " +
                    $"GS={currentGameplaySplinePosition:F3} | " +
                    $"Target={targetSplinePosition:F3} | " +
                    $"Blend={currentBlendActive} | " +
                    $"Players={cachedPlayerMovements.Count} | " +
                    $"GroupCenter={cachedGroupCenter} | " +
                    $"RespawnFrozen={player1CameraFrozen} | " +
                    $"ProjectionStates={playerProjectionStates.Count}",
                    this
                );
            }

            if (!splineCorrespondenceValid)
                return;

            if (combatMode)
            {
                if (!combatEnabled)
                    EndCombat();

                return;
            }

            UpdatePlayer1RespawnProtection();
        }

        // =========================================================
        // LATE UPDATE - CAMERA
        // =========================================================

        private void LateUpdate()
        {
            if (combatMode || !splineCorrespondenceValid)
                return;

            UpdateFollow();
            UpdateAutoScrollCameraRotation();
            UpdateGroupFraming();

            if (currentCombatStep < combatKnots.Count)
                CheckCombatKnot();
        }

        // =========================================================
        // PLAYER COLLECTION
        // =========================================================

        private void CollectAllPlayers()
        {
            cachedPlayerMovements.Clear();

            if (playerManager == null)
                return;

            playerManager.GetActivePlayers(cachedPlayerMovements);
        }

        private PlayerMovement GetPlayer1()
        {
            CollectAllPlayers();

            return cachedPlayerMovements.Count > 0
                ? cachedPlayerMovements[0]
                : null;
        }

        private bool HasValidPlayerMovement()
        {
            CollectAllPlayers();

            for (int i = 0; i < cachedPlayerMovements.Count; i++)
            {
                if (cachedPlayerMovements[i] != null)
                    return true;
            }

            return false;
        }

        // =========================================================
        // PLAYER 1 RESPAWN PROTECTION
        // =========================================================

        private void InitializePlayer1RespawnTracking()
        {
            if (!protectCameraDuringPlayer1Respawn)
                return;

            trackedPlayer1 = GetPlayer1();

            if (trackedPlayer1 != null)
                player1LastValidPosition = trackedPlayer1.transform.position;
        }

        private void UpdatePlayer1RespawnProtection()
        {
            if (!protectCameraDuringPlayer1Respawn)
                return;

            PlayerMovement player1 = GetPlayer1();

            if (player1 == null)
                return;

            if (trackedPlayer1 != player1)
            {
                trackedPlayer1 = player1;

                if (!player1CameraFrozen)
                    player1LastValidPosition = player1.transform.position;

                ResetGameplayProjection();
                return;
            }

            Vector3 playerPosition = player1.transform.position;

            if (!player1CameraFrozen &&
                playerPosition.y <= player1FallDetectionHeight)
            {
                FreezePlayer1Camera(playerPosition);
                return;
            }

            if (!player1CameraFrozen)
            {
                player1LastValidPosition = playerPosition;
                return;
            }

            if (!player1RespawnPositionDetected)
            {
                float respawnDistance = Vector3.Distance(
                    playerPosition,
                    player1FallPosition
                );

                bool returnedAboveFallHeight =
                    playerPosition.y > player1FallDetectionHeight;

                if (returnedAboveFallHeight &&
                    respawnDistance >= player1RespawnDetectionDistance)
                {
                    player1RespawnPositionDetected = true;
                    player1RespawnRecoveryTimer = player1RespawnRecoveryDelay;

                    if (debugPlayer1Respawn)
                    {
                        UnityDebug.Log(
                            $"[{nameof(VCameraSetup_Follow)}] " +
                            $"Player 1 respawn détecté. " +
                            $"Attente de {player1RespawnRecoveryDelay:F2}s.",
                            player1
                        );
                    }
                }
            }
            else
            {
                player1RespawnRecoveryTimer -= Time.deltaTime;

                if (player1RespawnRecoveryTimer <= 0f)
                    BeginPlayer1CameraRecovery(player1);
            }
        }

        private void FreezePlayer1Camera(Vector3 fallPosition)
        {
            player1CameraFrozen = true;
            player1RespawnPositionDetected = false;
            player1RespawnRecoveryTimer = 0f;

            respawnRecoveryActive = false;
            respawnRecoveryVelocity = 0f;

            player1FallPosition = fallPosition;

            frozenSplinePosition = currentSplinePosition;

            targetSplinePosition = currentSplinePosition;
            splineVelocity = 0f;

            previousFrameWasBlend = false;
            currentBlendActive = false;

            ClearBlendRailCache();

            if (debugPlayer1Respawn)
            {
                UnityDebug.LogWarning(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    "Caméra verrouillée : Player 1 est tombé dans le vide. " +
                    $"Position={fallPosition}",
                    this
                );
            }
        }

        private void BeginPlayer1CameraRecovery(PlayerMovement player1)
        {
            player1CameraFrozen = false;
            player1RespawnPositionDetected = false;

            player1LastValidPosition = player1.transform.position;

            splineVelocity = 0f;
            targetSplinePosition = currentSplinePosition;

            previousFrameWasBlend = false;
            currentBlendActive = false;

            ClearBlendRailCache();
            ResetGameplayProjection();

            respawnRecoveryActive = true;
            respawnRecoveryVelocity = 0f;

            if (debugPlayer1Respawn)
            {
                UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    "Reprise progressive du suivi après le respawn de Player 1.",
                    player1
                );
            }
        }

        private void UpdateRespawnRecovery()
        {
            if (!respawnRecoveryActive)
                return;

            if (!TryGetTrackingPoint(out _))
                return;

            float desiredPosition = GetTrackingSplinePosition();

            float smoothTime = player1RespawnRecoverySmoothTime > 0f
                ? player1RespawnRecoverySmoothTime
                : followSmoothTime;

            if (smoothTime <= 0f)
            {
                currentSplinePosition = desiredPosition;
                targetSplinePosition = desiredPosition;
                splineVelocity = 0f;
                respawnRecoveryActive = false;
                return;
            }

            currentSplinePosition = Mathf.SmoothDamp(
                currentSplinePosition,
                desiredPosition,
                ref respawnRecoveryVelocity,
                smoothTime
            );

            targetSplinePosition = desiredPosition;

            if (Mathf.Abs(currentSplinePosition - desiredPosition) <= 0.01f)
            {
                currentSplinePosition = desiredPosition;
                targetSplinePosition = desiredPosition;
                respawnRecoveryActive = false;
                splineVelocity = 0f;
            }
        }

        // =========================================================
        // MOVEMENT SPACE
        // =========================================================

        private bool TryGetCameraBlend(out MovementSpace.BlendResult blend)
        {
            blend = default;

            if (focusedTrackingPlayer == null)
                focusedTrackingPlayer = GetPlayer1();

            return focusedTrackingPlayer != null &&
                   focusedTrackingPlayer.TryGetActiveMovementSpaceBlend(out blend);
        }

        private Quaternion GetCameraMovementSpaceRotation()
        {
            if (focusedTrackingPlayer == null)
                focusedTrackingPlayer = GetPlayer1();

            return focusedTrackingPlayer != null
                ? focusedTrackingPlayer.GetEffectiveMovementSpaceRotation()
                : Quaternion.identity;
        }

        private void ApplyMovementSpaceToAllPlayers(Transform movementSpace)
        {
            if (movementSpace == null)
                return;

            CollectAllPlayers();

            foreach (PlayerMovement movement in cachedPlayerMovements)
            {
                if (movement != null)
                    movement.SetMovementSpace(movementSpace);
            }
        }

        // =========================================================
        // FOLLOW
        // =========================================================

        private void UpdateFollow()
        {
            if (player1CameraFrozen)
            {
                currentSplinePosition = frozenSplinePosition;
                targetSplinePosition = frozenSplinePosition;
                splineVelocity = 0f;

                autoScrollDolly.CameraPosition = frozenSplinePosition;
                return;
            }

            if (respawnRecoveryActive)
            {
                UpdateRespawnRecovery();
                autoScrollDolly.CameraPosition = currentSplinePosition;
                return;
            }

            if (!TryGetTrackingPoint(out _))
                return;

            bool hasBlend = TryGetCameraBlend(
                out MovementSpace.BlendResult blend
            );

            if (hasBlend && !previousFrameWasBlend)
                BeginBlendRail(blend);

            currentBlendActive = hasBlend;

            if (hasBlend)
                UpdateBlendFollow(blend);
            else
                UpdateNormalFollow();

            if (!hasBlend && previousFrameWasBlend)
                ClearBlendRailCache();

            previousFrameWasBlend = hasBlend;

            autoScrollDolly.CameraPosition = currentSplinePosition;
        }

        private void UpdateNormalFollow()
        {
            targetSplinePosition = GetTrackingSplinePosition();

            if (followSmoothTime <= 0f)
            {
                currentSplinePosition = targetSplinePosition;
                splineVelocity = 0f;
                return;
            }

            currentSplinePosition = Mathf.SmoothDamp(
                currentSplinePosition,
                targetSplinePosition,
                ref splineVelocity,
                followSmoothTime,
                Mathf.Infinity,
                Time.deltaTime
            );
        }

        private void BeginBlendRail(MovementSpace.BlendResult blend)
        {
            cachedBlendSource = blend.Source;
            cachedBlendTarget = blend.Target;
            cachedBlendSourceBoundary = blend.SourceBoundary;
            cachedBlendTargetBoundary = blend.TargetBoundary;
            cachedBlendWorldEnd = blend.WorldEnd;

            cachedBlendStartSplinePosition = currentSplinePosition;

            cachedBlendEndSplinePosition = GetBlendSplinePosition(
                blend.WorldEnd
            );

            blendRailCacheValid = true;
            targetSplinePosition = currentSplinePosition;

            if (debugEnabled)
            {
                UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] Blend Rail | " +
                    $"Start={cachedBlendStartSplinePosition:F3} | " +
                    $"End={cachedBlendEndSplinePosition:F3} | " +
                    $"T={blend.T:F3}",
                    this
                );
            }
        }

        private void UpdateBlendFollow(MovementSpace.BlendResult blend)
        {
            RefreshBlendRailCacheIfNeeded(blend);

            if (!blendRailCacheValid)
            {
                UpdateNormalFollow();
                return;
            }

            float desiredSplinePosition = Mathf.Lerp(
                cachedBlendStartSplinePosition,
                cachedBlendEndSplinePosition,
                Mathf.Clamp01(blend.T)
            );

            targetSplinePosition = blendTargetMaxSpeed <= 0f
                ? desiredSplinePosition
                : Mathf.MoveTowards(
                    targetSplinePosition,
                    desiredSplinePosition,
                    blendTargetMaxSpeed * Time.deltaTime
                );

            if (blendFollowSmoothTime <= 0f)
            {
                currentSplinePosition = targetSplinePosition;
                splineVelocity = 0f;
                return;
            }

            currentSplinePosition = Mathf.SmoothDamp(
                currentSplinePosition,
                targetSplinePosition,
                ref splineVelocity,
                blendFollowSmoothTime,
                Mathf.Infinity,
                Time.deltaTime
            );
        }

        private void RefreshBlendRailCacheIfNeeded(
            MovementSpace.BlendResult blend)
        {
            bool newBlend =
                !blendRailCacheValid ||
                cachedBlendSource != blend.Source ||
                cachedBlendTarget != blend.Target ||
                cachedBlendSourceBoundary != blend.SourceBoundary ||
                cachedBlendTargetBoundary != blend.TargetBoundary;

            if (newBlend)
            {
                BeginBlendRail(blend);
                return;
            }

            if ((cachedBlendWorldEnd - blend.WorldEnd).sqrMagnitude <= 0.0001f)
                return;

            cachedBlendWorldEnd = blend.WorldEnd;
            cachedBlendEndSplinePosition = GetBlendSplinePosition(blend.WorldEnd);
        }

        private void ClearBlendRailCache()
        {
            blendRailCacheValid = false;
            cachedBlendSource = null;
            cachedBlendTarget = null;
        }

        // =========================================================
        // TRACKING POINT - COLLECTIVE CENTER
        // =========================================================

        /// <summary>
        /// Retourne la moyenne des positions de tous les joueurs actifs.
        /// Le centre de la caméra ne dépend donc plus de Player 1,
        /// ni des extrémités d'une boîte englobante.
        /// </summary>
        private bool TryGetTrackingPoint(out Vector3 trackingPoint)
        {
            trackingPoint = Vector3.zero;

            if (player1CameraFrozen)
            {
                trackingPoint = player1LastValidPosition;
                return true;
            }

            // Le focus est mis à jour par GetTrackingSplinePosition().
            // En l'absence de projection initialisée, on utilise Player 1
            // comme référence temporaire.
            if (focusedTrackingPlayer == null)
                focusedTrackingPlayer = GetPlayer1();

            if (focusedTrackingPlayer == null)
                return false;

            trackingPoint = focusedTrackingPlayer.transform.position;
            return true;
        }

        // =========================================================
        // GAMEPLAY SPLINE PROJECTION CACHE
        // =========================================================

        private void BuildGameplayProjectionCache()
        {
            if (gameplaySplineContainer == null ||
                gameplaySplineContainer.Spline == null)
            {
                ClearGameplayProjectionCache();
                return;
            }

            Spline spline = gameplaySplineContainer.Spline;

            gameplayProjectionSampleCount = Mathf.Max(
                50,
                gameplayProjectionSamples
            );

            int arrayLength = gameplayProjectionSampleCount + 1;

            gameplayProjectionWorldSamples = new Vector3[arrayLength];
            gameplayProjectionKnotSamples = new float[arrayLength];

            float totalSampleDistance = 0f;

            for (int i = 0; i <= gameplayProjectionSampleCount; i++)
            {
                float t = i / (float)gameplayProjectionSampleCount;

                Vector3 localPosition = spline.EvaluatePosition(t);

                Vector3 worldPosition =
                    gameplaySplineContainer.transform.TransformPoint(localPosition);

                float knot = SplineUtility.ConvertIndexUnit(
                    spline,
                    t,
                    PathIndexUnit.Normalized,
                    PathIndexUnit.Knot
                );

                gameplayProjectionWorldSamples[i] = worldPosition;
                gameplayProjectionKnotSamples[i] = knot;

                if (i > 0)
                {
                    totalSampleDistance += Vector3.Distance(
                        gameplayProjectionWorldSamples[i - 1],
                        worldPosition
                    );
                }
            }

            gameplayProjectionAverageSampleSpacing =
                totalSampleDistance / Mathf.Max(1, gameplayProjectionSampleCount);

            cachedGameplaySpline = spline;
            cachedGameplayKnotCount = spline.Count;
            cachedGameplayLocalToWorldMatrix =
                gameplaySplineContainer.transform.localToWorldMatrix;

            if (debugGameplaySpline)
            {
                UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"GameplaySpline cache construit | " +
                    $"Samples={gameplayProjectionSampleCount} | " +
                    $"Knots={cachedGameplayKnotCount} | " +
                    $"AverageSpacing={gameplayProjectionAverageSampleSpacing:F4}",
                    this
                );
            }
        }

        private void ClearGameplayProjectionCache()
        {
            gameplayProjectionWorldSamples = null;
            gameplayProjectionKnotSamples = null;

            gameplayProjectionSampleCount = 0;
            gameplayProjectionAverageSampleSpacing = 0f;

            cachedGameplaySpline = null;
            cachedGameplayKnotCount = 0;
        }

        private bool EnsureGameplayProjectionCache()
        {
            if (gameplaySplineContainer == null ||
                gameplaySplineContainer.Spline == null)
                return false;

            Spline currentSpline = gameplaySplineContainer.Spline;

            bool cacheInvalid =
                gameplayProjectionWorldSamples == null ||
                gameplayProjectionKnotSamples == null ||
                cachedGameplaySpline != currentSpline ||
                cachedGameplayKnotCount != currentSpline.Count ||
                gameplayProjectionSampleCount != Mathf.Max(50, gameplayProjectionSamples);

            if (!cacheInvalid)
            {
                Matrix4x4 currentMatrix =
                    gameplaySplineContainer.transform.localToWorldMatrix;

                if (currentMatrix != cachedGameplayLocalToWorldMatrix)
                    cacheInvalid = true;
            }

            if (cacheInvalid)
            {
                BuildGameplayProjectionCache();
                ValidateSplineCorrespondence();
                ResetGameplayProjection();
            }

            return gameplayProjectionWorldSamples != null &&
                   gameplayProjectionKnotSamples != null &&
                   gameplayProjectionSampleCount > 0;
        }

        // =========================================================
        // INDIVIDUAL GAMEPLAY SPLINE PROJECTION
        // =========================================================

        private float GetTrackingSplinePosition()
        {
            if (!EnsureGameplayProjectionCache() ||
                !splineCorrespondenceValid)
                return currentSplinePosition;

            CollectAllPlayers();

            if (cachedPlayerMovements.Count == 0)
                return currentSplinePosition;

            PlayerMovement leadingPlayer = null;
            float leadingKnot = float.MinValue;
            float leadingDistance = 0f;
            int leadingSampleIndex = -1;

            // Chaque joueur conserve son propre historique de projection.
            // Le focus est attribué à celui dont le Knot est le plus élevé.
            for (int i = 0; i < cachedPlayerMovements.Count; i++)
            {
                PlayerMovement movement = cachedPlayerMovements[i];

                if (movement == null)
                    continue;

                if (!playerProjectionStates.TryGetValue(
                        movement,
                        out PlayerProjectionState state))
                {
                    state = new PlayerProjectionState();
                    playerProjectionStates.Add(movement, state);
                }

                Vector3 worldPosition = movement.transform.position;

                if (!TryProjectOntoGameplaySpline(
                        worldPosition,
                        state,
                        state.Initialized,
                        gameplayProjectionSearchWindow,
                        out float gameplayKnot,
                        out float projectionDistance,
                        out int projectedSampleIndex))
                {
                    if (state.Initialized && state.Knot > leadingKnot)
                    {
                        leadingPlayer = movement;
                        leadingKnot = state.Knot;
                        leadingDistance = float.MaxValue;
                        leadingSampleIndex = state.SampleIndex;
                    }

                    continue;
                }

                state.Knot = gameplayKnot;
                state.SampleIndex = projectedSampleIndex;
                state.LastWorldPosition = worldPosition;
                state.Initialized = true;

                if (gameplayKnot > leadingKnot)
                {
                    leadingPlayer = movement;
                    leadingKnot = gameplayKnot;
                    leadingDistance = projectionDistance;
                    leadingSampleIndex = projectedSampleIndex;
                }

                if (debugGameplaySpline &&
                    projectionDistance > gameplayProjectionWarningDistance)
                {
                    UnityDebug.LogWarning(
                        $"[{nameof(VCameraSetup_Follow)}] " +
                        $"Joueur éloigné de la GameplaySpline. " +
                        $"Player={movement.name} | " +
                        $"Distance={projectionDistance:F2} | " +
                        $"GS Knot={gameplayKnot:F3} | " +
                        $"Sample={projectedSampleIndex}",
                        movement
                    );
                }
            }

            RemoveInactiveProjectionStates();

            if (leadingPlayer == null)
                return currentSplinePosition;

            focusedTrackingPlayer = leadingPlayer;
            currentGameplaySplinePosition = leadingKnot;
            gameplayProjectionInitialized = true;

            if (debugGameplaySpline)
            {
                UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"Hard focus={leadingPlayer.name} | " +
                    $"GS Knot={leadingKnot:F3} | " +
                    $"Distance={leadingDistance:F2} | " +
                    $"Sample={leadingSampleIndex}",
                    leadingPlayer
                );
            }

            return MapGameplayKnotToCameraKnot(currentGameplaySplinePosition);
        }

        private void RemoveInactiveProjectionStates()
        {
            if (playerProjectionStates.Count == 0)
                return;

            playersToRemove.Clear();

            foreach (KeyValuePair<PlayerMovement, PlayerProjectionState> pair
                     in playerProjectionStates)
            {
                if (pair.Key == null ||
                    !cachedPlayerMovements.Contains(pair.Key))
                {
                    playersToRemove.Add(pair.Key);
                }
            }

            for (int i = 0; i < playersToRemove.Count; i++)
            {
                PlayerMovement player = playersToRemove[i];

                playerProjectionStates.Remove(player);
                leashVelocities.Remove(player);
                cameraConstraintVelocities.Remove(player);
            }
        }

        private float GetBlendSplinePosition(Vector3 worldPosition)
        {
            if (!EnsureGameplayProjectionCache() ||
                !splineCorrespondenceValid)
                return currentSplinePosition;

            // L'extrémité d'un Blend peut se trouver bien au-delà de la
            // progression courante, notamment lors d'un virage serré entre
            // deux Movement Spaces. Pendant le Blend, la progression normale
            // est suspendue : une recherche locale centrée sur
            // currentGameplaySplinePosition peut donc rester bloquée sur
            // l'ancien segment de GS. On projette cette extrémité globalement.
            float centerKnot = gameplayProjectionInitialized
                ? currentGameplaySplinePosition
                : 0f;

            if (!TryProjectOntoGameplaySpline(
                    worldPosition,
                    null,
                    false,
                    blendProjectionSearchWindow,
                    out float gameplayKnot,
                    out float projectionDistance,
                    out _))
            {
                if (debugGameplaySpline)
                {
                    UnityDebug.LogWarning(
                        $"[{nameof(VCameraSetup_Follow)}] " +
                        "Blend endpoint introuvable dans la fenêtre locale. " +
                        $"GS={centerKnot:F3} | WorldEnd={worldPosition}",
                        this
                    );
                }

                return MapGameplayKnotToCameraKnot(centerKnot);
            }

            if (debugGameplaySpline &&
                projectionDistance > gameplayProjectionWarningDistance)
            {
                UnityDebug.LogWarning(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"Blend endpoint éloigné de la GameplaySpline. " +
                    $"Distance={projectionDistance:F2} | GS Knot={gameplayKnot:F3}",
                    this
                );
            }

            return MapGameplayKnotToCameraKnot(gameplayKnot);
        }

        private bool TryProjectOntoGameplaySpline(
            Vector3 worldPosition,
            PlayerProjectionState state,
            bool useContinuity,
            float searchWindowKnots,
            out float closestKnot,
            out float closestDistance,
            out int closestSampleIndex)
        {
            closestKnot = 0f;
            closestDistance = float.MaxValue;
            closestSampleIndex = -1;

            // Les deux appelants valident/reconstruisent le cache avant
            // d'entrer ici. Ne pas répéter cette validation pour chaque joueur :
            // elle vérifie notamment la matrice du Transform et la Spline.
            if (cachedGameplaySpline == null ||
                gameplayProjectionWorldSamples == null ||
                gameplayProjectionKnotSamples == null)
                return false;

            int sampleCount = gameplayProjectionSampleCount;

            if (sampleCount <= 0)
                return false;

            Spline spline = cachedGameplaySpline;

            int centerIndex = 0;
            int searchRadius = sampleCount;

            if (useContinuity && state != null && state.Initialized)
            {
                centerIndex = state.SampleIndex >= 0
                    ? state.SampleIndex
                    : FindNearestProjectionSample(state.Knot);

                float knotRange = Mathf.Max(0.1f, searchWindowKnots);
                float knotCount = Mathf.Max(1, cachedGameplayKnotCount - 1);

                searchRadius = Mathf.CeilToInt(
                    knotRange / knotCount * sampleCount
                );

                searchRadius = Mathf.Max(2, searchRadius);

                // Ne pas réduire la fenêtre à partir du déplacement monde :
                // l'espacement des échantillons n'est pas uniforme en distance
                // lorsque la spline comporte des courbes serrées ou des tangentes
                // très différentes. Cette réduction pouvait exclure le bon point
                // de projection et faire décrocher le suivi dans les virages.

                searchRadius = Mathf.Min(
                    searchRadius,
                    spline.Closed
                        ? Mathf.Max(2, sampleCount / 2)
                        : sampleCount
                );
            }

            float bestScore = float.MaxValue;
            float bestDistanceSquared = float.MaxValue;

            int firstOffset = useContinuity ? -searchRadius : 0;
            int lastOffset = useContinuity ? searchRadius : sampleCount;

            for (int offset = firstOffset; offset <= lastOffset; offset++)
            {
                int sampleIndex;

                if (useContinuity)
                {
                    sampleIndex = centerIndex + offset;

                    if (spline.Closed)
                    {
                        sampleIndex %= sampleCount;

                        if (sampleIndex < 0)
                            sampleIndex += sampleCount;
                    }
                    else if (sampleIndex < 0 || sampleIndex > sampleCount)
                    {
                        continue;
                    }
                }
                else
                {
                    sampleIndex = offset;
                }

                Vector3 samplePosition =
                    gameplayProjectionWorldSamples[sampleIndex];

                float distanceSquared = GetHorizontalDistanceSquared(
                    worldPosition,
                    samplePosition
                );

                float progressionPenalty = 0f;

                if (useContinuity)
                {
                    float progressionDistance =
                        Mathf.Abs(offset) *
                        gameplayProjectionAverageSampleSpacing;

                    progressionPenalty =
                        progressionDistance * projectionContinuityWeight;

                    progressionPenalty *= progressionPenalty;
                }

                float score = distanceSquared + progressionPenalty;

                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestDistanceSquared = distanceSquared;

                closestKnot = gameplayProjectionKnotSamples[sampleIndex];
                closestSampleIndex = sampleIndex;
            }

            if (closestSampleIndex < 0)
                return false;

            closestDistance = Mathf.Sqrt(bestDistanceSquared);
            return true;
        }

        private int FindNearestProjectionSample(float knot)
        {
            int sampleCount = gameplayProjectionSampleCount;

            if (sampleCount <= 0 ||
                gameplayProjectionKnotSamples == null)
                return 0;

            int low = 0;
            int high = sampleCount;

            while (low < high)
            {
                int middle = (low + high) / 2;

                if (gameplayProjectionKnotSamples[middle] < knot)
                    low = middle + 1;
                else
                    high = middle;
            }

            if (low <= 0)
                return 0;

            if (low >= sampleCount)
                return sampleCount;

            float lowerDifference = Mathf.Abs(
                gameplayProjectionKnotSamples[low - 1] - knot
            );

            float upperDifference = Mathf.Abs(
                gameplayProjectionKnotSamples[low] - knot
            );

            return lowerDifference <= upperDifference
                ? low - 1
                : low;
        }

        private static float GetHorizontalDistanceSquared(
            Vector3 first,
            Vector3 second)
        {
            float dx = first.x - second.x;
            float dz = first.z - second.z;

            return dx * dx + dz * dz;
        }

        // =========================================================
        // GAMEPLAY KNOT -> CAMERA KNOT
        // =========================================================

        private float MapGameplayKnotToCameraKnot(float gameplayKnot)
        {
            if (!splineCorrespondenceValid ||
                cameraSplineContainer == null)
                return currentSplinePosition;

            int cameraKnotCount = cameraSplineContainer.Spline.Count;

            if (cameraKnotCount <= 1)
                return 0f;

            return Mathf.Clamp(
                gameplayKnot,
                0f,
                cameraKnotCount - 1f
            );
        }

        private void ResetGameplayProjection()
        {
            gameplayProjectionInitialized = false;
            currentGameplaySplinePosition = 0f;

            foreach (PlayerProjectionState state in playerProjectionStates.Values)
            {
                state.Knot = 0f;
                state.SampleIndex = -1;
                state.LastWorldPosition = Vector3.zero;
                state.Initialized = false;
            }
        }

        private void ValidateSplineCorrespondence()
        {
            if (gameplaySplineContainer == null ||
                cameraSplineContainer == null)
            {
                splineCorrespondenceValid = false;
                return;
            }

            int gameplayCount = gameplaySplineContainer.Spline.Count;
            int cameraCount = cameraSplineContainer.Spline.Count;

            if (gameplayCount != cameraCount)
            {
                splineCorrespondenceValid = false;

                UnityDebug.LogError(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    "Correspondance GS / CS impossible : " +
                    "les Splines doivent avoir exactement le même nombre de Knots. " +
                    $"GS={gameplayCount} | CS={cameraCount}.",
                    this
                );

                return;
            }

            splineCorrespondenceValid = true;

            UnityDebug.Log(
                $"[{nameof(VCameraSetup_Follow)}] " +
                $"Correspondance GS / CS validée : {gameplayCount} Knots.",
                this
            );
        }

        // =========================================================
        // CAMERA ROTATION
        // =========================================================

        private Transform GetAutoScrollRotationRoot()
        {
            if (autoScrollRotationRoot != null)
                return autoScrollRotationRoot;

            return autoScrollCamera != null
                ? autoScrollCamera.transform
                : null;
        }

        private void InitializeCameraReference()
        {
            if (autoScrollCamera == null ||
                !HasValidPlayerMovement())
                return;

            Quaternion movementSpaceRotation =
                GetCameraMovementSpaceRotation();

            Transform rotationRoot = GetAutoScrollRotationRoot();

            cameraRotationOffsetFromMovementSpace =
                Quaternion.Inverse(movementSpaceRotation) *
                rotationRoot.rotation;

            currentHorizontalCorrection = 0f;
            horizontalCorrectionVelocity = 0f;

            cameraReferenceInitialized = true;
        }

        private void UpdateAutoScrollCameraRotation()
        {
            if (autoScrollCamera == null ||
                player1CameraFrozen ||
                !HasValidPlayerMovement())
                return;

            if (!cameraReferenceInitialized)
                InitializeCameraReference();

            Quaternion movementSpaceRotation =
                GetCameraMovementSpaceRotation();

            Quaternion baseRotation =
                movementSpaceRotation *
                cameraRotationOffsetFromMovementSpace;

            float targetCorrection = 0f;

            if (framingCorrectionEnabled &&
                TryGetTrackingPoint(out Vector3 trackingPoint))
            {
                Vector3 framingPoint =
                    trackingPoint + framingTargetOffset;

                if (TryGetHorizontalViewportPosition(
                        baseRotation,
                        framingPoint,
                        out float viewportX))
                {
                    targetCorrection =
                        GetHorizontalScreenError(viewportX) *
                        maxHorizontalCorrectionAngle;
                }
            }

            if (currentBlendActive)
                targetCorrection *= framingMultiplierDuringBlend;

            if (horizontalCorrectionSmoothTime <= 0f)
            {
                currentHorizontalCorrection = targetCorrection;
                horizontalCorrectionVelocity = 0f;
            }
            else
            {
                currentHorizontalCorrection = Mathf.SmoothDampAngle(
                    currentHorizontalCorrection,
                    targetCorrection,
                    ref horizontalCorrectionVelocity,
                    horizontalCorrectionSmoothTime,
                    Mathf.Infinity,
                    Time.deltaTime
                );
            }

            Vector3 rotationAxis = movementSpaceRotation * Vector3.up;

            GetAutoScrollRotationRoot().rotation =
                Quaternion.AngleAxis(
                    currentHorizontalCorrection,
                    rotationAxis
                ) * baseRotation;
        }

        private bool TryGetHorizontalViewportPosition(
            Quaternion baseRotation,
            Vector3 worldPoint,
            out float viewportX)
        {
            viewportX = desiredViewportX;

            Vector3 localDirection =
                Quaternion.Inverse(baseRotation) *
                (worldPoint - autoScrollCamera.transform.position);

            if (localDirection.z <= 0.001f)
                return false;

            float aspect =
                outputCamera != null && outputCamera.aspect > 0f
                    ? outputCamera.aspect
                    : 16f / 9f;

            float verticalFov =
                Mathf.Clamp(
                    autoScrollCamera.Lens.FieldOfView,
                    1f,
                    179f
                ) * Mathf.Deg2Rad;

            float tanHalfHorizontalFov =
                Mathf.Tan(verticalFov * 0.5f) * aspect;

            if (tanHalfHorizontalFov <= 0.0001f)
                return false;

            viewportX =
                0.5f +
                localDirection.x /
                (localDirection.z * tanHalfHorizontalFov) *
                0.5f;

            return true;
        }

        private float GetHorizontalScreenError(float viewportX)
        {
            float left = desiredViewportX - horizontalDeadZoneHalfWidth;
            float right = desiredViewportX + horizontalDeadZoneHalfWidth;

            if (viewportX >= left && viewportX <= right)
                return 0f;

            if (viewportX > right)
            {
                return Mathf.Clamp01(
                    (viewportX - right) /
                    Mathf.Max(0.001f, 1f - right)
                );
            }

            return -Mathf.Clamp01(
                (left - viewportX) /
                Mathf.Max(0.001f, left)
            );
        }

        private void ApplyMovementSpaceCameraRotationImmediate()
        {
            if (autoScrollCamera == null ||
                player1CameraFrozen ||
                !HasValidPlayerMovement())
                return;

            if (!cameraReferenceInitialized)
                InitializeCameraReference();

            currentHorizontalCorrection = 0f;
            horizontalCorrectionVelocity = 0f;

            GetAutoScrollRotationRoot().rotation =
                GetCameraMovementSpaceRotation() *
                cameraRotationOffsetFromMovementSpace;
        }

        // =========================================================
        // MULTIPLAYER CAMERA CONSTRAINT
        // =========================================================

        /// <summary>
        /// Contraint tous les joueurs dans les limites relatives à la caméra.
        /// La correction est calculée en viewport et conserve la profondeur
        /// caméra du personnage.
        ///
        /// Cette méthode est utilisée uniquement en multijoueur.
        /// </summary>
        private void ConstrainAllPlayersToCamera()
        {
            if (!constrainAllPlayersToCamera ||
                combatMode ||
                cachedPlayerMovements.Count <= 1)
                return;

            if (outputCamera == null)
                outputCamera = Camera.main;

            if (outputCamera == null || !outputCamera.isActiveAndEnabled)
                return;

            float minX = -cameraBoundaryOffsetLeft;
            float maxX = 1f + cameraBoundaryOffsetRight;

            float minY = -cameraBoundaryOffsetBottom;
            float maxY = 1f + cameraBoundaryOffsetTop;

            float fixedDeltaTime = Time.fixedDeltaTime;

            // Tous les joueurs sont traités, y compris Player 1.
            for (int i = 0; i < cachedPlayerMovements.Count; i++)
            {
                PlayerMovement movement = cachedPlayerMovements[i];

                if (movement == null)
                    continue;

                Vector3 worldPosition = movement.transform.position;
                Vector3 targetPosition = worldPosition + framingTargetOffset;

                Vector3 viewportPosition =
                    outputCamera.WorldToViewportPoint(targetPosition);

                if (viewportPosition.z <= outputCamera.nearClipPlane + 0.05f)
                {
                    cameraConstraintVelocities.Remove(movement);
                    continue;
                }

                float correctedX = Mathf.Clamp(
                    viewportPosition.x,
                    minX,
                    maxX
                );

                float correctedY = Mathf.Clamp(
                    viewportPosition.y,
                    minY,
                    maxY
                );

                bool outsideHorizontal =
                    !Mathf.Approximately(correctedX, viewportPosition.x);

                bool outsideVertical =
                    !Mathf.Approximately(correctedY, viewportPosition.y);

                if (!outsideHorizontal && !outsideVertical)
                {
                    cameraConstraintVelocities.Remove(movement);
                    continue;
                }

                Vector3 correctedViewportPosition = new Vector3(
                    correctedX,
                    correctedY,
                    viewportPosition.z
                );

                Vector3 correctedTargetPosition =
                    outputCamera.ViewportToWorldPoint(correctedViewportPosition);

                Vector3 correctedWorldPosition =
                    correctedTargetPosition - framingTargetOffset;

                Vector3 correction =
                    correctedWorldPosition - worldPosition;

                correction = SmoothCorrection(
                    movement,
                    correction,
                    cameraConstraintVelocities,
                    cameraConstraintSmoothTime,
                    cameraConstraintMaxCorrectionSpeed,
                    fixedDeltaTime
                );

                if (correction.sqrMagnitude <= 0.000001f)
                    continue;

                movement.ApplyExternalMovement(correction);

                if (debugCameraConstraint && debugEnabled)
                {
                    UnityDebug.Log(
                        $"[{nameof(VCameraSetup_Follow)}] SCREEN WALL | " +
                        $"Player={movement.name} | " +
                        $"Index={i} | " +
                        $"Viewport={viewportPosition} | " +
                        $"Limits=({minX:F2},{maxX:F2},{minY:F2},{maxY:F2}) | " +
                        $"Correction={correction}",
                        movement
                    );
                }
            }
        }

        // =========================================================
        // GIZMOS
        // =========================================================

        private void OnDrawGizmos()
        {
            if (!drawCameraConstraintGizmos)
                return;

            DrawCameraConstraintGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawCameraConstraintGizmos ||
                drawCameraConstraintGizmosAlways)
                return;

            DrawCameraConstraintGizmos();
        }

        private Camera ResolveCameraForGizmos()
        {
            if (outputCamera != null)
                return outputCamera;

            if (Camera.main != null)
                return Camera.main;

            Camera[] cameras = Camera.allCameras;

            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null &&
                    cameras[i].enabled &&
                    cameras[i].gameObject.activeInHierarchy)
                    return cameras[i];
            }

            return GetComponentInChildren<Camera>(true);
        }

        private void DrawCameraConstraintGizmos()
        {
            Camera cameraToDraw = ResolveCameraForGizmos();

            if (cameraToDraw == null)
            {
#if UNITY_EDITOR
                Handles.color = Color.red;
                Handles.Label(
                    transform.position,
                    "VCameraSetup_Follow: NO OUTPUT CAMERA FOUND"
                );
#endif
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.75f);
                return;
            }

            Transform cameraTransform = cameraToDraw.transform;

            Vector3 cameraPosition = cameraTransform.position;
            Vector3 cameraRight = cameraTransform.right;
            Vector3 cameraUp = cameraTransform.up;
            Vector3 cameraForward = cameraTransform.forward;

            float referenceDepth = Mathf.Max(
                cameraToDraw.nearClipPlane + 0.1f,
                debugBoundsReferenceDepth
            );

            DrawBoundsAtDepth(
                cameraToDraw,
                cameraPosition,
                cameraRight,
                cameraUp,
                cameraForward,
                referenceDepth,
                "CAMERA REFERENCE",
                Mathf.Max(0.01f, debugBoundsVisualScale)
            );

            float markerSize = Mathf.Max(0.2f, referenceDepth * 0.025f);

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(cameraPosition, markerSize);

            Gizmos.DrawLine(
                cameraPosition,
                cameraPosition + cameraForward * referenceDepth
            );

#if UNITY_EDITOR
            if (drawDebugBoundsLabels)
            {
                Handles.color = Color.white;
                Handles.Label(
                    cameraPosition + cameraUp * markerSize * 2f,
                    $"OUTPUT CAMERA: {cameraToDraw.name}\nDepth: {referenceDepth:F1}"
                );
            }
#endif

            if (!Application.isPlaying ||
                !drawBoundsAtEachPlayerDepth ||
                playerManager == null)
                return;

            CollectAllPlayers();

            for (int i = 0; i < cachedPlayerMovements.Count; i++)
            {
                PlayerMovement movement = cachedPlayerMovements[i];

                if (movement == null)
                    continue;

                Vector3 targetPosition =
                    movement.transform.position + framingTargetOffset;

                float playerDepth = Vector3.Dot(
                    targetPosition - cameraPosition,
                    cameraForward
                );

                Gizmos.color =
                    playerDepth <= cameraToDraw.nearClipPlane
                        ? Color.red
                        : Color.magenta;

                Gizmos.DrawWireSphere(targetPosition, markerSize * 0.75f);

                if (playerDepth > cameraToDraw.nearClipPlane)
                {
                    DrawBoundsAtDepth(
                        cameraToDraw,
                        cameraPosition,
                        cameraRight,
                        cameraUp,
                        cameraForward,
                        playerDepth,
                        $"PLAYER {i} DEPTH",
                        Mathf.Max(0.01f, debugBoundsVisualScale)
                    );
                }
            }
        }

        private void DrawBoundsAtDepth(
            Camera cameraToDraw,
            Vector3 cameraPosition,
            Vector3 cameraRight,
            Vector3 cameraUp,
            Vector3 cameraForward,
            float depth,
            string label,
            float visualScale)
        {
            float halfHeight;
            float halfWidth;

            if (cameraToDraw.orthographic)
            {
                halfHeight = cameraToDraw.orthographicSize;
                halfWidth = halfHeight * cameraToDraw.aspect;
            }
            else
            {
                halfHeight = depth *
                    Mathf.Tan(
                        cameraToDraw.fieldOfView *
                        0.5f * Mathf.Deg2Rad
                    );

                halfWidth = halfHeight * cameraToDraw.aspect;
            }

            halfHeight *= visualScale;
            halfWidth *= visualScale;

            Vector3 planeCenter =
                cameraPosition + cameraForward * depth;

            DrawCameraConstraintRectangle(
                planeCenter,
                cameraRight,
                cameraUp,
                halfWidth,
                halfHeight,
                Color.cyan
            );

            float left = -cameraBoundaryOffsetLeft;
            float right = 1f + cameraBoundaryOffsetRight;
            float bottom = -cameraBoundaryOffsetBottom;
            float top = 1f + cameraBoundaryOffsetTop;

            float safeHalfWidth =
                halfWidth * (right - left);

            float safeHalfHeight =
                halfHeight * (top - bottom);

            Vector3 boundaryCenter =
                planeCenter +
                cameraRight * (halfWidth * (left + right - 1f)) +
                cameraUp * (halfHeight * (bottom + top - 1f));

            DrawCameraConstraintRectangle(
                boundaryCenter,
                cameraRight,
                cameraUp,
                safeHalfWidth * 0.5f,
                safeHalfHeight * 0.5f,
                Color.green
            );

            if (drawDebugBoundsMarkers)
            {
                float marker = Mathf.Max(0.08f, depth * 0.008f);

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(planeCenter, marker);

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(
                    planeCenter +
                    cameraRight * safeHalfWidth +
                    cameraUp * safeHalfHeight,
                    marker
                );
            }

#if UNITY_EDITOR
            if (drawDebugBoundsLabels)
            {
                Handles.color = Color.cyan;
                Handles.Label(
                    planeCenter,
                    $"{label} - VISIBLE\n" +
                    $"Depth: {depth:F1} | " +
                    $"Half: {halfWidth:F1} x {halfHeight:F1}"
                );

                Handles.color = Color.green;
                Handles.Label(
                    boundaryCenter + cameraUp * safeHalfHeight * 0.5f,
                    $"CAMERA WALLS\n" +
                    $"L: {cameraBoundaryOffsetLeft:+0.00;-0.00;0.00} | " +
                    $"R: {cameraBoundaryOffsetRight:+0.00;-0.00;0.00}\n" +
                    $"B: {cameraBoundaryOffsetBottom:+0.00;-0.00;0.00} | " +
                    $"T: {cameraBoundaryOffsetTop:+0.00;-0.00;0.00}"
                );
            }
#endif
        }

        private static void DrawCameraConstraintRectangle(
            Vector3 center,
            Vector3 right,
            Vector3 up,
            float halfWidth,
            float halfHeight,
            Color color)
        {
            Vector3 topLeft =
                center - right * halfWidth + up * halfHeight;

            Vector3 topRight =
                center + right * halfWidth + up * halfHeight;

            Vector3 bottomRight =
                center + right * halfWidth - up * halfHeight;

            Vector3 bottomLeft =
                center - right * halfWidth - up * halfHeight;

            Gizmos.color = color;

            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }


        // =========================================================
        // GROUP FRAMING / DYNAMIC FOV
        // =========================================================

        private void UpdateGroupFraming()
        {
            if (autoScrollCamera == null ||
                combatMode ||
                player1CameraFrozen)
                return;

            CollectAllPlayers();

            // En solo, le FOV revient à sa valeur de base/minimale.
            if (cachedPlayerMovements.Count <= 1 ||
                !groupFramingEnabled)
            {
                float soloFov = Mathf.Clamp(
                    baseAutoScrollFov,
                    minGroupFieldOfView,
                    maxGroupFieldOfView
                );

                SmoothGroupFov(soloFov);
                return;
            }

            float aspect =
                outputCamera != null && outputCamera.aspect > 0f
                    ? outputCamera.aspect
                    : 16f / 9f;

            Transform cameraTransform = autoScrollCamera.transform;

            float requiredTangent = 0f;

            foreach (PlayerMovement movement in cachedPlayerMovements)
            {
                if (movement == null)
                    continue;

                Vector3 worldPosition =
                    movement.transform.position + framingTargetOffset;

                Vector3 localPosition =
                    cameraTransform.InverseTransformPoint(worldPosition);

                if (localPosition.z <= 0.01f)
                    continue;

                float verticalTangent =
                    Mathf.Abs(localPosition.y) / localPosition.z;

                float horizontalTangent =
                    Mathf.Abs(localPosition.x) /
                    (localPosition.z * aspect);

                requiredTangent = Mathf.Max(
                    requiredTangent,
                    verticalTangent,
                    horizontalTangent
                );
            }

            float requiredFov = minGroupFieldOfView;

            if (requiredTangent > 0f)
            {
                float requiredHalfFov =
                    Mathf.Atan(requiredTangent * groupFramingMargin);

                requiredFov = Mathf.Max(
                    minGroupFieldOfView,
                    requiredHalfFov * 2f * Mathf.Rad2Deg
                );
            }

            requiredFov = Mathf.Clamp(
                requiredFov,
                minGroupFieldOfView,
                maxGroupFieldOfView
            );

            SmoothGroupFov(requiredFov);
        }

        private void SmoothGroupFov(float requiredFov)
        {
            if (groupFovSmoothTime <= 0f)
            {
                currentGroupFov = requiredFov;
                groupFovVelocity = 0f;
            }
            else
            {
                currentGroupFov = Mathf.SmoothDamp(
                    currentGroupFov,
                    requiredFov,
                    ref groupFovVelocity,
                    groupFovSmoothTime,
                    Mathf.Infinity,
                    Time.deltaTime
                );
            }

            autoScrollCamera.Lens.FieldOfView = currentGroupFov;
        }

        // =========================================================
        // COMBAT KNOTS
        // =========================================================

        private void CheckCombatKnot()
        {
            if (currentCombatStep >= combatKnots.Count)
                return;

            CombatKnot combatKnot = combatKnots[currentCombatStep];

            if (combatKnot == null)
            {
                currentCombatStep++;
                return;
            }

            if (autoScrollDolly.CameraPosition >=
                combatKnot.KnotIndex - combatTolerance)
            {
                StartCombat(combatKnot);
            }
        }

        private void StartCombat(CombatKnot combatKnot)
        {
            if (combatKnot == null)
                return;

            PlayerMovement player1 = GetPlayer1();

            movementSpaceBeforeCombat = player1 != null
                ? player1.GetMovementSpace()
                : null;

            combatMode = true;
            combatEnabled = true;

            SetCombatStem(true);

            GameManager.Instance.OnArenaEnter.Invoke();

            currentSplinePosition = combatKnot.KnotIndex;
            targetSplinePosition = currentSplinePosition;
            splineVelocity = 0f;

            previousFrameWasBlend = false;
            currentBlendActive = false;

            ClearBlendRailCache();

            autoScrollDolly.CameraPosition = currentSplinePosition;

            ApplyCombatPreset(combatKnot);

            ApplyCameraState();

            QueueCombatShake(
                combatKnot.StartShakePresetSo,
                combatKnot.StartShakeDelay
            );

            combatKnot.EnableCombatObjects();

            if (debugEnabled)
            {
                UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"Combat commencé : {combatKnot.name}.",
                    combatKnot
                );
            }
        }

        private void ApplyCombatPreset(CombatKnot combatKnot)
        {
            VCameraPresetSO presetSo = combatKnot.CameraPresetSo;

            if (presetSo == null)
                return;

            if (combatKnot.CombatCenter != null)
            {
                Vector3 cameraPosition =
                    combatKnot.CombatCenter.TransformPoint(presetSo.CameraOffset);

                Quaternion cameraRotation =
                    combatKnot.CombatCenter.rotation *
                    Quaternion.Euler(presetSo.CameraRotation);

                combatCamera.ForceCameraPosition(
                    cameraPosition,
                    cameraRotation
                );
            }

            if (combatKnot.MovementSpace != null)
            {
                combatKnot.MovementSpace.localRotation =
                    Quaternion.Euler(presetSo.MovementSpaceRotation);

                ApplyMovementSpaceToAllPlayers(combatKnot.MovementSpace);
            }

            combatCamera.Lens.FieldOfView = presetSo.FieldOfView;
        }

        private void ApplyInitialMovementSpace()
        {
            if (initialMovementSpace != null)
            {
                ApplyMovementSpaceToAllPlayers(
                    initialMovementSpace.transform
                );
            }
        }

        private void EndCombat()
        {
            SetCombatStem(false);

            CombatKnot completedCombatKnot =
                currentCombatStep >= 0 &&
                currentCombatStep < combatKnots.Count
                    ? combatKnots[currentCombatStep]
                    : null;

            completedCombatKnot?.DisableCombatObjects();

            Transform nextMovementSpace = null;

            if (completedCombatKnot?.NextMovementSpace != null)
            {
                nextMovementSpace =
                    completedCombatKnot.NextMovementSpace.transform;
            }
            else
            {
                nextMovementSpace = movementSpaceBeforeCombat;
            }

            if (nextMovementSpace != null)
            {
                ApplyMovementSpaceToAllPlayers(nextMovementSpace);
            }
            else
            {
                UnityDebug.LogWarning(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    "Aucun Movement Space disponible à la sortie du combat.",
                    this
                );
            }

            movementSpaceBeforeCombat = null;

            combatMode = false;
            currentCombatStep++;

            currentSplinePosition = autoScrollDolly.CameraPosition;
            targetSplinePosition = currentSplinePosition;

            splineVelocity = 0f;

            previousFrameWasBlend = false;
            currentBlendActive = false;

            ClearBlendRailCache();

            ApplyCameraState();

            currentHorizontalCorrection = 0f;
            horizontalCorrectionVelocity = 0f;

            ApplyMovementSpaceCameraRotationImmediate();

            if (completedCombatKnot != null)
            {
                QueueCombatShake(
                    completedCombatKnot.EndShakePresetSo,
                    completedCombatKnot.EndShakeDelay
                );
            }

            if (debugEnabled && completedCombatKnot != null)
            {
                UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"Combat terminé : {completedCombatKnot.name}.",
                    completedCombatKnot
                );
            }
        }

        // =========================================================
        // CAMERA SHAKE
        // =========================================================

        private void QueueCombatShake(
            VCameraShakePresetSO presetSo,
            float delay)
        {
            if (presetSo == null)
                return;

            if (pendingCombatShake != null)
                StopCoroutine(pendingCombatShake);

            pendingCombatShake = StartCoroutine(
                PlayCombatShake(presetSo, Mathf.Max(0f, delay))
            );
        }

        private IEnumerator PlayCombatShake(
            VCameraShakePresetSO presetSo,
            float delay)
        {
            yield return null;

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            pendingCombatShake = null;

            if (presetSo == null)
                yield break;

            if (!CameraShakeManager.HasInstance)
            {
                UnityDebug.LogError(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"Impossible de jouer '{presetSo.name}' : " +
                    "aucun CameraShakeManager actif.",
                    this
                );

                yield break;
            }

            CameraShakeManager.PlayShake(presetSo);

            if (debugEnabled)
            {
                UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"Combat Shake joué : {presetSo.name} | " +
                    $"Delay={delay:F2}s",
                    this
                );
            }
        }

        // =========================================================
        // VALIDATION
        // =========================================================

        private bool ValidateReferences()
        {
            bool valid = true;

            if (autoScrollCamera == null)
            {
                UnityDebug.LogError(
                    $"[{nameof(VCameraSetup_Follow)}] Auto Scroll Camera non assignée.",
                    this
                );
                valid = false;
            }

            if (combatCamera == null)
            {
                UnityDebug.LogError(
                    $"[{nameof(VCameraSetup_Follow)}] Combat Camera non assignée.",
                    this
                );
                valid = false;
            }

            if (autoScrollDolly == null)
            {
                UnityDebug.LogError(
                    $"[{nameof(VCameraSetup_Follow)}] Auto Scroll Dolly non assigné.",
                    this
                );
                valid = false;
            }

            if (gameplaySplineContainer == null)
            {
                UnityDebug.LogError(
                    $"[{nameof(VCameraSetup_Follow)}] GameplaySpline non assignée.",
                    this
                );
                valid = false;
            }

            if (playerManager == null)
            {
                UnityDebug.LogError(
                    $"[{nameof(VCameraSetup_Follow)}] PlayerManager non assigné.",
                    this
                );
                valid = false;
            }
            else if (!HasValidPlayerMovement())
            {
                UnityDebug.LogWarning(
                    $"[{nameof(VCameraSetup_Follow)}] Aucun joueur actif dans le PlayerManager.",
                    this
                );
            }

            return valid;
        }

        // =========================================================
        // COMBAT AUDIO STEM
        // =========================================================

        private void SetCombatStem(bool enabled)
        {
            if (!useCombatStem)
                return;

            RhythmManager rhythm = RhythmManager.Instance;

            if (rhythm == null ||
                !rhythm.IsReady ||
                rhythm.CurrentAudioMode != RhythmManager.AudioMode.StemSystem)
                return;

            rhythm.SetStem(combatStemId, enabled);

            if (debugEnabled)
            {
                UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"Combat stem '{combatStemId}' : " +
                    $"{(enabled ? "ON" : "OFF")}.",
                    this
                );
            }
        }

        // =========================================================
        // COMBAT KNOT VALIDATION
        // =========================================================

        private void ValidateCombatKnots()
        {
            if (combatKnots == null ||
                cameraSplineContainer == null)
                return;

            for (int i = 0; i < combatKnots.Count; i++)
            {
                CombatKnot knot = combatKnots[i];

                if (knot != null &&
                    (knot.KnotIndex < 0 ||
                     knot.KnotIndex >= cameraSplineContainer.Spline.Count))
                {
                    UnityDebug.LogError(
                        $"[{nameof(VCameraSetup_Follow)}] " +
                        $"Combat Knot '{knot.name}' : " +
                        $"Knot Index {knot.KnotIndex} invalide.",
                        knot
                    );
                }
            }
        }

        // =========================================================
        // CAMERA STATE
        // =========================================================

        private void ApplyCameraState()
        {
            autoScrollCamera.Priority.Value = combatMode ? 0 : 10;
            combatCamera.Priority.Value = combatMode ? 10 : 0;

            if (combatMode)
                combatCamera.Prioritize();
            else
                autoScrollCamera.Prioritize();
        }

        private void DisableCombat()
        {
            combatEnabled = false;
        }
    }
}