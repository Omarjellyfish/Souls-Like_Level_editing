using UnityEngine;

public interface IAttack
{
    AttackType Type { get; }
    void Attack();
    bool AttackInProgress { get; set; }
}
