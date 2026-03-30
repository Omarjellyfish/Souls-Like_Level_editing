using System;
using UnityEngine;

public class Stamina : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 15f; // How much recovers per second
    [SerializeField] private float regenDelay = 1.5f;      // How long to wait before regen starts

    private float _currentStamina;
    private float _lastUseTime;

    // Broadcasts to UI (current, max)
    public event Action<float, float> OnStaminaChanged;

    private void Awake()
    {
        _currentStamina = maxStamina;
    }

    private void Start()
    {
        OnStaminaChanged?.Invoke(_currentStamina, maxStamina);
    }

    private void Update()
    {
        // Regenerate stamina if we aren't at max, and enough time has passed since last use
        if (_currentStamina < maxStamina && Time.time >= _lastUseTime + regenDelay)
        {
            _currentStamina += staminaRegenRate * Time.deltaTime;
            _currentStamina = Mathf.Min(_currentStamina, maxStamina);
            
            OnStaminaChanged?.Invoke(_currentStamina, maxStamina);
        }
    }

    // Returns true if the action is allowed, false if not enough stamina
    public bool TryConsumeStamina(float amount)
    {
        if (_currentStamina >= amount)
        {
            _currentStamina -= amount;
            _lastUseTime = Time.time; // Reset the regen delay timer
            
            OnStaminaChanged?.Invoke(_currentStamina, maxStamina);
            return true;
        }
        
        return false;
    }

    // Used for actions that forcibly drain stamina without checking (like taking a massive hit)
    public void DrainStamina(float amount)
    {
        _currentStamina -= amount;
        _currentStamina = Mathf.Max(0, _currentStamina);
        _lastUseTime = Time.time;
        
        OnStaminaChanged?.Invoke(_currentStamina, maxStamina);
    }
}
