using UnityEngine;
using CombatSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(SoulsLike_InputHandler))]
public class SoulsLike_Movement : MonoBehaviour
{
    [Header("Locomotion Settings")]
    [SerializeField] private float _walkSpeed = 4f;
    [SerializeField] private float _sprintSpeed = 7f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _jumpForce = 7f;
    [SerializeField] private float _gravity = -20f;

    [Header("References")]
    [Tooltip("The Main Camera used to calculate movement direction mathematically.")]
    [SerializeField] private Transform _cameraTransform;
    [Tooltip("Optional Stamina component for sprinting")]
    [SerializeField] private Stamina _stamina;
    [Tooltip("Optional StateManager to lock movement during combat")]
    [SerializeField] private SoulsLike_StateManager _stateManager;
    [Tooltip("Reference to the LockOnManager for strafe behavior during lock-on.")]
    [SerializeField] private LockOnManager _lockOnManager;

    private CharacterController _controller;
    private SoulsLike_InputHandler _inputHandler;
    
    private float _verticalVelocity;
    private bool _isGrounded;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _inputHandler = GetComponent<SoulsLike_InputHandler>();

        if (_stateManager == null) _stateManager = GetComponent<SoulsLike_StateManager>(); // Auto-fetch

        if (_cameraTransform == null && Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    private void OnEnable()
    {
        _inputHandler.OnJumpPressed += HandleJump;
    }

    private void OnDisable()
    {
        _inputHandler.OnJumpPressed -= HandleJump;
    }

    private void Update()
    {
        CheckGrounded();
        HandleGravity();
        HandleMovement();
    }

    private void CheckGrounded()
    {
        // CharacterController isGrounded can be occasionally loose, a tiny raycast ensures perfect stability on slopes
        _isGrounded = _controller.isGrounded || Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.2f);

        if (_stateManager != null)
        {
            // If we are falling, tell StateManager
            if (!_isGrounded && _stateManager.CurrentState != SoulsLikePlayerState.WallRunning)
            {
                // Airborne state takes priority over Idle/Moving
                if (_stateManager.CurrentState == SoulsLikePlayerState.Idle || 
                    _stateManager.CurrentState == SoulsLikePlayerState.Moving ||
                    _stateManager.CurrentState == SoulsLikePlayerState.Sprinting)
                {
                    _stateManager.TryEnterState(SoulsLikePlayerState.Airborne);
                }
            }
            else if (_isGrounded && _stateManager.CurrentState == SoulsLikePlayerState.Airborne)
            {
                // Landed!
                _stateManager.ForceState(SoulsLikePlayerState.Idle);
            }
        }
    }

    private void HandleGravity()
    {
        // Small constant downward force when grounded to glue them to slopes perfectly
        if (_isGrounded && _verticalVelocity < 0)
        {
            _verticalVelocity = -2f; 
        }

        _verticalVelocity += _gravity * Time.deltaTime;
    }

    private void HandleJump()
    {
        // Ask the StateManager if we are allowed to jump! (Prevents jumping while swinging a sword!)
        if (_stateManager != null && !_stateManager.TryEnterState(SoulsLikePlayerState.Airborne))
             return;

        if (_isGrounded)
        {
            _verticalVelocity = Mathf.Sqrt(_jumpForce * -2f * _gravity);
        }
    }

    private void HandleMovement()
    {
        // 1. Check if we are physically allowed to run around!
        if (_stateManager != null)
        {
            SoulsLikePlayerState state = _stateManager.CurrentState;
            // Combat actions freeze locomotion completely!
            if (state == SoulsLikePlayerState.Attacking || 
                state == SoulsLikePlayerState.Dodging || 
                state == SoulsLikePlayerState.Parrying || 
                state == SoulsLikePlayerState.Staggered ||
                state == SoulsLikePlayerState.WallRunning || 
                state == SoulsLikePlayerState.Dead)
            {
                // We must still apply gravity so we don't float mid-air if we swing off a cliff
                _controller.Move(new Vector3(0, _verticalVelocity, 0) * Time.deltaTime);
                return;
            }
        }

        // 2. Calculate Direction relative to Camera!
        Vector2 input = _inputHandler.MovementInput;
        Vector3 moveDirection = Vector3.zero;

        if (input.magnitude >= 0.1f)
        {
            // Find camera angles (ignore vertical look so we don't fly pointlessly into floor)
            Vector3 camForward = _cameraTransform.forward;
            Vector3 camRight = _cameraTransform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            // Calculate exact world movement vector based on WASD input
            moveDirection = (camForward * input.y + camRight * input.x).normalized;
        }

        // 2b. Handle Rotation (Lock-On vs Free Movement)
        bool isLockedOn = _lockOnManager != null && _lockOnManager.IsLockedOn && _lockOnManager.CurrentTargetProxy != null;

        if (isLockedOn)
        {
            // Souls-like: Always face the enemy during lock-on, player strafes instead of spinning
            Vector3 dirToEnemy = _lockOnManager.CurrentTargetProxy.position - transform.position;
            dirToEnemy.y = 0;
            if (dirToEnemy.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dirToEnemy.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }
        }
        else if (moveDirection.sqrMagnitude > 0.01f)
        {
            // Free movement: Smoothly turn to face where we are walking
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }

        // 3. Sprint vs Walk Math
        float currentSpeed = _walkSpeed;
        bool isMoving = input.magnitude >= 0.1f;

        if (isMoving && _inputHandler.IsSprinting)
        {
            // Try to consume constant stamina, if we fail, cap speed to walking
            if (_stamina != null && _stamina.TryConsumeStamina(5f * Time.deltaTime))
            {
                currentSpeed = _sprintSpeed;
                if (_stateManager != null) _stateManager.TryEnterState(SoulsLikePlayerState.Sprinting);
            }
            else
            {
                if (_stateManager != null) _stateManager.TryEnterState(SoulsLikePlayerState.Moving);
            }
        }
        else if (isMoving)
        {
            if (_stateManager != null) _stateManager.TryEnterState(SoulsLikePlayerState.Moving);
        }
        else if (_isGrounded)
        {
            if (_stateManager != null) _stateManager.TryEnterState(SoulsLikePlayerState.Idle);
        }

        // Apply Final Velocity to Character Controller
        Vector3 finalVelocity = moveDirection * currentSpeed;
        finalVelocity.y = _verticalVelocity;

        _controller.Move(finalVelocity * Time.deltaTime);
    }
}
