using UnityEngine;

public interface IInteractable
{
    bool CanInteract(Vector3 playerPosition);
    bool Interact(PlayerMovement player);
    void SetHighlighted(bool highlighted);
}
