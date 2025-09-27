using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float moveSensitivity = 1f;
    [SerializeField] private bool lockYMovement = true;

    [Header("Rotation Settings")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float minMoveThreshold = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private CharacterController characterController;
    private PlayerInputController inputController;
    private Camera playerCamera;
    private Vector3 currentMoveDirection = Vector3.zero;
    private Vector3 lastMoveDirection = Vector3.zero;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("CharacterController component not found! Please add CharacterController to this GameObject.");
        }

        playerCamera = Camera.main;
        FindAndConnectInputController();
    }

    private void FindAndConnectInputController()
    {
        inputController = FindObjectOfType<PlayerInputController>();
        if (inputController != null)
        {
            inputController.OnDrag += HandleDrag;
            inputController.OnDragEnd += HandleDragEnd;

            if (showDebugInfo)
                Debug.Log("PlayerController connected to InputController");
        }
        else
        {
            Debug.LogWarning("PlayerInputController not found!");
        }
    }

    private void OnDestroy()
    {
        if (inputController != null)
        {
            inputController.OnDrag -= HandleDrag;
            inputController.OnDragEnd -= HandleDragEnd;
        }
    }

    private void Update()
    {
        // 회전 처리
        if (enableRotation && lastMoveDirection.magnitude > minMoveThreshold)
        {
            RotateTowardsMovement(lastMoveDirection);
        }

        // 디버그 표시
        if (showDebugInfo && IsMoving())
        {
            Debug.DrawRay(transform.position, lastMoveDirection * 2f, Color.blue, 0.1f);
            Debug.DrawRay(transform.position, transform.forward * 3f, Color.red, 0.1f);
        }
    }

    private void HandleDrag(Vector2 currentPosition, Vector2 deltaMove)
    {
        if (characterController == null) return;

        Vector3 worldDelta = ScreenToWorldMovement(deltaMove);
        Vector3 movement = worldDelta * moveSensitivity;

        if (lockYMovement)
            movement.y = 0f;

        // 이동 방향 저장 (회전용)
        if (movement.magnitude > minMoveThreshold)
        {
            lastMoveDirection = movement.normalized;
        }

        // 직접 즉시 이동 (부드러운 연속 이동)
        characterController.Move(movement);

        if (showDebugInfo)
            Debug.Log($"Player immediate movement: {movement}, Direction: {lastMoveDirection}");
    }

    private void HandleDragEnd(Vector2 endPosition)
    {
        currentMoveDirection = Vector3.zero;

        if (showDebugInfo)
            Debug.Log("Drag ended - Player movement stopped");
    }

    private void RotateTowardsMovement(Vector3 moveDirection)
    {
        if (moveDirection.magnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private Vector3 ScreenToWorldMovement(Vector2 screenDelta)
    {
        if (playerCamera == null) return Vector3.zero;

        Vector3 worldDelta = playerCamera.ScreenToWorldPoint(new Vector3(screenDelta.x, screenDelta.y, playerCamera.nearClipPlane));
        Vector3 worldOrigin = playerCamera.ScreenToWorldPoint(new Vector3(0, 0, playerCamera.nearClipPlane));

        return worldDelta - worldOrigin;
    }

    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    public void SetMoveDirection(Vector3 direction)
    {
        currentMoveDirection = direction;
        if (lockYMovement)
            currentMoveDirection.y = 0f;
    }

    public Vector3 GetCurrentPosition()
    {
        return transform.position;
    }

    public bool IsMoving()
    {
        return characterController != null && characterController.velocity.magnitude > 0.1f;
    }
}