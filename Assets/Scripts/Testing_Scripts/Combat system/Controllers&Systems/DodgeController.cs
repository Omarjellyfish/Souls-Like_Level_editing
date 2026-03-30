using System.Collections;
using UnityEngine;

public class DodgeController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The character's Animator component")]
    [SerializeField] private Animator _animator;
    [Tooltip("The Stamina component handling costs")]
    [SerializeField] private Stamina _stamina;
    [Tooltip("The Health component (for turning on iFrames)")]
    [SerializeField] private Health _health;
    [Tooltip("Camera Transform to calculate relative dodge direction")]
    [SerializeField] private Transform _cameraTransform;
    [Tooltip("The Character Controller (optional, fallback to Transform if null)")]
    [SerializeField] private CharacterController _characterController;
    [Tooltip("Optional StateManager to lock combat actions safely")]
    [SerializeField] private SoulsLike_StateManager _stateManager;

    [Header("Settings")]
    [Tooltip("Key used to dodge/roll")]
    [SerializeField] private KeyCode _dodgeKey = KeyCode.LeftAlt;
    [Tooltip("Stamina cost per dodge")]
    [SerializeField] private float _dodgeStaminaCost = 15f;
    [Tooltip("Total distance covered during the dodge")]
    [SerializeField] private float _dodgeDistance = 5f;
    [Tooltip("How long the dodge animation/movement takes in seconds")]
    [SerializeField] private float _dodgeDuration = 0.5f;
    
    [Tooltip("Animation curve representing the speed of the dodge over time. (e.g., fast at start, slow at end)")]
    [SerializeField] private AnimationCurve _dodgeSpeedCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    public bool IsDodging { get; private set; }
    
    // Hash optimizations for Animator triggers
    private static readonly int DodgeTrigger = Animator.StringToHash("Dodge");

    private void Awake()
    {
        // Auto-assign references if they are missing
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_stamina == null) _stamina = GetComponent<Stamina>();
        if (_health == null) _health = GetComponent<Health>();
        if (_characterController == null) _characterController = GetComponent<CharacterController>();
        if (_cameraTransform == null && Camera.main != null) _cameraTransform = Camera.main.transform;
        if (_stateManager == null) _stateManager = GetComponent<SoulsLike_StateManager>();
    }

    private void Update()
    {
        // Don't process other inputs if we are dodging
        if (IsDodging) return;

        if (Input.GetKeyDown(_dodgeKey))
        {
            TryDodge();
        }
    }

    private void TryDodge()
    {
        // 0. Check State Manager if we are allowed to dodge!
        if (_stateManager != null && !_stateManager.TryEnterState(SoulsLikePlayerState.Dodging))
            return;

        // 1. Check Stamina
        if (_stamina != null && !_stamina.TryConsumeStamina(_dodgeStaminaCost))
        {
            Debug.Log("Not enough stamina to dodge!");
            return;
        }

        // 2. Read Directional Input (WASD)
        Vector3 inputDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;
        Vector3 dodgeDirection = transform.forward; // Default to standard forward if no keys are pressed

        // If the player is holding a direction, we dodge relative to the camera
        if (inputDir.sqrMagnitude >= 0.1f && _cameraTransform != null)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
            dodgeDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            // Instantly snap rotation to face the dodge direction (standard for responsive rolls)
            transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
        }

        // 3. Start the Dodge Coroutine
        StartCoroutine(DodgeRoutine(dodgeDirection));
    }

    private IEnumerator DodgeRoutine(Vector3 direction)
    {
        // Lock State
        IsDodging = true;

        // Turn on iFrames
        if (_health != null) _health.IsInvincible = true;

        // Trigger Animator
        if (_animator != null) _animator.SetTrigger(DodgeTrigger);

        float elapsedTime = 0f;

        // Loop for the exact duration of the dodge
        while (elapsedTime < _dodgeDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / _dodgeDuration; // Goes from 0.0 to 1.0

            // Multiply the curve value by the total distance over time to get the speed this frame
            float speedCurveValue = _dodgeSpeedCurve.Evaluate(normalizedTime);
            float currentSpeed = speedCurveValue * (_dodgeDistance / _dodgeDuration);

            // The actual push vector
            Vector3 moveVector = direction * currentSpeed * Time.deltaTime;

            // Apply movement (CharacterController prevents clipping through walls)
            if (_characterController != null && _characterController.enabled)
            {
                _characterController.Move(moveVector);
            }
            else
            {
                transform.position += moveVector;
            }

            // Wait until next frame
            yield return null; 
        }

        // The dodge has ended!
        IsDodging = false;
        
        // Turn off iFrames
        if (_health != null) _health.IsInvincible = false;

        // Release player state back to Idle
        if (_stateManager != null && _stateManager.CurrentState == SoulsLikePlayerState.Dodging)
        {
            _stateManager.ForceState(SoulsLikePlayerState.Idle);
        }
    }
}
