using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public class Sprite3DOutlineToggle : MonoBehaviour
{
    // ================================================================
    // SHADER PROPERTY
    // ================================================================

    private static readonly int SpriteOutlineEnabledID =
        Shader.PropertyToID("_SpriteOutlineEnabled");


    // ================================================================
    // SETTINGS
    // ================================================================

    [Header("Sprite 3D Outline")]

    [SerializeField]
    private bool useOutline = true;


    // ================================================================
    // REFERENCES
    // ================================================================

    private Renderer targetRenderer;

    private MaterialPropertyBlock propertyBlock;


    // ================================================================
    // PUBLIC PROPERTY
    // ================================================================

    public bool UseOutline
    {
        get
        {
            return useOutline;
        }

        set
        {
            if (useOutline == value)
                return;

            useOutline = value;

            ApplyOutlineState();
        }
    }


    // ================================================================
    // UNITY
    // ================================================================

    private void Awake()
    {
        CacheReferences();

        ApplyOutlineState();
    }


    private void OnEnable()
    {
        CacheReferences();

        ApplyOutlineState();
    }


    private void Reset()
    {
        CacheReferences();

        useOutline = true;

        ApplyOutlineState();
    }


    private void OnValidate()
    {
        CacheReferences();

        ApplyOutlineState();
    }


    // ================================================================
    // CACHE
    // ================================================================

    private void CacheReferences()
    {
        if (targetRenderer == null)
        {
            targetRenderer =
                GetComponent<Renderer>();
        }


        if (propertyBlock == null)
        {
            propertyBlock =
                new MaterialPropertyBlock();
        }
    }


    // ================================================================
    // APPLY
    // ================================================================

    private void ApplyOutlineState()
    {
        if (targetRenderer == null)
            return;


        if (propertyBlock == null)
        {
            propertyBlock =
                new MaterialPropertyBlock();
        }


        // ------------------------------------------------------------
        // Récupère les autres propriétés déjà présentes.
        // ------------------------------------------------------------
        //
        // Important pour ne pas écraser d'autres MaterialPropertyBlock
        // utilisés sur le même Renderer.
        // ------------------------------------------------------------

        targetRenderer.GetPropertyBlock(
            propertyBlock
        );


        // ------------------------------------------------------------
        // OUTLINE TOGGLE
        // ------------------------------------------------------------

        propertyBlock.SetFloat(
            SpriteOutlineEnabledID,

            useOutline
                ? 1.0f
                : 0.0f
        );


        // ------------------------------------------------------------
        // APPLY
        // ------------------------------------------------------------

        targetRenderer.SetPropertyBlock(
            propertyBlock
        );
    }


    // ================================================================
    // PUBLIC API
    // ================================================================

    public void SetOutlineEnabled(
        bool enabled
    )
    {
        UseOutline =
            enabled;
    }


    public void EnableOutline()
    {
        UseOutline =
            true;
    }


    public void DisableOutline()
    {
        UseOutline =
            false;
    }


    public void ToggleOutline()
    {
        UseOutline =
            !UseOutline;
    }


    // ================================================================
    // FORCE REFRESH
    // ================================================================
    //
    // Utile si un autre script modifie le MaterialPropertyBlock
    // et que tu veux réappliquer l'état de l'outline.
    // ================================================================

    public void RefreshOutline()
    {
        CacheReferences();

        ApplyOutlineState();
    }
}