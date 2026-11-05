
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Splines;
using SynthOfRage.Scripts.Gameplay.Core;
using SynthOfRage.Scripts.Gameplay.Unit.Player;
using SynthOfRage.Scripts.Gameplay.Unit.Player.Movement.Utils;
using UnityDebug = UnityEngine.Debug;

namespace SynthOfRage.Scripts.CameraSystem
{
    [DefaultExecutionOrder(100)]
    public class VCameraSetup_Follow : MonoBehaviour
    {
        
        
        [Header("Cameras")]
        [SerializeField] private CinemachineCamera autoScrollCamera;
        [SerializeField] private CinemachineCamera combatCamera;

        [Header("Rail")]
        [SerializeField] private CinemachineSplineDolly autoScrollDolly;
        [SerializeField] private CinemachineSplineDolly combatDolly;
        [SerializeField, Range(50, 1000)] private int splineProjectionSamples = 500;

        [Header("Player Manager")]
        [SerializeField] private PlayerManager playerManager;

        [Header("Initial Movement Space")]
        [SerializeField] private MovementSpace initialMovementSpace;

        [Header("Player 1 Camera Lock")]
        [Tooltip("La caméra utilise uniquement le premier joueur actif comme référence.")]
        [SerializeField] private bool lockCameraToPlayer1 = true;

        [Header("Other Players Camera Constraint")]
        [Tooltip("Empêche les joueurs secondaires de dépasser la limite extérieure de la caméra.")]
        [SerializeField] private bool constrainOtherPlayersToCamera = true;

        [Tooltip("Distance maximale autorisée au-delà du cadre visible, en unités monde.")]
        [SerializeField, Min(0f)] private float cameraConstraintWorldTolerance = 2f;

        [Tooltip("Affiche les informations de détection et les corrections dans la Console.")]
        [SerializeField] private bool debugCameraConstraint = true;

        [Tooltip("Dessine dans la Scene View les limites visibles et la zone de tolérance de chaque joueur secondaire.")]
        [SerializeField] private bool drawCameraConstraintGizmos = true;

        [Tooltip("Dessine aussi les limites lorsque le composant n'est pas sélectionné.")]
        [SerializeField] private bool drawCameraConstraintGizmosAlways = true;

        [Header("Camera Constraint Bounds - Debug Definition")]
        [Tooltip("Distance fixe depuis la caméra à laquelle dessiner le rectangle de référence. Cette profondeur sert uniquement au DebugDraw.")]
        [SerializeField, Min(0.1f)] private float debugBoundsReferenceDepth = 10f;

        [Tooltip("Échelle visuelle appliquée aux rectangles du DebugDraw. 1 = dimensions réelles calculées depuis la caméra.")]
        [SerializeField, Min(0.01f)] private float debugBoundsVisualScale = 1f;

        [Tooltip("Dessine également les limites à la profondeur réelle de chaque joueur secondaire.")]
        [SerializeField] private bool drawBoundsAtEachPlayerDepth = true;

        [Tooltip("Dessine les coins et les centres des rectangles pour les rendre plus faciles à repérer.")]
        [SerializeField] private bool drawDebugBoundsMarkers = true;

        [Tooltip("Affiche les dimensions et la profondeur des limites dans la Scene View.")]
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
        [SerializeField] private bool groupFramingEnabled = true;
        [SerializeField, Min(1f)] private float groupFramingMargin = 1.2f;
        [SerializeField, Range(30f, 100f)] private float maxGroupFieldOfView = 75f;
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
        
        

        private float currentSplinePosition;
        private float targetSplinePosition;
        private float splineVelocity;
        private SplineContainer splineContainer;

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

        private Quaternion cameraRotationOffsetFromMovementSpace = Quaternion.identity;
        private float currentHorizontalCorrection;
        private float horizontalCorrectionVelocity;
        private bool cameraReferenceInitialized;

        private float baseAutoScrollFov;
        private float currentGroupFov;
        private float groupFovVelocity;

        private bool combatMode;
        private int currentCombatStep;
        private int debugUpdateCounter;
        private float lastPlayerDetectionDebugTime = -1f;

        private bool previousCameraLockState;

        private Coroutine pendingCombatShake;

        // Movement Space de Player 1 sauvegardé avant le combat.
        private Transform movementSpaceBeforeCombat;

        // Tous les joueurs actifs.
        private readonly List<PlayerMovement> cachedPlayerMovements = new();

        // Joueurs pris en compte par le calcul de la caméra.
        private readonly List<PlayerMovement> cachedCameraMovements = new();
        private readonly List<Transform> cachedCameraTargets = new();

        private void Start()
        {
            if (!ValidateReferences())
                return;

            splineContainer = autoScrollDolly.Spline;

            if (splineContainer == null)
            {
                if(debugEnabled)UnityDebug.LogError(
                    $"[{nameof(VCameraSetup_Follow)}] Aucun Spline Container assigné.",
                    this
                );
                return;
            }

            autoScrollDolly.PositionUnits = PathIndexUnit.Knot;

            if (combatDolly != null)
                combatDolly.enabled = false;

            outputCamera ??= Camera.main;

            baseAutoScrollFov = autoScrollCamera.Lens.FieldOfView;
            currentGroupFov = baseAutoScrollFov;

            currentSplinePosition = autoScrollDolly.CameraPosition;
            targetSplinePosition = currentSplinePosition;

            autoScrollCamera.gameObject.SetActive(true);
            combatCamera.gameObject.SetActive(true);

            ApplyCameraState();
            ApplyInitialMovementSpace();

            previousCameraLockState = lockCameraToPlayer1;

            InitializeCameraReference();
            ApplyMovementSpaceCameraRotationImmediate();
            ValidateCombatKnots();
        }

        private Coroutine cameraConstraintRoutine;

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnArenaExit += DisableCombat;

            cameraConstraintRoutine = StartCoroutine(
                CameraConstraintRoutine()
            );
        }

        private void OnDisable()
        {
            SetCombatStem(false);
            
            if (GameManager.Instance != null)
                GameManager.Instance.OnArenaExit -= DisableCombat;

            if (cameraConstraintRoutine != null)
            {
                StopCoroutine(cameraConstraintRoutine);
                cameraConstraintRoutine = null;
            }
        }

        private IEnumerator CameraConstraintRoutine()
        {
            WaitForEndOfFrame wait = new WaitForEndOfFrame();

            while (true)
            {
                yield return wait;

                if (!combatMode)
                    ConstrainOtherPlayersToCamera();
            }
        }

        private void Update()
        {
            if (debugEnabled && ++debugUpdateCounter >= 30)
            {
                debugUpdateCounter = 0;

                if(debugEnabled)UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] Camera={currentSplinePosition:F3} Target={targetSplinePosition:F3} Blend={currentBlendActive} LockP1={lockCameraToPlayer1}",
                    this
                );
            }

            if (combatMode)
            {
                if (!combatEnabled)
                    EndCombat();

                return;
            }

            UpdateFollow();

            if (currentCombatStep < combatKnots.Count)
                CheckCombatKnot();
        }

        private void LateUpdate()
        {
            if (combatMode)
                return;

            HandleCameraLockModeChange();

            UpdateAutoScrollCameraRotation();
            UpdateGroupFraming();
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

        private void CollectCameraTargets()
        {
            cachedCameraTargets.Clear();
            cachedCameraMovements.Clear();

            CollectAllPlayers();

            if (cachedPlayerMovements.Count == 0)
                return;

            if (lockCameraToPlayer1)
            {
                PlayerMovement player1 = cachedPlayerMovements[0];

                if (player1 != null)
                {
                    cachedCameraMovements.Add(player1);
                    cachedCameraTargets.Add(player1.transform);
                }

                return;
            }

            foreach (PlayerMovement movement in cachedPlayerMovements)
            {
                if (movement == null)
                    continue;

                cachedCameraMovements.Add(movement);
                cachedCameraTargets.Add(movement.transform);
            }
        }

        private bool HasValidPlayerMovement()
        {
            CollectCameraTargets();
            return cachedCameraMovements.Count > 0;
        }

        // =========================================================
        // MOVEMENT SPACE
        // =========================================================

        private bool TryGetCameraBlend(out MovementSpace.BlendResult blend)
        {
            blend = default;

            CollectCameraTargets();

            foreach (PlayerMovement movement in cachedCameraMovements)
            {
                if (movement != null &&
                    movement.TryGetActiveMovementSpaceBlend(out blend))
                    return true;
            }

            return false;
        }

        private Quaternion GetCameraMovementSpaceRotation()
        {
            CollectCameraTargets();

            if (cachedCameraMovements.Count == 0)
                return Quaternion.identity;

            if (lockCameraToPlayer1)
                return cachedCameraMovements[0].GetEffectiveMovementSpaceRotation();

            Quaternion averageRotation = Quaternion.identity;
            int validCount = 0;

            foreach (PlayerMovement movement in cachedCameraMovements)
            {
                if (movement == null)
                    continue;

                Quaternion playerRotation =
                    movement.GetEffectiveMovementSpaceRotation();

                validCount++;

                averageRotation = validCount == 1
                    ? playerRotation
                    : Quaternion.Slerp(
                        averageRotation,
                        playerRotation,
                        1f / validCount
                    );
            }

            return averageRotation;
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
            if (!TryGetTrackingPoint(out _))
                return;

            bool hasBlend = TryGetCameraBlend(out MovementSpace.BlendResult blend);

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
            if (!TryGetTrackingPoint(out Vector3 trackingPoint))
                return;

            targetSplinePosition = GetSplinePosition(trackingPoint);

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
                followSmoothTime
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
            cachedBlendEndSplinePosition = GetSplinePosition(blend.WorldEnd);
            blendRailCacheValid = true;

            targetSplinePosition = currentSplinePosition;

            if (debugEnabled)
            {
                UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] Blend Rail | Start={cachedBlendStartSplinePosition:F3} End={cachedBlendEndSplinePosition:F3} T={blend.T:F3}",
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
                blendFollowSmoothTime
            );
        }

        private void RefreshBlendRailCacheIfNeeded(MovementSpace.BlendResult blend)
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

            if (Vector3.SqrMagnitude(cachedBlendWorldEnd - blend.WorldEnd) <= 0.0001f)
                return;

            cachedBlendWorldEnd = blend.WorldEnd;
            cachedBlendEndSplinePosition = GetSplinePosition(blend.WorldEnd);
        }

        private void ClearBlendRailCache()
        {
            blendRailCacheValid = false;
            cachedBlendSource = null;
            cachedBlendTarget = null;
        }

        private bool TryGetTrackingPoint(out Vector3 trackingPoint)
        {
            trackingPoint = Vector3.zero;

            CollectCameraTargets();

            if (cachedCameraTargets.Count == 0)
                return false;

            if (lockCameraToPlayer1)
            {
                trackingPoint = cachedCameraTargets[0].position;
                return true;
            }

            Vector3 minimum = cachedCameraTargets[0].position;
            Vector3 maximum = minimum;

            for (int i = 1; i < cachedCameraTargets.Count; i++)
            {
                Vector3 position = cachedCameraTargets[i].position;

                minimum = Vector3.Min(minimum, position);
                maximum = Vector3.Max(maximum, position);
            }

            trackingPoint = (minimum + maximum) * 0.5f;
            return true;
        }

        // =========================================================
        // CAMERA ROTATION
        // =========================================================

        private void HandleCameraLockModeChange()
        {
            if (previousCameraLockState == lockCameraToPlayer1)
                return;

            // Conserve l'orientation actuelle de la caméra au changement de mode.
            Quaternion currentCameraRotation = autoScrollCamera.transform.rotation;
            Quaternion newMovementRotation = GetCameraMovementSpaceRotation();

            cameraRotationOffsetFromMovementSpace =
                Quaternion.Inverse(newMovementRotation) *
                currentCameraRotation;

            cameraReferenceInitialized = true;
            currentHorizontalCorrection = 0f;
            horizontalCorrectionVelocity = 0f;

            previousCameraLockState = lockCameraToPlayer1;

            previousFrameWasBlend = false;
            currentBlendActive = false;
            ClearBlendRailCache();

            if (debugEnabled)
            {
                UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] Camera lock changé : {lockCameraToPlayer1}",
                    this
                );
            }
        }

        private void InitializeCameraReference()
        {
            if (autoScrollCamera == null || !HasValidPlayerMovement())
                return;

            Quaternion movementSpaceRotation = GetCameraMovementSpaceRotation();

            cameraRotationOffsetFromMovementSpace =
                Quaternion.Inverse(movementSpaceRotation) *
                autoScrollCamera.transform.rotation;

            currentHorizontalCorrection = 0f;
            horizontalCorrectionVelocity = 0f;
            cameraReferenceInitialized = true;
        }

        private void UpdateAutoScrollCameraRotation()
        {
            if (autoScrollCamera == null || !HasValidPlayerMovement())
                return;

            if (!cameraReferenceInitialized)
                InitializeCameraReference();

            Quaternion movementSpaceRotation = GetCameraMovementSpaceRotation();

            Quaternion baseRotation =
                movementSpaceRotation * cameraRotationOffsetFromMovementSpace;

            float targetCorrection = 0f;

            if (framingCorrectionEnabled &&
                TryGetTrackingPoint(out Vector3 trackingPoint))
            {
                Vector3 framingPoint = trackingPoint + framingTargetOffset;

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
                    horizontalCorrectionSmoothTime
                );
            }

            Vector3 rotationAxis = movementSpaceRotation * Vector3.up;

            autoScrollCamera.transform.rotation =
                Quaternion.AngleAxis(currentHorizontalCorrection, rotationAxis) *
                baseRotation;
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

            float aspect = outputCamera != null && outputCamera.aspect > 0f
                ? outputCamera.aspect
                : 16f / 9f;

            float verticalFov =
                Mathf.Clamp(autoScrollCamera.Lens.FieldOfView, 1f, 179f) *
                Mathf.Deg2Rad;

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
                    (viewportX - right) / Mathf.Max(0.001f, 1f - right)
                );
            }

            return -Mathf.Clamp01(
                (left - viewportX) / Mathf.Max(0.001f, left)
            );
        }

        private void ApplyMovementSpaceCameraRotationImmediate()
        {
            if (autoScrollCamera == null || !HasValidPlayerMovement())
                return;

            if (!cameraReferenceInitialized)
                InitializeCameraReference();

            currentHorizontalCorrection = 0f;
            horizontalCorrectionVelocity = 0f;

            autoScrollCamera.transform.rotation =
                GetCameraMovementSpaceRotation() *
                cameraRotationOffsetFromMovementSpace;
        }

        // =========================================================
        // OTHER PLAYERS CAMERA CONSTRAINT
        // =========================================================

        private void ConstrainOtherPlayersToCamera()
        {
            if (!lockCameraToPlayer1 ||
                !constrainOtherPlayersToCamera ||
                combatMode)
            {
                return;
            }

            // Always refresh the PlayerManager list before checking its contents.
            CollectAllPlayers();

            // Diagnostic runs BEFORE the player-count and camera checks so we can
            // determine whether Player 2 is actually being returned by PlayerManager.
            DebugCameraPlayerDetection();

            // Resolve the real render camera at runtime as well as in the gizmo path.
            // CinemachineCamera is a virtual camera; constraints must use the Unity Camera
            // that actually renders its output.
            if (outputCamera == null)
                outputCamera = Camera.main;

            if (outputCamera == null)
            {
                if (debugCameraConstraint)
                    if(debugEnabled)UnityDebug.LogError(
                        $"[{nameof(VCameraSetup_Follow)}] CONSTRAINT ABORTED: outputCamera is NULL. " +
                        "Assign the real Unity Camera (Cinemachine Brain output) to Output Camera.",
                        this
                    );
                return;
            }

            if (cachedPlayerMovements.Count <= 1)
                return;

            PlayerMovement player1 = cachedPlayerMovements[0];

            if (player1 == null)
                return;

            Transform cameraTransform = outputCamera.transform;

            // Camera-relative axes: these automatically account for camera rotation.
            Vector3 cameraPosition = cameraTransform.position;
            Vector3 cameraRight = cameraTransform.right;
            Vector3 cameraUp = cameraTransform.up;
            Vector3 cameraForward = cameraTransform.forward;

            float halfHeight;
            float halfWidth;

            for (int i = 1; i < cachedPlayerMovements.Count; i++)
            {
                PlayerMovement movement = cachedPlayerMovements[i];

                if (movement == null || movement == player1)
                    continue;

                Vector3 worldPosition = movement.transform.position;
                Vector3 targetPosition = worldPosition + framingTargetOffset;
                Vector3 relativePosition = targetPosition - cameraPosition;

                float depth = Vector3.Dot(relativePosition, cameraForward);
                bool isBehindOrTooClose = depth <= outputCamera.nearClipPlane;

                // A player behind the camera must NOT be skipped. ViewportPoint
                // returns (0,0,0) for this case, but that does not mean the player
                // should be ignored by the leash. Project the correction onto a
                // small plane in front of the camera and place the player on the
                // nearest horizontal edge of the expanded bounds.
                if (isBehindOrTooClose)
                {
                    depth = Mathf.Max(outputCamera.nearClipPlane + 1f, 1.5f);
                }

                if (outputCamera.orthographic)
                {
                    halfHeight = outputCamera.orthographicSize;
                    halfWidth = halfHeight * outputCamera.aspect;
                }
                else
                {
                    halfHeight = depth *
                        Mathf.Tan(outputCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                    halfWidth = halfHeight * outputCamera.aspect;
                }

                float cameraX = Vector3.Dot(relativePosition, cameraRight);
                float cameraY = Vector3.Dot(relativePosition, cameraUp);

                if (isBehindOrTooClose)
                {
                    // Preserve a rough left/right preference, but force the
                    // target outside the visible frame so the normal leash
                    // correction moves it onto the tolerance boundary.
                    float side = Mathf.Abs(cameraX) > 0.01f
                        ? Mathf.Sign(cameraX)
                        : (Mathf.Abs(cameraY) > 0.01f ? Mathf.Sign(cameraY) : 1f);

                    cameraX = side * (halfWidth + cameraConstraintWorldTolerance + 0.25f);
                    cameraY = 0f;
                }

                float outsideX = Mathf.Max(Mathf.Abs(cameraX) - halfWidth, 0f);
                float outsideY = Mathf.Max(Mathf.Abs(cameraY) - halfHeight, 0f);
                float outsideDistance = new Vector2(outsideX, outsideY).magnitude;

                if (outsideDistance <= cameraConstraintWorldTolerance)
                    continue;

                float correctionRatio = cameraConstraintWorldTolerance / outsideDistance;

                float correctedX = cameraX;
                float correctedY = cameraY;

                if (outsideX > 0f)
                {
                    correctedX = Mathf.Sign(cameraX) *
                        (halfWidth + outsideX * correctionRatio);
                }

                if (outsideY > 0f)
                {
                    correctedY = Mathf.Sign(cameraY) *
                        (halfHeight + outsideY * correctionRatio);
                }

                Vector3 correctedTargetPosition =
                    cameraPosition +
                    cameraForward * depth +
                    cameraRight * correctedX +
                    cameraUp * correctedY;

                Vector3 correctedWorldPosition = correctedTargetPosition - framingTargetOffset;

                // Route the correction through PlayerMovement instead of writing directly
                // to Transform. Direct Transform writes can be overwritten by the next
                // CharacterController / MovementSpace update.
                Vector3 correctionDelta = correctedWorldPosition - worldPosition;
                movement.ApplyExternalMovement(correctionDelta);

                Vector3 actualAfterPosition = movement.transform.position;

                if (debugCameraConstraint)
                {
                    if(debugEnabled)UnityDebug.LogWarning(
                        $"[{nameof(VCameraSetup_Follow)}] PLAYER {i} CONSTRAINT APPLIED | " +
                        $"Name={movement.name} | Outside={outsideDistance:F2} | " +
                        $"Tolerance={cameraConstraintWorldTolerance:F2} | " +
                        $"BehindOrTooClose={isBehindOrTooClose} | ProjectedDepth={depth:F2} | " +
                        $"RequestedDelta={correctionDelta} | Before={worldPosition} | " +
                        $"RequestedAfter={correctedWorldPosition} | ActualAfter={actualAfterPosition}",
                        movement
                    );

                    UnityDebug.DrawLine(worldPosition, correctedWorldPosition, Color.red, 0.2f);
                    UnityDebug.DrawLine(correctedWorldPosition, actualAfterPosition, Color.magenta, 0.2f);
                    UnityDebug.DrawRay(actualAfterPosition, cameraRight * 0.5f, Color.green, 0.2f);
                }
            }
        }

        private void OnDrawGizmos()
        {
            // Intentionally independent from lockCameraToPlayer1 and
            // constrainOtherPlayersToCamera: the debug geometry must remain
            // visible even while the constraint itself is disabled.
            if (!drawCameraConstraintGizmos)
                return;

            DrawCameraConstraintGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            // OnDrawGizmos already handles the always-on case. This callback
            // is retained as a fallback when Always is disabled.
            if (!drawCameraConstraintGizmos || drawCameraConstraintGizmosAlways)
                return;

            DrawCameraConstraintGizmos();
        }

        private Camera ResolveCameraForGizmos()
        {
            if (outputCamera != null)
                return outputCamera;

            if (Camera.main != null)
                return Camera.main;

            // Camera.main only finds cameras tagged MainCamera. Cinemachine
            // output cameras are not guaranteed to carry that tag.
            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].enabled && cameras[i].gameObject.activeInHierarchy)
                    return cameras[i];
            }

            // Useful when this component is placed on the actual camera rig.
            return GetComponentInChildren<Camera>(true);
        }

        private void DrawCameraConstraintGizmos()
        {
            Camera cameraToDraw = ResolveCameraForGizmos();

            if (cameraToDraw == null)
            {
#if UNITY_EDITOR
                Handles.color = Color.red;
                Handles.Label(transform.position,
                    "VCameraSetup_Follow: NO OUTPUT CAMERA FOUND\nAssign Output Camera in the Inspector.");
#endif
                // Make the missing-camera state impossible to overlook.
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.75f);
                Gizmos.DrawLine(transform.position - Vector3.right, transform.position + Vector3.right);
                Gizmos.DrawLine(transform.position - Vector3.up, transform.position + Vector3.up);
                return;
            }

            Transform cameraTransform = cameraToDraw.transform;
            Vector3 cameraPosition = cameraTransform.position;
            Vector3 cameraRight = cameraTransform.right;
            Vector3 cameraUp = cameraTransform.up;
            Vector3 cameraForward = cameraTransform.forward;

            // Reference bounds: fixed world depth, always drawn when gizmos are enabled.
            float referenceDepth = Mathf.Max(cameraToDraw.nearClipPlane + 0.1f, debugBoundsReferenceDepth);
            DrawBoundsAtDepth(
                cameraToDraw, cameraPosition, cameraRight, cameraUp, cameraForward,
                referenceDepth, "CAMERA REFERENCE", Mathf.Max(0.01f, debugBoundsVisualScale)
            );

            // Draw a visible axis marker at the actual output camera position.
            float markerSize = Mathf.Max(0.2f, referenceDepth * 0.025f);
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(cameraPosition, markerSize);
            Gizmos.DrawLine(cameraPosition, cameraPosition + cameraForward * referenceDepth);

#if UNITY_EDITOR
            if (drawDebugBoundsLabels)
            {
                Handles.color = Color.white;
                Handles.Label(cameraPosition + cameraUp * markerSize * 2f,
                    $"OUTPUT CAMERA: {cameraToDraw.name}\nDepth: {referenceDepth:F1} | FOV: {cameraToDraw.fieldOfView:F1} | Aspect: {cameraToDraw.aspect:F2}");
            }
#endif

            // Player-dependent bounds are supplementary; the camera reference
            // rectangle above does not depend on PlayerManager or Play Mode.
            if (!Application.isPlaying || !drawBoundsAtEachPlayerDepth || playerManager == null)
                return;

            CollectAllPlayers();
            if (cachedPlayerMovements.Count <= 1 || cachedPlayerMovements[0] == null)
                return;

            for (int i = 1; i < cachedPlayerMovements.Count; i++)
            {
                PlayerMovement movement = cachedPlayerMovements[i];
                if (movement == null || movement == cachedPlayerMovements[0])
                    continue;

                Vector3 targetPosition = movement.transform.position + framingTargetOffset;
                float playerDepth = Vector3.Dot(targetPosition - cameraPosition, cameraForward);

                Gizmos.color = playerDepth <= cameraToDraw.nearClipPlane ? Color.red : Color.magenta;
                Gizmos.DrawWireSphere(targetPosition, markerSize * 0.75f);
                Gizmos.DrawLine(targetPosition, cameraPosition + cameraForward * Mathf.Max(0f, playerDepth));

                if (playerDepth > cameraToDraw.nearClipPlane)
                {
                    DrawBoundsAtDepth(
                        cameraToDraw, cameraPosition, cameraRight, cameraUp, cameraForward,
                        playerDepth, $"PLAYER {i} DEPTH", Mathf.Max(0.01f, debugBoundsVisualScale)
                    );
                }
#if UNITY_EDITOR
                else if (drawDebugBoundsLabels)
                {
                    Handles.color = Color.red;
                    Handles.Label(targetPosition, $"PLAYER {i}: BEHIND / BEFORE NEAR CLIP");
                }
#endif
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
                halfHeight = depth * Mathf.Tan(cameraToDraw.fieldOfView * 0.5f * Mathf.Deg2Rad);
                halfWidth = halfHeight * cameraToDraw.aspect;
            }

            halfHeight *= visualScale;
            halfWidth *= visualScale;
            Vector3 planeCenter = cameraPosition + cameraForward * depth;
            float tolerance = cameraConstraintWorldTolerance * visualScale;

            // Actual visible frame (cyan) and tolerance envelope (yellow).
            DrawCameraConstraintRectangle(
                planeCenter, cameraRight, cameraUp, halfWidth, halfHeight, Color.cyan
            );
            DrawCameraConstraintRectangle(
                planeCenter, cameraRight, cameraUp,
                halfWidth + tolerance, halfHeight + tolerance, Color.yellow
            );

            if (drawDebugBoundsMarkers)
            {
                float marker = Mathf.Max(0.08f, depth * 0.008f);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(planeCenter, marker);
                Gizmos.DrawWireSphere(planeCenter - cameraRight * halfWidth - cameraUp * halfHeight, marker);
                Gizmos.DrawWireSphere(planeCenter + cameraRight * halfWidth + cameraUp * halfHeight, marker);
                Gizmos.DrawWireSphere(planeCenter - cameraRight * halfWidth + cameraUp * halfHeight, marker);
                Gizmos.DrawWireSphere(planeCenter + cameraRight * halfWidth - cameraUp * halfHeight, marker);

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(planeCenter - cameraRight * (halfWidth + tolerance) - cameraUp * (halfHeight + tolerance), marker);
                Gizmos.DrawWireSphere(planeCenter + cameraRight * (halfWidth + tolerance) + cameraUp * (halfHeight + tolerance), marker);
            }

#if UNITY_EDITOR
            if (drawDebugBoundsLabels)
            {
                Handles.color = Color.cyan;
                Handles.Label(planeCenter,
                    $"{label} - VISIBLE\nDepth: {depth:F1} | Half: {halfWidth:F1} x {halfHeight:F1}");
                Handles.color = Color.yellow;
                Handles.Label(planeCenter + cameraUp * (halfHeight + tolerance),
                    $"OUTER LIMIT: +{cameraConstraintWorldTolerance:F1} units");
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
            Vector3 topLeft = center - right * halfWidth + up * halfHeight;
            Vector3 topRight = center + right * halfWidth + up * halfHeight;
            Vector3 bottomRight = center + right * halfWidth - up * halfHeight;
            Vector3 bottomLeft = center - right * halfWidth - up * halfHeight;

            Gizmos.color = color;
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }

        private void DebugCameraPlayerDetection()
        {
            if (!debugCameraConstraint)
                return;

            // One diagnostic report per second to avoid flooding the Console.
            if (Time.unscaledTime - lastPlayerDetectionDebugTime < 1f)
                return;

            lastPlayerDetectionDebugTime = Time.unscaledTime;

            string cameraName = outputCamera != null ? outputCamera.name : "NULL";
            string managerName = playerManager != null ? playerManager.name : "NULL";

            if(debugEnabled)UnityDebug.Log(
                $"========== CAMERA PLAYER DETECTION ==========" +
                $"\nCamera output: {cameraName}" +
                $"\nPlayerManager: {managerName}" +
                $"\nPlayerManager list count: {cachedPlayerMovements.Count}" +
                $"\nLock camera to Player 1: {lockCameraToPlayer1}" +
                $"\nConstraint enabled: {constrainOtherPlayersToCamera}",
                this
            );

            if (playerManager == null)
            {
                if(debugEnabled)UnityDebug.LogError(
                    $"[{nameof(VCameraSetup_Follow)}] PlayerManager is NULL. No players can be collected.",
                    this
                );
                return;
            }

            for (int i = 0; i < cachedPlayerMovements.Count; i++)
            {
                PlayerMovement player = cachedPlayerMovements[i];

                if (player == null)
                {
                    UnityDebug.LogWarning(
                        $"[{nameof(VCameraSetup_Follow)}] PlayerManager entry [{i}] is NULL.",
                        this
                    );
                    continue;
                }

                Vector3 worldPosition = player.transform.position;
                Vector3 viewportPosition = outputCamera != null
                    ? outputCamera.WorldToViewportPoint(worldPosition + framingTargetOffset)
                    : Vector3.zero;

                bool inFrontOfCamera = outputCamera != null &&
                    viewportPosition.z > outputCamera.nearClipPlane;

                bool insideViewport = inFrontOfCamera &&
                    viewportPosition.x >= 0f && viewportPosition.x <= 1f &&
                    viewportPosition.y >= 0f && viewportPosition.y <= 1f;

                if(debugEnabled)UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] PLAYER [{i}] " +
                    $"{(i == 0 ? "(CAMERA TARGET / PLAYER 1)" : "(SECONDARY PLAYER)")}" +
                    $"\nName: {player.name}" +
                    $"\nInstance ID: {player.GetEntityId()}" +
                    $"\nGameObject active: {player.gameObject.activeInHierarchy}" +
                    $"\nPlayerMovement enabled: {player.enabled}" +
                    $"\nWorld position: {worldPosition}" +
                    $"\nViewport position: {viewportPosition}" +
                    $"\nIn front of camera: {inFrontOfCamera}" +
                    $"\nInside visible viewport: {insideViewport}",
                    player
                );
            }

            if (cachedPlayerMovements.Count <= 1)
            {
                UnityDebug.LogWarning(
                    $"[{nameof(VCameraSetup_Follow)}] PlayerManager currently returns " +
                    $"{cachedPlayerMovements.Count} active PlayerMovement component(s). " +
                    "Player 2 is therefore NOT available to the camera constraint.",
                    this
                );
            }
        }

        // =========================================================
        // GROUP FRAMING / DYNAMIC FOV
        // =========================================================

        private void UpdateGroupFraming()
        {
            if (!groupFramingEnabled || autoScrollCamera == null)
                return;

            if (combatMode)
                return;

            CollectCameraTargets();

            float requiredFov = baseAutoScrollFov;

            if (cachedCameraTargets.Count > 0)
            {
                Transform cameraTransform = autoScrollCamera.transform;

                float aspect = outputCamera != null && outputCamera.aspect > 0f
                    ? outputCamera.aspect
                    : 16f / 9f;

                float requiredTangent = 0f;

                foreach (Transform playerTarget in cachedCameraTargets)
                {
                    if (playerTarget == null)
                        continue;

                    Vector3 worldPosition =
                        playerTarget.position + framingTargetOffset;

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

                if (requiredTangent > 0f)
                {
                    float requiredHalfFov = Mathf.Atan(
                        requiredTangent * groupFramingMargin
                    );

                    requiredFov = Mathf.Max(
                        baseAutoScrollFov,
                        requiredHalfFov * 2f * Mathf.Rad2Deg
                    );
                }
            }

            requiredFov = Mathf.Clamp(
                requiredFov,
                baseAutoScrollFov,
                maxGroupFieldOfView
            );

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
                    groupFovSmoothTime
                );
            }

            autoScrollCamera.Lens.FieldOfView = currentGroupFov;
        }

        // =========================================================
        // SPLINE
        // =========================================================

        private float GetSplinePosition(Vector3 worldPosition)
        {
            if (splineContainer == null)
                return 0f;

            int samples = Mathf.Max(50, splineProjectionSamples);
            float closestT = 0f;
            float closestDistance = float.MaxValue;

            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;

                Vector3 splinePosition =
                    splineContainer.transform.TransformPoint(
                        splineContainer.Spline.EvaluatePosition(t)
                    );

                Vector2 difference = new(
                    worldPosition.x - splinePosition.x,
                    worldPosition.z - splinePosition.z
                );

                float distance = difference.sqrMagnitude;

                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestT = t;
            }

            return SplineUtility.ConvertIndexUnit(
                splineContainer.Spline,
                closestT,
                PathIndexUnit.Normalized,
                PathIndexUnit.Knot
            );
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

            // Sauvegarde le Movement Space de Player 1 avant
            // toute modification effectuée par le preset de combat.
            PlayerMovement player1 = GetPlayer1();

            movementSpaceBeforeCombat = player1 != null
                ? player1.GetMovementSpace()
                : null;

            if (debugEnabled)
            {
                UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] Movement Space sauvegardé avant combat : {movementSpaceBeforeCombat?.name ?? "Aucun"}",
                    this
                );
            }

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

            autoScrollCamera.Priority.Value = 0;
            combatCamera.Priority.Value = 10;
            combatCamera.Prioritize();

            QueueCombatShake(
                combatKnot.StartShakePreset,
                combatKnot.StartShakeDelay
            );

            combatKnot.EnableCombatObjects();

            if (debugEnabled)
            {
                UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] Combat commencé : {combatKnot.name}.",
                    combatKnot
                );
            }
        }

        private void ApplyCombatPreset(CombatKnot combatKnot)
        {
            VCameraPreset preset = combatKnot.CameraPreset;

            if (preset == null)
                return;

            if (combatKnot.CombatCenter != null)
            {
                Vector3 cameraPosition =
                    combatKnot.CombatCenter.TransformPoint(preset.CameraOffset);

                Quaternion cameraRotation =
                    combatKnot.CombatCenter.rotation *
                    Quaternion.Euler(preset.CameraRotation);

                combatCamera.ForceCameraPosition(
                    cameraPosition,
                    cameraRotation
                );
            }

            if (combatKnot.MovementSpace != null)
            {
                combatKnot.MovementSpace.localRotation =
                    Quaternion.Euler(preset.MovementSpaceRotation);

                ApplyMovementSpaceToAllPlayers(
                    combatKnot.MovementSpace
                );
            }

            combatCamera.Lens.FieldOfView = preset.FieldOfView;
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

            // Priorité au Movement Space explicitement défini sur le Combat Knot.
            if (completedCombatKnot?.NextMovementSpace != null)
            {
                nextMovementSpace =
                    completedCombatKnot.NextMovementSpace.transform;
            }
            else
            {
                // Restauration du Movement Space sauvegardé avant le combat.
                nextMovementSpace = movementSpaceBeforeCombat;

                if (debugEnabled && nextMovementSpace != null)
                {
                    UnityDebug.Log(
                        $"[{nameof(VCameraSetup_Follow)}] Next Movement Space absent : restauration de {nextMovementSpace.name}.",
                        this
                    );
                }
            }

            if (nextMovementSpace != null)
            {
                ApplyMovementSpaceToAllPlayers(nextMovementSpace);
            }
            else
            {
                UnityDebug.LogWarning(
                    $"[{nameof(VCameraSetup_Follow)}] Aucun Movement Space disponible à la sortie du combat.",
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

            // Réactivation de la caméra de déplacement.
            autoScrollCamera.Priority.Value = 10;
            combatCamera.Priority.Value = 0;
            autoScrollCamera.Prioritize();

            // Conserve le décalage de rotation initial.
            // Ne pas réinitialiser cameraReferenceInitialized ici :
            // cela ferait mémoriser l'ancienne rotation de combat.
            currentHorizontalCorrection = 0f;
            horizontalCorrectionVelocity = 0f;

            ApplyMovementSpaceCameraRotationImmediate();

            if (completedCombatKnot != null)
            {
                QueueCombatShake(
                    completedCombatKnot.EndShakePreset,
                    completedCombatKnot.EndShakeDelay
                );
            }

            if (debugEnabled && completedCombatKnot != null)
            {
                UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] Combat terminé : {completedCombatKnot.name} | " +
                    $"Movement Space restauré : {nextMovementSpace?.name ?? "Aucun"}",
                    completedCombatKnot
                );
            }
        }

        // =========================================================
        // CAMERA SHAKE
        // =========================================================

        private void QueueCombatShake(CameraShakePreset preset, float delay)
        {
            if (preset == null)
                return;

            if (pendingCombatShake != null)
                StopCoroutine(pendingCombatShake);

            pendingCombatShake = StartCoroutine(
                PlayCombatShake(preset, Mathf.Max(0f, delay))
            );
        }

        private IEnumerator PlayCombatShake(CameraShakePreset preset, float delay)
        {
            yield return null;

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            pendingCombatShake = null;

            if (preset == null)
                yield break;

            if (!CameraShakeManager.HasInstance)
            {
                UnityDebug.LogError(
                    $"[{nameof(VCameraSetup_Follow)}] Impossible de jouer '{preset.name}' : aucun CameraShakeManager actif.",
                    this
                );
                yield break;
            }

            CameraShakeManager.PlayShake(preset);

            if (debugEnabled)
            {
                UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] Combat Shake joué : {preset.name} | Delay={delay:F2}s",
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
            {
                return;
            }

            rhythm.SetStem(combatStemId, enabled);

            if (debugEnabled)
            {
                UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] Combat stem '{combatStemId}' : {(enabled ? "ON" : "OFF")}.",
                    this
                );
            }
        }

        private void ValidateCombatKnots()
        {
            if (combatKnots == null || splineContainer == null)
                return;

            for (int i = 0; i < combatKnots.Count; i++)
            {
                CombatKnot knot = combatKnots[i];

                if (knot != null &&
                    (knot.KnotIndex < 0 ||
                     knot.KnotIndex >= splineContainer.Spline.Count))
                {
                    UnityDebug.LogError(
                        $"[{nameof(VCameraSetup_Follow)}] Combat Knot '{knot.name}' : Knot Index {knot.KnotIndex} invalide.",
                        knot
                    );
                }
            }
        }

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

