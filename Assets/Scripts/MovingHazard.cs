using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovingHazard : MonoBehaviour
{
    [SerializeField] private float moveDistance = 2f;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private string gameOverMessage = "Game Over";

    private Rigidbody rb;
    private Vector3 startPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        startPosition = rb.position;
    }

    void FixedUpdate()
    {
        float offset = Mathf.PingPong(Time.time * moveSpeed, moveDistance * 2f) - moveDistance;
        rb.MovePosition(startPosition + Vector3.right * offset);
    }

    void OnCollisionEnter(Collision collision)
    {
        PlayerMovement player = collision.collider.GetComponentInParent<PlayerMovement>();
        if (player == null)
        {
            return;
        }

        FallGameOver gameOver = player.GetComponent<FallGameOver>();
        if (gameOver != null)
        {
            gameOver.TriggerGameOver();
            return;
        }

        Debug.Log(gameOverMessage);
    }
}
