using System.Collections.Generic;
using UnityEngine;

// Pas besoin de DefaultExecutionOrder ici : le delta est capturé en LateUpdate
// (dernière position de la frame) et consommé au Update suivant par le joueur.
// Latence résiduelle : 1 frame = offset constant minime (≈ déplacement d'une
// frame de cabine), AUCUNE dérive cumulée.
public class MovingPlatformCarry : MonoBehaviour
{
    private static readonly Dictionary<CharacterController, MovingPlatformCarry> CarriesByController =
        new Dictionary<CharacterController, MovingPlatformCarry>();

    private Vector3 _lastPosition;
    private readonly HashSet<CharacterController> _riders = new HashSet<CharacterController>();
    private readonly Dictionary<CharacterController, Vector3> _pendingDeltas =
        new Dictionary<CharacterController, Vector3>();

    public void Register(CharacterController controller)
    {
        if (controller == null) return;
        _riders.Add(controller);
        CarriesByController[controller] = this;
        _pendingDeltas[controller] = Vector3.zero;
    }

    public void Unregister(CharacterController controller)
    {
        if (controller == null) return;
        _riders.Remove(controller);
        CarriesByController.Remove(controller);
        _pendingDeltas.Remove(controller);
    }

    private void OnEnable()
    {
        _lastPosition = transform.position;
    }

    private void OnDisable()
    {
        // Sécurité : tout désenregistrer si la cabine est désactivée en cours de trajet.
        foreach (CharacterController controller in new List<CharacterController>(_riders))
            Unregister(controller);
    }

    private void LateUpdate()
    {
        // LateUpdate : quelle que soit la façon dont la cabine bouge (Animator,
        // script, tween), on capture la position FINALE de la frame.
        Vector3 delta = transform.position - _lastPosition;
        _lastPosition = transform.position;

        if (delta.sqrMagnitude < 1e-10f || _riders.Count == 0)
            return;

        foreach (CharacterController controller in _riders)
        {
            if (controller == null) continue;
            _pendingDeltas[controller] = delta;
        }
    }

    /// <summary>
    /// Retourne (et consomme) le mouvement de plateforme que le contrôleur doit
    /// appliquer à sa frame. À appeler depuis le mouvement du joueur, en TOUT
    /// PREMIER dans Update(), avant la gravité et tout autre Move().
    /// </summary>
    public static bool TryConsumeDelta(CharacterController controller, out Vector3 delta)
    {
        if (controller != null &&
            CarriesByController.TryGetValue(controller, out MovingPlatformCarry carry) &&
            carry._pendingDeltas.TryGetValue(controller, out Vector3 pending) &&
            pending.sqrMagnitude > 1e-10f)
        {
            delta = pending;
            carry._pendingDeltas[controller] = Vector3.zero;
            return true;
        }

        delta = Vector3.zero;
        return false;
    }
}