using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HazardZone : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;

    private Rigidbody playerRigidbody;
    private Vector3 respawnPosition;
    private Quaternion respawnRotation;

    void Awake()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerMovement>();
        }

        if (player == null)
        {
            Debug.LogError("HazardZone could not find a PlayerMovement component.", this);
            enabled = false;
            return;
        }

        playerRigidbody = player.GetComponent<Rigidbody>();
        respawnPosition = player.transform.position;
        respawnRotation = player.transform.rotation;
    }

    void OnTriggerEnter(Collider other)
    {
        TryRespawn(other.GetComponentInParent<PlayerMovement>());
    }

    void OnCollisionEnter(Collision collision)
    {
        TryRespawn(collision.collider.GetComponentInParent<PlayerMovement>());
    }

    private void TryRespawn(PlayerMovement contactedPlayer)
    {
        if (contactedPlayer == null || contactedPlayer != player)
        {
            return;
        }

        PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
        if (interaction != null)
        {
            interaction.ReleaseBox();
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.position = respawnPosition;
            playerRigidbody.rotation = respawnRotation;
            return;
        }

        player.transform.SetPositionAndRotation(respawnPosition, respawnRotation);
    }
}
