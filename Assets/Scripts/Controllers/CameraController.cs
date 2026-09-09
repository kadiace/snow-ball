using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public PlayerController _player;

    [SerializeField] private float _distance = 5f;
    [SerializeField] private float _lookSensitivity = 0.15f;
    [SerializeField] private float _minPitch = -80f;
    [SerializeField] private float _maxPitch = 80f;

    private InputAction _look;

    private Vector3 _forward;
    private Vector3 _previousUp;

    private float _pitch;

    void Awake()
    {
        _look = InputSystem.actions.FindAction("Look");

        GameObject playerObject = Resources.Load<GameObject>($"Prefabs/Player");

        _player = Instantiate(playerObject).GetComponent<PlayerController>();

        Vector3 up = _player.GravityDir;

        _forward = Vector3.ProjectOnPlane(transform.forward, up);

        if (_forward.sqrMagnitude < 0.001f)
        {
            _forward =
                Vector3.ProjectOnPlane(_player.transform.forward, up);
        }
    }

    void LateUpdate()
    {
        Vector3 up = -_player.GravityDir;

        Quaternion gravityRotation =
            Quaternion.FromToRotation(_previousUp, up);

        _forward = gravityRotation * _forward;

        Vector2 lookInput = _look.ReadValue<Vector2>();

        ApplyYaw(lookInput.x, up);
        ApplyPitch(lookInput.y, up);

        transform.position =
            _player.transform.position - _forward * _distance;

        transform.rotation =
            Quaternion.LookRotation(_forward, up);

        _previousUp = up;
    }

    private void ApplyYaw(float input, Vector3 up)
    {
        Quaternion rotation =
            Quaternion.AngleAxis(
                input * _lookSensitivity,
                up
            );

        _forward = rotation * _forward;
    }

    private void ApplyPitch(float input, Vector3 up)
    {
        float pitchDelta =
            -input * _lookSensitivity;

        float targetPitch =
            Mathf.Clamp(
                _pitch + pitchDelta,
                _minPitch,
                _maxPitch
            );

        pitchDelta = targetPitch - _pitch;
        _pitch = targetPitch;

        Vector3 right =
            Vector3.Cross(up, _forward).normalized;

        Quaternion rotation =
            Quaternion.AngleAxis(
                pitchDelta,
                right
            );

        _forward = rotation * _forward;
    }
}
