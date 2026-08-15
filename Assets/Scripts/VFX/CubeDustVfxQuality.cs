using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class CubeDustVfxQuality : MonoBehaviour
{
    [Header("Coarse Dust")]
    [SerializeField] private Vector2Int coarseBurstCount = new Vector2Int(28, 40);
    [SerializeField] private Vector2 coarseLifetime = new Vector2(0.9f, 1.4f);
    [SerializeField] private Vector2 coarseSpeed = new Vector2(0.8f, 1.8f);

    [Header("Fine Dust")]
    [SerializeField] private Vector2Int fineBurstCount = new Vector2Int(90, 140);
    [SerializeField] private Vector2 fineLifetime = new Vector2(2.2f, 3f);
    [SerializeField] private Vector2 fineSpeed = new Vector2(0.2f, 0.7f);

    private ParticleSystem rootDust;
    private ParticleSystem fineDust;

    public void Configure(Vector3 emissionSize)
    {
        rootDust = GetComponent<ParticleSystem>();
        ConfigureCoarseDust(rootDust, emissionSize);
        ConfigureFineDust(GetOrCreateFineDust(), emissionSize);
        Destroy(gameObject, fineLifetime.y + 0.5f);
    }

    private void ConfigureCoarseDust(ParticleSystem system, Vector3 emissionSize)
    {
        ConfigureMainModule(system, coarseLifetime, coarseSpeed, new Vector2(0.12f, 0.22f));
        SetBurst(system, coarseBurstCount);
        ConfigureShape(system, emissionSize * 0.65f);
        ConfigureColorAndSize(system, new Color(0.42f, 0.34f, 0.26f, 0.65f));

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.45f;
        noise.frequency = 0.65f;
        noise.scrollSpeed = 0.25f;
    }

    private void ConfigureFineDust(ParticleSystem system, Vector3 emissionSize)
    {
        ConfigureMainModule(system, fineLifetime, fineSpeed, new Vector2(0.025f, 0.07f));
        SetBurst(system, fineBurstCount);
        ConfigureShape(system, emissionSize * 0.8f);
        ConfigureColorAndSize(system, new Color(0.5f, 0.46f, 0.4f, 0.48f));

        ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.28f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.18f;
    }

    private ParticleSystem GetOrCreateFineDust()
    {
        if (fineDust != null)
        {
            return fineDust;
        }

        Transform existing = transform.Find("Fine Dust");
        if (existing != null)
        {
            fineDust = existing.GetComponent<ParticleSystem>();
            return fineDust;
        }

        GameObject fineDustObject = new GameObject("Fine Dust");
        fineDustObject.transform.SetParent(transform, false);
        fineDust = fineDustObject.AddComponent<ParticleSystem>();

        ParticleSystemRenderer rootRenderer = rootDust.GetComponent<ParticleSystemRenderer>();
        ParticleSystemRenderer fineRenderer = fineDust.GetComponent<ParticleSystemRenderer>();
        fineRenderer.sharedMaterial = rootRenderer.sharedMaterial;
        fineRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        return fineDust;
    }

    private static void ConfigureMainModule(
        ParticleSystem system,
        Vector2 lifetime,
        Vector2 speed,
        Vector2 size)
    {
        ParticleSystem.MainModule main = system.main;
        main.duration = 0.25f;
        main.loop = false;
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.None;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 180;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startColor = Color.white;
    }

    private static void SetBurst(ParticleSystem system, Vector2Int count)
    {
        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)count.x, (short)count.y)
        });
    }

    private static void ConfigureShape(ParticleSystem system, Vector3 size)
    {
        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = Vector3.Max(size, Vector3.one * 0.1f);
    }

    private static void ConfigureColorAndSize(ParticleSystem system, Color dustColor)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(dustColor, 0f),
                new GradientColorKey(dustColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(dustColor.a, 0.12f),
                new GradientAlphaKey(0f, 1f)
            });

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0.35f));
    }
}
