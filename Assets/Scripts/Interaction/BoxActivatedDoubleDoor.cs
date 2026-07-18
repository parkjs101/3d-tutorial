using UnityEngine;

public class BoxActivatedDoubleDoor : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private string boxObjectName = "Box025";
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;

    [Header("Box Goal")]
    [SerializeField] private Vector2 targetXZ = new Vector2(43f, 9.8f);
    [SerializeField] private float activationRadius = 0.8f;

    [Header("Box Physics")]
    [SerializeField] private float boxMass = 3f;
    [SerializeField] private float boxLinearDamping = 0.2f;
    [SerializeField] private bool detachBoxFromMap = true;

    [Header("Door Open Motion")]
    [SerializeField] private float leftOpenAngle = -90f;
    [SerializeField] private float rightOpenAngle = 90f;
    [SerializeField] private float openSpeed = 90f;
    [SerializeField] private bool useRendererEdgesAsHinges = true;
    [SerializeField] private Vector3 leftLocalHingeOffset = new Vector3(-0.5f, 0f, 0f);
    [SerializeField] private Vector3 rightLocalHingeOffset = new Vector3(0.5f, 0f, 0f);

    private Transform boxTransform;
    private Vector3 leftHingePosition;
    private Vector3 rightHingePosition;
    private float currentLeftAngle;
    private float currentRightAngle;
    private bool isOpen;

    void Awake()
    {
        boxTransform = FindSceneTransform(boxObjectName);
        if (boxTransform == null || leftDoor == null || rightDoor == null)
        {
            Debug.LogError(
                $"BoxActivatedDoubleDoor requires Box '{boxObjectName}' and both door references.",
                this);
            enabled = false;
            return;
        }

        ConfigureMovableBox();
        CacheDoorHinges();
    }

    void Update()
    {
        if (!isOpen && IsBoxAtGoal())
        {
            Open();
        }

        if (isOpen)
        {
            UpdateDoorMotion();
        }
    }

    public void Open()
    {
        isOpen = true;
    }

    private void ConfigureMovableBox()
    {
        if (detachBoxFromMap && boxTransform.parent != null)
        {
            boxTransform.SetParent(null, true);
        }

        foreach (MeshCollider meshCollider in boxTransform.GetComponentsInChildren<MeshCollider>())
        {
            meshCollider.convex = true;
        }

        Rigidbody boxRigidbody = boxTransform.GetComponent<Rigidbody>();
        PushPullBox pushPullBox = boxTransform.GetComponent<PushPullBox>();
        if (boxRigidbody == null || pushPullBox == null)
        {
            Debug.LogError("Box025 requires Rigidbody and PushPullBox components.", boxTransform);
            enabled = false;
            return;
        }

        boxRigidbody.isKinematic = false;
        boxRigidbody.mass = Mathf.Max(0.01f, boxMass);
        boxRigidbody.linearDamping = Mathf.Max(0f, boxLinearDamping);
    }

    private bool IsBoxAtGoal()
    {
        Vector3 boxLocalPosition = boxTransform.localPosition;
        Vector2 boxXZ = new Vector2(boxLocalPosition.x, boxLocalPosition.z);
        return Vector2.Distance(boxXZ, targetXZ) <= activationRadius;
    }

    private void CacheDoorHinges()
    {
        leftHingePosition = GetHingePosition(leftDoor, true, leftLocalHingeOffset);
        rightHingePosition = GetHingePosition(rightDoor, false, rightLocalHingeOffset);
    }

    private Vector3 GetHingePosition(Transform door, bool useMinimumX, Vector3 localOffset)
    {
        if (!useRendererEdgesAsHinges)
        {
            return door.TransformPoint(localOffset);
        }

        Renderer doorRenderer = door.GetComponentInChildren<Renderer>();
        if (doorRenderer == null)
        {
            return door.TransformPoint(localOffset);
        }

        Bounds bounds = doorRenderer.bounds;
        float hingeX = useMinimumX ? bounds.min.x : bounds.max.x;
        return new Vector3(hingeX, bounds.center.y, bounds.center.z);
    }

    private void UpdateDoorMotion()
    {
        currentLeftAngle = RotateDoor(
            leftDoor,
            leftHingePosition,
            currentLeftAngle,
            leftOpenAngle);

        currentRightAngle = RotateDoor(
            rightDoor,
            rightHingePosition,
            currentRightAngle,
            rightOpenAngle);
    }

    private float RotateDoor(Transform door, Vector3 hingePosition, float currentAngle, float targetAngle)
    {
        float nextAngle = Mathf.MoveTowards(currentAngle, targetAngle, openSpeed * Time.deltaTime);
        float deltaAngle = nextAngle - currentAngle;
        if (Mathf.Abs(deltaAngle) > 0.001f)
        {
            door.RotateAround(hingePosition, Vector3.up, deltaAngle);
        }

        return nextAngle;
    }

    private Transform FindSceneTransform(string objectName)
    {
        foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (candidate.name == objectName)
            {
                return candidate;
            }
        }

        return null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Transform box = boxTransform != null ? boxTransform : FindSceneTransform(boxObjectName);
        Vector3 localTarget = new Vector3(targetXZ.x, 0f, targetXZ.y);
        Vector3 worldTarget = box != null && box.parent != null
            ? box.parent.TransformPoint(localTarget)
            : localTarget;
        Gizmos.DrawWireSphere(worldTarget, activationRadius);
    }
}
