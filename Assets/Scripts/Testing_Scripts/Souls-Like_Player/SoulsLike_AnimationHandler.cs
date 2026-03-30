using UnityEngine;

public class SoulsLike_AnimationHandler : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private SoulsLike_InputHandler _inputHandler;
    [SerializeField] private SoulsLike_StateManager _stateManager;

    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int IsGroundedParam = Animator.StringToHash("IsGrounded");
    private static readonly int IsWallRunningParam = Animator.StringToHash("IsWallRunning");

    private void Awake()
    {
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_inputHandler == null) _inputHandler = GetComponentInParent<SoulsLike_InputHandler>();
        if (_stateManager == null) _stateManager = GetComponentInParent<SoulsLike_StateManager>();
    }

    private void Update()
    {
        if (_animator == null || _inputHandler == null) return;

        // Drive basic Movement Animations
        float speed = _inputHandler.MovementInput.magnitude;
        if (_inputHandler.IsSprinting) speed *= 2f; 
        
        // Smooth damp the speed float so locomotion blends look natural
        _animator.SetFloat(SpeedParam, speed, 0.1f, Time.deltaTime);

        if (_stateManager != null)
        {
            _animator.SetBool(IsGroundedParam, _stateManager.CurrentState != SoulsLikePlayerState.Airborne);
            _animator.SetBool(IsWallRunningParam, _stateManager.CurrentState == SoulsLikePlayerState.WallRunning);
        }
    }
}
