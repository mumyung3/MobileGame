using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class PlayerInputController : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Camera mainCamera;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private Vector2 lastTouchPosition;
    [SerializeField] private bool isTouching;
    
    // Input Actions
    private InputAction touchPositionAction;
    private InputAction touchPressAction;
    
    // Events
    public System.Action<Vector2> OnTouchStart;
    public System.Action<Vector2> OnTouchHold;
    public System.Action<Vector2> OnTouchEnd;
    public System.Action<Vector2> OnTap;
    
    private void Awake()
    {
        // Get camera if not assigned
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        // Get PlayerInput component
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();
        
        // Enable Enhanced Touch Support for mobile
        EnhancedTouchSupport.Enable();
    }
    
    private void OnEnable()
    {
        // Get input actions from PlayerInput component
        if (playerInput != null)
        {
            touchPositionAction = playerInput.actions["TouchPosition"];
            touchPressAction = playerInput.actions["TouchPress"];
            
            // Subscribe to events
            touchPressAction.started += OnTouchStarted;
            touchPressAction.performed += OnTouchPerformed;
            touchPressAction.canceled += OnTouchCanceled;
        }
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events
        if (touchPressAction != null)
        {
            touchPressAction.started -= OnTouchStarted;
            touchPressAction.performed -= OnTouchPerformed;
            touchPressAction.canceled -= OnTouchCanceled;
        }
    }
    
    private void OnDestroy()
    {
        // Disable Enhanced Touch Support
        EnhancedTouchSupport.Disable();
    }
    
    private void Update()
    {
        // Handle continuous touch/mouse hold
        if (isTouching && touchPositionAction != null)
        {
            Vector2 touchPos = touchPositionAction.ReadValue<Vector2>();
            lastTouchPosition = touchPos;
            OnTouchHold?.Invoke(touchPos);
            
            if (showDebugInfo)
                Debug.DrawRay(GetWorldPosition(touchPos), Vector3.up * 2f, Color.yellow);
        }
    }
    
    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        isTouching = true;
        Vector2 touchPos = touchPositionAction.ReadValue<Vector2>();
        lastTouchPosition = touchPos;
        
        OnTouchStart?.Invoke(touchPos);
        
        if (showDebugInfo)
        {
            Debug.Log($"Touch Started at: {touchPos}");
            Debug.DrawRay(GetWorldPosition(touchPos), Vector3.up * 5f, Color.green, 1f);
        }
    }
    
    private void OnTouchPerformed(InputAction.CallbackContext context)
    {
        // This is called when a tap is completed
        Vector2 touchPos = touchPositionAction.ReadValue<Vector2>();
        OnTap?.Invoke(touchPos);
        
        // Check for raycast hit
        CheckForObjectHit(touchPos);
        
        if (showDebugInfo)
        {
            Debug.Log($"Tap Performed at: {touchPos}");
            Debug.DrawRay(GetWorldPosition(touchPos), Vector3.up * 3f, Color.cyan, 0.5f);
        }
    }
    
    private void OnTouchCanceled(InputAction.CallbackContext context)
    {
        isTouching = false;
        Vector2 touchPos = lastTouchPosition;
        
        OnTouchEnd?.Invoke(touchPos);
        
        if (showDebugInfo)
        {
            Debug.Log($"Touch Ended at: {touchPos}");
            Debug.DrawRay(GetWorldPosition(touchPos), Vector3.up * 5f, Color.red, 1f);
        }
    }
    
    private void CheckForObjectHit(Vector2 screenPosition)
    {
        // 3D Raycast
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 100f))
        {
            if (showDebugInfo)
                Debug.Log($"Hit 3D Object: {hit.collider.gameObject.name}");
            
            // Check for interactable components
            var interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.OnInteract();
            }
        }
        
        // 2D Raycast for UI or 2D objects
        RaycastHit2D hit2D = Physics2D.Raycast(mainCamera.ScreenToWorldPoint(screenPosition), Vector2.zero);
        if (hit2D.collider != null)
        {
            if (showDebugInfo)
                Debug.Log($"Hit 2D Object: {hit2D.collider.gameObject.name}");
            
            var interactable2D = hit2D.collider.GetComponent<IInteractable>();
            if (interactable2D != null)
            {
                interactable2D.OnInteract();
            }
        }
    }
    
    public Vector3 GetWorldPosition(Vector2 screenPosition)
    {
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane + 10f));
        return worldPos;
    }
    
    public Vector2 GetLastTouchPosition()
    {
        return lastTouchPosition;
    }
    
    public bool IsTouching()
    {
        return isTouching;
    }
    
    // Helper method for multi-touch support (mobile only)
    public List<Vector2> GetAllTouchPositions()
    {
        List<Vector2> touchPositions = new List<Vector2>();
        
        if (Touch.activeTouches.Count > 0)
        {
            foreach (var touch in Touch.activeTouches)
            {
                touchPositions.Add(touch.screenPosition);
            }
        }
        else if (isTouching)
        {
            // Fallback to mouse position if no touches
            touchPositions.Add(lastTouchPosition);
        }
        
        return touchPositions;
    }
}

// Interface for interactable objects
public interface IInteractable
{
    void OnInteract();
}