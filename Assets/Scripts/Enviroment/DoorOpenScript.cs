using UnityEngine;

public class DoorOpenScript : MonoBehaviour
{
    [SerializeField] private Vector3 _openRotationOffset = new Vector3(0f, -90f, 0f);
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private bool _isOpen = false;

    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private Quaternion _targetRotation;

    private void Awake()
    {
        _closedRotation = transform.rotation;
        _openRotation = _closedRotation * Quaternion.Euler(_openRotationOffset);

        _targetRotation = _isOpen ? _openRotation : _closedRotation;

        // Snap to initial state immediately
        transform.rotation = _targetRotation;
    }

    private void Update()
    {
        // Allow modifying the _isOpen bool via Inspector to immediately update the behavior
        _targetRotation = _isOpen ? _openRotation : _closedRotation;

        if (transform.rotation != _targetRotation)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * _rotationSpeed);
        }
    }

    public void ToggleDoor()
    {
        _isOpen = !_isOpen;
    }

    public void OpenDoor()
    {
        _isOpen = true;
    }

    public void CloseDoor()
    {
        _isOpen = false;
    }
}
