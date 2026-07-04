using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class VisionChasingNpc : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private float targetHeight = 1f;

    [Header("Vision")]
    [SerializeField] private float viewDistance = 8f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private float eyeHeight = 1.5f;
    [SerializeField] private LayerMask lineOfSightMask = ~0;

    [Header("Chase")]
    [SerializeField] private float destinationRefreshInterval = 0.15f;
    [SerializeField] private float approachRadius = 1.5f;
    [SerializeField] private float arrivalDistance = 0.4f;
    [SerializeField] private float navMeshSampleDistance = 2f;
    [SerializeField] private float pauseDuration = 1f;

    [Header("Debug")]
    [SerializeField] private bool drawVisionGizmos = true;
    [SerializeField] private int gizmoSegments = 16;
    [SerializeField] private bool logSightDebug;
    [SerializeField] private string lastSightStatus;
    [SerializeField] private string lastRaycastHit;

    private NavMeshAgent agent;
    private bool hasSpottedTarget;
    private bool isPaused;
    private float refreshTimer;
    private float pauseTimer;
    private Vector3 targetDestination;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        ResolveTarget();
    }

    void Update()
    {
        ResolveTarget();
        if (target == null)
        {
            SetSightStatus("No target assigned");
            return;
        }

        if (!agent.enabled)
        {
            SetSightStatus("NavMeshAgent disabled");
            return;
        }

        if (!agent.isOnNavMesh)
        {
            SetSightStatus("NPC is not on NavMesh");
            return;
        }

        bool canSeeTarget = CanSeeTarget();
        if (!canSeeTarget)
        {
            if (hasSpottedTarget)
            {
                StopChase("Lost sight");
            }

            return;
        }

        if (!hasSpottedTarget)
        {
            hasSpottedTarget = true;
            refreshTimer = destinationRefreshInterval;
        }

        if (isPaused)
        {
            UpdatePause();
            return;
        }

        if (HasReachedTargetRadius())
        {
            PauseChase();
            return;
        }

        refreshTimer += Time.deltaTime;
        if (refreshTimer < destinationRefreshInterval)
        {
            return;
        }

        refreshTimer = 0f;
        if (TrySetDestinationToTarget())
        {
            SetSightStatus("Chasing target");
        }
        else
        {
            SetSightStatus("SetDestination failed");
        }
    }

    private void ResolveTarget()
    {
        if (target != null)
        {
            return;
        }

        PlayerMovement player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            target = player.transform;
        }
    }

    private bool CanSeeTarget()
    {
        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPosition = target.position + Vector3.up * targetHeight;
        Vector3 directionToTarget = targetPosition - eyePosition;

        if (directionToTarget.magnitude > viewDistance)
        {
            SetSightStatus("Target outside view distance");
            return false;
        }

        Vector3 flatDirection = directionToTarget;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        float angleToTarget = Vector3.Angle(transform.forward, flatDirection.normalized);
        if (angleToTarget > viewAngle * 0.5f)
        {
            SetSightStatus("Target outside view angle");
            return false;
        }

        return HasLineOfSight(eyePosition, directionToTarget);
    }

    private bool HasReachedTargetRadius()
    {
        Vector3 offsetToTarget = target.position - transform.position;
        offsetToTarget.y = 0f;
        return offsetToTarget.magnitude <= approachRadius + arrivalDistance;
    }

    private Vector3 GetClosestApproachPoint(Vector3 targetPosition)
    {
        if (approachRadius <= 0f)
        {
            return targetPosition;
        }

        Vector3 directionFromTarget = transform.position - targetPosition;
        directionFromTarget.y = 0f;

        if (directionFromTarget.sqrMagnitude <= 0.001f)
        {
            return targetPosition;
        }

        return targetPosition + directionFromTarget.normalized * approachRadius;
    }

    private bool TrySetDestinationToTarget()
    {
        Vector3 desiredDestination = GetClosestApproachPoint(target.position);
        if (NavMesh.SamplePosition(desiredDestination, out NavMeshHit desiredHit, navMeshSampleDistance, agent.areaMask))
        {
            targetDestination = desiredHit.position;
            return agent.SetDestination(targetDestination);
        }

        if (NavMesh.SamplePosition(target.position, out NavMeshHit fallbackHit, navMeshSampleDistance, agent.areaMask))
        {
            targetDestination = fallbackHit.position;
            return agent.SetDestination(targetDestination);
        }

        return false;
    }

    private void PauseChase()
    {
        isPaused = true;
        pauseTimer = Mathf.Max(0f, pauseDuration);
        agent.isStopped = true;
        agent.ResetPath();
        SetSightStatus("Paused near target");
    }

    private void StopChase(string status)
    {
        hasSpottedTarget = false;
        isPaused = false;
        refreshTimer = 0f;
        agent.isStopped = false;
        agent.ResetPath();
        SetSightStatus(status);
    }

    private void UpdatePause()
    {
        pauseTimer -= Time.deltaTime;
        if (pauseTimer > 0f)
        {
            return;
        }

        isPaused = false;
        agent.isStopped = false;
        refreshTimer = destinationRefreshInterval;
        SetSightStatus("Resuming chase");
    }

    private bool HasLineOfSight(Vector3 eyePosition, Vector3 directionToTarget)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            eyePosition,
            directionToTarget.normalized,
            Mathf.Min(directionToTarget.magnitude, viewDistance),
            lineOfSightMask,
            QueryTriggerInteraction.Ignore);

        if (hits.Length == 0)
        {
            lastRaycastHit = "None";
            SetSightStatus("Target visible, no collider hit");
            return true;
        }

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            lastRaycastHit = hit.transform.name;
            if (hit.transform == target || hit.transform.IsChildOf(target))
            {
                SetSightStatus("Target visible");
                return true;
            }

            SetSightStatus("Line of sight blocked");
            return false;
        }

        lastRaycastHit = "Self only";
        SetSightStatus("Target visible, only NPC hit");
        return true;
    }

    void OnDrawGizmos()
    {
        if (!drawVisionGizmos)
        {
            return;
        }

        DrawVisionGizmos();
    }

    private void DrawVisionGizmos()
    {
        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        int segmentCount = Mathf.Max(2, gizmoSegments);
        float halfAngle = viewAngle * 0.5f;

        Gizmos.color = hasSpottedTarget ? Color.red : Color.yellow;
        Vector3 previousPoint = Vector3.zero;
        for (int index = 0; index <= segmentCount; index++)
        {
            float t = index / (float)segmentCount;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;
            Vector3 point = eyePosition + direction * viewDistance;

            Gizmos.DrawLine(eyePosition, point);
            if (index > 0)
            {
                Gizmos.DrawLine(previousPoint, point);
            }

            previousPoint = point;
        }

        Gizmos.DrawWireSphere(eyePosition, 0.12f);

        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position + Vector3.up * targetHeight;
        Gizmos.color = IsTargetInsideViewCone(targetPosition) ? Color.green : Color.gray;
        Gizmos.DrawLine(eyePosition, targetPosition);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(target.position, approachRadius);
        Gizmos.DrawWireSphere(targetDestination, arrivalDistance);
    }

    private bool IsTargetInsideViewCone(Vector3 targetPosition)
    {
        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 directionToTarget = targetPosition - eyePosition;
        if (directionToTarget.magnitude > viewDistance)
        {
            return false;
        }

        Vector3 flatDirection = directionToTarget;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        return Vector3.Angle(transform.forward, flatDirection.normalized) <= viewAngle * 0.5f;
    }

    private void SetSightStatus(string status)
    {
        if (lastSightStatus == status)
        {
            return;
        }

        lastSightStatus = status;
        if (logSightDebug)
        {
            Debug.Log($"{name}: {lastSightStatus} (hit: {lastRaycastHit})", this);
        }
    }
}
