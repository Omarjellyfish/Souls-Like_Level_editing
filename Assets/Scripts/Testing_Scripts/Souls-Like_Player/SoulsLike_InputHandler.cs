using System;
using UnityEngine;

public class SoulsLike_InputHandler : MonoBehaviour
{
    [Header("Input Values (Read-Only)")]
    public Vector2 MovementInput;
    public bool IsSprinting;

    // Events for single press actions
    public event Action OnJumpPressed;

    private void Update()
    {
        HandleLocomotionInput();
        HandleActionInput();
    }

    private void HandleLocomotionInput()
    {
        // Get raw input so it feels snappy and responsive rather than floaty
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        
        // Normalize so diagonal movement isn't mathematically faster
        MovementInput = new Vector2(h, v).normalized;

        // Shift key determines sprint
        IsSprinting = Input.GetKey(KeyCode.LeftShift);
    }

    private void HandleActionInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnJumpPressed?.Invoke();
        }
    }
}
