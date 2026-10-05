using System.Collections.Generic;
using SynthOfRage.Scripts.Core;
using SynthOfRage.Scripts.Unit.Player;
using SynthOfRage.Scripts.Unit.Player.Movement.Utils;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

namespace _Dev.Vincent.Camera_Setup.Scripts.VCameraSystem
{
    [DefaultExecutionOrder(100)]
    public class VCameraSetup_Follow : MonoBehaviour
    {
        // =========================================================
        // CAMERAS
        // =========================================================

        [Header("Cameras")]
        [SerializeField] private CinemachineCamera autoScrollCamera;
        [SerializeField] private CinemachineCamera combatCamera;

        // =========================================================
        // RAIL
        // =========================================================

        [Header("Rail")]
        [SerializeField] private CinemachineSplineDolly autoScrollDolly;

        [Tooltip("Optionnel. Si assigné, il sera désactivé pendant le combat car la position de la caméra est alors contrôlée par le preset.")]
        [SerializeField] private CinemachineSplineDolly combatDolly;

        [Tooltip("Nombre d'échantillons utilisés pour projeter les positions World sur le Camera Rail.")]
        [SerializeField] [Range(50, 1000)] private int splineProjectionSamples = 500;

        // =========================================================
        // PLAYER / GROUP
        // =========================================================

        [Header("Player / Group")]

        [Tooltip("Target principal. Utilisé lorsque Group Targets est vide.")]
        [SerializeField] private Transform target;

        [Tooltip("Optionnel. Si cette liste contient des Transforms valides, leur centre remplace Target pour le suivi et le cadrage.")]
        [SerializeField] private List<Transform> groupTargets = new List<Transform>();

        [Tooltip("PlayerMovement est la source de vérité pour le Movement Space courant et son blend.")]
        [SerializeField] private PlayerMovement playerMovement;

        // =========================================================
        // INITIAL MOVEMENT SPACE
        // =========================================================

        [Header("Initial Movement Space")]
        [Tooltip("Movement Space utilisé au début du niveau, avant le premier combat.")]
        [SerializeField] private MovementSpace initialMovementSpace;

        // =========================================================
        // POSITION FOLLOW
        // =========================================================

        [Header("Position Follow")]

        [Tooltip("Lissage normal de la POSITION de la caméra sur le rail hors blend.")]
        [SerializeField] [Min(0f)] private float followSmoothTime = 0.5f;

        [Tooltip("Lissage de POSITION utilisé pendant un blend de Movement Spaces. Il doit rester faible afin que position et rotation restent synchronisées dans les virages.")]
        [SerializeField] [Min(0f)] private float blendFollowSmoothTime = 0.06f;

        [Tooltip("Retard maximal autorisé pendant un blend, en Knot Units. 0 désactive cette limite.")]
        [SerializeField] [Min(0f)] private float maxBlendKnotLag = 0.12f;

        // =========================================================
        // CAMERA FRAMING
        // =========================================================

        [Header("Camera Framing")]

        [Tooltip("Autorise une petite correction horizontale lorsque le groupe approche d'un bord de l'écran. La rotation principale reste imposée par le Movement Space.")]
        [SerializeField] private bool framingCorrectionEnabled = true;

        [Tooltip("Offset World ajouté au centre du groupe pour déterminer le point de cadrage.")]
        [SerializeField] private Vector3 framingTargetOffset = new Vector3(0f, 1f, 0f);

        [Tooltip("Position horizontale idéale dans l'écran. 0.5 = centre.")]
        [SerializeField] [Range(0.1f, 0.9f)] private float desiredViewportX = 0.5f;

        [Tooltip("Demi-largeur de la zone morte. Avec 0.18 et un centre à 0.5, aucune correction entre 0.32 et 0.68.")]
        [SerializeField] [Range(0.02f, 0.45f)] private float horizontalDeadZoneHalfWidth = 0.18f;

        [Tooltip("Correction horizontale maximale autour de la rotation du Movement Space.")]
        [SerializeField] [Range(0f, 20f)] private float maxHorizontalCorrectionAngle = 4f;

        [Tooltip("Lissage UNIQUEMENT de la petite correction de cadrage. Le Movement Space n'est jamais SmoothDampé ici.")]
        [SerializeField] [Min(0f)] private float horizontalCorrectionSmoothTime = 0.18f;

        [Tooltip("Force de la correction de cadrage pendant un blend. 0 = le Movement Space possède entièrement la rotation pendant le virage.")]
        [SerializeField] [Range(0f, 1f)] private float framingMultiplierDuringBlend = 0f;

        [Tooltip("Caméra Unity finale. Si elle est vide, Camera.main est utilisée pour récupérer l'Aspect Ratio.")]
        [SerializeField] private Camera outputCamera;

        // =========================================================
        // COMBAT KNOTS
        // =========================================================

        [Header("Combat Knots")]
        [SerializeField] private bool combatEnabled;
        [SerializeField] private List<CombatKnot> combatKnots = new List<CombatKnot>();
        [SerializeField] private float combatTolerance = 0.01f;

        // =========================================================
        // DEBUG
        // =========================================================

        [Header("Debug")]
        [SerializeField] private bool debugEnabled = true;

        // =========================================================
        // RUNTIME - POSITION
        // =========================================================

        private float currentSplinePosition;
        private float targetSplinePosition;
        private float splineVelocity;
        private SplineContainer splineContainer;
        private bool previousFrameWasBlend;
        private bool currentBlendActive;

        // =========================================================
        // RUNTIME - BLEND RAIL CACHE
        // =========================================================

        private bool blendRailCacheValid;
        private MovementSpace cachedBlendSource;
        private MovementSpace cachedBlendTarget;
        private MovementSpace.Boundary cachedBlendSourceBoundary;
        private MovementSpace.Boundary cachedBlendTargetBoundary;
        private Vector3 cachedBlendWorldStart;
        private Vector3 cachedBlendWorldEnd;
        private float cachedBlendStartSplinePosition;
        private float cachedBlendEndSplinePosition;

        // =========================================================
        // RUNTIME - ROTATION
        // =========================================================

        private Quaternion cameraRotationOffsetFromMovementSpace = Quaternion.identity;
        private float currentHorizontalCorrection;
        private float horizontalCorrectionVelocity;
        private bool cameraReferenceInitialized;

        // =========================================================
        // RUNTIME - GENERAL
        // =========================================================

        private bool combatMode;
        private int currentCombatStep;
        private int debugUpdateCounter;

        // =========================================================
        // INITIALIZATION
        // =========================================================

        private void Start()
        {
            Debug.Log($"[{nameof(VCameraSetup_Follow)}] >>> START EXECUTED", this);

            if (!ValidateReferences())
                return;

            splineContainer = autoScrollDolly.Spline;

            if (splineContainer == null)
            {
                Debug.LogError($"[{nameof(VCameraSetup_Follow)}] Aucun Spline Container n'est assigné au Auto Scroll Dolly.", this);
                return;
            }

            autoScrollDolly.PositionUnits = PathIndexUnit.Knot;

            if (combatDolly != null)
                combatDolly.enabled = false;

            if (outputCamera == null)
                outputCamera = Camera.main;

            currentSplinePosition = autoScrollDolly.CameraPosition;
            targetSplinePosition = currentSplinePosition;
            splineVelocity = 0f;

            autoScrollCamera.gameObject.SetActive(true);
            combatCamera.gameObject.SetActive(true);

            ApplyCameraState();
            ApplyInitialMovementSpace();
            InitializeCameraReference();
            ApplyMovementSpaceCameraRotationImmediate();

            Debug.Log($"[{nameof(VCameraSetup_Follow)}] Initialisation OK | Spline Knots : {splineContainer.Spline.Count} | Position : {currentSplinePosition:F3}", this);

            ValidateCombatKnots();
        }

        private void OnEnable()
        {
            GameManager.Instance.OnArenaExit += DisableCombat;
        }

        private void OnDestroy()
        {
            GameManager.Instance.OnArenaExit -= DisableCombat; // TODO: Singleton instance is destroyed to early and can't be checked
        }

        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            if (debugEnabled)
            {
                debugUpdateCounter++;

                if (debugUpdateCounter >= 30)
                {
                    debugUpdateCounter = 0;
                    Debug.Log($"[{nameof(VCameraSetup_Follow)}] UPDATE | CombatMode = {combatMode} | CombatEnabled = {combatEnabled} | CombatStep = {currentCombatStep} | CameraPosition = {autoScrollDolly.CameraPosition:F3}", this);
                }
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

        // =========================================================
        // LATE UPDATE
        // =========================================================

        private void LateUpdate()
        {
            if (combatMode)
                return;

            UpdateAutoScrollCameraRotation();
        }

        // =========================================================
        // AUTO SCROLL FOLLOW
        // =========================================================

        private void UpdateFollow()
        {
            if (playerMovement == null)
                return;

            bool hasBlend = playerMovement.TryGetActiveMovementSpaceBlend(out MovementSpace.BlendResult blendResult);

            if (previousFrameWasBlend != hasBlend)
                splineVelocity = 0f;

            currentBlendActive = hasBlend;

            if (hasBlend)
                UpdateBlendFollow(blendResult);
            else
                UpdateNormalFollow();

            previousFrameWasBlend = hasBlend;
            autoScrollDolly.CameraPosition = currentSplinePosition;

            if (debugEnabled && debugUpdateCounter == 0)
                Debug.Log($"[{nameof(VCameraSetup_Follow)}] FOLLOW | Mode = {(hasBlend ? "BLEND" : "NORMAL")} | TargetSpline = {targetSplinePosition:F3} | CurrentSpline = {currentSplinePosition:F3} | NextKnot = {GetNextKnotDebugValue()}", this);
        }

        // =========================================================
        // NORMAL FOLLOW
        // =========================================================

        private void UpdateNormalFollow()
        {
            ClearBlendRailCache();

            if (!TryGetTrackingPoint(out Vector3 trackingPoint))
                return;

            targetSplinePosition = GetSplinePosition(trackingPoint);

            if (followSmoothTime <= 0f)
            {
                currentSplinePosition = targetSplinePosition;
                splineVelocity = 0f;
                return;
            }

            currentSplinePosition = Mathf.SmoothDamp(currentSplinePosition, targetSplinePosition, ref splineVelocity, followSmoothTime);
        }

        // =========================================================
        // BLEND FOLLOW
        // =========================================================

        private void UpdateBlendFollow(MovementSpace.BlendResult blendResult)
        {
            RefreshBlendRailCacheIfNeeded(blendResult);

            if (!blendRailCacheValid)
            {
                UpdateNormalFollow();
                return;
            }

            targetSplinePosition = Mathf.Lerp(cachedBlendStartSplinePosition, cachedBlendEndSplinePosition, Mathf.Clamp01(blendResult.T));

            if (blendFollowSmoothTime <= 0f)
            {
                currentSplinePosition = targetSplinePosition;
                splineVelocity = 0f;
            }
            else
            {
                currentSplinePosition = Mathf.SmoothDamp(currentSplinePosition, targetSplinePosition, ref splineVelocity, blendFollowSmoothTime);
            }

            ApplyMaximumBlendKnotLag();
        }

        // =========================================================
        // BLEND RAIL CACHE
        // =========================================================

        private void RefreshBlendRailCacheIfNeeded(MovementSpace.BlendResult blendResult)
        {
            bool identityChanged = !blendRailCacheValid || cachedBlendSource != blendResult.Source || cachedBlendTarget != blendResult.Target || cachedBlendSourceBoundary != blendResult.SourceBoundary || cachedBlendTargetBoundary != blendResult.TargetBoundary;
            bool geometryChanged = !blendRailCacheValid || Vector3.SqrMagnitude(cachedBlendWorldStart - blendResult.WorldStart) > 0.0001f || Vector3.SqrMagnitude(cachedBlendWorldEnd - blendResult.WorldEnd) > 0.0001f;

            if (!identityChanged && !geometryChanged)
                return;

            cachedBlendSource = blendResult.Source;
            cachedBlendTarget = blendResult.Target;
            cachedBlendSourceBoundary = blendResult.SourceBoundary;
            cachedBlendTargetBoundary = blendResult.TargetBoundary;
            cachedBlendWorldStart = blendResult.WorldStart;
            cachedBlendWorldEnd = blendResult.WorldEnd;

            cachedBlendStartSplinePosition = GetSplinePosition(blendResult.WorldStart);
            cachedBlendEndSplinePosition = GetSplinePosition(blendResult.WorldEnd);

            blendRailCacheValid = true;

            if (debugEnabled)
                Debug.Log($"[{nameof(VCameraSetup_Follow)}] Blend Rail Cache | {(cachedBlendSource != null ? cachedBlendSource.name : "NULL")} -> {(cachedBlendTarget != null ? cachedBlendTarget.name : "NULL")} | Rail {cachedBlendStartSplinePosition:F3} -> {cachedBlendEndSplinePosition:F3}", this);
        }

        private void ClearBlendRailCache()
        {
            blendRailCacheValid = false;
            cachedBlendSource = null;
            cachedBlendTarget = null;
        }

        private void ApplyMaximumBlendKnotLag()
        {
            if (maxBlendKnotLag <= 0f)
                return;

            float difference = currentSplinePosition - targetSplinePosition;

            if (Mathf.Abs(difference) <= maxBlendKnotLag)
                return;

            currentSplinePosition = targetSplinePosition + Mathf.Sign(difference) * maxBlendKnotLag;
            splineVelocity = 0f;
        }

        // =========================================================
        // TRACKING POINT
        // =========================================================

        private bool TryGetTrackingPoint(out Vector3 trackingPoint)
        {
            trackingPoint = Vector3.zero;

            if (groupTargets != null && groupTargets.Count > 0)
            {
                int validCount = 0;
                Vector3 minimum = Vector3.zero;
                Vector3 maximum = Vector3.zero;

                for (int i = 0; i < groupTargets.Count; i++)
                {
                    Transform groupTarget = groupTargets[i];

                    if (groupTarget == null)
                        continue;

                    if (validCount == 0)
                    {
                        minimum = groupTarget.position;
                        maximum = groupTarget.position;
                    }
                    else
                    {
                        minimum = Vector3.Min(minimum, groupTarget.position);
                        maximum = Vector3.Max(maximum, groupTarget.position);
                    }

                    validCount++;
                }

                if (validCount > 0)
                {
                    trackingPoint = (minimum + maximum) * 0.5f;
                    return true;
                }
            }

            if (target != null)
            {
                trackingPoint = target.position;
                return true;
            }

            return false;
        }

        // =========================================================
        // CAMERA REFERENCE INITIALIZATION
        // =========================================================

        private void InitializeCameraReference()
        {
            if (autoScrollCamera == null || playerMovement == null)
                return;

            Quaternion movementSpaceRotation = playerMovement.GetEffectiveMovementSpaceRotation();

            cameraRotationOffsetFromMovementSpace = Quaternion.Inverse(movementSpaceRotation) * autoScrollCamera.transform.rotation;

            currentHorizontalCorrection = 0f;
            horizontalCorrectionVelocity = 0f;
            cameraReferenceInitialized = true;

            if (debugEnabled)
                Debug.Log($"[{nameof(VCameraSetup_Follow)}] Camera Reference | Local Rotation = {cameraRotationOffsetFromMovementSpace.eulerAngles}", this);
        }

        // =========================================================
        // AUTO SCROLL CAMERA ROTATION
        // =========================================================

        private void UpdateAutoScrollCameraRotation()
        {
            if (autoScrollCamera == null || playerMovement == null)
                return;

            if (!cameraReferenceInitialized)
                InitializeCameraReference();

            Quaternion movementSpaceRotation = playerMovement.GetEffectiveMovementSpaceRotation();
            Quaternion baseRotation = movementSpaceRotation * cameraRotationOffsetFromMovementSpace;

            float targetHorizontalCorrection = 0f;

            if (framingCorrectionEnabled && TryGetTrackingPoint(out Vector3 trackingPoint))
            {
                Vector3 framingPoint = trackingPoint + framingTargetOffset;

                if (TryGetHorizontalViewportPosition(baseRotation, framingPoint, out float viewportX))
                    targetHorizontalCorrection = GetHorizontalScreenError(viewportX) * maxHorizontalCorrectionAngle;
            }

            if (currentBlendActive)
                targetHorizontalCorrection *= framingMultiplierDuringBlend;

            if (horizontalCorrectionSmoothTime <= 0f)
            {
                currentHorizontalCorrection = targetHorizontalCorrection;
                horizontalCorrectionVelocity = 0f;
            }
            else
            {
                currentHorizontalCorrection = Mathf.SmoothDampAngle(currentHorizontalCorrection, targetHorizontalCorrection, ref horizontalCorrectionVelocity, horizontalCorrectionSmoothTime);
            }

            Vector3 rotationAxis = movementSpaceRotation * Vector3.up;
            Quaternion correctionRotation = Quaternion.AngleAxis(currentHorizontalCorrection, rotationAxis);

            autoScrollCamera.transform.rotation = correctionRotation * baseRotation;
        }

        // =========================================================
        // VIEWPORT PROJECTION
        // =========================================================

        private bool TryGetHorizontalViewportPosition(Quaternion baseRotation, Vector3 worldPoint, out float viewportX)
        {
            viewportX = desiredViewportX;

            Vector3 direction = worldPoint - autoScrollCamera.transform.position;
            Vector3 localDirection = Quaternion.Inverse(baseRotation) * direction;

            if (localDirection.z <= 0.001f)
                return false;

            float aspect = outputCamera != null && outputCamera.aspect > 0f ? outputCamera.aspect : 16f / 9f;
            float verticalFov = Mathf.Clamp(autoScrollCamera.Lens.FieldOfView, 1f, 179f) * Mathf.Deg2Rad;
            float tanHalfHorizontalFov = Mathf.Tan(verticalFov * 0.5f) * aspect;

            if (tanHalfHorizontalFov <= 0.0001f)
                return false;

            float normalizedDeviceX = localDirection.x / (localDirection.z * tanHalfHorizontalFov);

            viewportX = 0.5f + normalizedDeviceX * 0.5f;

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
                float available = Mathf.Max(0.001f, 1f - right);
                return Mathf.Clamp01((viewportX - right) / available);
            }

            float availableLeft = Mathf.Max(0.001f, left);

            return -Mathf.Clamp01((left - viewportX) / availableLeft);
        }

        // =========================================================
        // IMMEDIATE MOVEMENT SPACE CAMERA ROTATION
        // =========================================================

        private void ApplyMovementSpaceCameraRotationImmediate()
        {
            if (autoScrollCamera == null || playerMovement == null)
                return;

            if (!cameraReferenceInitialized)
                InitializeCameraReference();

            currentHorizontalCorrection = 0f;
            horizontalCorrectionVelocity = 0f;

            Quaternion movementSpaceRotation = playerMovement.GetEffectiveMovementSpaceRotation();

            autoScrollCamera.transform.rotation = movementSpaceRotation * cameraRotationOffsetFromMovementSpace;
        }

        // =========================================================
        // COMBAT KNOT DETECTION
        // =========================================================

        private void CheckCombatKnot()
        {
            if (currentCombatStep >= combatKnots.Count)
                return;

            CombatKnot combatKnot = combatKnots[currentCombatStep];

            if (combatKnot == null)
            {
                Debug.LogWarning($"[{nameof(VCameraSetup_Follow)}] Combat Knot {currentCombatStep} est null.", this);

                currentCombatStep++;

                return;
            }

            int targetKnot = combatKnot.KnotIndex;
            float currentPosition = autoScrollDolly.CameraPosition;

            if (debugEnabled)
                Debug.Log($"[{nameof(VCameraSetup_Follow)}] CHECK KNOT | Current = {currentPosition:F3} | Target = {targetKnot} | Tolerance = {combatTolerance:F3} | CombatEnabled = {combatEnabled}", this);

            if (currentPosition >= targetKnot - combatTolerance)
            {
                Debug.Log($"[{nameof(VCameraSetup_Follow)}] >>> KNOT {targetKnot} ATTEINT <<<", this);

                StartCombat(combatKnot);
            }
        }

        // =========================================================
        // START COMBAT
        // =========================================================

        private void StartCombat(CombatKnot combatKnot)
        {
            if (combatKnot == null)
                return;

            combatMode = true;
            combatEnabled = true;

            GameManager.Instance.OnArenaEnter.Invoke();

            int targetKnot = combatKnot.KnotIndex;

            Debug.Log($"[{nameof(VCameraSetup_Follow)}] >>> START COMBAT <<<\nCombat Knot : {combatKnot.name}\nKnot Index : {targetKnot}", this);

            currentSplinePosition = targetKnot;
            targetSplinePosition = targetKnot;
            splineVelocity = 0f;
            previousFrameWasBlend = false;
            currentBlendActive = false;

            ClearBlendRailCache();

            autoScrollDolly.CameraPosition = targetKnot;

            ApplyCombatPreset(combatKnot);

            autoScrollCamera.Priority.Value = 0;
            combatCamera.Priority.Value = 10;

            combatCamera.Prioritize();

            Debug.Log($"[{nameof(VCameraSetup_Follow)}] Combat Camera prioritaire.", this);
        }

        // =========================================================
        // APPLY COMBAT PRESET
        // =========================================================

        private void ApplyCombatPreset(CombatKnot combatKnot)
        {
            if (combatKnot == null)
                return;

            VCameraPreset preset = combatKnot.CameraPreset;

            if (preset == null)
            {
                Debug.LogWarning($"[{nameof(VCameraSetup_Follow)}] Le CombatKnot '{combatKnot.name}' n'a aucun Camera Preset.", combatKnot);
                return;
            }

            Transform combatCenter = combatKnot.CombatCenter;
            Transform movementSpace = combatKnot.MovementSpace;

            if (combatCenter != null)
            {
                Vector3 cameraPosition = combatCenter.TransformPoint(preset.CameraOffset);
                Quaternion cameraRotation = combatCenter.rotation * Quaternion.Euler(preset.CameraRotation);

                combatCamera.ForceCameraPosition(cameraPosition, cameraRotation);

                Debug.Log($"[{nameof(VCameraSetup_Follow)}] Combat Camera appliquée.\nPosition : {cameraPosition}\nRotation : {cameraRotation.eulerAngles}", this);
            }
            else
            {
                Debug.LogWarning($"[{nameof(VCameraSetup_Follow)}] Le CombatKnot '{combatKnot.name}' n'a aucun Combat Center.", combatKnot);
            }

            if (movementSpace != null)
            {
                movementSpace.localRotation = Quaternion.Euler(preset.MovementSpaceRotation);

                Debug.Log($"[{nameof(VCameraSetup_Follow)}] Movement Space Combat : {movementSpace.name}\nLocal Rotation : {movementSpace.localEulerAngles}\nWorld Rotation : {movementSpace.eulerAngles}", this);

                if (playerMovement != null)
                {
                    playerMovement.SetMovementSpace(movementSpace);

                    Debug.Log($"[{nameof(VCameraSetup_Follow)}] Player Movement Space = {movementSpace.name}", this);
                }
                else
                {
                    Debug.LogWarning($"[{nameof(VCameraSetup_Follow)}] PlayerMovement n'est pas assigné.", this);
                }
            }
            else
            {
                Debug.LogWarning($"[{nameof(VCameraSetup_Follow)}] Le CombatKnot '{combatKnot.name}' n'a aucun Movement Space de combat.", combatKnot);
            }

            combatCamera.Lens.FieldOfView = preset.FieldOfView;

            Debug.Log($"[{nameof(VCameraSetup_Follow)}] FOV = {preset.FieldOfView}", this);
        }

        // =========================================================
        // INITIAL MOVEMENT SPACE
        // =========================================================

        private void ApplyInitialMovementSpace()
        {
            if (playerMovement == null)
            {
                Debug.LogWarning($"[{nameof(VCameraSetup_Follow)}] PlayerMovement n'est pas assigné. Impossible d'appliquer le Movement Space initial.", this);
                return;
            }

            if (initialMovementSpace == null)
            {
                Debug.LogWarning($"[{nameof(VCameraSetup_Follow)}] Aucun Initial Movement Space n'est assigné.", this);
                return;
            }

            playerMovement.SetMovementSpace(initialMovementSpace.transform);

            Debug.Log($"[{nameof(VCameraSetup_Follow)}] Initial Movement Space appliqué : {initialMovementSpace.name}", this);
        }

        // =========================================================
        // END COMBAT
        // =========================================================

        private void EndCombat()
        {
            Debug.Log($"[{nameof(VCameraSetup_Follow)}] >>> END COMBAT <<<", this);

            CombatKnot completedCombatKnot = null;

            if (combatKnots != null && currentCombatStep >= 0 && currentCombatStep < combatKnots.Count)
                completedCombatKnot = combatKnots[currentCombatStep];

            if (completedCombatKnot != null)
            {
                MovementSpace nextMovementSpace = completedCombatKnot.NextMovementSpace;

                if (nextMovementSpace != null)
                {
                    if (playerMovement != null)
                    {
                        playerMovement.SetMovementSpace(nextMovementSpace.transform);

                        Debug.Log($"[{nameof(VCameraSetup_Follow)}] Player Movement Space changé après combat : {nextMovementSpace.name}", this);
                    }
                    else
                    {
                        Debug.LogWarning($"[{nameof(VCameraSetup_Follow)}] PlayerMovement n'est pas assigné. Impossible d'appliquer le Next Movement Space.", this);
                    }
                }
                else
                {
                    Debug.LogWarning($"[{nameof(VCameraSetup_Follow)}] Le CombatKnot '{completedCombatKnot.name}' n'a aucun Next Movement Space assigné.", completedCombatKnot);
                }
            }
            else
            {
                Debug.LogWarning($"[{nameof(VCameraSetup_Follow)}] Impossible de retrouver le CombatKnot terminé. CombatStep = {currentCombatStep}", this);
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

            Debug.Log($"[{nameof(VCameraSetup_Follow)}] Retour Auto Scroll | Next Combat Step = {currentCombatStep}", this);
        }

        // =========================================================
        // SPLINE POSITION
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

                Vector3 splinePosition = splineContainer.transform.TransformPoint(splineContainer.Spline.EvaluatePosition(t));

                Vector2 difference = new Vector2(worldPosition.x - splinePosition.x, worldPosition.z - splinePosition.z);

                float distance = difference.sqrMagnitude;

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestT = t;
                }
            }

            return SplineUtility.ConvertIndexUnit(splineContainer.Spline, closestT, PathIndexUnit.Normalized, PathIndexUnit.Knot);
        }

        // =========================================================
        // VALIDATION
        // =========================================================

        private bool ValidateReferences()
        {
            bool valid = true;

            if (autoScrollCamera == null)
            {
                Debug.LogError($"[{nameof(VCameraSetup_Follow)}] Auto Scroll Camera non assignée.", this);
                valid = false;
            }

            if (combatCamera == null)
            {
                Debug.LogError($"[{nameof(VCameraSetup_Follow)}] Combat Camera non assignée.", this);
                valid = false;
            }

            if (autoScrollDolly == null)
            {
                Debug.LogError($"[{nameof(VCameraSetup_Follow)}] Auto Scroll Dolly non assigné.", this);
                valid = false;
            }

            if (target == null && !HasValidGroupTarget())
            {
                Debug.LogError($"[{nameof(VCameraSetup_Follow)}] Aucun Target valide n'est assigné.", this);
                valid = false;
            }

            if (playerMovement == null)
            {
                Debug.LogError($"[{nameof(VCameraSetup_Follow)}] PlayerMovement n'est pas assigné.", this);
                valid = false;
            }

            return valid;
        }

        private bool HasValidGroupTarget()
        {
            if (groupTargets == null)
                return false;

            for (int i = 0; i < groupTargets.Count; i++)
            {
                if (groupTargets[i] != null)
                    return true;
            }

            return false;
        }

        // =========================================================
        // COMBAT KNOT VALIDATION
        // =========================================================

        private void ValidateCombatKnots()
        {
            if (combatKnots == null || combatKnots.Count == 0)
            {
                Debug.LogWarning($"[{nameof(VCameraSetup_Follow)}] Aucun Combat Knot n'est configuré.", this);
                return;
            }

            for (int i = 0; i < combatKnots.Count; i++)
            {
                CombatKnot knot = combatKnots[i];

                if (knot == null)
                {
                    Debug.LogWarning($"[{nameof(VCameraSetup_Follow)}] Combat Knot Element {i} est null.", this);
                    continue;
                }

                if (knot.KnotIndex < 0 || knot.KnotIndex >= splineContainer.Spline.Count)
                    Debug.LogError($"[{nameof(VCameraSetup_Follow)}] Combat Knot '{knot.name}' utilise Knot Index {knot.KnotIndex}, mais la spline contient {splineContainer.Spline.Count} knots.", knot);

                if (knot.NextMovementSpace == null)
                    Debug.LogWarning($"[{nameof(VCameraSetup_Follow)}] Combat Knot '{knot.name}' n'a aucun Next Movement Space.", knot);

                Debug.Log($"[{nameof(VCameraSetup_Follow)}] Combat Knot [{i}] : {knot.name} | Knot Index = {knot.KnotIndex} | Next Movement Space = {(knot.NextMovementSpace != null ? knot.NextMovementSpace.name : "NONE")}", this);
            }
        }

        // =========================================================
        // DEBUG
        // =========================================================

        private string GetNextKnotDebugValue()
        {
            if (currentCombatStep >= combatKnots.Count)
                return "NONE";

            CombatKnot knot = combatKnots[currentCombatStep];

            if (knot == null)
                return "NULL";

            return knot.KnotIndex.ToString();
        }

        // =========================================================
        // CAMERA STATE
        // =========================================================

        private void ApplyCameraState()
        {
            if (combatMode)
            {
                autoScrollCamera.Priority.Value = 0;
                combatCamera.Priority.Value = 10;

                combatCamera.Prioritize();
            }
            else
            {
                autoScrollCamera.Priority.Value = 10;
                combatCamera.Priority.Value = 0;

                autoScrollCamera.Prioritize();
            }
        }

        private void DisableCombat()
        {
            combatEnabled = false;
        }
    }
}
