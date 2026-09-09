using Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class PlayerController : MonoBehaviour
{
    private PlayerInputActions _inputActions;
    private Rigidbody _rb;
    private CharacterController _characterController;
    private Transform _body;
    private CapsuleCollider _collider;
    private Transform _wing;

    private CameraController _camera;

    private Quaternion _bodyDefaultRotation;
    private Vector3 _wingDefaultScale;


    private Vector2 _moveInput;
    private Vector3 _characterMoveDirection;
    private float _characterFallSpeed;
    private bool _isGliding;

    public Vector3 GravityDir { get; set; }

    [Tooltip("Move")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _groundStickSpeed = 2f;
    [SerializeField] private float _bodyRecoverAngle = 1f;

    [Tooltip("Gravity")]
    [SerializeField] private float _gravityAcceleration = 9.8f;
    [SerializeField] private float _gravityRotationSpeed = 5f;
    [SerializeField] private float _maxFallSpeed = 30f;
    [SerializeField] private float _gravityAlignAngle = 1f;

    [Tooltip("Check Ground")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckDistance = 0.1f;

    [Tooltip("Jump")]
    [SerializeField] private float _jumpForce = 8f;

    [Tooltip("Glide")]
    [SerializeField] private float _glideVisualSpeed = 5f;
    [SerializeField] private float _glideTurnSpeed = 90f;
    [SerializeField] private float _glideFallSpeed = 2f;
    [SerializeField] private float _glideInitialSpeed = 5f;
    [SerializeField] private float _glideConversionSpeed = 10f;
    [SerializeField] private float _glideAcceleration = 50f;
    [SerializeField] private float _glideBankAngle = 30f;
    [SerializeField] private float _glideBankSensitivity = 1f;

    private void OnEnable()
    {
        _inputActions.Player.Enable();
    }

    void Awake()
    {
        _inputActions = new PlayerInputActions();
        _inputActions.Player.Jump.performed += OnJumpPerformed;
        _inputActions.Player.Jump.canceled += OnJumpCanceled;

        _rb = gameObject.GetorAddComponent<Rigidbody>();
        _characterController = gameObject.GetorAddComponent<CharacterController>();
        _body = transform.Find("Body");
        _wing = transform.Find("Body/Wing");
        _bodyDefaultRotation = _body.localRotation;
        _wingDefaultScale = _wing.localScale;
        _collider = _body.GetComponent<CapsuleCollider>();

        _camera = Camera.main.GetComponent<CameraController>();

        GravityDir = Vector3.down;
    }

    void Start()
    {

    }

    void Update()
    {
        _moveInput = _inputActions.Player.Move.ReadValue<Vector2>();
        UpdateBodyVisual();

        if (_characterController.enabled)
        {
            MoveCharacter(_moveInput, GravityDir);
            return;
        }
    }

    void FixedUpdate()
    {
        if (_characterController.enabled)
            return;

        Vector2 moveInput = _moveInput;
        Vector3 gravityDir = GravityDir;

        if (_isGliding)
        {
            if (IsGrounded())
            {
                _isGliding = false;
                SwitchToCharacterMode();
                return;
            }
            Glide(moveInput, gravityDir);
            return;
        }
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (context.interaction is TapInteraction)
            Jump();
        else if (context.interaction is HoldInteraction && !IsGrounded())
        {
            SwitchToRigidBodyMode();
            _isGliding = true;
        }
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        if (context.interaction is HoldInteraction)
        {
            _isGliding = false;
            SwitchToCharacterMode();
        }
    }

    private void Jump()
    {
        Vector3 velocity = _rb.linearVelocity;

        float gravityVelocity = Vector3.Dot(velocity, GravityDir);

        if (gravityVelocity > 0f)
        {
            velocity -= GravityDir * gravityVelocity;
            _rb.linearVelocity = velocity;
        }

        _rb.AddForce(-GravityDir * _jumpForce, ForceMode.Impulse);
    }

    private void Glide(Vector2 moveInput, Vector3 gravityDir)
    {
        // No Inputs
        if (moveInput.sqrMagnitude < 0.001f)
        {
            ApplyGravity(gravityDir, _glideAcceleration);
            return;
        }

        // Get camera dir, move dir
        Vector3 cameraForward =
            Vector3.ProjectOnPlane(_camera.transform.forward, gravityDir).normalized;
        if (cameraForward.sqrMagnitude < 0.001f)
            cameraForward = Vector3.ProjectOnPlane(_camera.transform.up, gravityDir);
        cameraForward.Normalize();
        Vector3 gravityUp = -gravityDir;
        Vector3 cameraRight =
            Vector3.Cross(gravityUp, cameraForward).normalized;
        Vector3 targetDirection =
            cameraForward * moveInput.y +
            cameraRight * moveInput.x;
        targetDirection.Normalize();

        // Extract base speed.
        Vector3 velocity = _rb.linearVelocity;
        Vector3 horizontalVelocity =
            Vector3.ProjectOnPlane(velocity, gravityDir);
        float gravitySpeed =
            Vector3.Dot(velocity, gravityDir);

        float horizontalSpeed =
            Mathf.Max(horizontalVelocity.magnitude, 4f);
        Vector3 horizontalDirection;

        // Convert plane velocity to target dir.
        if (horizontalVelocity.sqrMagnitude > 0.001f)
            horizontalDirection =
                Vector3.RotateTowards(
                    horizontalVelocity.normalized,
                    targetDirection,
                    _glideTurnSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime,
                    0f
                );
        else
            horizontalDirection = targetDirection;

        horizontalVelocity =
            horizontalDirection * horizontalSpeed;

        // Convert gravity dir velocity to plane velocity.
        float convertibleSpeed =
            Mathf.Max(0f, gravitySpeed - _glideFallSpeed);
        float convertedSpeed = Mathf.Min(
            convertibleSpeed,
            _glideConversionSpeed * Time.fixedDeltaTime
        );

        horizontalVelocity +=
            targetDirection * convertedSpeed;
        gravitySpeed -= convertedSpeed;

        _rb.linearVelocity =
            horizontalVelocity +
            gravityDir * gravitySpeed;
    }

    private void MoveCharacter(Vector2 moveInput, Vector3 gravityDir)
    {
        Vector3 moveDirection =
            GetMoveDirection(moveInput, gravityDir);

        if (IsGrounded())
            _characterFallSpeed = _groundStickSpeed;
        else
        {
            _characterFallSpeed +=
                _gravityAcceleration * Time.deltaTime;
        }

        Vector3 velocity =
            moveDirection * _moveSpeed +
            gravityDir * _characterFallSpeed;

        _characterController.Move(
            velocity * Time.deltaTime
        );
    }

    private Vector3 GetMoveDirection(Vector2 moveInput, Vector3 gravityDir)
    {
        Vector3 cameraForward =
            Vector3.ProjectOnPlane(
                _camera.transform.forward,
                gravityDir
            );

        if (cameraForward.sqrMagnitude < 0.001f)
        {
            cameraForward =
                Vector3.ProjectOnPlane(
                    _camera.transform.up,
                    gravityDir
                );
        }

        cameraForward.Normalize();

        Vector3 gravityUp = -gravityDir;

        Vector3 cameraRight =
            Vector3.Cross(
                gravityUp,
                cameraForward
            ).normalized;

        Vector3 direction =
            cameraForward * moveInput.y +
            cameraRight * moveInput.x;

        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        return direction;
    }

    private void SwitchToCharacterMode()
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        _rb.isKinematic = true;
        _collider.enabled = false;

        _characterController.enabled = true;
    }

    private void SwitchToRigidBodyMode()
    {
        _characterController.enabled = false;

        _collider.enabled = true;
        _rb.isKinematic = false;
    }

    private void ApplyGravity(Vector3 gravityDir, float acceleration)
    {
        float gravitySpeed =
            Vector3.Dot(_rb.linearVelocity, gravityDir);

        if (gravitySpeed >= _maxFallSpeed)
            return;

        float maxAcceleration =
            (_maxFallSpeed - gravitySpeed) / Time.fixedDeltaTime;

        float appliedAcceleration =
            Mathf.Min(acceleration, maxAcceleration);

        _rb.AddForce(
            gravityDir * appliedAcceleration,
            ForceMode.Acceleration
        );
    }

    private bool IsGrounded()
    {
        float radius = _collider.radius * 0.9f;

        Vector3 center = transform.TransformPoint(_collider.center);

        float bottomOffset =
            _collider.height * 0.5f - _collider.radius;

        Vector3 origin =
            center + GravityDir * bottomOffset;

        return Physics.SphereCast(
            origin,
            radius,
            GravityDir,
            out _,
            _groundCheckDistance,
            _groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    private void UpdateBodyVisual()
    {
        Vector3 gravityDir = GravityDir;
        Vector3 gravityUp = -gravityDir;

        Quaternion targetBodyRotation;
        Vector3 targetWingScale;

        if (_characterController.enabled)
        {
            Vector3 moveDirection =
                GetMoveDirection(_moveInput, gravityDir);

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion worldRotation =
                    Quaternion.LookRotation(
                        moveDirection,
                        gravityUp
                    );

                Quaternion localRotation =
                    Quaternion.Inverse(transform.rotation) *
                    worldRotation;

                targetBodyRotation =
                    localRotation * _bodyDefaultRotation;
            }
            else
            {
                targetBodyRotation =
                    _bodyDefaultRotation;
            }

            targetWingScale =
                _wingDefaultScale;
        }

        else if (_isGliding)
        {
            targetWingScale =
                _wingDefaultScale;

            targetWingScale.x *= 3f;

            Vector3 horizontalVelocity =
                Vector3.ProjectOnPlane(
                    _rb.linearVelocity,
                    gravityDir
                );

            if (_moveInput.sqrMagnitude < 0.001f)
            {
                // Body.up이 gravityDir을 향하도록
                Vector3 forward =
                    Vector3.ProjectOnPlane(
                        _body.forward,
                        gravityDir
                    );

                if (forward.sqrMagnitude < 0.001f)
                {
                    forward =
                        Vector3.ProjectOnPlane(
                            _camera.transform.forward,
                            gravityDir
                        );
                }

                forward.Normalize();

                Quaternion worldRotation =
                    Quaternion.LookRotation(
                        forward,
                        gravityDir
                    );

                Quaternion localRotation =
                    Quaternion.Inverse(transform.rotation) *
                    worldRotation;

                targetBodyRotation =
                    localRotation * _bodyDefaultRotation;
            }

            else if (horizontalVelocity.sqrMagnitude > 0.001f)
            {
                Vector3 glideDirection =
                    horizontalVelocity.normalized;

                Vector3 targetDirection =
                    GetMoveDirection(
                        _moveInput,
                        gravityDir
                    );

                Quaternion worldRotation =
                    Quaternion.LookRotation(
                        glideDirection,
                        gravityUp
                    );

                float turnAngle =
                    Vector3.SignedAngle(
                        glideDirection,
                        targetDirection,
                        gravityUp
                    );

                float bankAngle =
                    Mathf.Clamp(
                        turnAngle * _glideBankSensitivity,
                        -_glideBankAngle,
                        _glideBankAngle
                    );

                // 진행축을 중심으로 bank
                worldRotation =
                    Quaternion.AngleAxis(
                        -bankAngle,
                        glideDirection
                    ) *
                    worldRotation;

                Quaternion localRotation =
                    Quaternion.Inverse(transform.rotation) *
                    worldRotation;

                targetBodyRotation =
                    localRotation *
                    _bodyDefaultRotation *
                    Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                targetBodyRotation =
                    _bodyDefaultRotation *
                    Quaternion.Euler(90f, 0f, 0f);
            }
        }
        else
        {
            targetBodyRotation =
                _bodyDefaultRotation;

            targetWingScale =
                _wingDefaultScale;
        }

        _body.localRotation =
            Quaternion.Slerp(
                _body.localRotation,
                targetBodyRotation,
                _glideVisualSpeed * Time.deltaTime
            );

        _wing.localScale =
            Vector3.Lerp(
                _wing.localScale,
                targetWingScale,
                _glideVisualSpeed * Time.deltaTime
            );
    }
}
