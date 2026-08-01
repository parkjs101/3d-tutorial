using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshCollider), typeof(Rigidbody))]
public class TirePickup : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private float interactRadius = 1.2f;
    [SerializeField, Range(-1f, 1f)] private float minimumPickupFacingDot = 0.5f;

    [Header("Held Position")]
    [SerializeField] private Vector3 heldLocalPosition;
    [SerializeField] private Vector3 heldLocalEulerAngles;

    [Header("Lifting Motion")]
    [SerializeField, Range(0f, 0.95f)] private float tireMoveStartNormalizedTime = 0.65f;

    [Header("Dynamic Grip")]
    [SerializeField] private float gripHeight = 0.8f;
    [SerializeField] private float gripSpacing = 0.4f;
    [SerializeField] private float gripSurfaceOffset = 0.03f;

    [Header("Throw")]
    [SerializeField, Min(0.1f)] private float throwDuration = 0.75f;
    [SerializeField] private float throwSpin = 4f;

    private MeshCollider pickupCollider;
    private Rigidbody pickupRigidbody;
    private bool originalIsTrigger;
    private bool isHeld;
    private bool isPickingUp;
    private Transform leftGrip;
    private Transform rightGrip;
    private Transform activeCarryPoint;
    private Transform carrierVisual;
    private Vector3 carryPointOriginalLocalPosition;
    private Quaternion carryPointOriginalLocalRotation;

    private const string LiftingClipName = "Lifting Object";

    public static TirePickup HeldTire { get; private set; }
    public Transform LeftGrip => leftGrip;
    public Transform RightGrip => rightGrip;
    public bool IsLifting => isPickingUp;
    public bool IsCarrying => isHeld;

    void Awake()
    {
        pickupCollider = GetComponent<MeshCollider>();
        pickupRigidbody = GetComponent<Rigidbody>();
        if (pickupCollider == null)
        {
            Debug.LogError("TirePickup requires a MeshCollider on the tire.", this);
            enabled = false;
            return;
        }

        originalIsTrigger = pickupCollider.isTrigger;
        ResolveGrips();
    }

    public bool CanInteract(Vector3 playerPosition)
    {
        return isHeld || isPickingUp || Vector3.Distance(playerPosition, transform.position) <= interactRadius;
    }

    public bool Interact(PlayerMovement player)
    {
        if (player == null)
        {
            return false;
        }

        if (isPickingUp)
        {
            return false;
        }

        if (isHeld)
        {
            Drop();
            return true;
        }

        if (HeldTire != null || !CanInteract(player.transform.position) || !IsFacingPickup(player.transform))
        {
            return false;
        }

        Transform carryPoint = FindChildByName(player.transform, "CarryPoint");
        if (carryPoint == null)
        {
            Debug.LogError("TirePickup could not find CarryPoint below Player.", player);
            return false;
        }

        ResolveGrips();
        if (leftGrip == null || rightGrip == null)
        {
            Debug.LogError("TirePickup requires LeftGrip and RightGrip below the tire.", this);
            return false;
        }

        UpdateGripTargets(player.transform);

        if (!TryGetLiftingDuration(player, out float liftingDuration))
        {
            return false;
        }

        isPickingUp = true;
        HeldTire = this;

        pickupRigidbody.linearVelocity = Vector3.zero;
        pickupRigidbody.angularVelocity = Vector3.zero;
        pickupRigidbody.useGravity = false;
        pickupRigidbody.isKinematic = true;
        pickupCollider.isTrigger = true;
        Animator carrierAnimator = player.GetComponentInChildren<Animator>();
        StartCoroutine(MoveToCarryPoint(carryPoint, carrierAnimator.transform, liftingDuration));
        return true;
    }

    public void SetHighlighted(bool highlighted)
    {
    }

    public void Drop()
    {
        isHeld = false;
        if (HeldTire == this)
        {
            HeldTire = null;
        }

        transform.SetParent(null, worldPositionStays: true);
        RestoreCarryPointRotation();
        pickupCollider.isTrigger = originalIsTrigger;
        pickupRigidbody.isKinematic = false;
        pickupRigidbody.useGravity = true;
        pickupRigidbody.linearVelocity = Vector3.zero;
        pickupRigidbody.angularVelocity = Vector3.zero;
    }

    public void ThrowTo(Vector3 targetPosition)
    {
        if (!isHeld)
        {
            return;
        }

        Vector3 startPosition = transform.position;
        Drop();
        Vector3 displacement = targetPosition - startPosition;
        Vector3 launchVelocity = displacement / throwDuration - Physics.gravity * (throwDuration * 0.5f);
        pickupRigidbody.linearVelocity = launchVelocity;
        pickupRigidbody.angularVelocity = Vector3.Cross(Vector3.up, launchVelocity.normalized) * throwSpin;
    }

    private IEnumerator MoveToCarryPoint(Transform carryPoint, Transform visual, float liftingDuration)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 targetWorldScale = transform.lossyScale;
        float moveStartTime = liftingDuration * tireMoveStartNormalizedTime;
        float moveDuration = Mathf.Max(0.01f, liftingDuration - moveStartTime);
        float elapsed = 0f;

        while (elapsed < liftingDuration)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= moveStartTime)
            {
                float progress = Mathf.Clamp01((elapsed - moveStartTime) / moveDuration);
                float smoothProgress = progress * progress * (3f - 2f * progress);
                Vector3 targetPosition = carryPoint.TransformPoint(heldLocalPosition);
                Quaternion targetRotation = carryPoint.rotation * Quaternion.Euler(heldLocalEulerAngles);

                transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, targetPosition, smoothProgress),
                    Quaternion.Slerp(startRotation, targetRotation, smoothProgress));
            }

            yield return null;
        }

        transform.SetParent(carryPoint, worldPositionStays: false);
        transform.localPosition = heldLocalPosition;
        transform.localRotation = Quaternion.Euler(heldLocalEulerAngles);
        transform.localScale = GetLocalScaleForWorldScale(carryPoint, targetWorldScale);
        pickupCollider.isTrigger = true;
        activeCarryPoint = carryPoint;
        carrierVisual = visual;
        carryPointOriginalLocalPosition = carryPoint.localPosition;
        carryPointOriginalLocalRotation = carryPoint.localRotation;
        isPickingUp = false;
        isHeld = true;
    }

    void LateUpdate()
    {
        if (isHeld && activeCarryPoint != null && carrierVisual != null)
        {
            Transform carryPointParent = activeCarryPoint.parent;
            if (carryPointParent == null)
            {
                return;
            }

            Quaternion localFacingRotation = Quaternion.Inverse(carryPointParent.rotation) * carrierVisual.rotation;
            activeCarryPoint.localPosition = localFacingRotation * carryPointOriginalLocalPosition;
            activeCarryPoint.localRotation = localFacingRotation;
        }
    }

    private void ResolveGrips()
    {
        leftGrip = FindChildByName(transform, "LeftGrip");
        rightGrip = FindChildByName(transform, "RightGrip");
    }

    private bool IsFacingPickup(Transform player)
    {
        Vector3 directionToPickup = transform.position - player.position;
        directionToPickup.y = 0f;
        if (directionToPickup.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        Vector3 playerForward = player.forward;
        playerForward.y = 0f;
        return Vector3.Dot(playerForward.normalized, directionToPickup.normalized) >= minimumPickupFacingDot;
    }

    private void UpdateGripTargets(Transform player)
    {
        Vector3 queryPoint = player.position + Vector3.up * gripHeight;
        Vector3 surfacePoint = pickupCollider.ClosestPoint(queryPoint);
        Vector3 surfaceNormal = queryPoint - surfacePoint;
        surfaceNormal.y = 0f;

        if (surfaceNormal.sqrMagnitude <= 0.001f)
        {
            surfaceNormal = -player.forward;
            surfaceNormal.y = 0f;
        }

        surfaceNormal.Normalize();
        Vector3 gripRight = Vector3.Cross(surfaceNormal, Vector3.up).normalized;
        Vector3 gripCenter = surfacePoint + surfaceNormal * gripSurfaceOffset;
        Quaternion gripRotation = Quaternion.LookRotation(-surfaceNormal, Vector3.up);

        leftGrip.SetPositionAndRotation(
            gripCenter - gripRight * (gripSpacing * 0.5f),
            gripRotation);
        rightGrip.SetPositionAndRotation(
            gripCenter + gripRight * (gripSpacing * 0.5f),
            gripRotation);
    }

    private static bool TryGetLiftingDuration(PlayerMovement player, out float duration)
    {
        Animator animator = player.GetComponentInChildren<Animator>();
        RuntimeAnimatorController controller = animator != null ? animator.runtimeAnimatorController : null;
        if (controller != null)
        {
            foreach (AnimationClip clip in controller.animationClips)
            {
                if (clip.name == LiftingClipName)
                {
                    duration = Mathf.Max(0.01f, clip.length);
                    return true;
                }
            }
        }

        Debug.LogError($"TirePickup could not find the '{LiftingClipName}' clip on Player's Animator Controller.", player);
        duration = 0f;
        return false;
    }

    private static Transform FindChildByName(Transform root, string targetName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
        {
            if (string.Equals(child.name, targetName, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    private static Vector3 GetLocalScaleForWorldScale(Transform parent, Vector3 targetWorldScale)
    {
        Vector3 parentScale = parent.lossyScale;
        return new Vector3(
            SafeDivide(targetWorldScale.x, parentScale.x),
            SafeDivide(targetWorldScale.y, parentScale.y),
            SafeDivide(targetWorldScale.z, parentScale.z));
    }

    private static float SafeDivide(float numerator, float denominator)
    {
        return Mathf.Approximately(denominator, 0f) ? numerator : numerator / denominator;
    }

    private void RestoreCarryPointRotation()
    {
        if (activeCarryPoint != null)
        {
            activeCarryPoint.localPosition = carryPointOriginalLocalPosition;
            activeCarryPoint.localRotation = carryPointOriginalLocalRotation;
        }

        activeCarryPoint = null;
        carrierVisual = null;
    }

    void OnValidate()
    {
        interactRadius = Mathf.Max(0f, interactRadius);
        minimumPickupFacingDot = Mathf.Clamp(minimumPickupFacingDot, -1f, 1f);
        tireMoveStartNormalizedTime = Mathf.Clamp(tireMoveStartNormalizedTime, 0f, 0.95f);
        gripHeight = Mathf.Max(0f, gripHeight);
        gripSpacing = Mathf.Max(0f, gripSpacing);
        gripSurfaceOffset = Mathf.Max(0f, gripSurfaceOffset);
        throwDuration = Mathf.Max(0.1f, throwDuration);
        throwSpin = Mathf.Max(0f, throwSpin);
    }

    void OnDisable()
    {
        RestoreCarryPointRotation();
        if (HeldTire == this)
        {
            HeldTire = null;
        }
    }
}
