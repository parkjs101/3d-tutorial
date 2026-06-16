using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KeyPickup : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private float interactRadius = 1.8f;
    [SerializeField] private string keyId = "basement_key";

    [Header("Held Position")]
    [SerializeField] private string targetBoneName = "LeftIndex3";
    [SerializeField] private Vector3 heldLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 heldLocalEulerAngles = Vector3.zero;

    private bool isPickedUp;

    public string KeyId => keyId;

    public bool CanInteract(Vector3 playerPosition)
    {
        return !isPickedUp && Vector3.Distance(playerPosition, transform.position) <= interactRadius;
    }

    public bool Interact(PlayerMovement player)
    {
        if (player == null || isPickedUp)
        {
            return false;
        }

        Transform targetBone = FindTargetBone(player.transform);
        if (targetBone == null)
        {
            Debug.LogError($"KeyPickup could not find bone '{targetBoneName}' below Player.", this);
            return false;
        }

        PlayerKeyHolder keyHolder = player.GetComponent<PlayerKeyHolder>();
        if (keyHolder == null)
        {
            Debug.LogError("KeyPickup requires PlayerKeyHolder on Player.", player);
            return false;
        }

        keyHolder.HoldKey(this);
        AttachTo(targetBone, heldLocalPosition, heldLocalEulerAngles);
        return true;
    }

    public void AttachTo(Transform parent, Vector3 localPosition, Vector3 localEulerAngles)
    {
        if (parent == null)
        {
            return;
        }

        Vector3 targetWorldScale = transform.lossyScale;
        isPickedUp = true;
        DisablePickupPhysics();

        transform.SetParent(parent, worldPositionStays: false);
        transform.localPosition = localPosition;
        transform.localRotation = Quaternion.Euler(localEulerAngles);
        transform.localScale = GetLocalScaleForWorldScale(parent, targetWorldScale);
    }

    public void SetHighlighted(bool highlighted)
    {
    }

    private Transform FindTargetBone(Transform playerRoot)
    {
        foreach (Transform child in playerRoot.GetComponentsInChildren<Transform>(includeInactive: true))
        {
            if (string.Equals(child.name, targetBoneName, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    private Vector3 GetLocalScaleForWorldScale(Transform parent, Vector3 targetWorldScale)
    {
        Vector3 parentScale = parent.lossyScale;

        return new Vector3(
            SafeDivide(targetWorldScale.x, parentScale.x),
            SafeDivide(targetWorldScale.y, parentScale.y),
            SafeDivide(targetWorldScale.z, parentScale.z)
        );
    }

    private float SafeDivide(float numerator, float denominator)
    {
        if (Mathf.Approximately(denominator, 0f))
        {
            return numerator;
        }

        return numerator / denominator;
    }

    private void DisablePickupPhysics()
    {
        foreach (Collider pickupCollider in GetComponentsInChildren<Collider>(includeInactive: true))
        {
            pickupCollider.enabled = false;
        }

        Rigidbody pickupRigidbody = GetComponent<Rigidbody>();
        if (pickupRigidbody == null)
        {
            return;
        }

        pickupRigidbody.linearVelocity = Vector3.zero;
        pickupRigidbody.angularVelocity = Vector3.zero;
        pickupRigidbody.useGravity = false;
        pickupRigidbody.isKinematic = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
