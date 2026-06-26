using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SoundBell : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float interactRadius = 1.8f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bellClip;

    [Header("Noise")]
    [SerializeField] private NoiseEmitter noiseEmitter;
    [SerializeField] private float bellLoudness = 5f;
    [SerializeField] private float bellNoiseRadius = 15f;

    void Awake()
    {
        if (interactionPoint == null)
        {
            interactionPoint = transform;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (noiseEmitter == null)
        {
            noiseEmitter = GetComponent<NoiseEmitter>();
        }

        if (noiseEmitter == null)
        {
            noiseEmitter = gameObject.AddComponent<NoiseEmitter>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    public bool CanInteract(Vector3 playerPosition)
    {
        return Vector3.Distance(playerPosition, interactionPoint.position) <= interactRadius;
    }

    public bool Interact(PlayerMovement player)
    {
        if (bellClip != null)
        {
            audioSource.PlayOneShot(bellClip);
        }

        noiseEmitter.Emit(interactionPoint.position, bellLoudness, bellNoiseRadius);
        return true;
    }

    public void SetHighlighted(bool highlighted)
    {
    }

    void OnDrawGizmosSelected()
    {
        Vector3 center = interactionPoint != null ? interactionPoint.position : transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, interactRadius);

        Gizmos.color = new Color(1f, 0.35f, 0.05f, 0.45f);
        Gizmos.DrawWireSphere(center, bellNoiseRadius);
    }
}
