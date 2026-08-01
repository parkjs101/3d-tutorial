using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactRadius = 1.8f;

    private const string ThrowZoneName = "Throw Zone";
    private const string ThrowTargetName = "Throw Target";

    private IInteractable highlightedInteractable;

    public PushPullBox ActiveBox { get; private set; }
    public bool IsPushPulling => ActiveBox != null;

    public void UpdateHighlight(Vector3 playerPosition)
    {
        IInteractable nearestInteractable = FindNearbyInteractable(playerPosition);

        if (highlightedInteractable == nearestInteractable)
        {
            return;
        }

        if (highlightedInteractable != null)
        {
            highlightedInteractable.SetHighlighted(false);
        }

        highlightedInteractable = nearestInteractable;

        if (highlightedInteractable != null)
        {
            highlightedInteractable.SetHighlighted(true);
        }
    }

    public bool TryInteract(PlayerMovement player)
    {
        if (player == null)
        {
            return false;
        }

        if (TryReleaseHeldTire(player))
        {
            return true;
        }

        IInteractable interactable = FindNearbyInteractable(player.transform.position);
        return interactable != null && interactable.Interact(player);
    }

    private bool TryReleaseHeldTire(PlayerMovement player)
    {
        TirePickup heldTire = TirePickup.HeldTire;
        if (heldTire == null)
        {
            return false;
        }

        if (heldTire.IsLifting)
        {
            return true;
        }

        Collider throwZone = FindThrowZone(player);
        if (throwZone != null)
        {
            Transform throwTarget = FindThrowTarget();
            if (throwTarget != null)
            {
                heldTire.ThrowTo(throwTarget.position);
            }
            else
            {
                Debug.LogError($"Could not find '{ThrowTargetName}' in the active scene.", this);
            }
        }
        else
        {
            heldTire.Drop();
        }

        return true;
    }

    private static Transform FindThrowTarget()
    {
        GameObject throwTarget = GameObject.Find(ThrowTargetName);
        return throwTarget != null ? throwTarget.transform : null;
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

        ActiveBox.BeginPushPull(GetComponent<Rigidbody>());
        if (!ActiveBox.IsHeld)
        {
            ActiveBox = null;
            return false;
        }

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
        if (highlightedInteractable == null)
        {
            return;
        }

        highlightedInteractable.SetHighlighted(false);
        highlightedInteractable = null;
    }

    private PushPullBox FindNearbyBox(Vector3 playerPosition)
    {
        Collider[] hits = Physics.OverlapSphere(playerPosition, interactRadius, ~0, QueryTriggerInteraction.Collide);
        PushPullBox nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

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

    private static Collider FindThrowZone(PlayerMovement player)
    {
        Collider playerCollider = player.GetComponent<Collider>();
        if (playerCollider == null)
        {
            return null;
        }

        Bounds playerBounds = playerCollider.bounds;
        Collider[] hits = Physics.OverlapSphere(
            playerBounds.center,
            playerBounds.extents.magnitude,
            ~0,
            QueryTriggerInteraction.Collide);
        foreach (Collider hit in hits)
        {
            if (hit != null && hit.isTrigger && hit.name == ThrowZoneName &&
                hit.bounds.Intersects(playerBounds))
            {
                return hit;
            }
        }

        return null;
    }

    private IInteractable FindNearbyInteractable(Vector3 playerPosition)
    {
        Collider[] hits = Physics.OverlapSphere(playerPosition, interactRadius, ~0, QueryTriggerInteraction.Collide);
        IInteractable nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            IInteractable interactable = FindInteractable(hit);
            if (interactable == null)
            {
                continue;
            }

            if (!interactable.CanInteract(playerPosition))
            {
                continue;
            }

            float distance = Vector3.Distance(playerPosition, hit.ClosestPoint(playerPosition));
            if (distance < nearestDistance)
            {
                nearest = interactable;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private IInteractable FindInteractable(Collider hit)
    {
        if (hit == null)
        {
            return null;
        }

        IInteractable interactable = FindInteractableInBehaviours(hit.GetComponents<MonoBehaviour>());
        if (interactable != null)
        {
            return interactable;
        }

        interactable = FindInteractableInBehaviours(hit.GetComponentsInParent<MonoBehaviour>());
        if (interactable != null)
        {
            return interactable;
        }

        return FindInteractableInBehaviours(hit.GetComponentsInChildren<MonoBehaviour>());
    }

    private IInteractable FindInteractableInBehaviours(MonoBehaviour[] behaviours)
    {
        if (behaviours == null)
        {
            return null;
        }

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
            {
                continue;
            }

            if (behaviour is IInteractable interactable)
            {
                return interactable;
            }
        }

        return null;
    }

    void OnDisable()
    {
        ReleaseBox();
        ClearHighlight();
    }
}
