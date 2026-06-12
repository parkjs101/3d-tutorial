using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.UI;

public partial class KeypadLockInspectInteraction : MonoBehaviour, IInteractable
{
    public static bool IsAnyInspectViewOpen { get; private set; }

    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private Transform inspectTarget;
    [SerializeField] private MonoBehaviour unlockTarget;
    [SerializeField] private float interactRadius = 1.4f;

    [Header("Password")]
    [SerializeField] private string correctPassword = "12345678";
    [SerializeField] private KeypadPasswordInputDisplay passwordInputPrefab;

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
    private IUnlockable unlockable;
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
        unlockable = unlockTarget as IUnlockable;

        if (unlockTarget != null && unlockable == null)
        {
            Debug.LogError("Keypad unlock target must implement IUnlockable.", this);
        }
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

    private void UnlockTarget()
    {
        if (unlockable == null)
        {
            Debug.LogError("Keypad password accepted, but no unlockable target is assigned.", this);
            return;
        }

        unlockable.Unlock();
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
