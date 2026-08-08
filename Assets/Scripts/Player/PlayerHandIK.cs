using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerHandIK : MonoBehaviour
{
    [Header("Dynamic Grip")]
    [SerializeField] private float gripHeight = 1.15f;
    [SerializeField] private float gripSpacing = 0.45f;
    [SerializeField] private float surfaceOffset = 0.03f;

    [Header("Hand Rotation")]
    [SerializeField] private Vector3 leftHandEulerOffset;
    [SerializeField] private Vector3 rightHandEulerOffset;

    [Header("Blend")]
    [SerializeField] private float blendSpeed = 10f;

    private Animator animator;
    private PlayerInteraction interaction;
    private PushPullBox resolvedBox;
    private TirePickup resolvedTire;
    private CandlePickup resolvedCandle;
    private KnockDownInteractable resolvedKnockDown;
    private Transform leftGrip;
    private Transform rightGrip;
    private Collider gripSurface;
    private float ikWeight;

    void Awake()
    {
        animator = GetComponent<Animator>();
        interaction = GetComponentInParent<PlayerInteraction>();
    }

    void Update()
    {
        TirePickup activeTire = TirePickup.HeldTire;
        bool boxActive = interaction != null && interaction.IsPushPulling;
        CandlePickup activeCandle = CandlePickup.ReachingCandle;
        KnockDownInteractable activeKnockDown = KnockDownInteractable.ActivePull;
        bool active = boxActive || activeTire != null || activeCandle != null || activeKnockDown != null;
        ikWeight = Mathf.MoveTowards(ikWeight, active ? 1f : 0f, blendSpeed * Time.deltaTime);

        if (activeTire != resolvedTire)
        {
            resolvedTire = activeTire;
            resolvedBox = null;
            resolvedCandle = null;
            resolvedKnockDown = null;
            leftGrip = activeTire != null ? activeTire.LeftGrip : null;
            rightGrip = activeTire != null ? activeTire.RightGrip : null;
            gripSurface = null;
        }

        PushPullBox activeBox = boxActive && activeTire == null && activeCandle == null && activeKnockDown == null
            ? interaction.ActiveBox
            : null;
        if (activeBox != resolvedBox)
        {
            resolvedTire = null;
            resolvedCandle = null;
            resolvedKnockDown = null;
            ResolveGripTargets(activeBox);
        }

        CandlePickup candle = activeTire == null && !boxActive && activeKnockDown == null
            ? activeCandle
            : null;
        if (candle != resolvedCandle)
        {
            resolvedCandle = candle;
            resolvedTire = null;
            resolvedBox = null;
            resolvedKnockDown = null;
            leftGrip = null;
            rightGrip = candle != null ? candle.HandIkTarget : null;
            gripSurface = null;
        }

        KnockDownInteractable knockDown = activeTire == null && !boxActive && activeCandle == null
            ? activeKnockDown
            : null;
        if (knockDown != resolvedKnockDown)
        {
            resolvedKnockDown = knockDown;
            resolvedTire = null;
            resolvedBox = null;
            resolvedCandle = null;
            leftGrip = knockDown != null ? knockDown.GripPoint : null;
            rightGrip = knockDown != null ? knockDown.GripPoint : null;
            gripSurface = null;
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null)
        {
            return;
        }

        if (ikWeight > 0f && resolvedBox != null && leftGrip != null && rightGrip != null && gripSurface != null)
        {
            UpdateGripTargets();
        }

        if (resolvedKnockDown != null)
        {
            ApplyKnockDownHandIK();
            return;
        }

        ApplyHandIK(AvatarIKGoal.LeftHand, leftGrip, leftHandEulerOffset);
        if (resolvedCandle != null && resolvedCandle.IsPlacing)
        {
            ApplyHandIK(AvatarIKGoal.RightHand, rightGrip, resolvedCandle.HandIkRotation);
        }
        else
        {
            Vector3 activeRightHandOffset = resolvedCandle != null ? Vector3.zero : rightHandEulerOffset;
            ApplyHandIK(AvatarIKGoal.RightHand, rightGrip, activeRightHandOffset);
        }
    }

    private void ResolveGripTargets(PushPullBox activeBox)
    {
        resolvedBox = activeBox;
        leftGrip = null;
        rightGrip = null;
        gripSurface = null;

        if (resolvedBox == null)
        {
            return;
        }

        foreach (Transform child in resolvedBox.GetComponentsInChildren<Transform>(includeInactive: true))
        {
            if (child.name == "LeftGrip")
            {
                leftGrip = child;
            }
            else if (child.name == "RightGrip")
            {
                rightGrip = child;
            }
        }

        gripSurface = FindClosestSurface(resolvedBox.GetComponentsInChildren<Collider>());
        if (leftGrip == null || rightGrip == null || gripSurface == null)
        {
            Debug.LogWarning("PushPullBox requires LeftGrip, RightGrip, and a Collider for hand IK.", resolvedBox);
        }
    }

    private Collider FindClosestSurface(Collider[] colliders)
    {
        Collider closest = null;
        float closestDistance = float.MaxValue;
        Vector3 queryPoint = interaction.transform.position + Vector3.up * gripHeight;

        foreach (Collider candidate in colliders)
        {
            if (candidate == null || candidate.isTrigger)
            {
                continue;
            }

            float distance = (candidate.ClosestPoint(queryPoint) - queryPoint).sqrMagnitude;
            if (distance < closestDistance)
            {
                closest = candidate;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private void UpdateGripTargets()
    {
        Vector3 queryPoint = interaction.transform.position + Vector3.up * gripHeight;
        Vector3 surfacePoint = gripSurface.ClosestPoint(queryPoint);
        Vector3 surfaceNormal = queryPoint - surfacePoint;
        surfaceNormal.y = 0f;

        if (surfaceNormal.sqrMagnitude <= 0.001f)
        {
            surfaceNormal = interaction.transform.position - gripSurface.bounds.center;
            surfaceNormal.y = 0f;
        }

        surfaceNormal.Normalize();
        Vector3 gripRight = Vector3.Cross(surfaceNormal, Vector3.up).normalized;
        Vector3 gripCenter = surfacePoint + surfaceNormal * surfaceOffset;
        Quaternion gripRotation = Quaternion.LookRotation(-surfaceNormal, Vector3.up);

        leftGrip.SetPositionAndRotation(
            gripCenter - gripRight * (gripSpacing * 0.5f),
            gripRotation);
        rightGrip.SetPositionAndRotation(
            gripCenter + gripRight * (gripSpacing * 0.5f),
            gripRotation);
    }

    private void ApplyHandIK(AvatarIKGoal hand, Transform target, Vector3 eulerOffset)
    {
        float weight = target != null ? ikWeight : 0f;
        animator.SetIKPositionWeight(hand, weight);
        animator.SetIKRotationWeight(hand, weight);

        if (target == null)
        {
            return;
        }

        animator.SetIKPosition(hand, target.position);
        animator.SetIKRotation(hand, target.rotation * Quaternion.Euler(eulerOffset));
    }

    private void ApplyHandIK(AvatarIKGoal hand, Transform target, Quaternion targetRotation)
    {
        float weight = target != null ? ikWeight : 0f;
        animator.SetIKPositionWeight(hand, weight);
        animator.SetIKRotationWeight(hand, weight);

        if (target != null)
        {
            animator.SetIKPosition(hand, target.position);
            animator.SetIKRotation(hand, targetRotation);
        }
    }

    private void ApplyKnockDownHandIK()
    {
        Transform gripPoint = resolvedKnockDown.GripPoint;
        if (gripPoint == null)
        {
            return;
        }

        float halfSpacing = resolvedKnockDown.HandSpacing * 0.5f;
        Quaternion leftRotation = gripPoint.rotation *
                                  Quaternion.Euler(resolvedKnockDown.LeftHandEulerAngles);
        Quaternion rightRotation = gripPoint.rotation *
                                   Quaternion.Euler(resolvedKnockDown.RightHandEulerAngles);

        ApplyHandIK(
            AvatarIKGoal.LeftHand,
            gripPoint.position - gripPoint.right * halfSpacing,
            leftRotation);
        ApplyHandIK(
            AvatarIKGoal.RightHand,
            gripPoint.position + gripPoint.right * halfSpacing,
            rightRotation);
    }

    private void ApplyHandIK(AvatarIKGoal hand, Vector3 position, Quaternion rotation)
    {
        animator.SetIKPositionWeight(hand, ikWeight);
        animator.SetIKRotationWeight(hand, ikWeight);
        animator.SetIKPosition(hand, position);
        animator.SetIKRotation(hand, rotation);
    }
}
