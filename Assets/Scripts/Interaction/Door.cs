using System;
using UnityEngine;

public enum DoorOpenMode
{
    HandleOnly,
    RequiresKey
}

public class Door : MonoBehaviour, IInteractable, IUnlockable
{
    [Header("Access")]
    [SerializeField] private DoorOpenMode openMode = DoorOpenMode.HandleOnly;
    [SerializeField] private string requiredKeyId = "basement_key";
    [SerializeField] private Transform keyCheckPoint;
    [SerializeField] private float keyUseRadius = 0.45f;
    [SerializeField] private Vector3 insertedKeyLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 insertedKeyLocalEulerAngles = Vector3.zero;

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
    private Transform keyHoleVisual;
    private Transform resolvedKeyCheckPoint;

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

        keyHoleVisual = FindChildByName("KeyHole", "Keyhole");
        resolvedKeyCheckPoint = keyCheckPoint != null ? keyCheckPoint : (keyHoleVisual != null ? keyHoleVisual : FindChildByName("InteractionPoint"));

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
        Transform activeInteractionPoint = GetActiveInteractionPoint();
        if (isOpen || activeInteractionPoint == null)
        {
            return false;
        }

        return Vector3.Distance(playerPosition, activeInteractionPoint.position) <= interactRadius;
    }

    public bool TryOpen(Vector3 playerPosition)
    {
        if (!CanInteract(playerPosition) || UsesKeyMode())
        {
            return false;
        }

        Open();
        return true;
    }

    public void Open()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;
        SetHighlighted(false);
        UpdateHandleColor();
        Debug.Log("Door Opened");
    }

    public void Unlock()
    {
        Open();
    }

    public bool Interact(PlayerMovement player)
    {
        if (player == null || !CanInteract(player.transform.position))
        {
            return false;
        }

        if (!CanPlayerOpen(player))
        {
            Debug.Log("Door is locked.");
            return true;
        }

        if (UsesKeyMode())
        {
            InsertHeldKey(player);
        }

        Open();
        return true;
    }

    private bool CanPlayerOpen(PlayerMovement player)
    {
        if (!UsesKeyMode())
        {
            return true;
        }

        PlayerKeyHolder keyHolder = player.GetComponent<PlayerKeyHolder>();
        return keyHolder != null &&
               keyHolder.HasMatchingKey(requiredKeyId) &&
               IsHeldKeyNearKeyPoint(keyHolder);
    }

    private bool UsesKeyMode()
    {
        bool handleDisabled = doorHandle != null && !doorHandle.gameObject.activeSelf;
        bool keyHoleActive = keyHoleVisual != null && keyHoleVisual.gameObject.activeSelf;
        return openMode == DoorOpenMode.RequiresKey || handleDisabled || keyHoleActive;
    }

    private bool IsHeldKeyNearKeyPoint(PlayerKeyHolder keyHolder)
    {
        Transform heldKeyTransform = keyHolder.HeldKeyTransform;
        Transform point = GetKeyCheckPoint();

        if (heldKeyTransform == null || point == null)
        {
            return false;
        }

        return Vector3.Distance(heldKeyTransform.position, point.position) <= keyUseRadius;
    }

    private void InsertHeldKey(PlayerMovement player)
    {
        PlayerKeyHolder keyHolder = player.GetComponent<PlayerKeyHolder>();
        Transform point = GetKeyCheckPoint();

        if (keyHolder == null || keyHolder.HeldKey == null || point == null)
        {
            return;
        }

        keyHolder.HeldKey.AttachTo(point, insertedKeyLocalPosition, insertedKeyLocalEulerAngles);
        keyHolder.ClearKey();
    }

    private Transform GetActiveInteractionPoint()
    {
        if (UsesKeyMode())
        {
            return GetKeyCheckPoint();
        }

        return interactionPoint != null ? interactionPoint : transform;
    }

    private Transform GetKeyCheckPoint()
    {
        return resolvedKeyCheckPoint != null ? resolvedKeyCheckPoint : (interactionPoint != null ? interactionPoint : transform);
    }

    private Transform FindChildByName(params string[] childNames)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(includeInactive: true))
        {
            foreach (string childName in childNames)
            {
                if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
        }

        return null;
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

        Transform keyPoint = keyCheckPoint != null ? keyCheckPoint : resolvedKeyCheckPoint;
        if (keyPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(keyPoint.position, keyUseRadius);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.TransformPoint(localHingeOffset), 0.08f);
    }
}
