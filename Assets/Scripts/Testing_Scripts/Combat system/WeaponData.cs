using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Combat/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon Base Stats")]
    [Tooltip("Base damage dealt by this weapon before attack type multipliers.")]
    public float damage = 25f;

    [Tooltip("How fast the weapon swings (can be used to multiply animator speed).")]
    public float attackSpeed = 1f;

    [Tooltip("The physical reach of the weapon.")]
    public float attackRange = 1.5f;

    [Tooltip("Base stamina cost to swing this weapon.")]
    public float attackStaminaCost = 15f;

    [Tooltip("How much Poise Damage this weapon deals to enemies to stagger them.")]
    public float stagger = 30f;
}
