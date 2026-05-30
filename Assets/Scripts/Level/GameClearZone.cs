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
        if (GameSession.Current != null)
        {
            GameSession.Current.ClearLevel(player, clearMessage);
            return;
        }

        StopPlayer(player);
        Debug.Log(clearMessage);
    }

    private void StopPlayer(PlayerMovement player)
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
}
