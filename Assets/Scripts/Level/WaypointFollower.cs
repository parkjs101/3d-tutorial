using UnityEngine;

public class WaypointFollower : KinematicMover
{
    [SerializeField] private GameObject[] waypoints;
    [SerializeField] private float speed = 1f;

    private int currentWaypointIndex;

    void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0 || waypoints[currentWaypointIndex] == null)
        {
            StopMoving();
            return;
        }

        if (Vector3.Distance(waypoints[currentWaypointIndex].transform.position, rb.position) < 0.1f)
        {
            currentWaypointIndex++;

            if (currentWaypointIndex >= waypoints.Length)
            {
                currentWaypointIndex = 0;
            }

            if (waypoints[currentWaypointIndex] == null)
            {
                StopMoving();
                return;
            }
        }

        Vector3 nextPosition = Vector3.MoveTowards(
            rb.position,
            waypoints[currentWaypointIndex].transform.position,
            speed * Time.fixedDeltaTime
        );

        MoveTo(nextPosition);
    }
}
