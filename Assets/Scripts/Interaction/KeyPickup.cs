using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KeyPickup : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private float interactRadius = 1.8f;

    [Header("Held Position")]
    [SerializeField] private string targetBoneName = "LeftIndex3";
    [SerializeField] private Vector3 heldLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 heldLocalEulerAngles = Vector3.zero;

    private bool isPickedUp;

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

        AttachToBone(targetBone);
        return true;
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

    private void AttachToBone(Transform targetBone)
    {
        isPickedUp = true;
        DisablePickupPhysics();

        transform.SetParent(targetBone, worldPositionStays: false);
        transform.localPosition = heldLocalPosition;
        transform.localRotation = Quaternion.Euler(heldLocalEulerAngles);
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
