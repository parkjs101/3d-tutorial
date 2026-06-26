using UnityEngine;

public readonly struct NoiseEvent
{
    public NoiseEvent(Vector3 position, float loudness, float radius, float time, GameObject source)
    {
        Position = position;
        Loudness = Mathf.Max(0f, loudness);
        Radius = Mathf.Max(0f, radius);
        Time = time;
        Source = source;
    }

    public Vector3 Position { get; }
    public float Loudness { get; }
    public float Radius { get; }
    public float Time { get; }
    public GameObject Source { get; }
}
