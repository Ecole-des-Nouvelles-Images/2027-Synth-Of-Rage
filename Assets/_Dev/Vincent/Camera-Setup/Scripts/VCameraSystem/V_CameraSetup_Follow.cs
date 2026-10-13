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

        private bool combatMode;
        private int currentCombatStep;
        private int debugUpdateCounter;

        private Coroutine pendingCombatShake;

        private void Start()
        {
            if (!ValidateReferences()) return;

            splineContainer = autoScrollDolly.Spline;

            if (splineContainer == null)
            {
                UnityDebug.LogError($"[{nameof(VCameraSetup_Follow)}] Aucun Spline Container assigné.", this);
                return;
            }

            autoScrollDolly.PositionUnits = PathIndexUnit.Knot;

            if (combatDolly != null)
                combatDolly.enabled = false;

            outputCamera ??= Camera.main;

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
            GameManager.Instance.OnArenaExit += DisableCombat;
        }

        private void OnDestroy()
        {
            GameManager.Instance.OnArenaExit -= DisableCombat;
        }

        private void Update()
        {
            if (debugEnabled && ++debugUpdateCounter >= 30)
            {
                debugUpdateCounter = 0;
                UnityDebug.Log($"[{nameof(VCameraSetup_Follow)}] Camera={currentSplinePosition:F3} Target={targetSplinePosition:F3} Blend={currentBlendActive}", this);
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
            if (!combatMode)
                UpdateAutoScrollCameraRotation();
        }

        private void UpdateFollow()
        {
            if (playerMovement == null) return;

            bool hasBlend = playerMovement.TryGetActiveMovementSpaceBlend(out MovementSpace.BlendResult blend);

            if (hasBlend && !previousFrameWasBlend)
                BeginBlendRail(blend);

            currentBlendActive = hasBlend;

            if (hasBlend) UpdateBlendFollow(blend);
            else UpdateNormalFollow();

            if (!hasBlend && previousFrameWasBlend)
                ClearBlendRailCache();

            previousFrameWasBlend = hasBlend;
            autoScrollDolly.CameraPosition = currentSplinePosition;
        }

        private void UpdateNormalFollow()
        {
            if (!TryGetTrackingPoint(out Vector3 trackingPoint)) return;

            targetSplinePosition = GetSplinePosition(trackingPoint);

            if (followSmoothTime <= 0f)
            {
                currentSplinePosition = targetSplinePosition;
                splineVelocity = 0f;
                return;
            }

            currentSplinePosition = Mathf.SmoothDamp(currentSplinePosition, targetSplinePosition, ref splineVelocity, followSmoothTime);
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
                UnityDebug.Log($"[{nameof(VCameraSetup_Follow)}] Blend Rail | Start={cachedBlendStartSplinePosition:F3} End={cachedBlendEndSplinePosition:F3} T={blend.T:F3}", this);
        }

        private void UpdateBlendFollow(MovementSpace.BlendResult blend)
        {
            RefreshBlendRailCacheIfNeeded(blend);

            if (!blendRailCacheValid)
            {
                UpdateNormalFollow();
                return;
            }

            float desiredSplinePosition = Mathf.Lerp(cachedBlendStartSplinePosition, cachedBlendEndSplinePosition, Mathf.Clamp01(blend.T));

            targetSplinePosition = blendTargetMaxSpeed <= 0f
                ? desiredSplinePosition
                : Mathf.MoveTowards(targetSplinePosition, desiredSplinePosition, blendTargetMaxSpeed * Time.deltaTime);

            if (blendFollowSmoothTime <= 0f)
            {
                currentSplinePosition = targetSplinePosition;
                splineVelocity = 0f;
                return;
            }

            currentSplinePosition = Mathf.SmoothDamp(currentSplinePosition, targetSplinePosition, ref splineVelocity, blendFollowSmoothTime);
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

            int count = 0;
            Vector3 minimum = Vector3.zero;
            Vector3 maximum = Vector3.zero;

            if (groupTargets != null)
            {
                for (int i = 0; i < groupTargets.Count; i++)
                {
                    Transform groupTarget = groupTargets[i];
                    if (groupTarget == null) continue;

                    if (count == 0)
                    {
                        minimum = groupTarget.position;
                        maximum = groupTarget.position;
                    }
                    else
                    {
                        minimum = Vector3.Min(minimum, groupTarget.position);
                        maximum = Vector3.Max(maximum, groupTarget.position);
                    }

                    count++;
                }
            }

            if (count > 0)
            {
                trackingPoint = (minimum + maximum) * 0.5f;
                return true;
            }

            if (target == null) return false;

            trackingPoint = target.position;
            return true;
        }

        private void InitializeCameraReference()
        {
            if (autoScrollCamera == null || playerMovement == null) return;

            Quaternion movementSpaceRotation = playerMovement.GetEffectiveMovementSpaceRotation();

            cameraRotationOffsetFromMovementSpace =
                Quaternion.Inverse(movementSpaceRotation) *
                autoScrollCamera.transform.rotation;

            currentHorizontalCorrection = 0f;
            horizontalCorrectionVelocity = 0f;
            cameraReferenceInitialized = true;
        }

        private void UpdateAutoScrollCameraRotation()
        {
            if (autoScrollCamera == null || playerMovement == null) return;
            if (!cameraReferenceInitialized) InitializeCameraReference();

            Quaternion movementSpaceRotation = playerMovement.GetEffectiveMovementSpaceRotation();
            Quaternion baseRotation = movementSpaceRotation * cameraRotationOffsetFromMovementSpace;

            float targetCorrection = 0f;

            if (framingCorrectionEnabled && TryGetTrackingPoint(out Vector3 trackingPoint))
            {
                Vector3 framingPoint = trackingPoint + framingTargetOffset;

                if (TryGetHorizontalViewportPosition(baseRotation, framingPoint, out float viewportX))
                    targetCorrection = GetHorizontalScreenError(viewportX) * maxHorizontalCorrectionAngle;
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

        private bool TryGetHorizontalViewportPosition(Quaternion baseRotation, Vector3 worldPoint, out float viewportX)
        {
            viewportX = desiredViewportX;

            Vector3 localDirection =
                Quaternion.Inverse(baseRotation) *
                (worldPoint - autoScrollCamera.transform.position);

            if (localDirection.z <= 0.001f) return false;

            float aspect = outputCamera != null && outputCamera.aspect > 0f
                ? outputCamera.aspect
                : 16f / 9f;

            float verticalFov =
                Mathf.Clamp(autoScrollCamera.Lens.FieldOfView, 1f, 179f) *
                Mathf.Deg2Rad;

            float tanHalfHorizontalFov =
                Mathf.Tan(verticalFov * 0.5f) *
                aspect;

            if (tanHalfHorizontalFov <= 0.0001f) return false;

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
                return Mathf.Clamp01((viewportX - right) / Mathf.Max(0.001f, 1f - right));

            return -Mathf.Clamp01((left - viewportX) / Mathf.Max(0.001f, left));
        }

        private void ApplyMovementSpaceCameraRotationImmediate()
        {
            if (autoScrollCamera == null || playerMovement == null) return;
            if (!cameraReferenceInitialized) InitializeCameraReference();

            currentHorizontalCorrection = 0f;
            horizontalCorrectionVelocity = 0f;

            autoScrollCamera.transform.rotation =
                playerMovement.GetEffectiveMovementSpaceRotation() *
                cameraRotationOffsetFromMovementSpace;
        }

        private float GetSplinePosition(Vector3 worldPosition)
        {
            if (splineContainer == null) return 0f;

            int samples = Mathf.Max(50, splineProjectionSamples);
            float closestT = 0f;
            float closestDistance = float.MaxValue;

            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector3 splinePosition = splineContainer.transform.TransformPoint(splineContainer.Spline.EvaluatePosition(t));
                Vector2 difference = new(worldPosition.x - splinePosition.x, worldPosition.z - splinePosition.z);
                float distance = difference.sqrMagnitude;

                if (distance >= closestDistance) continue;

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

        private void CheckCombatKnot()
        {
            if (currentCombatStep >= combatKnots.Count) return;

            CombatKnot combatKnot = combatKnots[currentCombatStep];

            if (combatKnot == null)
            {
                currentCombatStep++;
                return;
            }

            if (autoScrollDolly.CameraPosition >= combatKnot.KnotIndex - combatTolerance)
                StartCombat(combatKnot);
        }

        private void StartCombat(CombatKnot combatKnot)
        {
            if (combatKnot == null) return;

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

            QueueCombatShake(combatKnot.StartShakePreset, combatKnot.StartShakeDelay);

            combatKnot.EnableCombatObjects();

            if (debugEnabled)
                UnityDebug.Log($"[{nameof(VCameraSetup_Follow)}] Combat commencé : {combatKnot.name}.", combatKnot);
        }

        private void ApplyCombatPreset(CombatKnot combatKnot)
        {
            VCameraPreset preset = combatKnot.CameraPreset;
            if (preset == null) return;

            if (combatKnot.CombatCenter != null)
            {
                Vector3 cameraPosition = combatKnot.CombatCenter.TransformPoint(preset.CameraOffset);
                Quaternion cameraRotation = combatKnot.CombatCenter.rotation * Quaternion.Euler(preset.CameraRotation);

                combatCamera.ForceCameraPosition(cameraPosition, cameraRotation);
            }

            if (combatKnot.MovementSpace != null)
            {
                combatKnot.MovementSpace.localRotation = Quaternion.Euler(preset.MovementSpaceRotation);

                if (playerMovement != null)
                    playerMovement.SetMovementSpace(combatKnot.MovementSpace);
            }

            combatCamera.Lens.FieldOfView = preset.FieldOfView;
        }

        private void ApplyInitialMovementSpace()
        {
            if (playerMovement != null && initialMovementSpace != null)
                playerMovement.SetMovementSpace(initialMovementSpace.transform);
        }

        private void EndCombat()
        {
            CombatKnot completedCombatKnot =
                currentCombatStep >= 0 && currentCombatStep < combatKnots.Count
                    ? combatKnots[currentCombatStep]
                    : null;

            completedCombatKnot?.DisableCombatObjects();

            if (completedCombatKnot?.NextMovementSpace != null && playerMovement != null)
                playerMovement.SetMovementSpace(completedCombatKnot.NextMovementSpace.transform);

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
                QueueCombatShake(completedCombatKnot.EndShakePreset, completedCombatKnot.EndShakeDelay);

            if (debugEnabled && completedCombatKnot != null)
                UnityDebug.Log($"[{nameof(VCameraSetup_Follow)}] Combat terminé : {completedCombatKnot.name}.", completedCombatKnot);
        }

        private void QueueCombatShake(CameraShakePreset preset, float delay)
        {
            if (preset == null) return;

            if (pendingCombatShake != null)
                StopCoroutine(pendingCombatShake);

            pendingCombatShake = StartCoroutine(PlayCombatShake(preset, Mathf.Max(0f, delay)));
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
                UnityDebug.LogError($"[{nameof(VCameraSetup_Follow)}] Impossible de jouer '{preset.name}' : aucun CameraShakeManager actif.", this);
                yield break;
            }

            CameraShakeManager.PlayShake(preset);

            if (debugEnabled)
                UnityDebug.Log($"[{nameof(VCameraSetup_Follow)}] Combat Shake joué : {preset.name} | Delay={delay:F2}s", this);
        }

        private bool ValidateReferences()
        {
            bool valid = true;

            if (autoScrollCamera == null) { UnityDebug.LogError($"[{nameof(VCameraSetup_Follow)}] Auto Scroll Camera non assignée.", this); valid = false; }
            if (combatCamera == null) { UnityDebug.LogError($"[{nameof(VCameraSetup_Follow)}] Combat Camera non assignée.", this); valid = false; }
            if (autoScrollDolly == null) { UnityDebug.LogError($"[{nameof(VCameraSetup_Follow)}] Auto Scroll Dolly non assigné.", this); valid = false; }
            if (target == null && !HasValidGroupTarget()) { UnityDebug.LogError($"[{nameof(VCameraSetup_Follow)}] Aucun Target valide.", this); valid = false; }
            if (playerMovement == null) { UnityDebug.LogError($"[{nameof(VCameraSetup_Follow)}] PlayerMovement non assigné.", this); valid = false; }

            return valid;
        }

        private bool HasValidGroupTarget()
        {
            if (groupTargets == null) return false;

            for (int i = 0; i < groupTargets.Count; i++)
                if (groupTargets[i] != null)
                    return true;

            return false;
        }

        private void ValidateCombatKnots()
        {
            if (combatKnots == null) return;

            for (int i = 0; i < combatKnots.Count; i++)
            {
                CombatKnot knot = combatKnots[i];

                if (knot != null && (knot.KnotIndex < 0 || knot.KnotIndex >= splineContainer.Spline.Count))
                    UnityDebug.LogError($"[{nameof(VCameraSetup_Follow)}] Combat Knot '{knot.name}' : Knot Index {knot.KnotIndex} invalide.", knot);
            }
        }

        private void ApplyCameraState()
        {
            autoScrollCamera.Priority.Value = combatMode ? 0 : 10;
            combatCamera.Priority.Value = combatMode ? 10 : 0;

            if (combatMode) combatCamera.Prioritize();
            else autoScrollCamera.Prioritize();
        }

        private void DisableCombat() => combatEnabled = false;
    }
}