using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private Transform doorHandle;
    [SerializeField] private Renderer doorHandleRenderer;
    [SerializeField] private float interactRadius = 1.2f;

    [Header("Open Motion")]
    [SerializeField] private Vector3 localHingeOffset = new Vector3(-0.5f, 0f, 0f);
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 120f;

    [Header("Handle Colors")]
    [SerializeField] private Color defaultHandleColor = Color.red;
    [SerializeField] private Color highlightedHandleColor = new Color(1f, 0.82f, 0.12f);
    [SerializeField] private Color openedHandleColor = new Color(0.15f, 0.8f, 0.35f);

    private Material handleMaterial;
    private Vector3 hingePosition;
    private Vector3 hingeAxis;
    private float currentAngle;
    private bool isOpen;
    private bool isHighlighted;

    void Awake()
    {
        if (interactionPoint == null)
        {
            interactionPoint = doorHandle != null ? doorHandle : transform;
        }

        if (doorHandleRenderer == null && doorHandle != null)
        {
            doorHandleRenderer = doorHandle.GetComponent<Renderer>();
        }

        if (doorHandleRenderer != null)
        {
            handleMaterial = doorHandleRenderer.material;
        }

        hingePosition = transform.TransformPoint(localHingeOffset);
        hingeAxis = transform.up;
        UpdateHandleColor();
    }

    void Update()
    {
        float targetAngle = isOpen ? openAngle : 0f;
        float nextAngle = Mathf.MoveTowards(currentAngle, targetAngle, openSpeed * Time.deltaTime);
        float deltaAngle = nextAngle - currentAngle;

        if (Mathf.Abs(deltaAngle) > 0.001f)
        {
            transform.RotateAround(hingePosition, hingeAxis, deltaAngle);

            if (doorHandle != null && !doorHandle.IsChildOf(transform))
            {
                doorHandle.RotateAround(hingePosition, hingeAxis, deltaAngle);
            }

            currentAngle = nextAngle;
        }
    }

    public bool CanInteract(Vector3 playerPosition)
    {
        if (isOpen || interactionPoint == null)
        {
            return false;
        }

        return Vector3.Distance(playerPosition, interactionPoint.position) <= interactRadius;
    }

    public bool TryOpen(Vector3 playerPosition)
    {
        if (!CanInteract(playerPosition))
        {
            return false;
        }

        isOpen = true;
        SetHighlighted(false);
        Debug.Log("Door Opened");
        return true;
    }

    public bool Interact(PlayerMovement player)
    {
        return player != null && TryOpen(player.transform.position);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted)
        {
            return;
        }

        isHighlighted = highlighted;
        UpdateHandleColor();
    }

    void UpdateHandleColor()
    {
        if (handleMaterial == null)
        {
            return;
        }

        if (isOpen)
        {
            handleMaterial.color = openedHandleColor;
            return;
        }

        handleMaterial.color = isHighlighted ? highlightedHandleColor : defaultHandleColor;
    }

    void OnDrawGizmosSelected()
    {
        Transform point = interactionPoint != null ? interactionPoint : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(point.position, interactRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.TransformPoint(localHingeOffset), 0.08f);
    }
}
