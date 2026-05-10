using System.Collections;
using UnityEngine;

public class FallGameOver : MonoBehaviour
{
    [SerializeField] private float fallLimitY = -8f;
    [SerializeField] private float restartDelay = 1f;
    [SerializeField] private string gameOverMessage = "game over";

    private Rigidbody rb;
    private PlayerMovement playerMovement;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool originalIsKinematic;
    private bool gameOver;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        startPosition = transform.position;
        startRotation = transform.rotation;

        if (rb != null)
        {
            originalIsKinematic = rb.isKinematic;
        }
    }

    void Update()
    {
        if (!gameOver && transform.position.y < fallLimitY)
        {
            TriggerGameOver();
        }
    }

    public void TriggerGameOver()
    {
        if (gameOver)
        {
            return;
        }

        StartCoroutine(HandleGameOver());
    }

    IEnumerator HandleGameOver()
    {
        gameOver = true;
        Debug.Log(gameOverMessage);

        if (playerMovement != null)
        {
            playerMovement.EnterDeadState();
            playerMovement.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        yield return new WaitForSecondsRealtime(restartDelay);

        transform.SetPositionAndRotation(startPosition, startRotation);

        if (rb != null)
        {
            rb.isKinematic = originalIsKinematic;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (playerMovement != null)
        {
            playerMovement.ResetAfterRespawn();
            playerMovement.enabled = true;
        }

        gameOver = false;
    }

    void OnGUI()
    {
        if (!gameOver)
        {
            return;
        }

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 48,
            normal = { textColor = Color.white }
        };

        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), gameOverMessage, style);
    }
}
