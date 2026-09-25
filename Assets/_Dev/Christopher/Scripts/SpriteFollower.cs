using UnityEngine;

public class SpriteFollower : MonoBehaviour
{
    private Quaternion initialWorldRotation;

    private void Awake()
    {
        // On récupère la rotation World exacte du sprite
        // au moment de son initialisation.
        initialWorldRotation =
            transform.rotation;
    }

    private void LateUpdate()
    {
        // On force en permanence le sprite à conserver
        // sa rotation World initiale.
        transform.rotation =
            initialWorldRotation;
    }
}