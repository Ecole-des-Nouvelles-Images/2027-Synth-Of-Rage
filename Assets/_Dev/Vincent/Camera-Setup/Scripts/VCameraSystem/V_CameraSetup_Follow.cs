using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Splines;
using SynthOfRage.Scripts.Player;
using _Dev.Vincent.Camera_Setup.Scripts.VCameraSystem;
using SynthOfRage.Scripts.Core;

namespace _Dev.Vincent.Camera_Setup.Scripts
{
    public class VCameraSetup_Follow : MonoBehaviour
    {
        // =========================================================
        // CAMERAS
        // =========================================================

        [Header("Cameras")]

        [SerializeField]
        private CinemachineCamera autoScrollCamera;

        [SerializeField]
        private CinemachineCamera combatCamera;


        // =========================================================
        // RAIL
        // =========================================================

        [Header("Rail")]

        [SerializeField]
        private CinemachineSplineDolly autoScrollDolly;

        [Tooltip(
            "Optionnel. Si assigné, il sera désactivé pendant le combat " +
            "car la position de la caméra est alors contrôlée par le preset."
        )]
        [SerializeField]
        private CinemachineSplineDolly combatDolly;


        // =========================================================
        // PLAYER
        // =========================================================

        [Header("Player")]

        [Tooltip(
            "PlayerMovement utilisé pour changer le Movement Space du joueur."
        )]
        [SerializeField]
        private PlayerMovement playerMovement;


        // =========================================================
        // INITIAL MOVEMENT SPACE
        // =========================================================

        [Header("Initial Movement Space")]

        [Tooltip(
            "Movement Space utilisé au début du niveau, avant le premier combat."
        )]
        [SerializeField]
        private MovementSpace initialMovementSpace;


        // =========================================================
        // PLAYER FOLLOW
        // =========================================================

        [Header("Player Follow")]

        [SerializeField]
        private Transform target;

        [SerializeField]
        private float followSmoothTime = 0.15f;


        // =========================================================
        // MOVEMENT SPACE CAMERA ROTATION
        // =========================================================

        [Header("Movement Space Camera Rotation")]

        [Tooltip(
            "Permet à la caméra Auto Scroll de suivre progressivement " +
            "la rotation Y du Movement Space actif."
        )]
        [SerializeField]
        private bool followMovementSpaceRotation = true;

        [Tooltip(
            "Temps de lissage de la rotation Y de la caméra."
        )]
        [SerializeField]
        private float movementSpaceCameraRotationSmoothTime = 0.15f;

        private float movementSpaceCameraRotationVelocity;


        // =========================================================
        // COMBAT KNOTS
        // =========================================================

        [Header("Combat Knots")]

        [SerializeField]
        private bool combatEnabled;

        [SerializeField]
        private List<CombatKnot> combatKnots =
            new List<CombatKnot>();

        [SerializeField]
        private float combatTolerance = 0.01f;


        // =========================================================
        // DEBUG
        // =========================================================

        [Header("Debug")]

        [SerializeField]
        private bool debugEnabled = true;


        // =========================================================
        // RUNTIME
        // =========================================================

        private bool combatMode;

        private int currentCombatStep;

        private float currentSplinePosition;

        private float targetSplinePosition;

        private float splineVelocity;

        private SplineContainer splineContainer;

        private int debugUpdateCounter;


        // =========================================================
        // INITIALIZATION
        // =========================================================

        private void Start()
        {
            Debug.Log(
                $"[{nameof(VCameraSetup_Follow)}] >>> START EXECUTED",
                this
            );

            if (!ValidateReferences())
                return;

            splineContainer =
                autoScrollDolly.Spline;

            if (splineContainer == null)
            {
                Debug.LogError(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    "Aucun Spline Container n'est assigné au Auto Scroll Dolly.",
                    this
                );

                return;
            }

            autoScrollDolly.PositionUnits =
                PathIndexUnit.Knot;

            if (combatDolly != null)
                combatDolly.enabled = false;

            currentSplinePosition =
                autoScrollDolly.CameraPosition;

            targetSplinePosition =
                currentSplinePosition;

            autoScrollCamera.gameObject.SetActive(true);
            combatCamera.gameObject.SetActive(true);

            ApplyCameraState();

            // -----------------------------------------------------
            // INITIAL MOVEMENT SPACE
            // -----------------------------------------------------

            ApplyInitialMovementSpace();

            // -----------------------------------------------------
            // INITIAL CAMERA ROTATION
            // -----------------------------------------------------

            ApplyMovementSpaceCameraRotationImmediate();

            Debug.Log(
                $"[{nameof(VCameraSetup_Follow)}] " +
                $"Initialisation OK | " +
                $"Spline Knots : {splineContainer.Spline.Count} | " +
                $"Position : {currentSplinePosition}",
                this
            );

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

                    Debug.Log(
                        $"[{nameof(VCameraSetup_Follow)}] " +
                        $"UPDATE | " +
                        $"CombatMode = {combatMode} | " +
                        $"CombatEnabled = {combatEnabled} | " +
                        $"CombatStep = {currentCombatStep} | " +
                        $"CameraPosition = " +
                        $"{autoScrollDolly.CameraPosition:F3}",
                        this
                    );
                }
            }


            // -----------------------------------------------------
            // COMBAT
            // -----------------------------------------------------

            if (combatMode)
            {
                if (!combatEnabled)
                    EndCombat();

                return;
            }


            // -----------------------------------------------------
            // AUTO SCROLL
            // -----------------------------------------------------

            UpdateFollow();


            // -----------------------------------------------------
            // COMBAT KNOT DETECTION
            // -----------------------------------------------------

            if (currentCombatStep < combatKnots.Count)
                CheckCombatKnot();
        }


        // =========================================================
        // LATE UPDATE
        // =========================================================
        //
        // La rotation est appliquée en LateUpdate afin de laisser
        // Cinemachine effectuer ses calculs de caméra avant notre
        // synchronisation finale.
        //
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
            if (target == null)
                return;

            targetSplinePosition =
                GetSplinePosition(
                    target.position
                );

            currentSplinePosition =
                Mathf.SmoothDamp(
                    currentSplinePosition,
                    targetSplinePosition,
                    ref splineVelocity,
                    followSmoothTime
                );

            autoScrollDolly.CameraPosition =
                currentSplinePosition;

            if (
                debugEnabled &&
                debugUpdateCounter == 0
            )
            {
                Debug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"FOLLOW | " +
                    $"TargetSpline = " +
                    $"{targetSplinePosition:F3} | " +
                    $"CurrentSpline = " +
                    $"{currentSplinePosition:F3} | " +
                    $"NextKnot = " +
                    GetNextKnotDebugValue(),
                    this
                );
            }
        }


        // =========================================================
        // MOVEMENT SPACE CAMERA ROTATION
        // =========================================================

        private void UpdateAutoScrollCameraRotation()
        {
            if (!followMovementSpaceRotation)
                return;

            if (autoScrollCamera == null)
                return;

            if (playerMovement == null)
                return;

            Transform currentMovementSpace =
                playerMovement.GetMovementSpace();

            if (currentMovementSpace == null)
                return;

            Transform cameraTransform =
                autoScrollCamera.transform;

            float targetY =
                currentMovementSpace.eulerAngles.y;

            float currentY =
                cameraTransform.eulerAngles.y;

            float smoothedY =
                Mathf.SmoothDampAngle(
                    currentY,
                    targetY,
                    ref movementSpaceCameraRotationVelocity,
                    movementSpaceCameraRotationSmoothTime
                );

            Vector3 currentEuler =
                cameraTransform.eulerAngles;

            cameraTransform.rotation =
                Quaternion.Euler(
                    currentEuler.x,
                    smoothedY,
                    currentEuler.z
                );
        }


        // =========================================================
        // IMMEDIATE MOVEMENT SPACE CAMERA ROTATION
        // =========================================================

        private void ApplyMovementSpaceCameraRotationImmediate()
        {
            if (!followMovementSpaceRotation)
                return;

            if (autoScrollCamera == null)
                return;

            if (playerMovement == null)
                return;

            Transform currentMovementSpace =
                playerMovement.GetMovementSpace();

            if (currentMovementSpace == null)
                return;

            float targetY =
                currentMovementSpace.eulerAngles.y;

            Vector3 currentEuler =
                autoScrollCamera.transform.eulerAngles;

            autoScrollCamera.transform.rotation =
                Quaternion.Euler(
                    currentEuler.x,
                    targetY,
                    currentEuler.z
                );

            movementSpaceCameraRotationVelocity =
                0f;
        }


        // =========================================================
        // COMBAT KNOT DETECTION
        // =========================================================

        private void CheckCombatKnot()
        {
            if (currentCombatStep >= combatKnots.Count)
                return;

            CombatKnot combatKnot =
                combatKnots[currentCombatStep];

            if (combatKnot == null)
            {
                Debug.LogWarning(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"Combat Knot {currentCombatStep} est null.",
                    this
                );

                currentCombatStep++;

                return;
            }

            int targetKnot =
                combatKnot.KnotIndex;

            float currentPosition =
                autoScrollDolly.CameraPosition;

            if (debugEnabled)
            {
                Debug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"CHECK KNOT | " +
                    $"Current = {currentPosition:F3} | " +
                    $"Target = {targetKnot} | " +
                    $"Tolerance = {combatTolerance:F3} | " +
                    $"CombatEnabled = {combatEnabled}",
                    this
                );
            }

            if (
                currentPosition >=
                targetKnot - combatTolerance
            )
            {
                Debug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $">>> KNOT {targetKnot} ATTEINT <<<",
                    this
                );

                StartCombat(
                    combatKnot
                );
            }
        }


        // =========================================================
        // START COMBAT
        // =========================================================

        private void StartCombat(
            CombatKnot combatKnot
        )
        {
            if (combatKnot == null)
                return;

            combatMode = true;
            combatEnabled = true;
            GameManager.Instance.OnArenaEnter.Invoke();

            int targetKnot =
                combatKnot.KnotIndex;

            Debug.Log(
                $"[{nameof(VCameraSetup_Follow)}] " +
                $">>> START COMBAT <<<\n" +
                $"Combat Knot : {combatKnot.name}\n" +
                $"Knot Index : {targetKnot}",
                this
            );

            currentSplinePosition =
                targetKnot;

            targetSplinePosition =
                targetKnot;

            splineVelocity = 0f;

            autoScrollDolly.CameraPosition =
                targetKnot;

            ApplyCombatPreset(
                combatKnot
            );

            autoScrollCamera.Priority.Value = 0;
            combatCamera.Priority.Value = 10;

            combatCamera.Prioritize();

            Debug.Log(
                $"[{nameof(VCameraSetup_Follow)}] " +
                "Combat Camera prioritaire.",
                this
            );
        }


        // =========================================================
        // APPLY COMBAT PRESET
        // =========================================================

        private void ApplyCombatPreset(
            CombatKnot combatKnot
        )
        {
            if (combatKnot == null)
                return;

            VCameraPreset preset =
                combatKnot.CameraPreset;

            if (preset == null)
            {
                Debug.LogWarning(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"Le CombatKnot '{combatKnot.name}' " +
                    "n'a aucun Camera Preset.",
                    combatKnot
                );

                return;
            }

            Transform combatCenter =
                combatKnot.CombatCenter;

            Transform movementSpace =
                combatKnot.MovementSpace;


            // -----------------------------------------------------
            // CAMERA
            // -----------------------------------------------------

            if (combatCenter != null)
            {
                Vector3 cameraPosition =
                    combatCenter.TransformPoint(
                        preset.CameraOffset
                    );

                Quaternion cameraRotation =
                    combatCenter.rotation *
                    Quaternion.Euler(
                        preset.CameraRotation
                    );

                combatCamera.ForceCameraPosition(
                    cameraPosition,
                    cameraRotation
                );

                Debug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"Combat Camera appliquée.\n" +
                    $"Position : {cameraPosition}\n" +
                    $"Rotation : {cameraRotation.eulerAngles}",
                    this
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"Le CombatKnot '{combatKnot.name}' " +
                    "n'a aucun Combat Center.",
                    combatKnot
                );
            }


            // -----------------------------------------------------
            // COMBAT MOVEMENT SPACE
            // -----------------------------------------------------

            if (movementSpace != null)
            {
                movementSpace.localRotation =
                    Quaternion.Euler(
                        preset.MovementSpaceRotation
                    );

                Debug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"Movement Space Combat : " +
                    $"{movementSpace.name}\n" +
                    $"Local Rotation : " +
                    $"{movementSpace.localEulerAngles}\n" +
                    $"World Rotation : " +
                    $"{movementSpace.eulerAngles}",
                    this
                );

                if (playerMovement != null)
                {
                    playerMovement.SetMovementSpace(
                        movementSpace
                    );

                    Debug.Log(
                        $"[{nameof(VCameraSetup_Follow)}] " +
                        $"Player Movement Space = " +
                        $"{movementSpace.name}",
                        this
                    );
                }
                else
                {
                    Debug.LogWarning(
                        $"[{nameof(VCameraSetup_Follow)}] " +
                        "PlayerMovement n'est pas assigné.",
                        this
                    );
                }
            }
            else
            {
                Debug.LogWarning(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"Le CombatKnot '{combatKnot.name}' " +
                    "n'a aucun Movement Space de combat.",
                    combatKnot
                );
            }


            // -----------------------------------------------------
            // FOV
            // -----------------------------------------------------

            combatCamera.Lens.FieldOfView =
                preset.FieldOfView;

            Debug.Log(
                $"[{nameof(VCameraSetup_Follow)}] " +
                $"FOV = {preset.FieldOfView}",
                this
            );
        }


        // =========================================================
        // INITIAL MOVEMENT SPACE
        // =========================================================

        private void ApplyInitialMovementSpace()
        {
            if (playerMovement == null)
            {
                Debug.LogWarning(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    "PlayerMovement n'est pas assigné. " +
                    "Impossible d'appliquer le Movement Space initial.",
                    this
                );

                return;
            }

            if (initialMovementSpace == null)
            {
                Debug.LogWarning(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    "Aucun Initial Movement Space n'est assigné.",
                    this
                );

                return;
            }

            playerMovement.SetMovementSpace(
                initialMovementSpace.transform
            );

            Debug.Log(
                $"[{nameof(VCameraSetup_Follow)}] " +
                $"Initial Movement Space appliqué : " +
                $"{initialMovementSpace.name}",
                this
            );
        }


        // =========================================================
        // END COMBAT
        // =========================================================

        private void EndCombat()
        {
            Debug.Log(
                $"[{nameof(VCameraSetup_Follow)}] " +
                ">>> END COMBAT <<<",
                this
            );


            // -----------------------------------------------------
            // RECUPERATION DU COMBAT TERMINE
            // -----------------------------------------------------

            CombatKnot completedCombatKnot = null;

            if (
                combatKnots != null &&
                currentCombatStep >= 0 &&
                currentCombatStep < combatKnots.Count
            )
            {
                completedCombatKnot =
                    combatKnots[currentCombatStep];
            }


            // -----------------------------------------------------
            // NEXT MOVEMENT SPACE
            // -----------------------------------------------------

            if (completedCombatKnot != null)
            {
                MovementSpace nextMovementSpace =
                    completedCombatKnot.NextMovementSpace;

                if (nextMovementSpace != null)
                {
                    if (playerMovement != null)
                    {
                        playerMovement.SetMovementSpace(
                            nextMovementSpace.transform
                        );

                        Debug.Log(
                            $"[{nameof(VCameraSetup_Follow)}] " +
                            $"Player Movement Space changé après combat : " +
                            $"{nextMovementSpace.name}",
                            this
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[{nameof(VCameraSetup_Follow)}] " +
                            "PlayerMovement n'est pas assigné. " +
                            "Impossible d'appliquer le Next Movement Space.",
                            this
                        );
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"[{nameof(VCameraSetup_Follow)}] " +
                        $"Le CombatKnot '{completedCombatKnot.name}' " +
                        "n'a aucun Next Movement Space assigné.",
                        completedCombatKnot
                    );
                }
            }
            else
            {
                Debug.LogWarning(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"Impossible de retrouver le CombatKnot " +
                    $"terminé. CombatStep = {currentCombatStep}",
                    this
                );
            }


            // -----------------------------------------------------
            // SORTIE DU MODE COMBAT
            // -----------------------------------------------------

            combatMode = false;


            // -----------------------------------------------------
            // NEXT COMBAT
            // -----------------------------------------------------

            currentCombatStep++;


            // -----------------------------------------------------
            // AUTO SCROLL STATE
            // -----------------------------------------------------

            currentSplinePosition =
                autoScrollDolly.CameraPosition;

            targetSplinePosition =
                currentSplinePosition;

            splineVelocity = 0f;


            // -----------------------------------------------------
            // RETURN TO AUTO SCROLL
            // -----------------------------------------------------

            autoScrollCamera.Priority.Value = 10;
            combatCamera.Priority.Value = 0;

            autoScrollCamera.Prioritize();

            // -----------------------------------------------------
            // SYNCHRONISATION CAMERA / MOVEMENT SPACE
            // -----------------------------------------------------

            ApplyMovementSpaceCameraRotationImmediate();

            Debug.Log(
                $"[{nameof(VCameraSetup_Follow)}] " +
                $"Retour Auto Scroll | " +
                $"Next Combat Step = " +
                $"{currentCombatStep}",
                this
            );
        }


        // =========================================================
        // SPLINE POSITION
        // =========================================================

        private float GetSplinePosition(
            Vector3 worldPosition
        )
        {
            if (splineContainer == null)
                return 0f;

            float closestT = 0f;
            float closestDistance = float.MaxValue;

            const int samples = 500;

            for (
                int i = 0;
                i <= samples;
                i++
            )
            {
                float t =
                    i / (float)samples;

                Vector3 splinePosition =
                    splineContainer.transform.TransformPoint(
                        splineContainer.Spline.EvaluatePosition(t)
                    );

                float distance =
                    Vector3.SqrMagnitude(
                        worldPosition -
                        splinePosition
                    );

                if (distance < closestDistance)
                {
                    closestDistance =
                        distance;

                    closestT =
                        t;
                }
            }

            float knotPosition =
                SplineUtility.ConvertIndexUnit(
                    splineContainer.Spline,
                    closestT,
                    PathIndexUnit.Normalized,
                    PathIndexUnit.Knot
                );

            return knotPosition;
        }


        // =========================================================
        // VALIDATION
        // =========================================================

        private bool ValidateReferences()
        {
            bool valid = true;

            if (autoScrollCamera == null)
            {
                Debug.LogError(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    "Auto Scroll Camera non assignée.",
                    this
                );

                valid = false;
            }

            if (combatCamera == null)
            {
                Debug.LogError(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    "Combat Camera non assignée.",
                    this
                );

                valid = false;
            }

            if (autoScrollDolly == null)
            {
                Debug.LogError(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    "Auto Scroll Dolly non assigné.",
                    this
                );

                valid = false;
            }

            if (target == null)
            {
                Debug.LogError(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    "Target non assignée.",
                    this
                );

                valid = false;
            }

            if (playerMovement == null)
            {
                Debug.LogWarning(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    "PlayerMovement n'est pas assigné. " +
                    "Le Movement Space ne pourra pas être transmis au joueur.",
                    this
                );
            }

            return valid;
        }


        // =========================================================
        // COMBAT KNOT VALIDATION
        // =========================================================

        private void ValidateCombatKnots()
        {
            if (
                combatKnots == null ||
                combatKnots.Count == 0
            )
            {
                Debug.LogWarning(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    "Aucun Combat Knot n'est configuré.",
                    this
                );

                return;
            }

            for (
                int i = 0;
                i < combatKnots.Count;
                i++
            )
            {
                CombatKnot knot =
                    combatKnots[i];

                if (knot == null)
                {
                    Debug.LogWarning(
                        $"[{nameof(VCameraSetup_Follow)}] " +
                        $"Combat Knot Element {i} est null.",
                        this
                    );

                    continue;
                }


                // -------------------------------------------------
                // KNOT INDEX
                // -------------------------------------------------

                if (
                    knot.KnotIndex < 0 ||
                    knot.KnotIndex >=
                    splineContainer.Spline.Count
                )
                {
                    Debug.LogError(
                        $"[{nameof(VCameraSetup_Follow)}] " +
                        $"Combat Knot '{knot.name}' " +
                        $"utilise Knot Index " +
                        $"{knot.KnotIndex}, " +
                        $"mais la spline contient " +
                        $"{splineContainer.Spline.Count} knots.",
                        knot
                    );
                }


                // -------------------------------------------------
                // NEXT MOVEMENT SPACE
                // -------------------------------------------------

                if (knot.NextMovementSpace == null)
                {
                    Debug.LogWarning(
                        $"[{nameof(VCameraSetup_Follow)}] " +
                        $"Combat Knot '{knot.name}' " +
                        "n'a aucun Next Movement Space.",
                        knot
                    );
                }


                Debug.Log(
                    $"[{nameof(VCameraSetup_Follow)}] " +
                    $"Combat Knot [{i}] : " +
                    $"{knot.name} | " +
                    $"Knot Index = " +
                    $"{knot.KnotIndex} | " +
                    $"Next Movement Space = " +
                    $"{(knot.NextMovementSpace != null ? knot.NextMovementSpace.name : "NONE")}",
                    this
                );
            }
        }


        // =========================================================
        // DEBUG
        // =========================================================

        private string GetNextKnotDebugValue()
        {
            if (
                currentCombatStep >=
                combatKnots.Count
            )
            {
                return "NONE";
            }

            CombatKnot knot =
                combatKnots[currentCombatStep];

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
