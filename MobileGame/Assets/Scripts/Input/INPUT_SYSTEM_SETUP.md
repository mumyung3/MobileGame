# Unity Input System 설정 가이드

## 🎮 Input System 설정 방법

### 1. Unity에서 프로젝트 열기
Unity Hub를 통해 프로젝트를 열면 자동으로 Input System 패키지가 설치됩니다.

### 2. Input System 활성화
1. **Edit > Project Settings > Player** 로 이동
2. **Configuration** 섹션에서 **Active Input Handling** 을 **Both** 또는 **Input System Package (New)** 로 설정
3. Unity가 재시작을 요청하면 재시작

### 3. Input Actions 에셋 설정
1. `Assets/InputActions/GameInputActions.inputactions` 파일이 이미 생성되어 있음
2. 이 파일을 더블클릭하여 Input Actions 편집기 열기
3. 필요시 추가 액션 설정 가능

### 4. 테스트 씬 설정

#### 씬에 필요한 오브젝트:
1. **Main Camera** - 기본 카메라
2. **InputSystemManager** - 빈 GameObject 생성 후 `InputSystemManager.cs` 스크립트 추가
3. **PlayerInputController** - 빈 GameObject 생성 후:
   - `PlayerInput` 컴포넌트 추가
   - `PlayerInputController.cs` 스크립트 추가
   - PlayerInput 컴포넌트 설정:
     - Actions: `GameInputActions` 에셋 할당
     - Default Map: "Player"
     - Behavior: "Invoke Unity Events" 선택

4. **테스트 오브젝트** - Cube나 Sphere 생성 후 `TouchTestObject.cs` 스크립트 추가

## 📱 터치/마우스 입력 테스트

### 지원되는 입력:
- **마우스 클릭** (에디터 및 PC)
- **터치 입력** (모바일 디바이스)
- **Unity Remote** (모바일 테스트)

### 테스트 방법:
1. Play 모드 실행
2. 화면 좌측 상단에 디버그 정보 표시됨:
   - Device: Mouse/Touch
   - Is Touching: true/false
   - Position: X, Y 좌표
   - Active Touches: 터치 개수

3. 오브젝트 클릭/터치시:
   - 색상 변경
   - 크기 애니메이션
   - Console에 로그 출력

## 🔧 스크립트 구성

### PlayerInputController.cs
- 메인 입력 처리 스크립트
- Input Actions와 연결
- 이벤트 시스템으로 입력 전달

### InputSystemManager.cs  
- 싱글톤 매니저
- 전역 입력 상태 관리
- 디버그 UI 표시

### TouchTestObject.cs
- IInteractable 인터페이스 구현
- 터치/클릭 테스트용 스크립트
- 시각적 피드백 제공

### IInteractable 인터페이스
```csharp
public interface IInteractable
{
    void OnInteract();
}
```
- 상호작용 가능한 오브젝트에 구현
- PlayerInputController가 자동으로 호출

## 🎯 사용 예제

### 1. 간단한 터치 감지
```csharp
public class SimpleTouch : MonoBehaviour
{
    void Start()
    {
        var inputManager = InputSystemManager.Instance;
        var controller = inputManager.GetInputController();
        
        controller.OnTap += (position) => {
            Debug.Log($"Tapped at {position}");
        };
    }
}
```

### 2. 드래그 구현
```csharp
public class DragObject : MonoBehaviour
{
    private bool isDragging;
    private PlayerInputController inputController;
    
    void Start()
    {
        inputController = InputSystemManager.Instance.GetInputController();
        
        inputController.OnTouchStart += StartDrag;
        inputController.OnTouchHold += UpdateDrag;
        inputController.OnTouchEnd += EndDrag;
    }
    
    void StartDrag(Vector2 pos)
    {
        // Raycast로 이 오브젝트가 선택되었는지 확인
        isDragging = true;
    }
    
    void UpdateDrag(Vector2 pos)
    {
        if (isDragging)
        {
            Vector3 worldPos = inputController.GetWorldPosition(pos);
            transform.position = worldPos;
        }
    }
    
    void EndDrag(Vector2 pos)
    {
        isDragging = false;
    }
}
```

## 📋 체크리스트

- [ ] Input System 패키지 설치됨
- [ ] Project Settings에서 Input System 활성화
- [ ] GameInputActions 에셋 생성됨
- [ ] InputSystemManager 씬에 추가
- [ ] PlayerInputController 설정 완료
- [ ] 테스트 오브젝트에 TouchTestObject 추가
- [ ] Play 모드에서 터치/클릭 테스트
- [ ] 디버그 UI 정상 표시

## 🚀 다음 단계

1. **모바일 빌드 테스트**
   - iOS/Android 빌드 설정
   - 실제 디바이스에서 테스트

2. **UI 통합**
   - Canvas와 EventSystem 설정
   - UI 버튼과 Input System 연동

3. **멀티터치 지원**
   - 핀치 줌
   - 두 손가락 회전

4. **게임 오브젝트 통합**
   - 기존 Prefab에 IInteractable 구현
   - 게임 로직과 연결