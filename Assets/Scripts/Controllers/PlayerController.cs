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

    private readonly Vector3 _gravityDir = Vector3.down;
    private Vector3 _groundNormal = Vector3.up;


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

        Debug.Log($"{_groundNormal}");
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

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        Vector3 targetVelocity =
            moveDirection * _moveSpeed;

        _moveVelocity = Vector3.MoveTowards(
            _moveVelocity,
            targetVelocity,
            _moveAcceleration * Time.deltaTime
        );

        ApplyGravity();

        Vector3 finalVelocity =
            _moveVelocity +
            _gravityVelocity;

        _characterController.Move(
            finalVelocity * Time.deltaTime
        );
    }

    private void ApplyGravity()
    {
        bool movingAgainstGravity =
            Vector3.Dot(
                _gravityVelocity,
                _gravityDir
            ) < 0f;

        if (_characterController.isGrounded &&
    !movingAgainstGravity)
        {
            _gravityVelocity =
            Vector3.ProjectOnPlane(
                _gravityVelocity,
                _groundNormal
            );

            Vector3 slopeAcceleration =
                Vector3.ProjectOnPlane(
                    _gravityDir * _gravityAcceleration,
                    _groundNormal
                );

            _gravityVelocity +=
                slopeAcceleration *
                Time.deltaTime;
        }
        else
            _gravityVelocity +=
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
        Vector3 totalVelocity =
            _moveVelocity +
            _gravityVelocity;

        Vector3 surfaceVelocity =
            Vector3.ProjectOnPlane(
                totalVelocity,
                _groundNormal
            );

        if (surfaceVelocity.sqrMagnitude < 0.001f)
            return;

        Vector3 moveDirection =
            surfaceVelocity.normalized;

        Vector3 rotationAxis =
            Vector3.Cross(
                _groundNormal,
                moveDirection
            );

        float distance =
            surfaceVelocity.magnitude * Time.deltaTime;

        float rotationAngle =
            distance / _bodyRadius * Mathf.Rad2Deg;

        _body.rotation =
            Quaternion.AngleAxis(
                rotationAngle,
                rotationAxis
            ) * _body.rotation;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (((1 << hit.gameObject.layer) & _groundLayer) == 0)
            return;

        float groundDot =
            Vector3.Dot(hit.normal, -_gravityDir);

        if (groundDot > 0f)
        {
            _groundNormal = hit.normal;
        }
    }
}
