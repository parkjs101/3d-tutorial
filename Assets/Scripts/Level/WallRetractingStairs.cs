using UnityEngine;

public class WallRetractingStairs : MonoBehaviour
{
    private enum StairState
    {
        Idle,
        Extending,
        WaitingBeforeRetract,
        Retracting,
        Complete
    }

    [Header("References")]
    [SerializeField] private RetractingStairStep[] steps;
    [SerializeField] private Transform player;
    [SerializeField] private Transform firstStep;

    [Header("Activation")]
    [SerializeField] private float activationDistance = 1.5f;
    [SerializeField] private bool startHidden = true;
    [SerializeField] private bool repeatAfterPlayerLeaves = true;

    [Header("Motion")]
    [SerializeField] private float visibleLocalX = -0.5f;
    [SerializeField] private float hiddenLocalX = -1.5f;
    [SerializeField] private float moveSpeed = 2f;

    [Header("Retract Timing")]
    [SerializeField] private float retractStartDelay = 1f;
    [SerializeField] private float retractInterval = 1f;

    private StairState state = StairState.Idle;
    private float timer;
    private int retractingStepCount;

    void Awake()
    {
        ResolveReferences();

        if (startHidden)
        {
            SetAllStepsX(hiddenLocalX);
        }
    }

    void FixedUpdate()
    {
        ResolvePlayer();

        switch (state)
        {
            case StairState.Idle:
                if (IsPlayerNearFirstStep())
                {
                    StartExtending();
                }
                break;
            case StairState.Extending:
                UpdateExtending();
                break;
            case StairState.WaitingBeforeRetract:
                UpdateWaitingBeforeRetract();
                break;
            case StairState.Retracting:
                UpdateRetracting();
                break;
            case StairState.Complete:
                UpdateComplete();
                break;
        }
    }

    private void ResolveReferences()
    {
        if (steps == null || steps.Length == 0)
        {
            steps = GetComponentsInChildren<RetractingStairStep>();
        }

        if (firstStep == null && steps != null && steps.Length > 0)
        {
            firstStep = steps[0].transform;
        }

        ResolvePlayer();
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            player = playerMovement.transform;
        }
    }

    private bool IsPlayerNearFirstStep()
    {
        return player != null
            && firstStep != null
            && Vector3.Distance(player.position, firstStep.position) <= activationDistance;
    }

    private void StartExtending()
    {
        state = StairState.Extending;
        timer = 0f;
        retractingStepCount = 0;
    }

    private void UpdateExtending()
    {
        if (!MoveAllSteps(visibleLocalX))
        {
            return;
        }

        state = StairState.WaitingBeforeRetract;
        timer = 0f;
    }

    private void UpdateWaitingBeforeRetract()
    {
        timer += Time.fixedDeltaTime;
        if (timer < retractStartDelay)
        {
            return;
        }

        state = StairState.Retracting;
        timer = 0f;
        retractingStepCount = Mathf.Min(1, steps.Length);
    }

    private void UpdateRetracting()
    {
        timer += Time.fixedDeltaTime;
        if (timer >= retractInterval && retractingStepCount < steps.Length)
        {
            timer = 0f;
            retractingStepCount++;
        }

        bool allArrived = MoveSteps(retractingStepCount, hiddenLocalX, visibleLocalX);
        if (retractingStepCount >= steps.Length && allArrived)
        {
            state = StairState.Complete;
        }
    }

    private void UpdateComplete()
    {
        if (!repeatAfterPlayerLeaves || IsPlayerNearFirstStep())
        {
            return;
        }

        state = StairState.Idle;
    }

    private bool MoveAllSteps(float targetX)
    {
        bool allArrived = true;
        for (int index = 0; index < steps.Length; index++)
        {
            if (steps[index] != null && !steps[index].MoveLocalX(targetX, moveSpeed))
            {
                allArrived = false;
            }
        }

        return allArrived;
    }

    private bool MoveSteps(int hiddenStepCount, float hiddenTargetX, float visibleTargetX)
    {
        bool allArrived = true;
        for (int index = 0; index < steps.Length; index++)
        {
            if (steps[index] == null)
            {
                continue;
            }

            float targetX = index < hiddenStepCount ? hiddenTargetX : visibleTargetX;
            if (!steps[index].MoveLocalX(targetX, moveSpeed))
            {
                allArrived = false;
            }
        }

        return allArrived;
    }

    private void SetAllStepsX(float localX)
    {
        for (int index = 0; index < steps.Length; index++)
        {
            if (steps[index] != null)
            {
                steps[index].SnapLocalX(localX);
            }
        }
    }
}
