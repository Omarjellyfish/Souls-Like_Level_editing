using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Attack Modifiers Shared Data", fileName = "NewAttackModifiersData")]
public class AttackModifiersData : ScriptableObject
{
    [SerializeField] private float _lightAttackModifier = 0.5f;
    [SerializeField] private float _heavyAttackModifier = 1f;
    [SerializeField] private float _specialAttackModifier = 1.4f;

    public float GetModifier(AttackType attackType)
    {
        return attackType switch
        {
            AttackType.Light => _lightAttackModifier,
            AttackType.Heavy => _heavyAttackModifier,
            AttackType.Special => _specialAttackModifier,
            _ => 1f
        };
    }
}
