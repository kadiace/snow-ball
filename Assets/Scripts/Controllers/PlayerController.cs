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


    [Tooltip("Move")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _moveAcceleration = 15f;

    [Tooltip("Gravity")]
    [SerializeField] private float _gravityAcceleration = 9.8f;

    [Tooltip("Check Ground")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckDistance = 0.1f;

    [Tooltip("Jump")]
    [SerializeField] private float _jumpForce = 8f;


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

        UpdateBodyVisual();

        if (_characterController.enabled)
        {
            MoveCharacter(_moveInput);
        }
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

        // 수평 이동만 사용
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

        // 목표 수평 속도
        Vector3 targetVelocity =
            moveDirection * _moveSpeed;

        // 현재 수평 속도
        Vector3 horizontalVelocity =
            new Vector3(
                _velocity.x,
                0f,
                _velocity.z
            );

        // 가속도를 적용해서 목표 속도까지 접근
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
        if (IsGrounded() && _velocity.y < 0f)
        {
            // 땅과 계속 붙어 있도록 아주 약한 하강 속도 유지
            _velocity.y = -1f;
            return;
        }

        _velocity +=
            _gravityDir *
            _gravityAcceleration *
            Time.deltaTime;
    }

    private bool IsGrounded()
    {
        Vector3 center =
            transform.TransformPoint(
                _characterController.center
            );

        float bottom =
            _characterController.height * 0.5f
            - _characterController.radius;

        Vector3 sphereCenter =
            center +
            Vector3.down * bottom;

        return Physics.SphereCast(
            sphereCenter,
            _characterController.radius * 0.9f,
            Vector3.down,
            out _,
            _groundCheckDistance,
            _groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    private void UpdateBodyVisual()
    {
        if (_moveInput.sqrMagnitude < 0.001f)
        {
            return;
        }

        Vector3 cameraForward = _camera.transform.forward;
        Vector3 cameraRight = _camera.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 direction =
            cameraForward * _moveInput.y +
            cameraRight * _moveInput.x;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        direction.Normalize();

        _body.rotation =
            Quaternion.LookRotation(
                direction,
                Vector3.up
            );
    }
}
