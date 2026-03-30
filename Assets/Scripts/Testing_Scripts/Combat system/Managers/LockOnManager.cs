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

        [Header("Indicator Settings")]
        [Tooltip("Size of the lock-on diamond indicator.")]
        [SerializeField] private float _indicatorSize = 0.4f;
        [Tooltip("Width of the indicator lines.")]
        [SerializeField] private float _indicatorLineWidth = 0.04f;
        [Tooltip("Color of the lock-on indicator.")]
        [SerializeField] private Color _indicatorColor = Color.yellow;

        [Header("Events (Modular Hooks)")]
        [Tooltip("Fired when a target is successfully locked. Returns the Transform of the Proxy.")]
        public UnityEvent<Transform> OnTargetLocked;
        
        [Tooltip("Fired when lock-on drops or is disabled.")]
        public UnityEvent OnTargetUnlocked;

        public bool IsLockedOn { get; private set; }

        /// <summary>
        /// Exposes the proxy transform so other scripts (like Movement) can read where the enemy is.
        /// Returns null when not locked on.
        /// </summary>
        public Transform CurrentTargetProxy => IsLockedOn ? _lockOnProxy : null;

        private Collider _currentTarget;
        private Transform _lockOnProxy;
        private GameObject _indicatorObject;

        private void Awake()
        {
            // We create a dummy object at runtime.
            // This object will be snapped to the exact center of the enemy's collider.
            GameObject proxyObj = new GameObject("LockOnProxy");
            _lockOnProxy = proxyObj.transform;
            _lockOnProxy.SetParent(null);

            CreateIndicator();
        }

        /// <summary>
        /// Creates a yellow diamond shape using LineRenderer. No prefab needed.
        /// The diamond is parented to the proxy so it automatically follows the enemy.
        /// </summary>
        private void CreateIndicator()
        {
            _indicatorObject = new GameObject("LockOn_Indicator");
            _indicatorObject.transform.SetParent(_lockOnProxy);
            _indicatorObject.transform.localPosition = Vector3.zero;

            LineRenderer lr = _indicatorObject.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = 4;
            lr.startWidth = _indicatorLineWidth;
            lr.endWidth = _indicatorLineWidth;

            // Diamond shape (4 points: top, right, bottom, left)
            lr.SetPosition(0, new Vector3(0, _indicatorSize, 0));
            lr.SetPosition(1, new Vector3(_indicatorSize, 0, 0));
            lr.SetPosition(2, new Vector3(0, -_indicatorSize, 0));
            lr.SetPosition(3, new Vector3(-_indicatorSize, 0, 0));

            // Unlit yellow material so it's always visible
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = _indicatorColor;
            lr.endColor = _indicatorColor;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            _indicatorObject.SetActive(false);
        }

        private void Update()
        {
            HandleInput();

            if (IsLockedOn)
            {
                UpdateLockOn();
                BillboardIndicator();
            }
        }

        /// <summary>
        /// Keeps the diamond indicator facing the camera so it's always readable.
        /// </summary>
        private void BillboardIndicator()
        {
            if (_indicatorObject != null && playerCamera != null)
            {
                Vector3 lookDir = _indicatorObject.transform.position - playerCamera.position;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    _indicatorObject.transform.rotation = Quaternion.LookRotation(lookDir);
                }
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

            // Show the yellow diamond
            if (_indicatorObject != null) _indicatorObject.SetActive(true);
            
            OnTargetLocked?.Invoke(_lockOnProxy);
        }

        private void Unlock()
        {
            _currentTarget = null;
            IsLockedOn = false;

            // Hide the yellow diamond
            if (_indicatorObject != null) _indicatorObject.SetActive(false);
            
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
                        minAngle = angle;
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
            currentDir.y = 0; 
            currentDir.Normalize();

            foreach (var hit in validTargets)
            {
                Vector3 dirToTarget = (hit.bounds.center - playerCamera.position).normalized;
                dirToTarget.y = 0; 
                dirToTarget.Normalize();

                float signedAngle = Vector3.SignedAngle(currentDir, dirToTarget, Vector3.up);

                if (direction > 0 && signedAngle > 0)
                {
                    if (signedAngle < closestAngle)
                    {
                        closestAngle = signedAngle;
                        bestTarget = hit;
                    }
                }
                else if (direction < 0 && signedAngle < 0)
                {
                    if (Mathf.Abs(signedAngle) < closestAngle)
                    {
                        closestAngle = Mathf.Abs(signedAngle);
                        bestTarget = hit;
                    }
                }
            }

            if (bestTarget != null)
            {
                LockOnTo(bestTarget);
            }
        }
    }
}
