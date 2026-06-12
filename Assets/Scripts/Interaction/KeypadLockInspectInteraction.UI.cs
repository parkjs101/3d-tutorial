using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;

public partial class KeypadLockInspectInteraction
{
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
        if (passwordInputPrefab == null)
        {
            Debug.LogError("Keypad password input prefab is not assigned.", this);
            return;
        }

        passwordInputDisplay = Instantiate(passwordInputPrefab, panel);
        passwordInputDisplay.Initialize(correctPassword, UnlockTarget);

        CreateCloseButton(panel);
        CreateHintText(panel);
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
}
