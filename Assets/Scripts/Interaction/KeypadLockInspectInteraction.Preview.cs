using UnityEngine;

public partial class KeypadLockInspectInteraction
{
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
}
