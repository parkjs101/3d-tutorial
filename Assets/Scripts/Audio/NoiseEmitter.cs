using UnityEngine;

public class NoiseEmitter : MonoBehaviour
{
    [SerializeField] private float loudness = 1f;
    [SerializeField] private float radius = 5f;

    public float Loudness => loudness;
    public float Radius => radius;

    public void Emit()
    {
        Emit(transform.position, loudness, radius);
    }

    public void Emit(Vector3 position, float eventLoudness, float eventRadius)
    {
        NoiseManager.Emit(new NoiseEvent(
            position,
            eventLoudness,
            eventRadius,
            Time.time,
            gameObject
        ));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.1f, 0.7f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
