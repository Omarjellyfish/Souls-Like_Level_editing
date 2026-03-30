using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The character's Animator component")]
    [SerializeField] private Animator _animator;
    [Tooltip("The Stamina component handling costs")]
    [SerializeField] private Stamina _stamina;
    [Tooltip("The Poise system for handling getting staggered mid-swing")]
    [SerializeField] private PoiseSystem _poiseSystem;
    [Tooltip("The currently equipped weapon object")]
    [SerializeField] private GameObject _equippedWeaponObject;

    [Header("Settings")]
    [Tooltip("How long Left Click must be held to trigger Heavy Attack")]
    [SerializeField] private float _holdThreshold = 0.25f;
    [Tooltip("Maximum amount of swings in a generic combo chain before resetting")]
    [SerializeField] private int _maxComboSteps = 3;
    [Tooltip("Optional StateManager to lock combat actions safely")]
    [SerializeField] private SoulsLike_StateManager _stateManager;

    private IWeapon _currentWeapon;
    private IAttack _lightAttack;
    private IAttack _heavyAttack;
    private IAttack _jumpingAttack; // New Jumping Attack Slot

    // State Tracking
    private float _timePressed;
    private bool _isHolding;
    private bool _isAttacking; // Prevents attacking if an attack is already in progress
    private bool _canChainCombo; // Toggled true midway through an animation via Event!
    private int _comboStep = 1;

    // Hash optimizations for Animator triggers
    private static readonly int LightAttackTrigger = Animator.StringToHash("LightAttack");
    private static readonly int HeavyAttackTrigger = Animator.StringToHash("HeavyAttack");
    private static readonly int JumpingAttackTrigger = Animator.StringToHash("JumpingAttack");
    private static readonly int ComboStepParam = Animator.StringToHash("ComboStep");

    private void Awake()
    {
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_stamina == null) _stamina = GetComponent<Stamina>();
        if (_poiseSystem == null) _poiseSystem = GetComponent<PoiseSystem>();
        if (_stateManager == null) _stateManager = GetComponent<SoulsLike_StateManager>();

        EquipWeapon(_equippedWeaponObject);
    }

    private void OnEnable()
    {
        // If we get staggered, forcefully end our attack so we aren't stuck unable to swing again!
        if (_poiseSystem != null) _poiseSystem.OnStaggered += FinishCurrentAttack;
    }

    private void OnDisable()
    {
        if (_poiseSystem != null) _poiseSystem.OnStaggered -= FinishCurrentAttack;
    }

    // Call this if you ever pick up a new weapon or swap weapons!
    public void EquipWeapon(GameObject weaponObj)
    {
        if (weaponObj == null) return;

        _currentWeapon = weaponObj.GetComponent<IWeapon>();
        
        // Grab all attack logic scripts attached to the weapon
        IAttack[] attacks = weaponObj.GetComponents<IAttack>();
        foreach (var attack in attacks)
        {
            if (attack.Type == AttackType.Light) _lightAttack = attack;
            else if (attack.Type == AttackType.Heavy) _heavyAttack = attack;
            else if (attack.Type == AttackType.Jumping) _jumpingAttack = attack;
        }
    }

    private void Update()
    {
        if (_currentWeapon == null) return;

        // If we are attacking but NOT allowed to chain yet, completely block input
        if (_isAttacking && !_canChainCombo) return; 

        HandleInput();
    }

    private void HandleInput()
    {
        // 1. Player presses button
        if (Input.GetMouseButtonDown(0))
        {
            // JUMP ATTACK CHECK: Cast a tiny ray exactly down to check if we are in the air
            bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.4f);
            
            if (!isGrounded && _jumpingAttack != null)
            {
                ExecuteAttack(_jumpingAttack, JumpingAttackTrigger);
                return; // Plunging attacks interrupt standard Light/Heavy charging input!
            }

            _isHolding = true;
            _timePressed = 0f;
        }

        // 2. Button is being held
        if (_isHolding)
        {
            _timePressed += Time.deltaTime;

            // Player released before threshold -> LIGHT ATTACK
            if (Input.GetMouseButtonUp(0))
            {
                ExecuteAttack(_lightAttack, LightAttackTrigger);
            }
            // Player held it past the threshold -> HEAVY ATTACK
            else if (_timePressed >= _holdThreshold)
            {
                ExecuteAttack(_heavyAttack, HeavyAttackTrigger);
            }
        }
    }

    private void ExecuteAttack(IAttack attack, int animatorHash)
    {
        _isHolding = false; // Reset holding state
        
        if (attack == null) return;

        // State machine check!
        if (_stateManager != null && !_stateManager.TryEnterState(SoulsLikePlayerState.Attacking))
            return;

        float staminaCost = _currentWeapon.AttackStaminaCost;  

        if (attack.Type == AttackType.Heavy) staminaCost *= 1.5f;

        if (_stamina != null && !_stamina.TryConsumeStamina(staminaCost))
        {
            Debug.Log("Not enough stamina to attack!");
            return;
        }

        // --- COMBO MATH ---
        // If we struck while allowed to chain, go to next step! Otherwise lock to 1.
        if (_canChainCombo && _isAttacking) 
        {
            _comboStep++;
            if (_comboStep > _maxComboSteps) _comboStep = 1; // Wrap around to step 1
        }
        else
        {
            _comboStep = 1;
        }

        // Lock combat state so we can't swing randomly
        _isAttacking = true;
        _canChainCombo = false; 
        
        attack.AttackInProgress = true;
        
        if (_poiseSystem != null && attack.Type == AttackType.Heavy)
        {
            _poiseSystem.IsHyperArmorActive = true;
        }

        attack.Attack(); 

        // Send the Combo Step AND the Trigger so the Animator knows WHICH Light Attack to play!
        if (_animator != null)
        {
            _animator.SetInteger(ComboStepParam, _comboStep);
            _animator.SetTrigger(animatorHash);
        }
    }

    /// <summary>
    /// Fire this Animation Event ~70% of the way through a swing animation.
    /// It allows the player a brief window to click again to queue up the NEXT combo hit smoothly!
    /// </summary>
    public void OpenComboWindow()
    {
        _canChainCombo = true;
    }

    /// <summary>
    /// Fire this Animation Event at the very end (100%) of every swing animation.
    /// If you don't click inside the Combo Window before this fires, your combo completely resets!
    /// </summary>
    public void FinishCurrentAttack()
    {
        _isAttacking = false;
        _canChainCombo = false;
        _comboStep = 1; // You swung and didn't chain, so you drop back to Swing 1!
        
        if (_lightAttack != null) _lightAttack.AttackInProgress = false;
        if (_heavyAttack != null) _heavyAttack.AttackInProgress = false;
        if (_jumpingAttack != null) _jumpingAttack.AttackInProgress = false;

        // Reset Hyper Armor just in case
        if (_poiseSystem != null) _poiseSystem.IsHyperArmorActive = false;
        
        if (_stateManager != null && _stateManager.CurrentState == SoulsLikePlayerState.Attacking)
        {
             _stateManager.ForceState(SoulsLikePlayerState.Idle);
        }
    }
}
