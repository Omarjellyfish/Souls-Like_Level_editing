using UnityEngine;

public interface IWeapon
{
    float Damage { get; }
    float AttackSpeed { get; }
    float AttackRange { get; }
    float AttackStaminaCost { get; }
    float Stagger { get; }
}
