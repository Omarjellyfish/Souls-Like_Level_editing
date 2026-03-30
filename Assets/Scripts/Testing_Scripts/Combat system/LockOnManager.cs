using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CombatSystem
{
    public class LockOnManager : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Maximum distance the player can be to lock onto an enemy.")]
        public float maxLockOnDistance = 15f;
        
        [Tooltip("The maximum angle from the camera's forward direction to consider a target.")]
        public float maxViewAngle = 60f;
        
        [Tooltip("Objects on these layers can be locked onto.")]
        public LayerMask targetLayer;
        
        [Tooltip("Objects on these layers will block line of sight (e.g. walls, ground).")]
        public LayerMask obstructionLayer;

        [Header("References")]
        [Tooltip("The main camera or the camera calculating what 'forward' is.")]
        public Transform playerCamera;

        [Header("Events (Modular Hooks)")]
        [Tooltip("Fired when a target is successfully locked. Returns the Transform of the Proxy (to be used by Cinemachine LookAt).")]
        public UnityEvent<Transform> OnTargetLocked;
        
        [Tooltip("Fired when lock-on drops or is disabled.")]
        public UnityEvent OnTargetUnlocked;

        public bool IsLockedOn { get; private set; }

        private Collider _currentTarget;
        private Transform _lockOnProxy;

        private void Awake()
        {
            // We create a dummy object at runtime.
            // This object will be snapped to the exact center of the enemy's collider.
            // You can easily plug this Transform into a Cinemachine 'LookAt' target.
            GameObject proxyObj = new GameObject("LockOnProxy");
            _lockOnProxy = proxyObj.transform;
            _lockOnProxy.SetParent(null); // Ensure it's not parented so it moves cleanly in world space
        }

        private void Update()
        {
            HandleInput();

            if (IsLockedOn)
            {
                UpdateLockOn();
            }
        }

        private void HandleInput()
        {
            // Toggle Lock-On
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (!IsLockedOn)
                {
                    TryLockOn();
                }
                else
                {
                    Unlock();
                }
            }

            // Target Switching
            if (IsLockedOn)
            {
                float scrollAmount = Input.mouseScrollDelta.y;
                if (scrollAmount > 0)
                {
                    SwitchTarget(1); // Right
                }
                else if (scrollAmount < 0)
                {
                    SwitchTarget(-1); // Left
                }
            }
        }

        private void UpdateLockOn()
        {
            // 1. Check Distance
            if (Vector3.Distance(playerCamera.position, _currentTarget.transform.position) > maxLockOnDistance)
            {
                Unlock();
                return;
            }

            // 2. Check Line of Sight
            Vector3 targetCenter = _currentTarget.bounds.center;
            Vector3 dirToTarget = (targetCenter - playerCamera.position).normalized;
            float distToTarget = Vector3.Distance(playerCamera.position, targetCenter);

            if (Physics.Raycast(playerCamera.position, dirToTarget, distToTarget, obstructionLayer))
            {
                // Blocked by a wall or obstacle
                Unlock();
                return;
            }

            // 3. Keep Proxy attached to the Center of the Bounds
            _lockOnProxy.position = targetCenter;
        }

        private void TryLockOn()
        {
            Collider bestTarget = GetBestTarget();
            if (bestTarget != null)
            {
                LockOnTo(bestTarget);
            }
        }

        private void LockOnTo(Collider target)
        {
            _currentTarget = target;
            IsLockedOn = true;
            
            // Snap proxy to position immediately so the camera doesn't jerk from (0,0,0)
            _lockOnProxy.position = _currentTarget.bounds.center;
            
            OnTargetLocked?.Invoke(_lockOnProxy);
        }

        private void Unlock()
        {
            _currentTarget = null;
            IsLockedOn = false;
            OnTargetUnlocked?.Invoke();
        }

        private Collider GetBestTarget()
        {
            Collider[] hits = Physics.OverlapSphere(playerCamera.position, maxLockOnDistance, targetLayer);
            Collider bestTarget = null;
            float minAngle = maxViewAngle;

            foreach (var hit in hits)
            {
                Vector3 targetCenter = hit.bounds.center;
                Vector3 dirToTarget = (targetCenter - playerCamera.position).normalized;
                float angle = Vector3.Angle(playerCamera.forward, dirToTarget);

                // If within our view cone and closest to the center of the screen
                if (angle < minAngle)
                {
                    // Verify Line of Sight
                    float dist = Vector3.Distance(playerCamera.position, targetCenter);
                    if (!Physics.Raycast(playerCamera.position, dirToTarget, dist, obstructionLayer))
                    {
                        bestTarget = hit;
                        minAngle = angle; // We want the one closest to the center of the screen (lowest angle)
                    }
                }
            }

            return bestTarget;
        }

        private void SwitchTarget(int direction)
        {
            Collider[] hits = Physics.OverlapSphere(playerCamera.position, maxLockOnDistance, targetLayer);
            List<Collider> validTargets = new List<Collider>();

            // Collect all valid targets in range & sight
            foreach (var hit in hits)
            {
                if (hit == _currentTarget) continue;

                Vector3 targetCenter = hit.bounds.center;
                Vector3 dirToTarget = (targetCenter - playerCamera.position).normalized;

                if (Vector3.Angle(playerCamera.forward, dirToTarget) < maxViewAngle)
                {
                    float dist = Vector3.Distance(playerCamera.position, targetCenter);
                    if (!Physics.Raycast(playerCamera.position, dirToTarget, dist, obstructionLayer))
                    {
                        validTargets.Add(hit);
                    }
                }
            }

            if (validTargets.Count == 0) return;

            Collider bestTarget = null;
            float closestAngle = float.MaxValue;

            Vector3 currentDir = (_currentTarget.bounds.center - playerCamera.position).normalized;
            // Project onto the XZ plane to ignore height differences when picking left/right
            currentDir.y = 0; 
            currentDir.Normalize();

            foreach (var hit in validTargets)
            {
                Vector3 dirToTarget = (hit.bounds.center - playerCamera.position).normalized;
                dirToTarget.y = 0; 
                dirToTarget.Normalize();

                // Signed angle tells us if the target is to the Left (-) or Right (+)
                float signedAngle = Vector3.SignedAngle(currentDir, dirToTarget, Vector3.up);

                // direction == 1 means we are scrolling to the Right
                if (direction > 0 && signedAngle > 0)
                {
                    if (signedAngle < closestAngle)
                    {
                        closestAngle = signedAngle;
                        bestTarget = hit;
                    }
                }
                // direction == -1 means we are scrolling to the Left
                else if (direction < 0 && signedAngle < 0)
                {
                    if (Mathf.Abs(signedAngle) < closestAngle)
                    {
                        closestAngle = Mathf.Abs(signedAngle);
                        bestTarget = hit;
                    }
                }
            }

            // Only switch if we found someone explicitly in that direction
            if (bestTarget != null)
            {
                LockOnTo(bestTarget);
            }
        }
    }
}
