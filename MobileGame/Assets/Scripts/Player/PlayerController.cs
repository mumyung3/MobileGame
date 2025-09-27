using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private bool lockYPosition = true;
    [SerializeField] private float yPosition = 0f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;
    private PlayerInputController inputController;
    private Camera playerCamera;

    private void Start()
    {
        targetPosition = transform.position;

        if (lockYPosition)
            yPosition = transform.position.y;

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
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );
    }

    private void HandleDrag(Vector2 currentPosition, Vector2 deltaMove)
    {
        Vector3 worldDelta = ScreenToWorldMovement(deltaMove);
        targetPosition += worldDelta * moveSpeed * Time.deltaTime;

        if (lockYPosition)
            targetPosition.y = yPosition;

        if (showDebugInfo)
            Debug.Log($"Player target position: {targetPosition}, Delta: {worldDelta}");
    }

    private void HandleDragEnd(Vector2 endPosition)
    {
        if (showDebugInfo)
            Debug.Log("Drag ended - Player movement stopped");
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

    public void SetTargetPosition(Vector3 newTarget)
    {
        targetPosition = newTarget;
        if (lockYPosition)
            targetPosition.y = yPosition;
    }

    public Vector3 GetTargetPosition()
    {
        return targetPosition;
    }
}