using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NoiseChasingNpc : MonoBehaviour
{
    [Header("Hearing")]
    [SerializeField] private float hearingMultiplier = 1f;
    [SerializeField] private float equalLoudnessTolerance = 0.05f;
    [SerializeField] private float memoryDuration = 5f;

    [Header("Navigation")]
    [SerializeField] private float approachRadius = 1.5f;
    [SerializeField] private float arrivalDistance = 0.4f;
    [SerializeField] private float navMeshSampleDistance = 2f;
    [SerializeField] private float pauseDuration = 1f;

    private NavMeshAgent agent;
    private bool hasTargetNoise;
    private bool isPaused;
    private NoiseEvent targetNoise;
    private Vector3 targetDestination;
    private float pauseTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void OnEnable()
    {
        NoiseManager.NoiseEmitted += HandleNoiseEmitted;
    }

    void OnDisable()
    {
        NoiseManager.NoiseEmitted -= HandleNoiseEmitted;
    }

    void Update()
    {
        if (!hasTargetNoise)
        {
            return;
        }

        if (Time.time - targetNoise.Time > memoryDuration)
        {
            hasTargetNoise = false;
            return;
        }

        if (isPaused)
        {
            UpdatePause();
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= arrivalDistance)
        {
            PauseChase();
        }
    }

    private void HandleNoiseEmitted(NoiseEvent noiseEvent)
    {
        if (!agent.enabled || !agent.isOnNavMesh || !CanHear(noiseEvent) || !ShouldUseNoise(noiseEvent))
        {
            return;
        }

        Vector3 desiredDestination = GetClosestApproachPoint(noiseEvent.Position);
        if (!TrySetDestination(desiredDestination, noiseEvent.Position))
        {
            return;
        }

        isPaused = false;
        agent.isStopped = false;

        hasTargetNoise = true;
        targetNoise = noiseEvent;
    }

    private bool CanHear(NoiseEvent noiseEvent)
    {
        float hearingRadius = noiseEvent.Radius * Mathf.Max(0f, hearingMultiplier);
        return Vector3.Distance(transform.position, noiseEvent.Position) <= hearingRadius;
    }

    private bool ShouldUseNoise(NoiseEvent noiseEvent)
    {
        if (!hasTargetNoise)
        {
            return true;
        }

        if (noiseEvent.Loudness > targetNoise.Loudness + equalLoudnessTolerance)
        {
            return true;
        }

        bool similarLoudness = Mathf.Abs(noiseEvent.Loudness - targetNoise.Loudness) <= equalLoudnessTolerance;
        return similarLoudness && noiseEvent.Time > targetNoise.Time;
    }

    private Vector3 GetClosestApproachPoint(Vector3 noisePosition)
    {
        if (approachRadius <= 0f)
        {
            return noisePosition;
        }

        Vector3 directionFromNoise = transform.position - noisePosition;
        directionFromNoise.y = 0f;

        if (directionFromNoise.sqrMagnitude <= 0.001f)
        {
            return noisePosition;
        }

        return noisePosition + directionFromNoise.normalized * approachRadius;
    }

    private bool TrySetDestination(Vector3 desiredDestination, Vector3 fallbackDestination)
    {
        if (NavMesh.SamplePosition(desiredDestination, out NavMeshHit desiredHit, navMeshSampleDistance, agent.areaMask))
        {
            targetDestination = desiredHit.position;
            agent.SetDestination(targetDestination);
            return true;
        }

        if (NavMesh.SamplePosition(fallbackDestination, out NavMeshHit fallbackHit, navMeshSampleDistance, agent.areaMask))
        {
            targetDestination = fallbackHit.position;
            agent.SetDestination(targetDestination);
            return true;
        }

        return false;
    }

    private void PauseChase()
    {
        isPaused = true;
        pauseTimer = Mathf.Max(0f, pauseDuration);
        agent.isStopped = true;
        agent.ResetPath();
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

        if (Time.time - targetNoise.Time > memoryDuration)
        {
            hasTargetNoise = false;
            return;
        }

        Vector3 desiredDestination = GetClosestApproachPoint(targetNoise.Position);
        if (!TrySetDestination(desiredDestination, targetNoise.Position))
        {
            hasTargetNoise = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!hasTargetNoise)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetNoise.Position, approachRadius);
        Gizmos.DrawWireSphere(targetDestination, arrivalDistance);
        Gizmos.DrawLine(transform.position, targetDestination);
    }
}
