using UnityEngine;
using UnityEngine.InputSystem;

public class LittleNightmareSideCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(6f, 4f, -10f);
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float followSharpness = 8f;
    [SerializeField] private float lookAroundDistance = 2f;
    [SerializeField] private float verticalLookAroundDistance = 2f;
    [SerializeField] private float cameraLookAroundShift = 1f;
    [SerializeField] private float lookAroundSharpness = 10f;

    private Vector3 currentLookAroundOffset;
    private Vector3 currentCameraLookAroundOffset;

    void Awake()
    {
        if (target == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        UpdateLookAroundOffset();

        Vector3 desiredPosition = target.position + offset + currentCameraLookAroundOffset;
        float followT = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followT);

        Vector3 lookAtPosition = target.position + lookAtOffset + currentLookAroundOffset;
        Vector3 lookDirection = lookAtPosition - transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }

    void UpdateLookAroundOffset()
    {
        Vector3 targetLookAroundOffset = Vector3.zero;
        Vector3 targetCameraLookAroundOffset = Vector3.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.rightArrowKey.isPressed)
            {
                targetLookAroundOffset += Vector3.forward;
                targetCameraLookAroundOffset += Vector3.forward;
            }

            if (Keyboard.current.leftArrowKey.isPressed)
            {
                targetLookAroundOffset += Vector3.back;
                targetCameraLookAroundOffset += Vector3.back;
            }

            if (Keyboard.current.upArrowKey.isPressed)
            {
                targetLookAroundOffset += Vector3.up;
                targetCameraLookAroundOffset += Vector3.up;
            }

            if (Keyboard.current.downArrowKey.isPressed)
            {
                targetLookAroundOffset += Vector3.down;
                targetCameraLookAroundOffset += Vector3.down;
            }
        }

        if (targetLookAroundOffset.sqrMagnitude > 1f)
        {
            targetLookAroundOffset.Normalize();
        }

        targetLookAroundOffset = new Vector3(
            0f,
            targetLookAroundOffset.y * verticalLookAroundDistance,
            targetLookAroundOffset.z * lookAroundDistance
        );

        targetCameraLookAroundOffset *= cameraLookAroundShift;

        float lookT = 1f - Mathf.Exp(-lookAroundSharpness * Time.deltaTime);
        currentLookAroundOffset = Vector3.Lerp(currentLookAroundOffset, targetLookAroundOffset, lookT);
        currentCameraLookAroundOffset = Vector3.Lerp(currentCameraLookAroundOffset, targetCameraLookAroundOffset, lookT);
    }
}
