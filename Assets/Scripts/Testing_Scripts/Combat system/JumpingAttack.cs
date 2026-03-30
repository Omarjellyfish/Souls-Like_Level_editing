using System.Collections;
using UnityEngine;

public class JumpingAttack : MonoBehaviour, IAttack
{
    private IWeapon weapon;
    [SerializeField] private AttackModifiersData _modifiersData;

    [Header("Plunge Physics")]
    [Tooltip("How fast the character slams into the ground during the attack.")]
    [SerializeField] private float _slamDownwardSpeed = 20f;
    [Tooltip("Delay before they get forced down (lets the swing-windup animation play in the air).")]
    [SerializeField] private float _hangTime = 0.2f;

    public AttackType Type => AttackType.Jumping;
    public bool AttackInProgress { get; set; }

    private CharacterController _characterController;

    private void Awake()
    {
        weapon = GetComponent<IWeapon>();
        
        // Find CharacterController on the root player object
        _characterController = GetComponentInParent<CharacterController>();
    }

    public void Attack()
    {
        if (_characterController != null)
        {
            StartCoroutine(SlamRoutine());
        }
    }

    private IEnumerator SlamRoutine()
    {
        // 1. Hang in the air for a fraction of a second so the animation looks impactful
        yield return new WaitForSeconds(_hangTime);

        // 2. Continually smash downward until the attack finishes
        while (AttackInProgress)
        {
            if (_characterController != null)
            {
                // Force brutal downward movement!
                _characterController.Move(Vector3.down * _slamDownwardSpeed * Time.deltaTime);

                // Stop pushing if we successfully hit the floor
                if (_characterController.isGrounded)
                {
                    break;
                }
            }
            yield return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IDamagable>(out var damagable))
        {
            float modifier = 1.3f; // Plunging attacks do 130% damage by default!

            if (_modifiersData != null)
            {
                modifier *= _modifiersData.GetModifier(Type);
            }

            // Thread the root gameObject so we can riposte later if this was a parried jump attack
            damagable.TakeDamage(weapon.Damage * modifier, weapon.Stagger * modifier, transform.root.gameObject);
        }
    }
}
