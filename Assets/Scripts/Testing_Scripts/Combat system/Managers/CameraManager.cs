using UnityEngine;
using CombatSystem;

/// <summary>
/// Simplified CameraManager. We use a SINGLE exploration camera for everything.
/// No camera switching needed. The "lock-on effect" comes from the player
/// facing the enemy (handled by SoulsLike_Movement), which naturally makes 
/// the camera point toward the enemy since it always trails behind the player.
/// </summary>
public class CameraManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The LockOnManager that handles finding enemies.")]
    [SerializeField] private LockOnManager _lockOnManager;

    private void Awake()
    {
        if (_lockOnManager == null) _lockOnManager = GetComponentInParent<LockOnManager>();
        
        if (_lockOnManager == null)
        {
            Debug.LogError("CameraManager needs a reference to your LockOnManager!");
        }
    }

    private void OnEnable()
    {
        if (_lockOnManager != null)
        {
            _lockOnManager.OnTargetLocked.AddListener(HandleTargetLocked);
            _lockOnManager.OnTargetUnlocked.AddListener(HandleTargetUnlocked);
        }
    }

    private void OnDisable()
    {
        if (_lockOnManager != null)
        {
            _lockOnManager.OnTargetLocked.RemoveListener(HandleTargetLocked);
            _lockOnManager.OnTargetUnlocked.RemoveListener(HandleTargetUnlocked);
        }
    }

    private void HandleTargetLocked(Transform enemyProxy)
    {
        // No camera switching! The exploration camera stays active.
        // The yellow diamond indicator and player strafing handle the rest.
        Debug.Log($"Locked onto target at {enemyProxy.position}");
    }

    private void HandleTargetUnlocked()
    {
        Debug.Log("Lock-on released. Returning to free movement.");
    }
}
