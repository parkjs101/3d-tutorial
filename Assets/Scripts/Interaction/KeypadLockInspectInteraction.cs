using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;

public class KeypadLockInspectInteraction : MonoBehaviour, IInteractable
{
    public static bool IsAnyInspectViewOpen { get; private set; }

    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private Transform inspectTarget;
    [SerializeField] private Door targetDoor;
    [SerializeField] private float interactRadius = 1.4f;

    [Header("Inspect Camera")]
    [SerializeField] private Vector3 localFrontDirection = Vector3.forward;
    [SerializeField] private float cameraDistanceMultiplier = 2.2f;
    [SerializeField] private float cameraFieldOfView = 24f;
    [SerializeField] private Color cameraBackgroundColor = new Color(0.02f, 0.018f, 0.014f, 1f);

    [Header("Preview Object")]
    [SerializeField] private int previewLayer = 30;
    [SerializeField] private Vector3 previewPosition = new Vector3(10000f, 10000f, 10000f);
    [SerializeField] private Vector3 previewRotationEuler = Vector3.zero;

    [Header("UI")]
    [SerializeField] private int renderTextureSize = 768;
    [SerializeField] private Vector2 inspectImageSize = new Vector2(620f, 620f);
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.82f);
    [SerializeField] private Color frameColor = new Color(0.78f, 0.7f, 0.46f, 1f);

    private Canvas inspectCanvas;
    private RawImage inspectImage;
    private KeypadPasswordInputDisplay passwordInputDisplay;
    private Camera inspectCamera;
    private Light inspectLight;
    private RenderTexture inspectTexture;
    private GameObject previewObject;
    private PlayerMovement activePlayer;
    private Rigidbody activePlayerRigidbody;
    private Renderer[] renderers;
    private Vector3 originalScale;
    private bool isOpen;
    private bool isHighlighted;

    void Awake()
    {
        if (interactionPoint == null)
        {
            interactionPoint = transform;
        }

        if (inspectTarget == null)
        {
            inspectTarget = transform;
        }

        renderers = GetComponentsInChildren<Renderer>();
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (!isOpen || !WasClosePressed())
        {
            return;
        }

        CloseInspectView();
    }

    public bool CanInteract(Vector3 playerPosition)
    {
        if (isOpen || interactionPoint == null)
        {
            return false;
        }

        return Vector3.Distance(playerPosition, interactionPoint.position) <= interactRadius;
    }

    public bool Interact(PlayerMovement player)
    {
        if (player == null || !CanInteract(player.transform.position))
        {
            return false;
        }

        OpenInspectView(player);
        return true;
    }

    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted)
        {
            return;
        }

        isHighlighted = highlighted;
        ApplyHighlight();
    }

    private void OpenInspectView(PlayerMovement player)
    {
        EnsureUi();
        EnsurePreviewObject();
        ConfigureInspectCamera();

        activePlayer = player;
        activePlayerRigidbody = player.GetComponent<Rigidbody>();

        if (activePlayerRigidbody != null)
        {
            activePlayerRigidbody.linearVelocity = Vector3.zero;
        }

        activePlayer.enabled = false;
        inspectCanvas.gameObject.SetActive(true);
        inspectCamera.gameObject.SetActive(true);
        previewObject.SetActive(true);
        passwordInputDisplay.ResetInput();
        IsAnyInspectViewOpen = true;
        isOpen = true;
        SetHighlighted(false);
    }

    private void CloseInspectView()
    {
        isOpen = false;
        IsAnyInspectViewOpen = false;

        if (inspectCanvas != null)
        {
            inspectCanvas.gameObject.SetActive(false);
        }

        if (inspectCamera != null)
        {
            inspectCamera.gameObject.SetActive(false);
        }

        if (previewObject != null)
        {
            previewObject.SetActive(false);
        }

        if (activePlayer != null)
        {
            activePlayer.enabled = true;
        }

        activePlayer = null;
        activePlayerRigidbody = null;
    }

    private void EnsureUi()
    {
        if (inspectCanvas != null)
        {
            return;
        }

        EnsureEventSystem();
        CreateRenderTexture();
        CreateInspectCamera();
        CreateCanvas();
        CreatePanel();
        inspectCanvas.gameObject.SetActive(false);
        inspectCamera.gameObject.SetActive(false);
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private void CreateRenderTexture()
    {
        inspectTexture = new RenderTexture(renderTextureSize, renderTextureSize, 16, RenderTextureFormat.ARGB32)
        {
            name = "Keypad Lock Inspect Texture"
        };
        inspectTexture.Create();
    }

    private void CreateInspectCamera()
    {
        GameObject cameraObject = new GameObject("Keypad Lock Inspect Camera");
        inspectCamera = cameraObject.AddComponent<Camera>();
        inspectCamera.clearFlags = CameraClearFlags.SolidColor;
        inspectCamera.backgroundColor = cameraBackgroundColor;
        inspectCamera.cullingMask = 1 << previewLayer;
        inspectCamera.fieldOfView = cameraFieldOfView;
        inspectCamera.nearClipPlane = 0.01f;
        inspectCamera.farClipPlane = 50f;
        inspectCamera.targetTexture = inspectTexture;

        inspectLight = cameraObject.AddComponent<Light>();
        inspectLight.type = LightType.Directional;
        inspectLight.intensity = 2.4f;
    }

    private void CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Keypad Lock Inspect Canvas");
        inspectCanvas = canvasObject.AddComponent<Canvas>();
        inspectCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        inspectCanvas.sortingOrder = 80;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
    }

    private void CreatePanel()
    {
        RectTransform panel = CreateRect("Inspect Panel", inspectCanvas.transform);
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;

        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = panelColor;

        RectTransform frame = CreateRect("Lock Frame", panel);
        frame.anchorMin = new Vector2(0.5f, 0.5f);
        frame.anchorMax = new Vector2(0.5f, 0.5f);
        frame.pivot = new Vector2(0.5f, 0.5f);
        frame.sizeDelta = inspectImageSize + new Vector2(36f, 36f);
        frame.anchoredPosition = Vector2.zero;

        Image frameImage = frame.gameObject.AddComponent<Image>();
        frameImage.color = frameColor;

        RectTransform imageRect = CreateRect("Lock View", frame);
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = new Vector2(18f, 18f);
        imageRect.offsetMax = new Vector2(-18f, -18f);

        inspectImage = imageRect.gameObject.AddComponent<RawImage>();
        inspectImage.texture = inspectTexture;
        passwordInputDisplay = KeypadPasswordInputDisplay.Create(panel);
        passwordInputDisplay.Initialize(OpenTargetDoor);

        CreateCloseButton(panel);
        CreateHintText(panel);
    }

    private void OpenTargetDoor()
    {
        if (targetDoor == null)
        {
            Debug.LogError("Keypad password accepted, but no target door is assigned.", this);
            return;
        }

        targetDoor.Open();
    }

    private void CreateCloseButton(RectTransform parent)
    {
        RectTransform buttonRect = CreateRect("Close Button", parent);
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.sizeDelta = new Vector2(64f, 64f);
        buttonRect.anchoredPosition = new Vector2(-32f, -32f);

        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.color = new Color(0.12f, 0.1f, 0.08f, 0.95f);

        Button closeButton = buttonRect.gameObject.AddComponent<Button>();
        closeButton.onClick.AddListener(CloseInspectView);

        RectTransform textRect = CreateRect("Close Label", buttonRect);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text label = textRect.gameObject.AddComponent<Text>();
        label.text = "X";
        label.alignment = TextAnchor.MiddleCenter;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 32;
        label.color = Color.white;
    }

    private void CreateHintText(RectTransform parent)
    {
        RectTransform hintRect = CreateRect("Inspect Hint", parent);
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.sizeDelta = new Vector2(560f, 48f);
        hintRect.anchoredPosition = new Vector2(0f, 48f);

        Text hint = hintRect.gameObject.AddComponent<Text>();
        hint.text = "ESC";
        hint.alignment = TextAnchor.MiddleCenter;
        hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hint.fontSize = 28;
        hint.color = new Color(0.82f, 0.78f, 0.66f, 1f);
    }

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject rectObject = new GameObject(objectName);
        rectObject.transform.SetParent(parent, false);
        return rectObject.AddComponent<RectTransform>();
    }

    private void ConfigureInspectCamera()
    {
        EnsurePreviewObject();

        Bounds bounds = CalculateTargetBounds();
        Vector3 center = bounds.center;
        float radius = Mathf.Max(bounds.extents.magnitude, 0.25f);
        Quaternion previewRotation = GetPreviewRotation();
        Vector3 viewDirection = previewRotation * localFrontDirection.normalized;

        if (viewDirection.sqrMagnitude < 0.001f)
        {
            viewDirection = Vector3.forward;
        }

        float distance = radius * cameraDistanceMultiplier;
        inspectCamera.transform.position = center + viewDirection * distance;
        Vector3 lookDirection = center - inspectCamera.transform.position;
        Vector3 upDirection = previewRotation * Vector3.up;

        if (Mathf.Abs(Vector3.Dot(lookDirection.normalized, upDirection.normalized)) > 0.98f)
        {
            upDirection = Vector3.up;
        }

        inspectCamera.transform.rotation = Quaternion.LookRotation(lookDirection, upDirection);

        if (inspectLight != null)
        {
            inspectLight.transform.rotation = inspectCamera.transform.rotation;
        }
    }

    private Bounds CalculateTargetBounds()
    {
        Transform boundsTarget = previewObject != null ? previewObject.transform : inspectTarget;
        Renderer[] targetRenderers = boundsTarget.GetComponentsInChildren<Renderer>(true);
        if (targetRenderers.Length == 0)
        {
            return new Bounds(boundsTarget.position, Vector3.one);
        }

        Bounds bounds = targetRenderers[0].bounds;
        for (int i = 1; i < targetRenderers.Length; i++)
        {
            bounds.Encapsulate(targetRenderers[i].bounds);
        }

        return bounds;
    }

    private void EnsurePreviewObject()
    {
        if (previewObject != null)
        {
            previewObject.transform.position = previewPosition;
            previewObject.transform.rotation = GetPreviewRotation();
            previewObject.SetActive(false);
            return;
        }

        previewObject = Instantiate(inspectTarget.gameObject, previewPosition, GetPreviewRotation());
        previewObject.name = "Keypad Lock Inspect Preview";
        previewObject.SetActive(false);
        StripPreviewObjectRuntimeComponents(previewObject);
        SetLayerRecursively(previewObject, previewLayer);
    }

    private Quaternion GetPreviewRotation()
    {
        return inspectTarget.rotation * Quaternion.Euler(previewRotationEuler);
    }

    private void StripPreviewObjectRuntimeComponents(GameObject root)
    {
        foreach (Collider previewCollider in root.GetComponentsInChildren<Collider>(true))
        {
            previewCollider.enabled = false;
        }

        foreach (Rigidbody previewRigidbody in root.GetComponentsInChildren<Rigidbody>(true))
        {
            previewRigidbody.isKinematic = true;
            previewRigidbody.useGravity = false;
        }

        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            behaviour.enabled = false;
        }
    }

    private void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;

        foreach (Transform child in root.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private bool WasClosePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private void ApplyHighlight()
    {
        if (renderers == null)
        {
            return;
        }

        float scale = isHighlighted ? 1.06f : 1f;
        transform.localScale = originalScale * scale;
    }

    void OnDestroy()
    {
        if (inspectTexture != null)
        {
            inspectTexture.Release();
        }

        if (previewObject != null)
        {
            Destroy(previewObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        Transform point = interactionPoint != null ? interactionPoint : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(point.position, interactRadius);
    }
}
