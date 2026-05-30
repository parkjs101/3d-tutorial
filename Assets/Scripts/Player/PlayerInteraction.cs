using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactRadius = 1.4f;

    private Door highlightedDoor;

    public PushPullBox ActiveBox { get; private set; }
    public bool IsPushPulling => ActiveBox != null;

    public void UpdateDoorHighlight(Vector3 playerPosition)
    {
        Door nearestDoor = FindNearbyDoor(playerPosition, requireInteractable: true);

        if (highlightedDoor == nearestDoor)
        {
            return;
        }

        if (highlightedDoor != null)
        {
            highlightedDoor.SetHighlighted(false);
        }

        highlightedDoor = nearestDoor;

        if (highlightedDoor != null)
        {
            highlightedDoor.SetHighlighted(true);
        }
    }

    public bool TryInteractWithDoor(Vector3 playerPosition)
    {
        Door door = FindNearbyDoor(playerPosition, requireInteractable: true);
        return door != null && door.TryOpen(playerPosition);
    }

    public bool TogglePushPull(Vector3 playerPosition)
    {
        if (ActiveBox != null)
        {
            ReleaseBox();
            return false;
        }

        ActiveBox = FindNearbyBox(playerPosition);
        if (ActiveBox == null)
        {
            return false;
        }

        ActiveBox.BeginPushPull();
        return true;
    }

    public void SetPushPullVelocity(Vector3 velocity)
    {
        if (ActiveBox != null)
        {
            ActiveBox.SetMoveVelocity(velocity);
        }
    }

    public void ReleaseBox()
    {
        if (ActiveBox == null)
        {
            return;
        }

        ActiveBox.EndPushPull();
        ActiveBox = null;
    }

    public void ClearHighlight()
    {
        if (highlightedDoor == null)
        {
            return;
        }

        highlightedDoor.SetHighlighted(false);
        highlightedDoor = null;
    }

    private PushPullBox FindNearbyBox(Vector3 playerPosition)
    {
        Collider[] hits = Physics.OverlapSphere(playerPosition, interactRadius);
        PushPullBox nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            PushPullBox box = hit.GetComponentInParent<PushPullBox>();
            if (box == null)
            {
                continue;
            }

            float distance = Vector3.Distance(playerPosition, box.transform.position);
            if (distance < nearestDistance)
            {
                nearest = box;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private Door FindNearbyDoor(Vector3 playerPosition, bool requireInteractable)
    {
        Door[] doors = Object.FindObjectsByType<Door>(FindObjectsInactive.Exclude);
        Door nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Door door in doors)
        {
            if (door == null)
            {
                continue;
            }

            if (requireInteractable && !door.CanInteract(playerPosition))
            {
                continue;
            }

            float distance = Vector3.Distance(playerPosition, door.transform.position);
            if (distance < nearestDistance)
            {
                nearest = door;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    void OnDisable()
    {
        ReleaseBox();
        ClearHighlight();
    }
}
