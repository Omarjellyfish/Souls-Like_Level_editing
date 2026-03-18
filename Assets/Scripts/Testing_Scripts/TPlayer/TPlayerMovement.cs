using UnityEngine;
using System;

[RequireComponent(typeof(CharacterController))]
public class TPlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _walkSpeed = 5f;
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _gravity = -19.62f;

    [Header("Ground Check")]
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private float _groundDistance = 0.4f;
    [SerializeField] private LayerMask _groundMask;

    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _isGrounded;
    private bool _hasControl = true;

    // Events
    public event Action OnJumped;
    public event Action<bool> OnGroundedStateChanged;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!_hasControl) return;

        CheckGrounded();
        HandleMovement();
        HandleJump();
        ApplyGravity();
    }

    private void CheckGrounded()
    {
        if (_groundCheck == null) return;
        
        bool wasGrounded = _isGrounded;
        _isGrounded = Physics.CheckSphere(_groundCheck.position, _groundDistance, _groundMask);

        if (_isGrounded != wasGrounded)
        {
            OnGroundedStateChanged?.Invoke(_isGrounded);
        }

        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f; // Slight downward force to keep grounded
        }
    }

    private void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        _controller.Move(move.normalized * _walkSpeed * Time.deltaTime);

        // Animation Action placeholders
        // if (GetComponent<AnimationActions>() != null)
        // {
        //     if (move.magnitude > 0.1f) GetComponent<AnimationActions>().PlayRun();
        //     else GetComponent<AnimationActions>().PlayIdle();
        // }
    }

    private void HandleJump()
    {
        // Standalone jump logic
        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
        {
            PerformJump(_jumpForce);
        }
    }

    private void ApplyGravity()
    {
        _velocity.y += _gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    public void PerformJump(float force)
    {
        _velocity.y = Mathf.Sqrt(force * -2f * _gravity);
        OnJumped?.Invoke();
        
        // Animation Action placeholders
        // GetComponent<AnimationActions>()?.PlayJump();
    }

    // API for Wall Running scripts to take back/yield control
    public void SetControl(bool state)
    {
        _hasControl = state;
        if (!state)
        {
            _velocity = Vector3.zero; // Reset velocity when losing control
        }
    }
}
