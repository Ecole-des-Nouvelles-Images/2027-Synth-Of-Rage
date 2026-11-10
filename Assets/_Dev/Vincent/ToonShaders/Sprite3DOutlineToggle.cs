
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public class Sprite3DOutlineToggle : MonoBehaviour
{
    private static readonly int OutlineEnabledID =
        Shader.PropertyToID("_SpriteOutlineEnabled");

    private static readonly int OutlineOcclusionEnabledID =
        Shader.PropertyToID("_SpriteOutlineOcclusionEnabled");

    [Header("Outline")]
    [SerializeField]
    private bool useOutline = true;

    [SerializeField]
    private bool affectOtherOutlines = true;

    [Header("Renderer")]
    [SerializeField]
    private Renderer targetRenderer;

    private MaterialPropertyBlock propertyBlock;

    public bool UseOutline => useOutline;
    public bool AffectOtherOutlines => affectOtherOutlines;

    private void Awake()
    {
        Initialize();
        RefreshOutline();
    }

    private void OnEnable()
    {
        Initialize();
        RefreshOutline();
    }

    private void OnValidate()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (!Application.isPlaying)
        {
            Initialize();
            RefreshOutline();
        }
    }

    private void Reset()
    {
        targetRenderer = GetComponent<Renderer>();
    }

    private void Initialize()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();
    }

    public void SetOutlineEnabled(bool enabled)
    {
        useOutline = enabled;
        RefreshOutline();
    }

    public void EnableOutline() => SetOutlineEnabled(true);
    public void DisableOutline() => SetOutlineEnabled(false);
    public void ToggleOutline() => SetOutlineEnabled(!useOutline);

    public void SetAffectOtherOutlines(bool enabled)
    {
        affectOtherOutlines = enabled;
        RefreshOutline();
    }

    public void EnableOtherOutlinesOcclusion() =>
        SetAffectOtherOutlines(true);

    public void DisableOtherOutlinesOcclusion() =>
        SetAffectOtherOutlines(false);

    public void ToggleOtherOutlinesOcclusion() =>
        SetAffectOtherOutlines(!affectOtherOutlines);

    public void RefreshOutline()
    {
        if (targetRenderer == null)
            return;

        Initialize();

        // Preserve existing properties set by other systems.
        targetRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetFloat(
            OutlineEnabledID,
            useOutline ? 1f : 0f
        );

        propertyBlock.SetFloat(
            OutlineOcclusionEnabledID,
            affectOtherOutlines ? 1f : 0f
        );

        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}
