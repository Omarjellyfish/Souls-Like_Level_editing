using UnityEngine;

public class MeleeWeapon : MonoBehaviour, IWeapon
{
    [Header("Configuration")]
    [Tooltip("The shared data file that defines this weapon's stats.")]
    [SerializeField] private WeaponData _weaponData;

    // --- IWeapon Interface Implementation ---
    // Safely returns the value from the ScriptableObject, or a fallback if none is assigned!
    
    public float Damage => _weaponData != null ? _weaponData.damage : 0f;
    public float AttackSpeed => _weaponData != null ? _weaponData.attackSpeed : 1f;
    public float AttackRange => _weaponData != null ? _weaponData.attackRange : 0f;
    public float AttackStaminaCost => _weaponData != null ? _weaponData.attackStaminaCost : 0f;
    public float Stagger => _weaponData != null ? _weaponData.stagger : 0f;
}
