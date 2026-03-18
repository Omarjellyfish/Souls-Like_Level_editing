using UnityEngine;

[CreateAssetMenu(fileName = "TWallRunData", menuName = "Data/TWallRunData")]
public class TWallRunDataSO : ScriptableObject
{
    [Header("Detection")]
    [SerializeField] private float _wallCheckDistance = 1f;
    [SerializeField] private float _minRunHeight = 1.5f;
    [SerializeField] private LayerMask _wallLayer;

    [Header("Horizontal Wall Run")]
    [SerializeField] private float _horizontalSpeed = 8f;
    [SerializeField] private float _horizontalGravityModifier = 0f;
    [SerializeField] private float _maxHorizontalDuration = 3f;

    [Header("Vertical Wall Run")]
    [SerializeField] private float _verticalSpeed = 6f;
    [SerializeField] private float _maxVerticalDuration = 2f;

    [Header("Jumping & Exiting")]
    [SerializeField] private float _wallJumpForce = 10f;
    [SerializeField] private float _wallJumpSideForce = 5f;

    // Public properties for outward access
    public float WallCheckDistance => _wallCheckDistance;
    public float MinRunHeight => _minRunHeight;
    public LayerMask WallLayer => _wallLayer;

    public float HorizontalSpeed => _horizontalSpeed;
    public float HorizontalGravityModifier => _horizontalGravityModifier;
    public float MaxHorizontalDuration => _maxHorizontalDuration;

    public float VerticalSpeed => _verticalSpeed;
    public float MaxVerticalDuration => _maxVerticalDuration;

    public float WallJumpForce => _wallJumpForce;
    public float WallJumpSideForce => _wallJumpSideForce;
}
