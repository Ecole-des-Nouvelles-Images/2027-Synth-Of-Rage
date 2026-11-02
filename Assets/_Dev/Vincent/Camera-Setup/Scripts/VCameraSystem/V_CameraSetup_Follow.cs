
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
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

        [Header("Player / Group")]
        [SerializeField] private Transform target;
        [SerializeField] private List<Transform> groupTargets = new();
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private List<PlayerMovement> playerMovements = new();

        [Header("Initial Movement Space")]
        [SerializeField] private MovementSpace initialMovementSpace;

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

        private Coroutine pendingCombatShake;

        // Reusable collections to avoid allocations during camera updates.
        private readonly List<Transform> cachedGroupTargets = new();
        private readonly HashSet<Transform> uniqueGroupTargets = new();

        private void Start()
        {
            if (!ValidateReferences())
                return;

            splineContainer = autoScrollDolly.Spline;

            if (splineContainer == null)
            {
                UnityDebug.LogError(
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
            InitializeCameraReference();
            ApplyMovementSpaceCameraRotationImmediate();
            ValidateCombatKnots();
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnArenaExit += DisableCombat;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnArenaExit -= DisableCombat;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnArenaExit -= DisableCombat;
        }

        private void Update()
        {
            if (debugEnabled && ++debugUpdateCounter >= 30)
            {
                debugUpdateCounter = 0;

                UnityDebug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] Camera={currentSplinePosition:F3} Target={targetSplinePosition:F3} Blend={currentBlendActive}",
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

            UpdateAutoScrollCameraRotation();
            UpdateGroupFraming();
        }

        // =========================================================
        // GROUP TARGETS
        // =========================================================

        private void CollectGroupTargets()
        {
            cachedGroupTargets.Clear();
            uniqueGroupTargets.Clear();

            if (groupTargets != null)
            {
                foreach (Transform groupTarget in groupTargets)
                    AddGroupTarget(groupTarget);
            }

            if (playerMovement != null)
                AddGroupTarget(playerMovement.transform);

            if (playerMovements != null)
            {
                foreach (PlayerMovement movement in playerMovements)
                {
                    if (movement != null)
                        AddGroupTarget(movement.transform);
                }
            }

            if (cachedGroupTargets.Count == 0 && target != null)
                AddGroupTarget(target);
        }

        private void AddGroupTarget(Transform playerTarget)
        {
            if (playerTarget == null)
                return;

            if (uniqueGroupTargets.Add(playerTarget))
                cachedGroupTargets.Add(playerTarget);
        }

        private bool HasValidPlayerMovement()
        {
            if (playerMovement != null)
                return true;

            if (playerMovements != null)
            {
                foreach (PlayerMovement movement in playerMovements)
                {
                    if (movement != null)
                        return true;
                }
            }

            if (groupTargets != null)
            {
                foreach (Transform groupTarget in groupTargets)
                {
                    if (groupTarget != null &&
                        groupTarget.GetComponentInParent<PlayerMovement>() != null)
                        return true;
                }
            }

            return false;
        }

        // =========================================================
        // GROUP MOVEMENT SPACE
        // =========================================================

        private bool TryGetGroupBlend(out MovementSpace.BlendResult blend)
        {
            blend = default;

            CollectGroupTargets();

            foreach (Transform groupTarget in cachedGroupTargets)
            {
                if (groupTarget == null)
                    continue;

                PlayerMovement movement =
                    groupTarget.GetComponentInParent<PlayerMovement>();

                if (movement != null &&
                    movement.TryGetActiveMovementSpaceBlend(out blend))
                    return true;
            }

            return false;
        }

        private Quaternion GetGroupMovementSpaceRotation()
        {
            if (TryGetGroupBlend(out MovementSpace.BlendResult blend))
                return blend.Rotation;

            if (playerMovement != null)
                return playerMovement.GetEffectiveMovementSpaceRotation();

            CollectGroupTargets();

            foreach (Transform groupTarget in cachedGroupTargets)
            {
                if (groupTarget == null)
                    continue;

                PlayerMovement movement =
                    groupTarget.GetComponentInParent<PlayerMovement>();

                if (movement != null)
                    return movement.GetEffectiveMovementSpaceRotation();
            }

            return Quaternion.identity;
        }

        private void ApplyMovementSpaceToAllPlayers(Transform movementSpace)
        {
            if (movementSpace == null)
                return;

            HashSet<PlayerMovement> appliedPlayers = new();

            void Apply(PlayerMovement movement)
            {
                if (movement == null || !appliedPlayers.Add(movement))
                    return;

                movement.SetMovementSpace(movementSpace);
            }

            Apply(playerMovement);

            if (playerMovements != null)
            {
                foreach (PlayerMovement movement in playerMovements)
                    Apply(movement);
            }

            CollectGroupTargets();

            foreach (Transform groupTarget in cachedGroupTargets)
            {
                if (groupTarget == null)
                    continue;

                Apply(groupTarget.GetComponentInParent<PlayerMovement>());
            }
        }

        // =========================================================
        // FOLLOW
        // =========================================================

        private void UpdateFollow()
        {
            if (!TryGetTrackingPoint(out _))
                return;

            bool hasBlend = TryGetGroupBlend(out MovementSpace.BlendResult blend);

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

            CollectGroupTargets();

            if (cachedGroupTargets.Count == 0)
                return false;

            Vector3 minimum = cachedGroupTargets[0].position;
            Vector3 maximum = minimum;

            for (int i = 1; i < cachedGroupTargets.Count; i++)
            {
                Vector3 position = cachedGroupTargets[i].position;

                minimum = Vector3.Min(minimum, position);
                maximum = Vector3.Max(maximum, position);
            }

            trackingPoint = (minimum + maximum) * 0.5f;
            return true;
        }

        // =========================================================
        // CAMERA ROTATION
        // =========================================================

        private void InitializeCameraReference()
        {
            if (autoScrollCamera == null || !HasValidPlayerMovement())
                return;

            Quaternion movementSpaceRotation = GetGroupMovementSpaceRotation();

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

            Quaternion movementSpaceRotation = GetGroupMovementSpaceRotation();

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
                GetGroupMovementSpaceRotation() *
                cameraRotationOffsetFromMovementSpace;
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

            CollectGroupTargets();

            float requiredFov = baseAutoScrollFov;

            if (cachedGroupTargets.Count > 0)
            {
                Transform cameraTransform = autoScrollCamera.transform;

                float aspect = outputCamera != null && outputCamera.aspect > 0f
                    ? outputCamera.aspect
                    : 16f / 9f;

                float requiredTangent = 0f;

                foreach (Transform playerTarget in cachedGroupTargets)
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

            combatMode = true;
            combatEnabled = true;

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
            CombatKnot completedCombatKnot =
                currentCombatStep >= 0 &&
                currentCombatStep < combatKnots.Count
                    ? combatKnots[currentCombatStep]
                    : null;

            completedCombatKnot?.DisableCombatObjects();

            if (completedCombatKnot?.NextMovementSpace != null)
            {
                ApplyMovementSpaceToAllPlayers(
                    completedCombatKnot.NextMovementSpace.transform
                );
            }

            combatMode = false;
            currentCombatStep++;

            currentSplinePosition = autoScrollDolly.CameraPosition;
            targetSplinePosition = currentSplinePosition;
            splineVelocity = 0f;
            previousFrameWasBlend = false;
            currentBlendActive = false;

            ClearBlendRailCache();

            autoScrollCamera.Priority.Value = 10;
            combatCamera.Priority.Value = 0;
            autoScrollCamera.Prioritize();

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
                    $"[{nameof(VCameraSetup_Follow)}] Combat terminé : {completedCombatKnot.name}.",
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

            if (!HasValidPlayerMovement() &&
                !HasValidGroupTarget())
            {
                UnityDebug.LogError(
                    $"[{nameof(VCameraSetup_Follow)}] Aucun joueur ou Target valide.",
                    this
                );
                valid = false;
            }

            return valid;
        }

        private bool HasValidGroupTarget()
        {
            if (groupTargets != null)
            {
                foreach (Transform groupTarget in groupTargets)
                {
                    if (groupTarget != null)
                        return true;
                }
            }

            return target != null;
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
