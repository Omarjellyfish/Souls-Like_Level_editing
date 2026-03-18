using UnityEngine;
using System;

[RequireComponent(typeof(CharacterController), typeof(TPlayerMovement))]
public class TVerticalWallRun : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TWallRunDataSO _data;
    [SerializeField] private Transform _orientation;

    private CharacterController _controller;
    private TPlayerMovement _playerMovement;

    private bool _isWallFront;
    private bool _isWallRunningVertical;
    private RaycastHit _frontWallHit;

    private float _wallRunTimer;

    // Events
    public event Action OnVerticalWallRunStarted;
    public event Action OnVerticalWallRunEnded;

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
        _isWallFront = Physics.Raycast(transform.position, _orientation.forward, out _frontWallHit, _data.WallCheckDistance, _data.WallLayer);
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
        float verticalInput = Input.GetAxisRaw("Vertical");

        // Start vertical wall run if pushing forward into wall and holding space
        if (_isWallFront && isHoldingSpace && verticalInput > 0 && AboveGround())
        {
            if (!_isWallRunningVertical)
            {
                StartWallRun();
            }

            if (_wallRunTimer < _data.MaxVerticalDuration)
            {
                _wallRunTimer += Time.deltaTime;
                WallRunMovement();
            }
            else
            {
                if (_isWallRunningVertical) StopWallRun();
            }
        }
        else if (_isWallRunningVertical)
        {
            StopWallRun();
        }
    }

    private void StartWallRun()
    {
        _isWallRunningVertical = true;
        _wallRunTimer = 0f;
        _playerMovement.SetControl(false);
        OnVerticalWallRunStarted?.Invoke();
        
        // Animation Action placeholders
        // GetComponent<AnimationActions>()?.PlayVerticalWallRun();
    }

    private void WallRunMovement()
    {
        Vector3 move = Vector3.up * _data.VerticalSpeed;
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
        _isWallRunningVertical = false;
        _playerMovement.SetControl(true);
        OnVerticalWallRunEnded?.Invoke();
        
        // Animation Action placeholders
        // GetComponent<AnimationActions>()?.StopVerticalWallRun();
    }
}
