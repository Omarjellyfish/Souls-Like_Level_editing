using UnityEngine;
using System;

[RequireComponent(typeof(CharacterController), typeof(TPlayerMovement))]
public class THorizontalWallRun : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TWallRunDataSO _data;
    [SerializeField] private Transform _orientation;

    private CharacterController _controller;
    private TPlayerMovement _playerMovement;

    private bool _isWallRight;
    private bool _isWallLeft;
    private bool _isWallRunning;
    private RaycastHit _leftWallHit;
    private RaycastHit _rightWallHit;

    private float _wallRunTimer;

    // Events
    public event Action OnHorizontalWallRunStarted;
    public event Action OnHorizontalWallRunEnded;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _playerMovement = GetComponent<TPlayerMovement>();
    }

    private void Update()
    {
        CheckForWall();
        StateMachine();
    }

    private void CheckForWall()
    {
        if (_orientation == null || _data == null) return;

        _isWallRight = Physics.Raycast(transform.position, _orientation.right, out _rightWallHit, _data.WallCheckDistance, _data.WallLayer);
        _isWallLeft = Physics.Raycast(transform.position, -_orientation.right, out _leftWallHit, _data.WallCheckDistance, _data.WallLayer);
    }

    private bool AboveGround()
    {
        if (_data == null) return false;
        return !Physics.Raycast(transform.position, Vector3.down, _data.MinRunHeight, _data.WallLayer);
    }

    private void StateMachine()
    {
        if (_data == null) return;

        bool isHoldingSpace = Input.GetKey(KeyCode.Space);

        // State 1 - Wallrunning
        if ((_isWallLeft || _isWallRight) && isHoldingSpace && AboveGround())
        {
            if (!_isWallRunning)
            {
                StartWallRun();
            }

            if (_wallRunTimer < _data.MaxHorizontalDuration)
            {
                _wallRunTimer += Time.deltaTime;
                WallRunMovement();
            }
            else
            {
                if (_isWallRunning) StopWallRun();
            }
        }
        // State 2 - Exiting
        else if (_isWallRunning)
        {
            StopWallRun();
        }
    }

    private void StartWallRun()
    {
        _isWallRunning = true;
        _wallRunTimer = 0f;
        _playerMovement.SetControl(false);
        OnHorizontalWallRunStarted?.Invoke();

        // Animation Action placeholders
        // GetComponent<AnimationActions>()?.PlayHorizontalWallRun(_isWallLeft);
    }

    private void WallRunMovement()
    {
        // Find wall normal
        Vector3 wallNormal = _isWallRight ? _rightWallHit.normal : _leftWallHit.normal;

        // Find forward direction along the wall
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        // Ensure player moves forward, not backward relative to their facing direction
        if ((transform.forward - wallForward).magnitude > (transform.forward - -wallForward).magnitude)
        {
            wallForward = -wallForward;
        }

        Vector3 move = wallForward * _data.HorizontalSpeed;
        
        // Apply weak gravity if desired
        move.y += _data.HorizontalGravityModifier * -9.81f;

        _controller.Move(move * Time.deltaTime);

        // Independent jump logic from wall
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StopWallRun();
            _playerMovement.PerformJump(_data.WallJumpForce);
        }
    }

    private void StopWallRun()
    {
        _isWallRunning = false;
        _playerMovement.SetControl(true);
        OnHorizontalWallRunEnded?.Invoke();

         // Animation Action placeholders
        // GetComponent<AnimationActions>()?.StopHorizontalWallRun();
    }
}
