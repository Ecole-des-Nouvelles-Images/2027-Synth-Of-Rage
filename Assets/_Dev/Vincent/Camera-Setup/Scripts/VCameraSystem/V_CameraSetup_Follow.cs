using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Splines;

namespace _Dev.Vincent.Camera_Setup.Scripts
{
    public class VCameraSetup_Follow : MonoBehaviour
    {
        [Header("Cameras")]
        [SerializeField] private CinemachineCamera autoScrollCamera;
        [SerializeField] private CinemachineCamera combatCamera;

        [Header("Rail")]
        [SerializeField] private CinemachineSplineDolly autoScrollDolly;
        [SerializeField] private CinemachineSplineDolly combatDolly;

        [Header("Player Follow")]
        [SerializeField] private Transform target;
        [SerializeField] private float followSmoothTime = 0.15f;

        [Header("Combat Knots")]
        [SerializeField] private bool combatEnabled;
        [SerializeField] private List<int> combatKnots = new List<int> { 1, 5, 8 };
        [SerializeField] private float combatTolerance = 0.01f;

        private bool combatMode;
        private int currentCombatStep;

        private float currentSplinePosition;
        private float targetSplinePosition;
        private float splineVelocity;

        private SplineContainer splineContainer;

        private void Start()
        {
            splineContainer = autoScrollDolly.Spline;

            if (splineContainer == null)
            {
                Debug.LogError("Aucun Spline Container n'est assigné au Auto Scroll Dolly.");
                return;
            }

            if (target == null)
            {
                Debug.LogError("Aucun Transform à suivre n'est assigné.");
                return;
            }

            // Position de départ.
            currentSplinePosition = autoScrollDolly.CameraPosition;
            targetSplinePosition = currentSplinePosition;

            // Synchronise les deux Dolly.
            combatDolly.CameraPosition = currentSplinePosition;

            // Les deux caméras restent actives.
            autoScrollCamera.gameObject.SetActive(true);
            combatCamera.gameObject.SetActive(true);

            ApplyCameraState();
        }

        private void Update()
        {
            // ---------------------------------------------------------
            // SORTIE DE COMBAT
            // ---------------------------------------------------------

            if (!combatEnabled && combatMode)
            {
                EndCombat();
            }

            // ---------------------------------------------------------
            // SUIVI DU JOUEUR
            // ---------------------------------------------------------

            if (!combatMode)
            {
                UpdateFollow();
            }

            // ---------------------------------------------------------
            // VERIFICATION DES KNOTS DE COMBAT
            // ---------------------------------------------------------

            if (!combatMode && currentCombatStep < combatKnots.Count)
            {
                CheckCombatKnot();
            }
        }

        private void UpdateFollow()
        {
            if (target == null)
                return;

            // Trouve la position du joueur sur la spline.
            targetSplinePosition = GetSplinePosition(target.position);

            // La caméra suit progressivement le joueur.
            currentSplinePosition = Mathf.SmoothDamp(
                currentSplinePosition,
                targetSplinePosition,
                ref splineVelocity,
                followSmoothTime
            );

            // La caméra reste sur la spline.
            autoScrollDolly.CameraPosition = currentSplinePosition;
            combatDolly.CameraPosition = currentSplinePosition;
        }

        private void CheckCombatKnot()
        {
            int targetKnot = combatKnots[currentCombatStep];

            float currentPosition = autoScrollDolly.CameraPosition;

            // On déclenche lorsque la caméra atteint
            // ou dépasse légèrement le Knot.
            if (currentPosition >= targetKnot - combatTolerance)
            {
                StartCombat(targetKnot);
            }
        }

        private void StartCombat(int targetKnot)
        {
            combatMode = true;

            // Active automatiquement le bool.
            combatEnabled = true;

            // Position exacte du Knot.
            currentSplinePosition = targetKnot;
            targetSplinePosition = targetKnot;

            // Annule toute vélocité de SmoothDamp.
            splineVelocity = 0f;

            // Place les deux caméras exactement au Knot.
            autoScrollDolly.CameraPosition = targetKnot;
            combatDolly.CameraPosition = targetKnot;

            // Passe sur la caméra Combat.
            autoScrollCamera.Priority.Value = 0;
            combatCamera.Priority.Value = 10;

            combatCamera.Prioritize();
        }

        private void EndCombat()
        {
            combatMode = false;

            // Passe au prochain Knot de combat.
            currentCombatStep++;

            // Récupère la position actuelle.
            currentSplinePosition = autoScrollDolly.CameraPosition;
            targetSplinePosition = currentSplinePosition;

            // Empêche un mouvement brusque à la reprise.
            splineVelocity = 0f;

            // Synchronise les deux Dolly.
            combatDolly.CameraPosition = currentSplinePosition;

            // Retour à la caméra normale.
            autoScrollCamera.Priority.Value = 10;
            combatCamera.Priority.Value = 0;

            autoScrollCamera.Prioritize();
        }

        private float GetSplinePosition(Vector3 worldPosition)
        {
            float closestT = 0f;
            float closestDistance = float.MaxValue;

            // Précision de recherche sur la spline.
            const int samples = 200;

            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;

                Vector3 splinePosition =
                    splineContainer.transform.TransformPoint(
                        splineContainer.Spline.EvaluatePosition(t)
                    );

                float distance =
                    Vector3.SqrMagnitude(worldPosition - splinePosition);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestT = t;
                }
            }

            // Conversion de 0-1 vers les unités Knot.
            return closestT * (splineContainer.Spline.Count - 1);
        }

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
    }
}