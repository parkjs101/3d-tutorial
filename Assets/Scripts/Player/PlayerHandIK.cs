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
        bool active = interaction != null && interaction.IsPushPulling;
        ikWeight = Mathf.MoveTowards(ikWeight, active ? 1f : 0f, blendSpeed * Time.deltaTime);

        PushPullBox activeBox = active ? interaction.ActiveBox : null;
        if (activeBox != resolvedBox)
        {
            ResolveGripTargets(activeBox);
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null)
        {
            return;
        }

        if (ikWeight > 0f && leftGrip != null && rightGrip != null && gripSurface != null)
        {
            UpdateGripTargets();
        }

        ApplyHandIK(AvatarIKGoal.LeftHand, leftGrip, leftHandEulerOffset);
        ApplyHandIK(AvatarIKGoal.RightHand, rightGrip, rightHandEulerOffset);
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
}
