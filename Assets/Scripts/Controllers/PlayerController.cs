using Input;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private PlayerInputActions _inputActions;
    private CharacterController _characterController;
    private Transform _body;

    private CameraController _camera;

    private Vector2 _moveInput;
    private Vector3 _velocity;

    private readonly Vector3 _gravityDir = Vector3.down;


    [Header("Move")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _moveAcceleration = 15f;

    [Header("Gravity")]
    [SerializeField] private float _gravityAcceleration = 9.8f;

    [SerializeField] private float _groundCheckOffset = 0.05f;

    [Header("Check Ground")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckDistance = 0.3f;
    [SerializeField] private float _maxGroundAngle = 80f;

    [Header("Jump")]
    [SerializeField] private float _jumpForce = 8f;

    [Header("Body Visual")]
    [SerializeField] private float _bodyRadius = 0.5f;


    private void Awake()
    {
        _inputActions = new PlayerInputActions();
        _inputActions.Player.Jump.performed += OnJumpPerformed;

        _characterController =
            gameObject.GetorAddComponent<CharacterController>();

        _body = transform.Find("Body");

        _camera = Camera.main.GetComponent<CameraController>();
    }

    private void OnEnable()
    {
        _inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        _inputActions.Player.Disable();
    }

    private void Update()
    {
        _moveInput =
            _inputActions.Player.Move.ReadValue<Vector2>();

        if (_characterController.enabled)
            MoveCharacter(_moveInput);

        UpdateBodyVisual();
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (IsGrounded())
        {
            Jump();
        }
    }

    private void Jump()
    {
        _velocity.y = _jumpForce;
    }

    private void MoveCharacter(Vector2 moveInput)
    {
        Vector3 cameraForward = _camera.transform.forward;
        Vector3 cameraRight = _camera.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection =
            cameraForward * moveInput.y +
            cameraRight * moveInput.x;

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        Vector3 targetVelocity =
            moveDirection * _moveSpeed;

        Vector3 horizontalVelocity =
            new Vector3(
                _velocity.x,
                0f,
                _velocity.z
            );

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            _moveAcceleration * Time.deltaTime
        );

        _velocity.x = horizontalVelocity.x;
        _velocity.z = horizontalVelocity.z;

        ApplyGravity();

        _characterController.Move(
            _velocity * Time.deltaTime
        );
    }

    private void ApplyGravity()
    {
        _velocity +=
            _gravityDir *
            _gravityAcceleration *
            Time.deltaTime;
    }

    private bool IsGrounded()
    {
        Vector3 center =
    transform.TransformPoint(_characterController.center);

        float radius =
            _characterController.radius * 0.9f;

        if (Physics.SphereCast(
            center,
            radius,
            Vector3.down,
            out RaycastHit hit,
            radius + _groundCheckDistance,
            _groundLayer,
            QueryTriggerInteraction.Ignore))
        {
            float angle =
                Vector3.Angle(hit.normal, Vector3.up);

            return angle <= _maxGroundAngle;
        }

        return false;
    }

    private void UpdateBodyVisual()
    {
        Vector3 moveVelocity = new Vector3(
            _velocity.x,
            0f,
            _velocity.z
        );

        if (moveVelocity.sqrMagnitude < 0.001f)
            return;

        Vector3 moveDirection =
            moveVelocity.normalized;

        Vector3 rotationAxis =
            Vector3.Cross(
                Vector3.up,
                moveDirection
            );

        float distance =
            moveVelocity.magnitude * Time.deltaTime;

        float rotationAngle =
            distance / _bodyRadius * Mathf.Rad2Deg;

        _body.rotation =
            Quaternion.AngleAxis(
                rotationAngle,
                rotationAxis
            ) * _body.rotation;
    }
}
