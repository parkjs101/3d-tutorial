using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class WatchtowerSearchLight : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visionOrigin;
    [SerializeField] private Transform player;
    [SerializeField] private Light searchLight;
    [SerializeField] private LineRenderer rangeLineRenderer;

    [Header("Detection")]
    [SerializeField] private float range = 10f;
    [SerializeField] private float beamAngle = 120f;
    [SerializeField] private float playerTargetHeight = 1f;
    [SerializeField] private LayerMask lineOfSightMask = ~0;

    [Header("Timing")]
    [SerializeField] private bool startsActive = true;
    [SerializeField] private float activeDuration = 5f;
    [SerializeField] private float inactiveDuration = 4f;

    [Header("Light")]
    [SerializeField] private float lightIntensity = 8f;

    [Header("Visual")]
    [SerializeField] private bool generateRangeLine = true;
    [SerializeField] private bool showRangeOnlyWhenActive = true;
    [SerializeField] private Color rangeColor = new Color(1f, 0f, 0f, 0.25f);
    [SerializeField] private int edgeSampleCount = 24;
    [SerializeField] private float edgeLineWidth = 0.05f;

    private bool isActive;
    private bool isResetting;
    private float stateTimer;
    private Material rangeMaterial;

    void Awake()
    {
        ResolveReferences();
        EnsureRangeLine();
        ConfigureSearchLight();
        SetActiveState(startsActive);
    }

    void Update()
    {
        ResolvePlayer();
        ConfigureSearchLight();
        UpdateLightCycle();
        UpdateRangeLine();

        if (isActive && !isResetting && IsPlayerInSearchLight())
        {
            ResetCurrentScene();
        }
    }

    void OnValidate()
    {
        range = Mathf.Max(0f, range);
        beamAngle = Mathf.Clamp(beamAngle, 0f, 179f);
        activeDuration = Mathf.Max(0.01f, activeDuration);
        inactiveDuration = Mathf.Max(0.01f, inactiveDuration);
        lightIntensity = Mathf.Max(0f, lightIntensity);
        edgeSampleCount = Mathf.Max(3, edgeSampleCount);
        edgeLineWidth = Mathf.Max(0.001f, edgeLineWidth);
    }

    private void ResolveReferences()
    {
        if (visionOrigin == null)
        {
            visionOrigin = transform;
        }

        if (searchLight == null)
        {
            searchLight = GetComponentInChildren<Light>(includeInactive: true);
        }

        if (searchLight == null && visionOrigin != null)
        {
            searchLight = CreateSearchLight();
        }

        ResolvePlayer();
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            player = playerMovement.transform;
        }
    }

    private void UpdateLightCycle()
    {
        stateTimer += Time.deltaTime;
        float duration = isActive ? activeDuration : inactiveDuration;
        if (stateTimer < duration)
        {
            return;
        }

        SetActiveState(!isActive);
    }

    private void SetActiveState(bool active)
    {
        isActive = active;
        stateTimer = 0f;

        if (searchLight != null)
        {
            searchLight.enabled = isActive;
        }

        if (rangeLineRenderer != null)
        {
            rangeLineRenderer.enabled = !showRangeOnlyWhenActive || isActive;
        }
    }

    private bool IsPlayerInSearchLight()
    {
        if (player == null || visionOrigin == null)
        {
            return false;
        }

        Vector3 originPosition = visionOrigin.position;
        Vector3 targetPosition = player.position + Vector3.up * playerTargetHeight;
        Vector3 directionToPlayer = targetPosition - originPosition;

        if (directionToPlayer.magnitude > range)
        {
            return false;
        }

        float angleToPlayer = Vector3.Angle(visionOrigin.forward, directionToPlayer.normalized);
        if (angleToPlayer > beamAngle * 0.5f)
        {
            return false;
        }

        return HasLineOfSight(originPosition, directionToPlayer);
    }

    private bool HasLineOfSight(Vector3 originPosition, Vector3 directionToPlayer)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            originPosition,
            directionToPlayer.normalized,
            Mathf.Min(directionToPlayer.magnitude, range),
            lineOfSightMask,
            QueryTriggerInteraction.Ignore);

        if (hits.Length == 0)
        {
            return true;
        }

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        return true;
    }

    private void ResetCurrentScene()
    {
        isResetting = true;
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
            return;
        }

        SceneManager.LoadScene(activeScene.name);
    }

    private void ConfigureSearchLight()
    {
        if (searchLight == null)
        {
            return;
        }

        searchLight.type = LightType.Spot;
        searchLight.range = range;
        searchLight.spotAngle = beamAngle;
        searchLight.intensity = lightIntensity;
    }

    private Light CreateSearchLight()
    {
        GameObject lightObject = new GameObject("Search Light");
        lightObject.transform.SetParent(visionOrigin, false);
        return lightObject.AddComponent<Light>();
    }

    private void EnsureRangeLine()
    {
        if (!generateRangeLine || visionOrigin == null)
        {
            return;
        }

        if (rangeLineRenderer == null)
        {
            GameObject rangeObject = new GameObject("Search Light Range");
            rangeObject.transform.SetParent(visionOrigin, false);
            rangeLineRenderer = rangeObject.AddComponent<LineRenderer>();
        }

        rangeMaterial = CreateRangeMaterial();
        rangeLineRenderer.sharedMaterial = rangeMaterial;
        rangeLineRenderer.useWorldSpace = true;
        rangeLineRenderer.loop = true;
        rangeLineRenderer.widthMultiplier = edgeLineWidth;
        rangeLineRenderer.startColor = rangeColor;
        rangeLineRenderer.endColor = rangeColor;
        rangeLineRenderer.positionCount = edgeSampleCount;
        rangeLineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        rangeLineRenderer.receiveShadows = false;
        UpdateRangeLine();
    }

    private Material CreateRangeMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", rangeColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", rangeColor);
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 0f);
        }

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = (int)RenderQueue.Transparent;

        return material;
    }

    private void UpdateRangeLine()
    {
        if (rangeLineRenderer == null || visionOrigin == null || !rangeLineRenderer.enabled)
        {
            return;
        }

        int sampleCount = Mathf.Max(3, edgeSampleCount);
        if (rangeLineRenderer.positionCount != sampleCount)
        {
            rangeLineRenderer.positionCount = sampleCount;
        }

        rangeLineRenderer.widthMultiplier = edgeLineWidth;
        rangeLineRenderer.startColor = rangeColor;
        rangeLineRenderer.endColor = rangeColor;

        for (int index = 0; index < sampleCount; index++)
        {
            float t = index / (float)sampleCount;
            rangeLineRenderer.SetPosition(index, GetEdgePoint(t));
        }
    }

    private Vector3 GetEdgePoint(float normalizedIndex)
    {
        float radius = Mathf.Tan(beamAngle * 0.5f * Mathf.Deg2Rad) * range;
        float angle = normalizedIndex * Mathf.PI * 2f;
        Vector3 localEdgePoint = new Vector3(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius,
            range
        );

        Vector3 direction = visionOrigin.TransformDirection(localEdgePoint.normalized);
        Vector3 originPosition = visionOrigin.position;
        RaycastHit[] hits = Physics.RaycastAll(
            originPosition,
            direction,
            range,
            lineOfSightMask,
            QueryTriggerInteraction.Ignore);

        if (hits.Length == 0)
        {
            return originPosition + direction * range;
        }

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            return hit.point;
        }

        return originPosition + direction * range;
    }
}
