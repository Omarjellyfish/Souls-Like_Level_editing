using UnityEngine;

public interface IDamagable
{
    void TakeDamage(float damage, float staggerAmount = 0f, GameObject attacker = null);
}
