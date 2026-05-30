using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool CrouchPressed { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool InteractPressed { get; private set; }
    public float ClimbInput { get; private set; }

    public void Tick(bool inputBlocked)
    {
        if (inputBlocked || Keyboard.current == null)
        {
            Clear();
            return;
        }

        Vector2 moveInput = Vector2.zero;

        if (Keyboard.current.dKey.isPressed) moveInput.y = 1f;
        if (Keyboard.current.aKey.isPressed) moveInput.y = -1f;
        if (Keyboard.current.wKey.isPressed) moveInput.x = -1f;
        if (Keyboard.current.sKey.isPressed) moveInput.x = 1f;

        MoveInput = moveInput;
        ClimbInput = GetClimbInput();
        SprintHeld = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
        CrouchPressed = Keyboard.current.leftCtrlKey.wasPressedThisFrame || Keyboard.current.rightCtrlKey.wasPressedThisFrame;
        JumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        InteractPressed = Keyboard.current.eKey.wasPressedThisFrame;
    }

    private float GetClimbInput()
    {
        float climbInput = 0f;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            climbInput += 1f;
        }

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            climbInput -= 1f;
        }

        return Mathf.Clamp(climbInput, -1f, 1f);
    }

    private void Clear()
    {
        MoveInput = Vector2.zero;
        SprintHeld = false;
        CrouchPressed = false;
        JumpPressed = false;
        InteractPressed = false;
        ClimbInput = 0f;
    }
}
