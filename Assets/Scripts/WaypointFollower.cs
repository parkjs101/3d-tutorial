using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WaypointFollower : MonoBehaviour
{
    [SerializeField] private GameObject[] waypoints; // 두 개의 Waypoint 오브젝트를 할당
    [SerializeField] private float speed = 1f;       // 이동 속도

    private int currentWaypointIndex = 0; // 현재 목표로 하는 Waypoint 인덱스
    private Rigidbody rb;
    private Vector3 previousPosition;

    public Vector3 Velocity { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        previousPosition = rb.position;
    }

    void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0 || waypoints[currentWaypointIndex] == null)
        {
            Velocity = Vector3.zero;
            return;
        }

        // 1. 현재 목표 Waypoint와 내 위치 사이의 거리 체크
        // 거리가 아주 가까워지면(0.1f 미만) 다음 Waypoint로 타겟을 변경
        if (Vector3.Distance(waypoints[currentWaypointIndex].transform.position, rb.position) < 0.1f)
        {
            currentWaypointIndex++;
            
            // 인덱스가 배열 크기를 넘어가면 다시 0으로 초기화 (왕복/순환)
            if (currentWaypointIndex >= waypoints.Length)
            {
                currentWaypointIndex = 0;
            }

            if (waypoints[currentWaypointIndex] == null)
            {
                Velocity = Vector3.zero;
                return;
            }
        }

        // 2. 현재 목표 Waypoint를 향해 이동
        Vector3 nextPosition = Vector3.MoveTowards(
            rb.position,
            waypoints[currentWaypointIndex].transform.position, 
            speed * Time.fixedDeltaTime
        );

        Velocity = (nextPosition - previousPosition) / Time.fixedDeltaTime;
        rb.MovePosition(nextPosition);
        previousPosition = nextPosition;
    }
}
