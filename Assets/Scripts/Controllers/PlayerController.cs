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
    private Vector3 _moveVelocity;
    private Vector3 _gravityVelocity;
    private Vector3 _slopeVelocity;

    private readonly Vector3 _gravityDir = Vector3.down;
    private Vector3 _groundNormal = Vector3.up;


    [Header("Move")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _moveAcceleration = 15f;
    [SerializeField] private float _moveResponseTime = 0.2f;

    [Header("Gravity")]
    [SerializeField] private float _gravityAcceleration = 9.8f;
    [SerializeField] private float _groundStickSpeed = 2f;

    [Header("Check Ground")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckOffset = 0.05f;
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
            Jump();
    }

    private void Jump()
    {
        _gravityVelocity.y = _jumpForce;
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

        bool hasMoveInput =
            moveDirection.sqrMagnitude > 0.001f;

        if (hasMoveInput)
            moveDirection.Normalize();

        bool grounded = IsGrounded();

        if (hasMoveInput)
        {
            if (grounded)
            {
                moveDirection = Vector3.ProjectOnPlane(
                    moveDirection, _groundNormal).normalized;

                Vector3 surfaceVelocity = Vector3.ProjectOnPlane(
                    _moveVelocity, _groundNormal);

                Vector3 targetVelocity = moveDirection * _moveSpeed;

                Vector3 acceleration = (targetVelocity - surfaceVelocity) /
                    Mathf.Max(_moveResponseTime, 0.001f);

                _moveVelocity = surfaceVelocity +
                    acceleration * Time.deltaTime;
            }
            else
            {
                float currentSpeedInInputDirection = Vector3.Dot(
                    _moveVelocity, moveDirection);

                float missingSpeed = Mathf.Max(0f,
                    _moveSpeed - currentSpeedInInputDirection);

                Vector3 acceleration = moveDirection *
                    (missingSpeed / Mathf.Max(_moveResponseTime, 0.001f));

                _moveVelocity += acceleration * Time.deltaTime;
            }
        }

        ApplyGravity(grounded, moveInput, moveDirection);

        Vector3 finalVelocity = _moveVelocity + _gravityVelocity
            + _slopeVelocity;

        _characterController.Move(finalVelocity * Time.deltaTime);
    }

    private void ApplyGravity(
        bool grounded,
        Vector2 moveInput,
        Vector3 moveDirection)
    {
        bool movingAgainstGravity = Vector3.Dot(
            _gravityVelocity, _gravityDir) < 0f;

        if (!grounded || movingAgainstGravity)
        {
            _gravityVelocity += _gravityDir *
                _gravityAcceleration *
                Time.deltaTime;

            _slopeVelocity = Vector3.zero;

            return;
        }

        _gravityVelocity = _gravityDir * _groundStickSpeed;

        Vector3 slopeGravity = Vector3.ProjectOnPlane(
            _gravityDir * _gravityAcceleration, _groundNormal);

        bool hasMoveInput = moveInput.sqrMagnitude > 0.001f;

        bool shouldApplySlopeGravity = !hasMoveInput;

        if (hasMoveInput && slopeGravity.sqrMagnitude > 0.001f)
        {
            Vector3 slopeDirection = slopeGravity.normalized;

            float inputDot = Vector3.Dot(
                moveDirection, slopeDirection);

            shouldApplySlopeGravity = inputDot > 0.001f;
        }

        if (shouldApplySlopeGravity)
            _slopeVelocity += slopeGravity * Time.deltaTime;
        else
            _slopeVelocity = Vector3.zero;
    }

    private bool IsGrounded()
    {
        Vector3 center = transform.TransformPoint(_characterController.center);

        float radius = _characterController.radius * 0.9f;

        if (Physics.SphereCast(
            center, radius, Vector3.down, out RaycastHit hit,
            radius + _groundCheckDistance, _groundLayer, QueryTriggerInteraction.Ignore))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);

            return angle <= _maxGroundAngle;
        }

        return false;
    }

    private void UpdateBodyVisual()
    {
        Vector3 totalVelocity = _moveVelocity + _gravityVelocity;

        Vector3 rotationUp = IsGrounded() ? _groundNormal : Vector3.up;

        Vector3 rollingVelocity = Vector3.ProjectOnPlane(totalVelocity, rotationUp);

        if (rollingVelocity.sqrMagnitude < 0.001f)
            return;

        Vector3 moveDirection = rollingVelocity.normalized;
        Vector3 rotationAxis = Vector3.Cross(rotationUp, moveDirection);

        float distance = rollingVelocity.magnitude * Time.deltaTime;
        float rotationAngle = distance / _bodyRadius * Mathf.Rad2Deg;

        _body.rotation =
            Quaternion.AngleAxis(rotationAngle, rotationAxis) * _body.rotation;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (((1 << hit.gameObject.layer) & _groundLayer) == 0)
            return;

        float groundDot = Vector3.Dot(hit.normal, -_gravityDir);

        if (groundDot > 0f)
            _groundNormal = hit.normal;
    }
}
