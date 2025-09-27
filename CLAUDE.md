# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity iOS mobile game project built with Unity 2021.3.15f1 on macOS. The project is designed for rapid prototyping of simple mobile games, currently focused on an idle/management game with game objects like buckets, counters, ovens, and money collection systems.

## Project Structure

```
MobileGame/
├── Assets/
│   ├── Scenes/          # Unity scenes (GameMap.unity is main scene)
│   ├── Prefabs/         # Game object prefabs (Bucket, Counter, Oven, MoneyFloor, Upgrade, UpgradeUI)
│   ├── Practice/        # Practice assets including sprites, materials, prefabs, animations
│   │   ├── Sprites/     # 2D sprites and UI assets
│   │   ├── Materials/   # Material assets
│   │   ├── Prefabs/     # Practice prefabs
│   │   ├── Animation/   # Animation controllers and clips
│   │   └── FBX/         # 3D models
│   └── TextMesh Pro/    # TextMesh Pro package assets
└── ProjectSettings/     # Unity project configuration
```

## iOS Development Environment Setup

### Prerequisites
- **macOS**: 10.14 or higher
- **Xcode**: 13.0 or higher (download from App Store)
- **Unity 2021.3.15f1** with iOS Build Support module
- **Apple Developer Account** (free for testing, $99/year for App Store)

### Unity iOS Module Installation
```bash
# Via Unity Hub
1. Open Unity Hub > Installs
2. Click gear icon on Unity 2021.3.15f1
3. Add Modules > iOS Build Support
```

### iOS Build Settings Optimization
1. File > Build Settings > Switch Platform to iOS
2. Player Settings recommendations:
   - Bundle Identifier: com.yourcompany.gamename
   - Target minimum iOS version: 12.0
   - Architecture: ARM64
   - Camera Usage Description: Required for AR features
   - Location Usage Description: If using location services

## macOS Unity Commands & Automation

### Unity Hub CLI Commands
```bash
# Open project from terminal
/Applications/Unity\ Hub.app/Contents/MacOS/Unity\ Hub -- --headless

# Build iOS from command line
/Applications/Unity/Hub/Editor/2021.3.15f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath "/Users/mumyung/Documents/SuperCent/MobileGame" \
  -buildTarget iOS \
  -executeMethod BuildScript.BuildiOS
```

### Xcode Build Commands
```bash
# Build from command line
xcodebuild -project Unity-iPhone.xcodeproj -scheme Unity-iPhone -sdk iphoneos -configuration Release

# Archive for TestFlight
xcodebuild archive -project Unity-iPhone.xcodeproj -scheme Unity-iPhone -archivePath build/MyGame.xcarchive

# Export IPA
xcodebuild -exportArchive -archivePath build/MyGame.xcarchive -exportPath build -exportOptionsPlist ExportOptions.plist
```

## Key Assets

### Prefabs
- **Bucket.prefab**: Core game object, likely for resource collection
- **Counter.prefab**: Counter/cashier object for transactions
- **Oven.prefab**: Cooking/production equipment
- **MoneyFloor.prefab**: Money collection system
- **Upgrade.prefab**: Upgrade system object
- **UpgradeUI.prefab**: UI for upgrade interactions

### Scenes
- **GameMap.unity**: Main game scene located in Assets/Scenes/

## Development Notes

- Project uses standard Unity packages including TextMeshPro, Timeline, and uGUI
- No custom C# scripts are currently present in the Assets directory
- The Practice folder contains various game assets suggesting active development
- Project follows Unity standard folder structure and naming conventions
- Mobile-focused development with appropriate Unity modules enabled

## Asset Organization

- Sprites and UI elements are organized in Practice/Sprites/
- 3D models stored in Practice/FBX/
- Materials in Practice/Materials/
- Animations in Practice/Animation/
- Main game prefabs in root Prefabs/ directory for easy access

## Rapid Prototyping Strategies

### Quick Start Templates

#### 1. Idle/Clicker Game Template
```csharp
// Basic idle game structure
public class IdleGameManager : MonoBehaviour {
    public float currency = 0;
    public float currencyPerSecond = 1;
    public float tapValue = 1;
    
    void Update() {
        currency += currencyPerSecond * Time.deltaTime;
    }
    
    public void OnTap() {
        currency += tapValue;
    }
}
```

#### 2. Simple Touch Input Handler
```csharp
public class TouchController : MonoBehaviour {
    void Update() {
        if (Input.touchCount > 0) {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) {
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit)) {
                    // Handle object interaction
                }
            }
        }
    }
}
```

### Recommended Free Assets
- **UI**: Unity's built-in UI system (uGUI)
- **Animations**: DOTween (free version)
- **Particle Effects**: Unity Particle Pack (Asset Store)
- **Sound Effects**: Freesound.org integration
- **Icons**: Game-Icons.net

### Visual Scripting (for non-programmers)
- Use Unity Visual Scripting (formerly Bolt)
- Window > Visual Scripting > Visual Scripting Graph
- Drag-and-drop logic building

## Simple Game Systems

### Save System using PlayerPrefs
```csharp
public class SaveManager : MonoBehaviour {
    public void SaveGame() {
        PlayerPrefs.SetFloat("Currency", GameManager.Instance.currency);
        PlayerPrefs.SetInt("Level", GameManager.Instance.level);
        PlayerPrefs.Save();
    }
    
    public void LoadGame() {
        GameManager.Instance.currency = PlayerPrefs.GetFloat("Currency", 0);
        GameManager.Instance.level = PlayerPrefs.GetInt("Level", 1);
    }
}
```

### Basic UI System Structure
```
Canvas
├── MainMenu
│   ├── PlayButton
│   ├── SettingsButton
│   └── QuitButton
├── GameHUD
│   ├── CurrencyDisplay
│   ├── ScoreDisplay
│   └── PauseButton
└── Popups
    ├── PauseMenu
    ├── GameOverScreen
    └── RewardPopup
```

### Unity Ads Integration (Quick Monetization)
```csharp
using UnityEngine.Advertisements;

public class AdsManager : MonoBehaviour, IUnityAdsListener {
    string gameId = "YOUR_GAME_ID";
    
    void Start() {
        Advertisement.AddListener(this);
        Advertisement.Initialize(gameId, true); // true for test mode
    }
    
    public void ShowRewardedAd() {
        if (Advertisement.IsReady("rewardedVideo")) {
            Advertisement.Show("rewardedVideo");
        }
    }
}
```

## Performance Optimization for iOS

### Texture Settings
- **Compression**: Use PVRTC for older devices, ASTC for newer
- **Max Size**: 2048x2048 for UI, 1024x1024 for game objects
- **Mip Maps**: Disable for UI elements

### Draw Call Optimization
- Use Sprite Atlas for 2D games
- Batch similar materials
- Static batching for non-moving objects
- Dynamic batching for small objects (<300 vertices)

### iOS-Specific Settings
```
Edit > Project Settings > Player > iOS
- Static Batching: ✓
- Dynamic Batching: ✓
- GPU Skinning: ✓
- Graphics Jobs: ✓ (for Metal)
- Multithreaded Rendering: ✓
```

### Profiler Usage
- Window > Analysis > Profiler
- Key metrics to monitor:
  - FPS (target 30-60)
  - Draw Calls (<100 for simple games)
  - Memory Usage (<1GB)
  - Battery Usage

## Testing & Debugging

### Unity Remote 5 Setup
1. Download Unity Remote 5 from App Store on iOS device
2. Connect device via USB
3. Edit > Project Settings > Editor
4. Device: Any iOS Device
5. Compression: JPEG
6. Resolution: Normal

### Debug Console on Device
```csharp
public class DebugDisplay : MonoBehaviour {
    string myLog;
    Queue myLogQueue = new Queue();
    
    void OnEnable() {
        Application.logMessageReceived += HandleLog;
    }
    
    void OnDisable() {
        Application.logMessageReceived -= HandleLog;
    }
    
    void HandleLog(string logString, string stackTrace, LogType type) {
        myLog = logString;
        myLogQueue.Enqueue(myLog);
        if (myLogQueue.Count > 5) myLogQueue.Dequeue();
    }
    
    void OnGUI() {
        GUILayout.Label(myLog);
    }
}
```

### TestFlight Deployment Process
1. Build from Unity to Xcode project
2. Open in Xcode and set Team/Signing
3. Archive: Product > Archive
4. Upload: Window > Organizer > Distribute App
5. Select TestFlight & App Store
6. Wait for processing (usually 10-30 minutes)
7. Manage testers in App Store Connect

## Common iOS Issues & Solutions

### Issue: Black screen on launch
- Check: Camera Clear Flags set to Solid Color
- Check: Lighting settings properly configured

### Issue: Touch input not working
- Ensure Event System exists in scene
- Check Canvas Render Mode (Screen Space recommended)

### Issue: Performance drops
- Reduce texture sizes
- Simplify shaders (use Mobile shaders)
- Limit particle effects
- Use object pooling for frequently spawned objects

## Quick Development Checklist

- [ ] iOS Build Support installed
- [ ] Xcode installed and updated
- [ ] Bundle ID set in Player Settings
- [ ] Orientation locked (Portrait/Landscape)
- [ ] Icons and splash screens configured
- [ ] Touch input tested with Unity Remote
- [ ] Performance profiled on actual device
- [ ] Save/Load system implemented
- [ ] Basic analytics integrated
- [ ] Test build deployed via TestFlight