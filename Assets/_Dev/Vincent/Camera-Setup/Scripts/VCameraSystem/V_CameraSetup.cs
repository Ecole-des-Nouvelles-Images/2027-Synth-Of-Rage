using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

namespace _Dev.Vincent.Camera_Setup.Scripts
{
    public class VCameraSetup : MonoBehaviour
    {
        [Header("Cameras")]
        [SerializeField] private CinemachineCamera autoScrollCamera;
        [SerializeField] private CinemachineCamera combatCamera;

        [Header("Auto Scroll")]
        [SerializeField] private CinemachineSplineDolly autoScrollDolly;
        [SerializeField] private float autoScrollGlobalSpeed;

        [Header("Combat Dolly")]
        [SerializeField] private CinemachineSplineDolly combatDolly;

        [Header("Combat Steps")]
        [SerializeField] private bool combatEnabled;
        [SerializeField] private List<int> combatKnots = new List<int> { 1, 5, 8 };
        [SerializeField] private float combatTolerance = 0.01f;

        [Header("Combat Deceleration")]
        [SerializeField] private float combatDecelerationDistance = 0.25f;

        private bool combatMode;
        private int currentCombatStep;

        private SplineAutoDolly.FixedSpeed autoScrollFixedSpeed;
        private SplineAutoDolly.FixedSpeed combatFixedSpeed;

        private void Start()
        {
            autoScrollFixedSpeed =
                autoScrollDolly.AutomaticDolly.Method as SplineAutoDolly.FixedSpeed;

            combatFixedSpeed =
                combatDolly.AutomaticDolly.Method as SplineAutoDolly.FixedSpeed;

            if (autoScrollFixedSpeed == null)
            {
                Debug.LogError("Auto Scroll Dolly n'utilise pas Fixed Speed.");
                return;
            }

            if (combatFixedSpeed == null)
            {
                Debug.LogError("Combat Dolly n'utilise pas Fixed Speed.");
                return;
            }

            autoScrollFixedSpeed.Speed = autoScrollGlobalSpeed;
            combatFixedSpeed.Speed = autoScrollGlobalSpeed;

            // Les deux Dolly commencent au même endroit.
            combatDolly.CameraPosition = autoScrollDolly.CameraPosition;

            // Les deux caméras restent actives.
            autoScrollCamera.gameObject.SetActive(true);
            combatCamera.gameObject.SetActive(true);

            // Seule AutoScroll est prioritaire au départ.
            ApplyCameraState();
        }

        private void Update()
        {
            // Le Combat Dolly suit toujours le Dolly principal
            // lorsqu'aucun combat n'est en cours.
            if (!combatMode)
            {
                SynchronizeCombatDolly();
            }

            // Le joueur termine manuellement le combat.
            if (!combatEnabled && combatMode)
            {
                EndCombat();
            }

            // Recherche du prochain point de combat.
            if (!combatMode && currentCombatStep < combatKnots.Count)
            {
                CheckCombatKnot();
            }
        }

        private void SynchronizeCombatDolly()
        {
            combatDolly.CameraPosition = autoScrollDolly.CameraPosition;
        }

        private void CheckCombatKnot()
        {
            int targetKnot = combatKnots[currentCombatStep];

            float currentPosition = autoScrollDolly.CameraPosition;

            // Distance restante sur la spline avant le Knot.
            float distanceToKnot = targetKnot - currentPosition;

            // ---------------------------------------------------------
            // DECELERATION
            // ---------------------------------------------------------

            if (distanceToKnot <= combatDecelerationDistance)
            {
                // 1 = vitesse normale
                // 0 = arrêt
                float speedMultiplier =
                    Mathf.Clamp01(
                        distanceToKnot / combatDecelerationDistance
                    );

                // Décélération linéaire sur la spline.
                autoScrollFixedSpeed.Speed =
                    autoScrollGlobalSpeed * speedMultiplier;
            }
            else
            {
                // En dehors de la zone de décélération :
                // vitesse normale.
                autoScrollFixedSpeed.Speed =
                    autoScrollGlobalSpeed;
            }

            // ---------------------------------------------------------
            // DECLENCHEMENT DU COMBAT
            // ---------------------------------------------------------

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

            // Place les deux Dolly exactement sur le Knot.
            autoScrollDolly.CameraPosition = targetKnot;
            combatDolly.CameraPosition = targetKnot;

            // Garde Automatic Dolly actif,
            // mais arrête les deux Dolly.
            autoScrollDolly.AutomaticDolly.Enabled = true;
            combatDolly.AutomaticDolly.Enabled = true;

            autoScrollFixedSpeed.Speed = 0f;
            combatFixedSpeed.Speed = 0f;

            // Passe sur la caméra Combat.
            autoScrollCamera.Priority.Value = 0;
            combatCamera.Priority.Value = 10;

            combatCamera.Prioritize();
        }

        private void EndCombat()
        {
            combatMode = false;

            // Passe au prochain combat.
            currentCombatStep++;

            // Synchronise les deux Dolly.
            combatDolly.CameraPosition = autoScrollDolly.CameraPosition;

            // Restaure la vitesse normale.
            autoScrollFixedSpeed.Speed = autoScrollGlobalSpeed;
            combatFixedSpeed.Speed = autoScrollGlobalSpeed;

            // S'assure que l'Automatic Dolly est bien actif.
            autoScrollDolly.AutomaticDolly.Enabled = true;
            combatDolly.AutomaticDolly.Enabled = true;

            // Retour à Auto Scroll.
            autoScrollCamera.Priority.Value = 10;
            combatCamera.Priority.Value = 0;

            autoScrollCamera.Prioritize();
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