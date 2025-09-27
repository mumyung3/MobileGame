using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystemManager : MonoBehaviour
{
    private static InputSystemManager instance;
    public static InputSystemManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<InputSystemManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("InputSystemManager");
                    instance = go.AddComponent<InputSystemManager>();
                }
            }
            return instance;
        }
    }
    
    [Header("Input System Setup")]
    [SerializeField] private PlayerInputController playerInputController;
    [SerializeField] private bool autoCreateInputController = true;
    
    [Header("Debug Settings")]
    [SerializeField] private bool showDebugGUI = true;
    [SerializeField] private GUIStyle debugStyle;
    
    private Vector2 currentTouchPosition;
    private bool isTouching;
    private string currentDevice = "Unknown";
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        SetupInputSystem();
    }
    
    private void SetupInputSystem()
    {
        // Find or create PlayerInputController
        if (playerInputController == null && autoCreateInputController)
        {
            playerInputController = FindObjectOfType<PlayerInputController>();
            
            if (playerInputController == null)
            {
                GameObject inputControllerGO = new GameObject("PlayerInputController");
                inputControllerGO.transform.SetParent(transform);
                
                // Add PlayerInput component
                PlayerInput playerInput = inputControllerGO.AddComponent<PlayerInput>();
                
                // Add our controller
                playerInputController = inputControllerGO.AddComponent<PlayerInputController>();
                
                Debug.Log("Created PlayerInputController automatically");
            }
        }
        
        // Subscribe to input events
        if (playerInputController != null)
        {
            playerInputController.OnTouchStart += HandleTouchStart;
            playerInputController.OnTouchHold += HandleTouchHold;
            playerInputController.OnTouchEnd += HandleTouchEnd;
            playerInputController.OnTap += HandleTap;
        }
        
        // Detect current input device
        DetectInputDevice();
    }
    
    private void DetectInputDevice()
    {
        if (Application.isMobilePlatform)
        {
            currentDevice = "Touch";
        }
        else
        {
            currentDevice = "Mouse";
        }
        
        Debug.Log($"Detected Input Device: {currentDevice}");
    }
    
    private void HandleTouchStart(Vector2 position)
    {
        currentTouchPosition = position;
        isTouching = true;
        Debug.Log($"Touch Started at: {position}");
    }

    private void HandleTouchHold(Vector2 position)
    {
        currentTouchPosition = position;
        // Can be used for drag operations
        //Debug.Log($"Touch Holding at: {position}");
        
    }
    
    private void HandleTouchEnd(Vector2 position)
    {
        currentTouchPosition = position;
        isTouching = false;
        Debug.Log($"Touch Ended at: {position}");
    }
    
    private void HandleTap(Vector2 position)
    {
        Debug.Log($"Tap detected at: {position}");
    }
    
    private void OnGUI()
    {
        if (!showDebugGUI) return;
        
        // Setup debug style if not initialized
        if (debugStyle == null)
        {
            debugStyle = new GUIStyle(GUI.skin.label);
            debugStyle.fontSize = 20;
            debugStyle.normal.textColor = Color.white;
        }
        
        // Create background box
        GUI.Box(new Rect(10, 10, 300, 150), "");
        
        // Display debug info
        int yOffset = 15;
        GUI.Label(new Rect(15, yOffset, 290, 30), $"Device: {currentDevice}", debugStyle);
        yOffset += 30;
        
        GUI.Label(new Rect(15, yOffset, 290, 30), $"Is Touching: {isTouching}", debugStyle);
        yOffset += 30;
        
        GUI.Label(new Rect(15, yOffset, 290, 30), $"Position: {currentTouchPosition.x:F0}, {currentTouchPosition.y:F0}", debugStyle);
        yOffset += 30;
        
        if (playerInputController != null)
        {
            var allTouches = playerInputController.GetAllTouchPositions();
            GUI.Label(new Rect(15, yOffset, 290, 30), $"Active Touches: {allTouches.Count}", debugStyle);
        }
    }
    
    public Vector2 GetCurrentTouchPosition()
    {
        return currentTouchPosition;
    }
    
    public bool IsTouching()
    {
        return isTouching;
    }
    
    public PlayerInputController GetInputController()
    {
        return playerInputController;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (playerInputController != null)
        {
            playerInputController.OnTouchStart -= HandleTouchStart;
            playerInputController.OnTouchHold -= HandleTouchHold;
            playerInputController.OnTouchEnd -= HandleTouchEnd;
            playerInputController.OnTap -= HandleTap;
        }
    }
}