using System;
using UnityEngine;

public class PoiseSystem : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Health script on this same object")]
    [SerializeField] private Health _health;
    [Tooltip("The Animator to play the Stagger animation")]
    [SerializeField] private Animator _animator;

    [Header("Poise Stats")]
    [Tooltip("Maximum amount of stagger damage before the character flinches")]
    [SerializeField] private float _maxPoise = 100f;
    [Tooltip("How much Poise regenerates per second")]
    [SerializeField] private float _poiseRecoveryRate = 20f;
    [Tooltip("Time to wait after taking damage before poise starts recovering")]
    [SerializeField] private float _poiseRecoveryDelay = 2f;

    // "Hyper Armor" protects you from being staggered while wearing heavy armor or doing heavy attacks
    public bool IsHyperArmorActive { get; set; }

    [Tooltip("Optional State Manager to freeze player movement completely while staggering")]
    [SerializeField] private SoulsLike_StateManager _stateManager;

    // Fired when poise reaches 0. CombatManager should listen to this to cancel incoming attacks!
    public event Action OnStaggered;

    private float _currentPoise;
    private float _lastDamageTime;

    // Hash for the animator trigger to avoid string allocations
    private static readonly int StaggerTrigger = Animator.StringToHash("Stagger");

    private void Awake()
    {
        if (_health == null) _health = GetComponent<Health>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_stateManager == null) _stateManager = GetComponent<SoulsLike_StateManager>();

        _currentPoise = _maxPoise;
    }

    private void OnEnable()
    {
        if (_health != null)
        {
            _health.OnStaggerDamageReceived += HandleStaggerDamage;
        }
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.OnStaggerDamageReceived -= HandleStaggerDamage;
        }
    }

    private void HandleStaggerDamage(float staggerAmount)
    {
        // Hyper Armor halves incoming stagger damage (doubling the amount needed to stagger you)
        if (IsHyperArmorActive)
        {
            staggerAmount *= 0.5f;
        }

        _currentPoise -= staggerAmount;
        _lastDamageTime = Time.time;

        if (_currentPoise <= 0)
        {
            TriggerStagger();
        }
    }

    private void TriggerStagger()
    {
        // Reset poise back to full immediately upon a stagger
        _currentPoise = _maxPoise;

        if (_animator != null)
        {
            _animator.SetTrigger(StaggerTrigger);
        }

        if (_stateManager != null)
        {
            // Forces the player to immediately freeze in place while the stagger animation plays!
            _stateManager.ForceState(SoulsLikePlayerState.Staggered);
        }

        // Tell the rest of the systems that we flinched (so they can cancel swings!)
        OnStaggered?.Invoke();

        Debug.Log($"{gameObject.name} was staggered!");
    }

    private void Update()
    {
        // Recover poise naturally if we haven't taken damage recently
        if (_currentPoise < _maxPoise && Time.time >= _lastDamageTime + _poiseRecoveryDelay)
        {
            _currentPoise += _poiseRecoveryRate * Time.deltaTime;
            _currentPoise = Mathf.Min(_currentPoise, _maxPoise);
        }
    }

    // Called instantly when someone successfully parries this character!
    public void SufferParry()
    {
        // Unconditionally destroy poise and stagger them
        _currentPoise = 0f;
        TriggerStagger();

        // Open up the Riposte Window
        StartCoroutine(VulnerabilityWindowRoutine());
    }

    private System.Collections.IEnumerator VulnerabilityWindowRoutine()
    {
        // For 3 seconds, taking damage triggers the massive Riposte damage multiplier!
        if (_health != null) _health.IsVulnerable = true;
        
        yield return new WaitForSeconds(3f);
        
        if (_health != null) _health.IsVulnerable = false;
    }
}
