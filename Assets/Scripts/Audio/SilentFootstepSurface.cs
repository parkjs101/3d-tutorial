using UnityEngine;

public class SilentFootstepSurface : MonoBehaviour
{
    [SerializeField] private bool muteFootstepAudio = true;
    [SerializeField] private bool muteFootstepNoise = true;

    public bool MuteFootstepAudio => muteFootstepAudio;
    public bool MuteFootstepNoise => muteFootstepNoise;
}
