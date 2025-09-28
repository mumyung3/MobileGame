using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxMoveSpeed = 5f;
    [SerializeField] private bool lockYMovement = true;

    private float initialYPosition;

    [Header("Virtual Joystick Settings")]
    [SerializeField] private float joystickRadius = 100f;
    [SerializeField] private float deadZone = 0.1f;
    [SerializeField] private bool useVirtualJoystick = true;

    [Header("Rotation Settings")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float minMoveThreshold = 0.1f;


    [Header("Camera Follow")]
    [SerializeField] private bool followCamera = true;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 5, -5);
    [SerializeField] private float cameraFollowSpeed = 5f;

    [Header("Animation")]
    [SerializeField] private bool useAnimator = true;

    private CharacterController characterController;
    private PlayerInputController inputController;
    private Camera playerCamera;
    private Animator playerAnimator;
    private Vector3 currentMoveDirection = Vector3.zero;
    private Vector3 lastMoveDirection = Vector3.zero;

    // Virtual Joystick variables
    private Vector2 joystickCenter = Vector2.zero;
    private Vector2 currentTouchPos = Vector2.zero;
    private bool isDragging = false;
    private float currentMoveSpeed = 0f;

    // Animation state
    private bool isGrabbed = false;

    private void Start()
    {
        // 시작 시 Y축 위치 저장
        initialYPosition = transform.position.y;

        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
        }

        // 메인 카메라 찾기
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }

        // Animator 컴포넌트 찾기
        if (useAnimator)
        {
            playerAnimator = GetComponent<Animator>();
            if (playerAnimator == null)
            {
                playerAnimator = GetComponentInChildren<Animator>();
            }

            if (playerAnimator == null)
            {
                useAnimator = false;
            }
        }

        FindAndConnectInputController();
    }


    private void FindAndConnectInputController()
    {
        inputController = FindObjectOfType<PlayerInputController>();
        if (inputController != null)
        {
            inputController.OnTouchStart += HandleTouchStart;
            inputController.OnDrag += HandleDrag;
            inputController.OnDragEnd += HandleDragEnd;

        }
        else
        {
        }
    }

    private void OnDestroy()
    {
        if (inputController != null)
        {
            inputController.OnTouchStart -= HandleTouchStart;
            inputController.OnDrag -= HandleDrag;
            inputController.OnDragEnd -= HandleDragEnd;
        }
    }

    private void Update()
    {
        // Virtual Joystick 방식으로 이동 처리
        if (useVirtualJoystick && isDragging)
        {
            ProcessVirtualJoystickMovement();
        }

        // 플레이어 회전 처리
        if (enableRotation && currentMoveDirection.magnitude > minMoveThreshold)
        {
            RotateTowardsMovement(currentMoveDirection);
        }

        // 카메라 따라가기
        if (followCamera && playerCamera != null)
        {
            UpdateCameraPosition();
        }

        // 애니메이터 파라미터 업데이트
        if (useAnimator && playerAnimator != null)
        {
            UpdateAnimatorParameters();
        }

        // Y축을 초기 위치에 고정
        if (lockYMovement)
        {
            ForceGroundPosition();
        }
    }

    private void HandleTouchStart(Vector2 position)
    {
        joystickCenter = position;
        currentTouchPos = position;
        isDragging = true;

    }

    private void HandleDrag(Vector2 currentPosition, Vector2 deltaMove)
    {
        if (!useVirtualJoystick)
        {
            // 기존 직접 이동 방식
            HandleDirectMovement(currentPosition, deltaMove);
            return;
        }

        // Virtual Joystick 방식
        currentTouchPos = currentPosition;
    }

    private void HandleDirectMovement(Vector2 currentPosition, Vector2 deltaMove)
    {
        if (characterController == null) return;

        Vector3 worldDelta = ScreenToWorldMovement(deltaMove);
        Vector3 movement = worldDelta;

        if (lockYMovement)
            movement.y = 0f;

        characterController.Move(movement);

        if (movement.magnitude > minMoveThreshold)
        {
            currentMoveDirection = movement.normalized;
        }
    }

    private void HandleDragEnd(Vector2 endPosition)
    {
        isDragging = false;
        currentMoveDirection = Vector3.zero;
        currentMoveSpeed = 0f;

        // 애니메이션 즉시 중지
        if (useAnimator && playerAnimator != null)
        {
            playerAnimator.SetBool("bIsRunning", false);
        }
    }

    private void ProcessVirtualJoystickMovement()
    {
        if (characterController == null) return;

        // 조이스틱 중심에서 현재 터치 위치까지의 벡터
        Vector2 offset = currentTouchPos - joystickCenter;
        float distance = offset.magnitude;

        // 데드존 체크
        if (distance < deadZone)
        {
            currentMoveDirection = Vector3.zero;
            currentMoveSpeed = 0f;
            return;
        }

        // 조이스틱 반지름을 벗어나지 않도록 제한
        if (distance > joystickRadius)
        {
            offset = offset.normalized * joystickRadius;
            distance = joystickRadius;
        }

        // 이동 강도 계산 (0~1)
        float moveIntensity = (distance - deadZone) / (joystickRadius - deadZone);
        currentMoveSpeed = moveIntensity * maxMoveSpeed;

        // 화면 좌표를 월드 좌표로 변환
        Vector3 worldDirection = ScreenToWorldDirection(offset.normalized);

        if (lockYMovement)
            worldDirection.y = 0f;

        currentMoveDirection = worldDirection.normalized;

        // 실제 이동 적용 (Y축 강제로 0으로 설정)
        Vector3 movement = currentMoveDirection * currentMoveSpeed * Time.deltaTime;
        movement.y = 0f; // Y축 이동 완전 차단
        characterController.Move(movement);
    }

    private Vector3 ScreenToWorldDirection(Vector2 screenDirection)
    {
        if (playerCamera == null) return Vector3.zero;

        Vector3 worldDir = playerCamera.transform.TransformDirection(new Vector3(screenDirection.x, 0, screenDirection.y));
        worldDir.y = 0; // Y축 제거
        return worldDir.normalized;
    }


    private void RotateTowardsMovement(Vector3 moveDirection)
    {
        if (moveDirection.magnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void UpdateCameraPosition()
    {
        Vector3 targetPosition = transform.position + cameraOffset;

        if (cameraFollowSpeed > 0)
        {
            playerCamera.transform.position = Vector3.Lerp(
                playerCamera.transform.position,
                targetPosition,
                cameraFollowSpeed * Time.deltaTime
            );
        }
        else
        {
            playerCamera.transform.position = targetPosition;
        }
    }

    private void UpdateAnimatorParameters()
    {
        // bisrunning: 현재 움직이고 있는지 체크
        bool isRunning = IsMoving();
        playerAnimator.SetBool("bIsRunning", isRunning);

        // bisgrabbed: 잡고 있는 상태인지 체크
        playerAnimator.SetBool("bIsGrabbed", isGrabbed);

    }

    private Vector3 ScreenToWorldMovement(Vector2 screenDelta)
    {
        if (playerCamera == null) return Vector3.zero;

        Vector3 worldDelta = playerCamera.ScreenToWorldPoint(new Vector3(screenDelta.x, screenDelta.y, playerCamera.nearClipPlane));
        Vector3 worldOrigin = playerCamera.ScreenToWorldPoint(new Vector3(0, 0, playerCamera.nearClipPlane));

        return worldDelta - worldOrigin;
    }

    public void SetMaxMoveSpeed(float newSpeed)
    {
        maxMoveSpeed = newSpeed;
    }

    public float GetCurrentMoveSpeed()
    {
        return currentMoveSpeed;
    }

    public void SetJoystickRadius(float radius)
    {
        joystickRadius = radius;
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
        // 드래그 중이 아니면 무조건 false
        if (!isDragging)
        {
            return false;
        }

        // 드래그 중이면서 실제로 움직이고 있거나, 방향이 있으면 true
        bool isActuallyMoving = characterController != null && characterController.velocity.magnitude > 0.1f;
        bool hasDirection = currentMoveDirection.magnitude > 0.1f;

        return isActuallyMoving || hasDirection;
    }

    // Grabbed 상태 관리 메서드들
    public void SetGrabbed(bool grabbed)
    {
        isGrabbed = grabbed;
    }

    public bool IsGrabbed()
    {
        return isGrabbed;
    }

    private void ForceGroundPosition()
    {
        Vector3 currentPos = transform.position;
        if (Mathf.Abs(currentPos.y - initialYPosition) > 0.01f)
        {
            transform.position = new Vector3(currentPos.x, initialYPosition, currentPos.z);
        }
    }
}