using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public PlayerController _player;

    [SerializeField] private float _distance = 5f;

    [SerializeField] private float _lookSensitivity = 0.15f;
    [SerializeField] private float _minPitch = -80f;
    [SerializeField] private float _maxPitch = 80f;

    [Header("Follow")]
    [SerializeField] private float _followDamping = 20f;

    private InputAction _look;

    private float _yaw;
    private float _pitch;

    void Awake()
    {
        _look = InputSystem.actions.FindAction("Look");

        GameObject playerObject =
            Resources.Load<GameObject>("Prefabs/Player");

        _player =
            Instantiate(playerObject)
            .GetComponent<PlayerController>();

        Vector3 angles = transform.eulerAngles;

        _yaw = angles.y;
        _pitch = angles.x;

        if (_pitch > 180f)
            _pitch -= 360f;
    }

    void LateUpdate()
    {
        UpdateLook();
        FollowPlayer();
    }

    private void UpdateLook()
    {
        Vector2 lookInput =
            _look.ReadValue<Vector2>();

        _yaw +=
            lookInput.x * _lookSensitivity;

        _pitch -=
            lookInput.y * _lookSensitivity;

        _pitch =
            Mathf.Clamp(
                _pitch,
                _minPitch,
                _maxPitch
            );
    }

    private void FollowPlayer()
    {
        Quaternion rotation =
            Quaternion.Euler(
                _pitch,
                _yaw,
                0f
            );

        Vector3 targetPosition =
            _player.transform.position +
            rotation * Vector3.back * _distance;

        float t =
            1f -
            Mathf.Exp(
                -_followDamping * Time.deltaTime
            );

        transform.position =
            Vector3.Lerp(
                transform.position,
                targetPosition,
                t
            );

        Vector3 lookDirection =
            _player.transform.position -
            transform.position;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation =
                Quaternion.LookRotation(
                    lookDirection,
                    Vector3.up
                );
        }
    }
}
