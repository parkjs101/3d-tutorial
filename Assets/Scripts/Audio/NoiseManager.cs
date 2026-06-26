using System;

public static class NoiseManager
{
    public static event Action<NoiseEvent> NoiseEmitted;

    public static void Emit(NoiseEvent noiseEvent)
    {
        NoiseEmitted?.Invoke(noiseEvent);
    }
}
