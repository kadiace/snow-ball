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
    private Vector3 _rollControlVelocity;
    private Vector3 _slopeVelocity;

    private Vector3 _airVelocity;

    private Vector3 _gravityVelocity;
    private Vector3 _groundStickVelocity;
    private bool _isGrounded;
    private float _groundCheckIgnoreTimer;
    private readonly Vector3 _gravityDir = Vector3.down;
    private Vector3 _groundNormal = Vector3.up;


    [Header("Move")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _moveResponseTime = 0.2f;

    [Header("Gravity")]
    [SerializeField] private float _gravityAcceleration = 9.8f;
    [SerializeField] private float _groundStickSpeed = 2f;

    [Header("Check Ground")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckOffset = 0.01f;
    [SerializeField] private float _maxGroundAngle = 30f;

    [Header("Jump")]
    [SerializeField] private float _jumpForce = 8f;

    [Header("Air Move")]
    [SerializeField] private float _airMoveAcceleration = 15f;

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

    private void OnDestroy()
    {
        _inputActions.Player.Jump.performed -= OnJumpPerformed;
        _inputActions.Dispose();
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
        if (_groundCheckIgnoreTimer > 0f)
            _groundCheckIgnoreTimer -= Time.deltaTime;

        bool wasGrounded = _isGrounded;
        bool isGrounded = IsGrounded();
        _isGrounded = isGrounded;

        _moveInput =
            _inputActions.Player.Move.ReadValue<Vector2>();

        MoveCharacter(_moveInput, isGrounded);

        UpdateBodyVisual(isGrounded);

        Debug.Log($"Ground: {isGrounded}\n Ground Normal: {_groundNormal}\nJumpTimer:{_groundCheckIgnoreTimer}");
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (_isGrounded)
            Jump();
    }

    private bool IsGrounded()
    {
        if (_groundCheckIgnoreTimer > 0f)
            return false;

        Vector3 center = transform.TransformPoint(_characterController.center);

        float controllerRadius = _characterController.radius;
        float checkRadius = controllerRadius * 0.9f;

        float castDistance = controllerRadius - checkRadius +
            _groundCheckOffset;

        if (Physics.SphereCast(
            center,
            checkRadius,
            Vector3.down,
            out RaycastHit hit,
            castDistance,
            _groundLayer,
            QueryTriggerInteraction.Ignore))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);

            if (angle <= _maxGroundAngle)
            {
                _groundNormal = hit.normal;
                return true;
            }
        }

        return false;
    }

    private void Jump()
    {
        _slopeVelocity =
            Vector3.ProjectOnPlane(
                _slopeVelocity,
                Vector3.up
            );

        _gravityVelocity =
            Vector3.up * _jumpForce;

        _groundCheckIgnoreTimer = 0.1f;
    }

    private void MoveCharacter(Vector2 moveInput, bool grounded)
    {
        Vector3 worldMoveInput = GetWorldMoveInput(moveInput);

        if (grounded)
            Roll(worldMoveInput);
        else
            AirMove(worldMoveInput);

        Vector3 finalVelocity = _rollControlVelocity + _slopeVelocity
            + _groundStickVelocity + _airVelocity + _gravityVelocity;

        _characterController.Move(finalVelocity * Time.deltaTime);
    }

    private Vector3 GetWorldMoveInput(Vector2 moveInput)
    {
        Vector3 cameraForward = _camera.transform.forward;
        Vector3 cameraRight = _camera.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 worldMoveInput = cameraForward * moveInput.y +
            cameraRight * moveInput.x;

        return Vector3.ClampMagnitude(worldMoveInput, 1f);
    }

    private void Roll(Vector3 worldMoveInput)
    {
        _gravityVelocity = Vector3.zero;

        TransferAirVelocityToRoll();

        Vector3 groundMoveDirection =
            GetGroundMoveDirection(worldMoveInput);

        bool hasMoveInput =
            worldMoveInput.sqrMagnitude > 0.001f;

        UpdateSlopeVelocity(
            groundMoveDirection,
            worldMoveInput.sqrMagnitude > 0.001f
        );

        UpdateGroundMoveVelocity(
            groundMoveDirection,
            worldMoveInput.magnitude
        );

        UpdateGroundStickVelocity();
    }

    private void TransferAirVelocityToRoll()
    {
        if (_airVelocity.sqrMagnitude <= 0.001f)
            return;

        _rollControlVelocity +=
            Vector3.ProjectOnPlane(
                _airVelocity,
                _groundNormal
            );

        _airVelocity = Vector3.zero;
    }

    private Vector3 GetGroundMoveDirection(Vector3 worldMoveInput)
    {
        Vector3 groundMoveDirection =
            Vector3.ProjectOnPlane(
                worldMoveInput,
                _groundNormal
            );

        if (groundMoveDirection.sqrMagnitude <= 0.001f)
            return Vector3.zero;

        return groundMoveDirection.normalized;
    }

    private void UpdateSlopeVelocity(
        Vector3 groundMoveDirection,
        bool hasMoveInput)
    {
        _slopeVelocity =
            Vector3.ProjectOnPlane(
                _slopeVelocity,
                _groundNormal
            );

        Vector3 gravity =
            _gravityDir * _gravityAcceleration;

        Vector3 slopeGravity =
            Vector3.ProjectOnPlane(
                gravity,
                _groundNormal
            );

        bool isDownhillInput =
            Vector3.Dot(
                groundMoveDirection,
                slopeGravity
            ) >= 0f;

        if (!hasMoveInput || isDownhillInput)
        {
            _slopeVelocity +=
                slopeGravity * Time.deltaTime;
        }
    }

    private void UpdateGroundMoveVelocity(
        Vector3 groundMoveDirection,
        float inputMagnitude)
    {
        inputMagnitude =
            Mathf.Clamp01(inputMagnitude);

        if (inputMagnitude <= 0.001f)
        {
            float _responseSpeed =
                _moveSpeed / _moveResponseTime;

            _rollControlVelocity =
                Vector3.MoveTowards(
                    _rollControlVelocity,
                    Vector3.zero,
                    _responseSpeed * Time.deltaTime
                );

            return;
        }

        float targetSpeed =
            _moveSpeed * inputMagnitude;

        Vector3 currentRollVelocity =
            _rollControlVelocity +
            _slopeVelocity;

        float currentSpeed =
            Vector3.Dot(
                currentRollVelocity,
                groundMoveDirection
            );

        if (currentSpeed >= targetSpeed)
            return;

        float remainingSpeed =
            targetSpeed - currentSpeed;

        float responseSpeed =
            _moveSpeed / _moveResponseTime;

        float addedSpeed =
            Mathf.Min(
                responseSpeed * Time.deltaTime,
                remainingSpeed
            );

        _rollControlVelocity +=
            groundMoveDirection * addedSpeed;
    }

    private void UpdateGroundStickVelocity()
    {
        _groundStickVelocity =
            -_groundNormal * _groundStickSpeed;
    }

    private void AirMove(Vector3 worldMoveInput)
    {
        TransferGroundVelocityToAir();

        UpdateAirMoveVelocity(worldMoveInput);
        UpdateGravityVelocity();
    }

    private void TransferGroundVelocityToAir()
    {
        _groundStickVelocity = Vector3.zero;
        _airVelocity += _rollControlVelocity + _slopeVelocity;
        _rollControlVelocity = Vector3.zero;
        _slopeVelocity = Vector3.zero;
    }

    private void UpdateAirMoveVelocity(Vector3 worldMoveInput)
    {
        float inputMagnitude =
            Mathf.Clamp01(worldMoveInput.magnitude);

        if (inputMagnitude <= 0f)
            return;

        Vector3 moveDirection =
            worldMoveInput.normalized;

        float targetSpeed =
            _moveSpeed * inputMagnitude;

        float currentSpeed =
            Vector3.Dot(
                _airVelocity,
                moveDirection
            );

        float remainingSpeed =
            targetSpeed - currentSpeed;

        if (remainingSpeed <= 0f)
            return;

        float accelerationSpeed =
            _airMoveAcceleration *
            inputMagnitude *
            Time.deltaTime;

        accelerationSpeed =
            Mathf.Min(
                accelerationSpeed,
                remainingSpeed
            );

        _airVelocity +=
            moveDirection * accelerationSpeed;
    }

    private void UpdateGravityVelocity()
    {
        Vector3 gravity =
            _gravityDir * _gravityAcceleration;

        _gravityVelocity +=
            gravity * Time.deltaTime;
    }

    private void UpdateBodyVisual(bool grounded)
    {
        Vector3 totalVelocity = _rollControlVelocity + _slopeVelocity
            + _groundStickVelocity + _airVelocity + _gravityVelocity; ;

        Vector3 rotationUp = grounded ? _groundNormal : Vector3.up;

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
