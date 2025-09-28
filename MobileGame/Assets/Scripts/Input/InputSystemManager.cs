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
        
    }
    
    private void HandleTouchStart(Vector2 position)
    {
        currentTouchPosition = position;
        isTouching = true;
    }

    private void HandleTouchHold(Vector2 position)
    {
        currentTouchPosition = position;
        // Can be used for drag operations
        
    }
    
    private void HandleTouchEnd(Vector2 position)
    {
        currentTouchPosition = position;
        isTouching = false;
    }
    
    private void HandleTap(Vector2 position)
    {
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