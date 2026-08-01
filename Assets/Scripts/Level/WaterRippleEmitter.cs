using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterRippleEmitter : MonoBehaviour
{
    [SerializeField] private WaterRipple ripplePrefab;
    [SerializeField] private Transform waterSurface;
    [SerializeField, Min(0f)] private float minimumImpactSpeed = 0.5f;
    [SerializeField, Min(0f)] private float surfaceOffset = 0.01f;
    [SerializeField, Min(0.1f)] private float minimumSizeMultiplier = 0.8f;
    [SerializeField, Min(0.1f)] private float maximumSizeMultiplier = 1.8f;

    void Reset()
    {
        Collider volumeCollider = GetComponent<Collider>();
        volumeCollider.isTrigger = true;
        waterSurface = transform.parent;
    }

    void Awake()
    {
        if (waterSurface == null)
        {
            waterSurface = transform.parent;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null || ripplePrefab == null || waterSurface == null)
        {
            return;
        }

        float impactSpeed = Mathf.Max(0f, -body.linearVelocity.y);
        if (impactSpeed < minimumImpactSpeed)
        {
            return;
        }

        Vector3 impactPosition = other.ClosestPoint(body.position);
        impactPosition.y = waterSurface.position.y + surfaceOffset;

        WaterRipple ripple = Instantiate(ripplePrefab, impactPosition, Quaternion.identity);
        float sizeMultiplier = Mathf.Lerp(
            minimumSizeMultiplier,
            maximumSizeMultiplier,
            Mathf.InverseLerp(minimumImpactSpeed, minimumImpactSpeed * 4f, impactSpeed));
        ripple.Initialize(sizeMultiplier);
    }

    void OnValidate()
    {
        minimumImpactSpeed = Mathf.Max(0f, minimumImpactSpeed);
        surfaceOffset = Mathf.Max(0f, surfaceOffset);
        minimumSizeMultiplier = Mathf.Max(0.1f, minimumSizeMultiplier);
        maximumSizeMultiplier = Mathf.Max(minimumSizeMultiplier, maximumSizeMultiplier);
    }
}
