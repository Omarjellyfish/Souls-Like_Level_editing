using System.Collections;
using UnityEngine;

public class ParryController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Character Animator")]
    [SerializeField] private Animator _animator;
    [Tooltip("Health Script to track parry state")]
    [SerializeField] private Health _health;
    [Tooltip("Stamina Script to deduct cost")]
    [SerializeField] private Stamina _stamina;
    [Tooltip("Optional StateManager to prevent parrying mid-jump")]
    [SerializeField] private SoulsLike_StateManager _stateManager;

    [Header("Settings")]
    [Tooltip("Key to trigger a Parry (Mouse1 is Right Click)")]
    [SerializeField] private KeyCode _parryKey = KeyCode.Mouse1;
    [Tooltip("Stamina cost to attempt a parry")]
    [SerializeField] private float _parryStaminaCost = 15f;
    [Tooltip("How long the parry window actually lasts (the 'active frames')")]
    [SerializeField] private float _parryWindowDuration = 0.3f;
    [Tooltip("Extra cooldown after the window closes so you can't just spam the button")]
    [SerializeField] private float _spamBlockCooldown = 0.3f;

    private static readonly int ParryTrigger = Animator.StringToHash("Parry");
    private bool _isAttemptingParry; // Locks out normal behavior while the animation plays

    private void Awake()
    {
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_health == null) _health = GetComponent<Health>();
        if (_stamina == null) _stamina = GetComponent<Stamina>();
        if (_stateManager == null) _stateManager = GetComponent<SoulsLike_StateManager>();
    }

    private void OnEnable()
    {
        // The core of the design: we literally just listen to Health.cs to see if we deflected something!
        if (_health != null) _health.OnSuccessfulParry += HandleSuccessfulParry;
    }

    private void OnDisable()
    {
        if (_health != null) _health.OnSuccessfulParry -= HandleSuccessfulParry;
    }

    private void Update()
    {
        if (_isAttemptingParry) return;

        if (Input.GetKeyDown(_parryKey))
        {
            TryParry();
        }
    }

    private void TryParry()
    {
        if (_stateManager != null && !_stateManager.TryEnterState(SoulsLikePlayerState.Parrying))
            return;

        if (_stamina != null && !_stamina.TryConsumeStamina(_parryStaminaCost))
        {
            Debug.Log("Not enough stamina to parry!");
            return;
        }

        if (_animator != null) _animator.SetTrigger(ParryTrigger);

        StartCoroutine(ParrySequenceRoutine());
    }

    private IEnumerator ParrySequenceRoutine()
    {
        _isAttemptingParry = true;
        
        // 1. You hit Right Click. The Parry Window immediately opens.
        if (_health != null) _health.IsParrying = true;

        // 2. We wait out the duration. Any sword hitting you during this frame is parried instantly!
        yield return new WaitForSeconds(_parryWindowDuration);

        // 3. You failed to parry anything. Vulnerability resumes.
        if (_health != null) _health.IsParrying = false;
        
        // 4. Slight cooldown before you can try again
        yield return new WaitForSeconds(_spamBlockCooldown);
        
        _isAttemptingParry = false;
        
        if (_stateManager != null && _stateManager.CurrentState == SoulsLikePlayerState.Parrying)
        {
             _stateManager.ForceState(SoulsLikePlayerState.Idle);
        }
    }

    // Fired automatically by Health.TakeDamage if your timing was perfect
    private void HandleSuccessfulParry(GameObject attacker)
    {
        Debug.Log($"SUCCESSFUL PARRY on {attacker.name}!");
        
        // Play huge sound/particle here!

        // Find the attacker's poise system and completely break it
        PoiseSystem enemyPoise = attacker.GetComponentInChildren<PoiseSystem>();
        if (enemyPoise != null)
        {
            // Forces them to stagger and opens the gigantic 3x damage Riposte window
            enemyPoise.SufferParry(); 
        }
    }
}
