using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamagable
{
    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    
    // Follows rules: private _camelCase
    private float _currentHealth;

    // Follows rules: C# events for Cross-system communication
    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;

    public bool IsDead => _currentHealth <= 0;

    // Follows rules: Awake for caching/initialization
    private void Awake()
    {
        _currentHealth = maxHealth;
    }

    private void Start()
    {
        // Initial broadcast to UI
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
    }

    public event Action<float> OnStaggerDamageReceived;
    public event Action<GameObject> OnSuccessfulParry;

    public bool IsInvincible { get; set; } // Controlled by dodges
    public bool IsParrying { get; set; }   // Controlled by the ParryController
    public bool IsVulnerable { get; set; } // Controlled when poise breaks during a parry

    // Follows rules: PascalCase for methods, camelCase for parameters
    public void TakeDamage(float damageAmount, float staggerAmount = 0f, GameObject attacker = null)
    {
        if (IsDead || IsInvincible) return; // iFrames block damage completely

        // --- PARRY MECHANIC ---
        if (IsParrying)
        {
            if (attacker != null)
            {
                OnSuccessfulParry?.Invoke(attacker);
            }
            return; // Completely negate the damage because we parried it!
        }

        // --- RIPOSTE / VULNERABLE MECHANIC ---
        if (IsVulnerable)
        {
            damageAmount *= 3f; // 300% Critical Hit Damage!
            Debug.Log("CRITICAL RIPOSTE!");
        }

        _currentHealth -= damageAmount;
        _currentHealth = Mathf.Max(0, _currentHealth); // Ensure health doesn't go below 0

        OnHealthChanged?.Invoke(_currentHealth, maxHealth);

        // Tell the Poise system (if any) to process the stagger damage
        if (staggerAmount > 0f)
        {
            OnStaggerDamageReceived?.Invoke(staggerAmount);
        }

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float healAmount)
    {
        if (IsDead) return;

        _currentHealth += healAmount;
        _currentHealth = Mathf.Min(_currentHealth, maxHealth); // Ensure health doesn't overflow

        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
    }

    private void Die()
    {
        OnDied?.Invoke();
        // The CombatManager or EnemyManager will listen to OnDied to play death animations
        // rather than Health.cs making assumptions! One script = one job.
    }
}
