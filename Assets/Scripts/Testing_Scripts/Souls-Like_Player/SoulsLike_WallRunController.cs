using UnityEngine;
using System;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(SoulsLike_InputHandler))]
public class SoulsLike_WallRunController : MonoBehaviour
{
    [Header("Wall Run Settings")]
    [Tooltip("How far left/right to look for a runnable wall")]
    [SerializeField] private float _wallCheckDistance = 1f;
    [Tooltip("Speed when running across the wall")]
    [SerializeField] private float _wallRunSpeed = 6f;
    [Tooltip("Gravity is usually reduced while wall running so you stick to it longer")]
    [SerializeField] private float _wallRunGravity = -2f; 
    [Tooltip("Maximum seconds you can run on a wall before falling off")]
    [SerializeField] private float _maxWallRunTime = 2f;
    
    [Header("References")]
    [SerializeField] private SoulsLike_StateManager _stateManager;
    [SerializeField] private LayerMask _wallLayer;

    private CharacterController _controller;
    private SoulsLike_InputHandler _inputHandler;
    
    private bool _isWallRunning;
    private float _wallRunTimer;
    private Vector3 _wallNormal;
    private Vector3 _wallRunDir;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _inputHandler = GetComponent<SoulsLike_InputHandler>();
        if (_stateManager == null) _stateManager = GetComponent<SoulsLike_StateManager>();
    }

    private void Update()
    {
        if (!_isWallRunning)
        {
             CheckForWall();
        }
        else
        {
             HandleWallRun();
             CheckForWall(); // Constantly check if the wall ended!
        }
    }

    private void CheckForWall()
    {
        if (_stateManager == null) return;
        
        // Ensure player is Sprinting AND Airborne to initiate
        if (_stateManager.CurrentState != SoulsLikePlayerState.Airborne && _stateManager.CurrentState != SoulsLikePlayerState.WallRunning) return;
        
        if (!_inputHandler.IsSprinting) 
        {
            StopWallRun();
            return;
        }

        // Raycast left and right exactly horizontally
        bool wallRight = Physics.Raycast(transform.position, transform.right, out RaycastHit rightHit, _wallCheckDistance, _wallLayer);
        bool wallLeft = Physics.Raycast(transform.position, -transform.right, out RaycastHit leftHit, _wallCheckDistance, _wallLayer);

        if (wallRight || wallLeft)
        {
            RaycastHit hit = wallRight ? rightHit : leftHit;
            _wallNormal = hit.normal;

            // Start wall run if we aren't already
            if (!_isWallRunning && _stateManager.TryEnterState(SoulsLikePlayerState.WallRunning))
            {
                StartWallRun();
            }
        }
        else
        {
            StopWallRun();
        }
    }

    private void StartWallRun()
    {
        _isWallRunning = true;
        _wallRunTimer = _maxWallRunTime;
    }

    private void HandleWallRun()
    {
        _wallRunTimer -= Time.deltaTime;
        
        // Stop if timer ends or we somehow hit the ground
        if (_wallRunTimer <= 0 || _controller.isGrounded)
        {
            StopWallRun();
            return;
        }

        // The direction ALONG the wall is the cross product of the wall normal and UP
        _wallRunDir = Vector3.Cross(_wallNormal, Vector3.up);
        
        // Cross product can point backward depending on if the wall is on the left or right, 
        // so ensure we are running forward relative to where we are looking!
        if ((transform.forward - _wallRunDir).magnitude > (transform.forward - -_wallRunDir).magnitude)
        {
             _wallRunDir = -_wallRunDir;
        }

        Vector3 moveVelocity = _wallRunDir * _wallRunSpeed;
        moveVelocity.y = _wallRunGravity; // Apply custom slow gravity

        _controller.Move(moveVelocity * Time.deltaTime);

        // Turn character to slightly face the direction they are wall running into
        Quaternion targetRotation = Quaternion.LookRotation(_wallRunDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
    }

    private void StopWallRun()
    {
        if (!_isWallRunning) return;

        _isWallRunning = false;
        
        if (_stateManager != null && _stateManager.CurrentState == SoulsLikePlayerState.WallRunning)
        {
            _stateManager.ForceState(SoulsLikePlayerState.Airborne); // Exit into falling
        }
    }
}
