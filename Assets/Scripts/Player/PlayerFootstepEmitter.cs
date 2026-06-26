using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerFootstepEmitter : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("Timing")]
    [SerializeField] private float walkStepInterval = 0.55f;
    [SerializeField] private float runStepInterval = 0.35f;
    [SerializeField] private float runSpeedThreshold = 3f;
    [SerializeField] private float minimumMoveSpeed = 0.1f;

    [Header("Noise")]
    [SerializeField] private float walkLoudness = 1f;
    [SerializeField] private float walkNoiseRadius = 4f;
    [SerializeField] private float runLoudness = 2f;
    [SerializeField] private float runNoiseRadius = 7f;

    private PlayerMovement playerMovement;
    private Rigidbody playerRigidbody;
    private NoiseEmitter noiseEmitter;
    private float stepTimer;
    private int lastClipIndex = -1;

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerRigidbody = GetComponent<Rigidbody>();
        noiseEmitter = GetComponent<NoiseEmitter>();

        if (noiseEmitter == null)
        {
            noiseEmitter = gameObject.AddComponent<NoiseEmitter>();
        }

        audioSource = ResolveAudioSource();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    void Update()
    {
        if (!ShouldPlayFootsteps(out float horizontalSpeed, out bool isRunning))
        {
            stepTimer = 0f;
            return;
        }

        float interval = isRunning ? runStepInterval : walkStepInterval;
        stepTimer += Time.deltaTime;

        if (stepTimer < interval)
        {
            return;
        }

        stepTimer = 0f;
        PlayFootstep(isRunning);
    }

    private bool ShouldPlayFootsteps(out float horizontalSpeed, out bool isRunning)
    {
        horizontalSpeed = GetHorizontalSpeed();
        isRunning = horizontalSpeed >= runSpeedThreshold;

        if (horizontalSpeed < minimumMoveSpeed)
        {
            return false;
        }

        return playerMovement.CurrentState == PlayerState.Walk;
    }

    private float GetHorizontalSpeed()
    {
        Vector3 velocity = playerRigidbody.linearVelocity;
        velocity.y = 0f;
        return velocity.magnitude;
    }

    private AudioSource ResolveAudioSource()
    {
        if (audioSource != null)
        {
            return audioSource;
        }

        AudioSource existingAudioSource = GetComponent<AudioSource>();
        return existingAudioSource != null ? existingAudioSource : gameObject.AddComponent<AudioSource>();
    }

    private void PlayFootstep(bool isRunning)
    {
        AudioClip clip = GetRandomFootstepClip();
        if (clip != null)
        {
            audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
            audioSource.PlayOneShot(clip);
        }

        float loudness = isRunning ? runLoudness : walkLoudness;
        float radius = isRunning ? runNoiseRadius : walkNoiseRadius;
        noiseEmitter.Emit(transform.position, loudness, radius);
    }

    private AudioClip GetRandomFootstepClip()
    {
        if (footstepClips == null || footstepClips.Length == 0)
        {
            return null;
        }

        if (footstepClips.Length == 1)
        {
            lastClipIndex = 0;
            return footstepClips[0];
        }

        int clipIndex = Random.Range(0, footstepClips.Length);
        if (clipIndex == lastClipIndex)
        {
            clipIndex = (clipIndex + 1) % footstepClips.Length;
        }

        lastClipIndex = clipIndex;
        return footstepClips[clipIndex];
    }
}
