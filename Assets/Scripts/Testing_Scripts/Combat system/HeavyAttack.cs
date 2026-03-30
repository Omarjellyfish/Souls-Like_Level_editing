using UnityEngine;

public class HeavyAttack : MonoBehaviour, IAttack
{

    private IWeapon weapon;
    [SerializeField] private AttackModifiersData _modifiersData;

    public AttackType Type => AttackType.Heavy;

    private void Awake()
    {
        weapon = GetComponent<IWeapon>();
    }

    public void Attack()
    {
        Debug.Log("Heavy Attack");
    }

    public bool AttackInProgress { get; set; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IDamagable>(out var damagable))
        {
            float modifier = 1f;

            if (_modifiersData != null)
            {
                modifier = _modifiersData.GetModifier(Type);
            }
            else
            {
                Debug.LogWarning("AttackModifiersData is not assigned on LightAttack. Defaulting modifier to 1.");
            }

            damagable.TakeDamage(weapon.Damage * modifier, weapon.Stagger * modifier, transform.root.gameObject);
        }
    }
}
