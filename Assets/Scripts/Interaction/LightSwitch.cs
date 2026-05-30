using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LightSwitch : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float interactRadius = 1.6f;

    [Header("Light")]
    [SerializeField] private Light targetLight;
    [SerializeField] private bool startsOn;

    [Header("Visuals")]
    [SerializeField] private Renderer sphereRenderer;
    [SerializeField] private Color offColor = new Color(0.18f, 0.18f, 0.18f);
    [SerializeField] private Color onColor = new Color(1f, 0.82f, 0.25f);
    [SerializeField] private Color highlightedColor = new Color(0.4f, 0.75f, 1f);
    [SerializeField] private float emissionIntensity = 2f;

    private Material sphereMaterial;
    private bool isOn;
    private bool isHighlighted;

    void Awake()
    {
        if (interactionPoint == null)
        {
            interactionPoint = transform;
        }

        if (targetLight == null)
        {
            targetLight = GetComponentInChildren<Light>(includeInactive: true);
        }

        if (sphereRenderer == null)
        {
            sphereRenderer = GetComponentInChildren<Renderer>();
        }

        if (sphereRenderer != null)
        {
            sphereMaterial = sphereRenderer.material;
        }

        isOn = startsOn || (targetLight != null && targetLight.gameObject.activeSelf && targetLight.enabled);
        ApplyState();
    }

    public bool CanInteract(Vector3 playerPosition)
    {
        return Vector3.Distance(playerPosition, interactionPoint.position) <= interactRadius;
    }

    public bool Interact(PlayerMovement player)
    {
        isOn = !isOn;
        ApplyState();
        return true;
    }

    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted)
        {
            return;
        }

        isHighlighted = highlighted;
        ApplyVisualState();
    }

    private void ApplyState()
    {
        if (targetLight != null)
        {
            targetLight.gameObject.SetActive(isOn);
            targetLight.enabled = isOn;
        }

        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        if (sphereMaterial == null)
        {
            return;
        }

        Color baseColor = isHighlighted ? highlightedColor : (isOn ? onColor : offColor);
        SetMaterialColor(baseColor);
        SetEmissionColor(isOn || isHighlighted ? baseColor * emissionIntensity : Color.black);
    }

    private void SetMaterialColor(Color color)
    {
        if (sphereMaterial.HasProperty("_BaseColor"))
        {
            sphereMaterial.SetColor("_BaseColor", color);
            return;
        }

        if (sphereMaterial.HasProperty("_Color"))
        {
            sphereMaterial.SetColor("_Color", color);
        }
    }

    private void SetEmissionColor(Color color)
    {
        if (!sphereMaterial.HasProperty("_EmissionColor"))
        {
            return;
        }

        if (color == Color.black)
        {
            sphereMaterial.DisableKeyword("_EMISSION");
        }
        else
        {
            sphereMaterial.EnableKeyword("_EMISSION");
        }

        sphereMaterial.SetColor("_EmissionColor", color);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = interactionPoint != null ? interactionPoint.position : transform.position;
        Gizmos.DrawWireSphere(center, interactRadius);
    }
}
