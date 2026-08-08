using System.Collections;
using UnityEngine;

public class CandlePickup : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private float interactRadius = 1.8f;
    [SerializeField, Min(0f)] private float reachDuration = 0.35f;

    [Header("Attachment Points")]
    [SerializeField] private Transform rightGripPoint;
    [SerializeField] private Transform candleGripPoint;
    [SerializeField] private Transform placementPoint;

    [Header("Held Position")]
    [SerializeField] private Vector3 heldLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 heldLocalEulerAngles = Vector3.zero;

    [Header("Place Hand Pose")]
    [SerializeField] private Vector3 placeHandEulerAngles = Vector3.zero;

    private Collider[] itemColliders;
    private bool[] originalTriggerStates;
    private Rigidbody itemRigidbody;
    private Vector3 originalWorldScale;
    private bool isHeld;
    private bool isPlaced;
    private bool isReaching;
    private bool isPlacing;
    private Quaternion placeHandRotation;

    private static CandlePickup heldCandle;

    public static CandlePickup ReachingCandle { get; private set; }
    public static bool IsTransitioning => ReachingCandle != null;
    public Transform HandIkTarget => isPlacing ? placementPoint : candleGripPoint;
    public Quaternion HandIkRotation => isPlacing ? placeHandRotation : candleGripPoint.rotation;
    public bool IsPlacing => isPlacing;

    private void Awake()
    {
        itemColliders = GetComponentsInChildren<Collider>(includeInactive: true);
        originalTriggerStates = new bool[itemColliders.Length];
        for (int i = 0; i < itemColliders.Length; i++)
        {
            originalTriggerStates[i] = itemColliders[i].isTrigger;
        }

        itemRigidbody = GetComponent<Rigidbody>();
        originalWorldScale = transform.lossyScale;
    }

    public bool CanInteract(Vector3 playerPosition)
    {
        if (isPlaced || isReaching || isPlacing)
        {
            return false;
        }

        if (isHeld)
        {
            return placementPoint != null &&
                   Vector3.Distance(playerPosition, placementPoint.position) <= interactRadius;
        }

        return heldCandle == null &&
               Vector3.Distance(playerPosition, transform.position) <= interactRadius;
    }

    public bool Interact(PlayerMovement player)
    {
        if (player == null || isPlaced || isReaching || isPlacing)
        {
            return false;
        }

        if (isHeld)
        {
            return BeginPlace();
        }

        return PickUp();
    }

    public void SetHighlighted(bool highlighted)
    {
    }

    private bool PickUp()
    {
        if (rightGripPoint == null)
        {
            Debug.LogError("CandlePickup requires a Right Grip Point.", this);
            return false;
        }

        if (placementPoint == null)
        {
            Debug.LogError("CandlePickup requires a Placement Point.", this);
            return false;
        }

        if (candleGripPoint == null)
        {
            Debug.LogError("CandlePickup requires a Candle Grip Point.", this);
            return false;
        }

        heldCandle = this;
        SetHeldPhysics();
        StartCoroutine(ReachAndPickUp());
        return true;
    }

    private IEnumerator ReachAndPickUp()
    {
        isReaching = true;
        ReachingCandle = this;

        if (reachDuration > 0f)
        {
            yield return new WaitForSeconds(reachDuration);
        }

        AttachGripTo(rightGripPoint);
        isHeld = true;
        isReaching = false;
        ReachingCandle = null;
    }

    private bool BeginPlace()
    {
        if (placementPoint == null)
        {
            return false;
        }

        placeHandRotation = rightGripPoint.rotation * Quaternion.Euler(placeHandEulerAngles);
        StartCoroutine(ReachAndPlace());
        return true;
    }

    private IEnumerator ReachAndPlace()
    {
        isPlacing = true;
        ReachingCandle = this;

        if (reachDuration > 0f)
        {
            yield return new WaitForSeconds(reachDuration);
        }

        AttachTo(placementPoint);
        RestoreColliderStates();
        isHeld = false;
        isPlacing = false;
        isPlaced = true;
        heldCandle = null;
        ReachingCandle = null;
    }

    private void AttachTo(Transform parent)
    {
        transform.SetParent(parent, worldPositionStays: false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = GetLocalScaleForWorldScale(parent, originalWorldScale);
    }

    private void AttachGripTo(Transform target)
    {
        transform.SetParent(target, worldPositionStays: true);
        transform.localScale = GetLocalScaleForWorldScale(target, originalWorldScale);

        Quaternion targetRotation = target.rotation * Quaternion.Euler(heldLocalEulerAngles);
        Quaternion rotationDelta = targetRotation * Quaternion.Inverse(candleGripPoint.rotation);
        transform.rotation = rotationDelta * transform.rotation;
        Vector3 targetPosition = target.TransformPoint(heldLocalPosition);
        transform.position += targetPosition - candleGripPoint.position;
    }

    private void SetHeldPhysics()
    {
        foreach (Collider itemCollider in itemColliders)
        {
            itemCollider.isTrigger = true;
        }

        if (itemRigidbody == null)
        {
            return;
        }

        itemRigidbody.linearVelocity = Vector3.zero;
        itemRigidbody.angularVelocity = Vector3.zero;
        itemRigidbody.useGravity = false;
        itemRigidbody.isKinematic = true;
    }

    private void RestoreColliderStates()
    {
        for (int i = 0; i < itemColliders.Length; i++)
        {
            itemColliders[i].isTrigger = originalTriggerStates[i];
        }
    }

    private static Vector3 GetLocalScaleForWorldScale(Transform parent, Vector3 worldScale)
    {
        Vector3 parentScale = parent.lossyScale;
        return new Vector3(
            SafeDivide(worldScale.x, parentScale.x),
            SafeDivide(worldScale.y, parentScale.y),
            SafeDivide(worldScale.z, parentScale.z));
    }

    private static float SafeDivide(float numerator, float denominator)
    {
        return Mathf.Approximately(denominator, 0f) ? numerator : numerator / denominator;
    }

    private void OnDestroy()
    {
        if (heldCandle == this)
        {
            heldCandle = null;
        }

        if (ReachingCandle == this)
        {
            ReachingCandle = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
