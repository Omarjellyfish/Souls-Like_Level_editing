using UnityEngine;
using CombatSystem; // To access LockOnManager

public class CameraManager : MonoBehaviour
{
    [Header("Camera Setup")]
    [Tooltip("The Cinemachine FreeLook camera object used for normal exploration.")]
    [SerializeField] private GameObject _freeLookCamera;
    
    [Tooltip("The Cinemachine Virtual Camera object used during combat lock-on.")]
    [SerializeField] private GameObject _lockOnCamera;

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

        // Start the game in Exploration mode
        EnableFreeLook();
    }

    private void OnEnable()
    {
        if (_lockOnManager != null)
        {
            _lockOnManager.OnTargetLocked.AddListener(HandleTargetLocked);
            _lockOnManager.OnTargetUnlocked.AddListener(EnableFreeLook);
        }
    }

    private void OnDisable()
    {
        if (_lockOnManager != null)
        {
            _lockOnManager.OnTargetLocked.RemoveListener(HandleTargetLocked);
            _lockOnManager.OnTargetUnlocked.RemoveListener(EnableFreeLook);
        }
    }

    private void HandleTargetLocked(Transform enemyProxy)
    {
        // 1. Turn off the FreeLook camera
        if (_freeLookCamera != null) _freeLookCamera.SetActive(false);
        
        // 2. Turn on the LockOn camera (Cinemachine automatically creates a super smooth blend when we do this!)
        if (_lockOnCamera != null) _lockOnCamera.SetActive(true);
        
        Debug.Log("Switched to Lock-On Camera!");
    }

    private void EnableFreeLook()
    {
        // 1. Turn off the combat camera
        if (_lockOnCamera != null) _lockOnCamera.SetActive(false);
        
        // 2. Turn on the exploration camera
        if (_freeLookCamera != null) _freeLookCamera.SetActive(true);
    }
}
