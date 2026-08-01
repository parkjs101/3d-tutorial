using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WaterRipple : MonoBehaviour
{
    [SerializeField, Min(3)] private int segments = 48;
    [SerializeField, Min(0f)] private float startRadius = 0.15f;
    [SerializeField, Min(0.01f)] private float lifetime = 1f;

    private LineRenderer lineRenderer;
    private float elapsed;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    public void Initialize(float sizeMultiplier)
    {
        startRadius *= sizeMultiplier;
        DrawRing(startRadius);
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void DrawRing(float radius)
    {
        lineRenderer.positionCount = segments;

        for (int index = 0; index < segments; index++)
        {
            float angle = index * Mathf.PI * 2f / (segments - 1);
            lineRenderer.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

    void OnValidate()
    {
        segments = Mathf.Max(3, segments);
        startRadius = Mathf.Max(0f, startRadius);
        lifetime = Mathf.Max(0.01f, lifetime);
    }
}
