using UnityEngine;

public class GameClearZone : MonoBehaviour
{
    [SerializeField] private string clearMessage = "game clear";

    private bool gameCleared;

    void OnCollisionEnter(Collision collision)
    {
        TryClear(collision.collider);
    }

    void OnCollisionStay(Collision collision)
    {
        TryClear(collision.collider);
    }

    void TryClear(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (gameCleared || player == null)
        {
            return;
        }

        gameCleared = true;
        StopPlayer(player);
        Debug.Log(clearMessage);
    }

    void StopPlayer(PlayerMovement player)
    {
        player.enabled = false;

        FallGameOver fallGameOver = player.GetComponent<FallGameOver>();
        if (fallGameOver != null)
        {
            fallGameOver.enabled = false;
        }

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    void OnGUI()
    {
        if (!gameCleared)
        {
            return;
        }

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 48,
            normal = { textColor = Color.white }
        };

        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), clearMessage, style);
    }
}
