using UnityEngine;
using UnityEngine.UI;

public class CustomerWorldUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private GameObject breadCollectionUI;
    [SerializeField] private GameObject eatingUI;
    [SerializeField] private GameObject paymentUI;
    [SerializeField] private float uiHeightOffset = 2.5f;
    [SerializeField] private Vector3 uiScale = Vector3.one;

    [Header("UI Prefabs (Auto-assigned if null)")]
    [SerializeField] private GameObject breadCollectionUIPrefab;
    [SerializeField] private GameObject eatingUIPrefab;
    [SerializeField] private GameObject paymentUIPrefab;

    private Camera mainCamera;
    private Transform customerTransform;
    private GameObject currentActiveUI;

    public enum UIType
    {
        None,
        BreadCollection,
        Eating,
        Payment
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        // 메인 카메라 찾기
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }

        // 고객 Transform 찾기
        customerTransform = transform;

        // 월드 캔버스 설정
        SetupWorldCanvas();

        // UI 프리팹들 생성
        CreateUIElements();

        // 모든 UI 비활성화
        HideAllUI();
    }

    private void SetupWorldCanvas()
    {
        if (worldCanvas == null)
        {
            // 월드 캔버스 생성
            GameObject canvasObj = new GameObject("CustomerWorldCanvas");
            canvasObj.transform.SetParent(transform);

            // 고객 머리 위 위치 설정
            canvasObj.transform.localPosition = Vector3.up * uiHeightOffset;
            canvasObj.transform.localRotation = Quaternion.identity;

            worldCanvas = canvasObj.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.worldCamera = mainCamera;

            // 캔버스 크기 설정 (더 작게)
            RectTransform canvasRect = worldCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(100, 100);
            canvasRect.localScale = uiScale * 0.01f; // 월드 스케일 조정

            // 캔버스 위치 강제 설정
            canvasRect.anchoredPosition3D = Vector3.zero;
            canvasRect.localPosition = Vector3.up * uiHeightOffset;

            // CanvasScaler 추가 (UI 크기 일관성)
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            // GraphicRaycaster 추가 (상호작용을 위해)
            canvasObj.AddComponent<GraphicRaycaster>();

            // 정렬 순서 설정 (다른 UI보다 앞에)
            worldCanvas.sortingOrder = 100;

            // UI Material 설정 (World Space UI에 적합한 재질)
            Material uiMaterial = Resources.GetBuiltinResource<Material>("Sprites/Default");
            if (uiMaterial != null)
            {
                worldCanvas.GetComponent<CanvasRenderer>()?.SetMaterial(uiMaterial, 0);
            }

            Debug.Log($"[CustomerWorldUI] 월드 캔버스 생성 완료: {canvasObj.name}, 위치: {canvasObj.transform.position}");
        }
    }

    private void CreateUIElements()
    {
        // 빵 수집 UI 생성
        if (breadCollectionUI == null)
        {
            breadCollectionUI = CreateUIElement(breadCollectionUIPrefab, "BreadCollectionUI", "🍞");
        }

        // 먹기 UI 생성
        if (eatingUI == null)
        {
            eatingUI = CreateUIElement(eatingUIPrefab, "EatingUI", "🍽️");
        }

        // 결제 UI 생성
        if (paymentUI == null)
        {
            paymentUI = CreateUIElement(paymentUIPrefab, "PaymentUI", "💳");
        }
    }

    private GameObject CreateUIElement(GameObject prefab, string name, string defaultIcon)
    {
        GameObject uiElement;

        if (prefab != null)
        {
            // 프리팹 인스턴스화
            GameObject prefabInstance = Instantiate(prefab);

            // 프리팹이 캔버스인지 확인
            Canvas prefabCanvas = prefabInstance.GetComponent<Canvas>();
            if (prefabCanvas != null)
            {
                // 캔버스라면 내부 UI 요소들을 추출
                uiElement = ExtractUIFromCanvas(prefabInstance, name);

                // 원본 캔버스는 삭제
                Destroy(prefabInstance);
            }
            else
            {
                // 캔버스가 아니라면 그대로 사용하고 부모 설정
                uiElement = prefabInstance;
                uiElement.transform.SetParent(worldCanvas.transform, false);
            }
        }
        else
        {
            // 기본 UI 생성
            uiElement = CreateDefaultUI(name, defaultIcon);
        }

        uiElement.name = name;
        uiElement.SetActive(false);

        return uiElement;
    }

    private GameObject ExtractUIFromCanvas(GameObject canvasPrefab, string name)
    {
        // 캔버스에서 UI 요소들을 추출
        Transform canvasTransform = canvasPrefab.transform;

        if (canvasTransform.childCount > 0)
        {
            // 컨테이너 GameObject 생성 (여러 UI 요소를 담기 위해)
            GameObject container = new GameObject(name + "_Container");
            container.transform.SetParent(worldCanvas.transform, false);

            // RectTransform 추가
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(100, 100);

            // 모든 자식 UI 요소들을 컨테이너로 이동
            for (int i = canvasTransform.childCount - 1; i >= 0; i--)
            {
                Transform child = canvasTransform.GetChild(i);
                child.SetParent(container.transform, false);

                // 각 자식의 RectTransform 설정
                RectTransform childRect = child.GetComponent<RectTransform>();
                if (childRect != null)
                {
                    // 로컬 좌표계로 설정
                    childRect.localScale = Vector3.one;
                }
            }

            Debug.Log($"[CustomerWorldUI] 캔버스에서 UI 추출 완료: {name}, 자식 수: {container.transform.childCount}");
            return container;
        }
        else
        {
            // 자식이 없다면 기본 UI 생성
            Debug.LogWarning($"[CustomerWorldUI] 캔버스 프리팹에 자식 UI가 없음: {name}");
            return CreateDefaultUI(name, "❓");
        }
    }

    private GameObject CreateDefaultUI(string name, string icon)
    {
        // 기본 UI 패널 생성
        GameObject uiPanel = new GameObject(name);
        uiPanel.transform.SetParent(worldCanvas.transform, false);

        // Image 컴포넌트 추가 (배경)
        Image backgroundImage = uiPanel.AddComponent<Image>();

        // 기본 UI 스프라이트 설정
        backgroundImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        if (backgroundImage.sprite == null)
        {
            // 백업: 기본 스프라이트 생성
            backgroundImage.sprite = CreateDefaultSprite();
        }

        backgroundImage.color = new Color(0.9f, 0.9f, 0.9f, 1f); // 더 진한 회색
        backgroundImage.type = Image.Type.Simple;

        // RectTransform 설정 (월드 스페이스에 맞게 조정)
        RectTransform rectTransform = uiPanel.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 100);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        // 아이콘 텍스트 추가
        GameObject textObj = new GameObject("Icon");
        textObj.transform.SetParent(uiPanel.transform, false);

        Text iconText = textObj.AddComponent<Text>();
        iconText.text = icon;
        iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (iconText.font == null)
        {
            iconText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        iconText.fontSize = 45;
        iconText.color = new Color(0.1f, 0.1f, 0.1f, 1f); // 진한 검정
        iconText.alignment = TextAnchor.MiddleCenter;
        iconText.fontStyle = FontStyle.Bold;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(100, 100);
        textRect.anchoredPosition = Vector2.zero;
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Debug.Log($"[CustomerWorldUI] 기본 UI 생성 완료: {name} - {icon}");
        return uiPanel;
    }

    private Sprite CreateDefaultSprite()
    {
        // 기본 흰색 텍스처 생성
        Texture2D texture = new Texture2D(64, 64);
        Color[] colors = new Color[64 * 64];

        // 흰색으로 채우기
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }

        texture.SetPixels(colors);
        texture.Apply();

        // 스프라이트 생성
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        return sprite;
    }

    private void CheckUIComponents(GameObject uiObject, string uiName)
    {
        if (uiObject == null) return;

        // Image 컴포넌트 체크
        Image image = uiObject.GetComponent<Image>();
        if (image != null)
        {
            Debug.Log($"[CustomerWorldUI] {uiName} Image - 스프라이트: {image.sprite?.name}, 색상: {image.color}, 활성화: {image.enabled}");
        }

        // Text 컴포넌트 체크
        Text text = uiObject.GetComponentInChildren<Text>();
        if (text != null)
        {
            Debug.Log($"[CustomerWorldUI] {uiName} Text - 내용: '{text.text}', 폰트: {text.font?.name}, 색상: {text.color}, 활성화: {text.enabled}");
        }

        // Canvas 확인
        Debug.Log($"[CustomerWorldUI] {uiName} Canvas 상태 - 활성화: {worldCanvas.enabled}, 렌더모드: {worldCanvas.renderMode}");
    }

    private void Update()
    {
        // 캔버스 위치를 고객 머리 위로 지속적으로 업데이트
        if (worldCanvas != null)
        {
            // 위치 업데이트 (고객이 움직일 때 따라 움직이도록)
            Vector3 targetPosition = transform.position + Vector3.up * uiHeightOffset;
            worldCanvas.transform.position = targetPosition;

            // 캔버스가 항상 카메라를 바라보도록 설정
            if (mainCamera != null)
            {
                worldCanvas.transform.LookAt(worldCanvas.transform.position + mainCamera.transform.rotation * Vector3.forward,
                                            mainCamera.transform.rotation * Vector3.up);
            }
        }
    }

    public void ShowUI(UIType uiType)
    {
        Debug.Log($"[CustomerWorldUI] ShowUI 호출됨: {uiType}");
        HideAllUI();

        switch (uiType)
        {
            case UIType.BreadCollection:
                if (breadCollectionUI != null)
                {
                    breadCollectionUI.SetActive(true);
                    currentActiveUI = breadCollectionUI;
                    Debug.Log($"[CustomerWorldUI] 빵 수집 UI 표시됨 - 활성화: {breadCollectionUI.activeInHierarchy}, 위치: {breadCollectionUI.transform.position}");

                    // UI 컴포넌트 상태 체크
                    CheckUIComponents(breadCollectionUI, "BreadCollection");
                }
                else
                {
                    Debug.LogWarning($"[CustomerWorldUI] 빵 수집 UI가 null입니다!");
                }
                break;

            case UIType.Eating:
                if (eatingUI != null)
                {
                    eatingUI.SetActive(true);
                    currentActiveUI = eatingUI;
                    Debug.Log($"[CustomerWorldUI] 먹기 UI 표시됨");
                }
                else
                {
                    Debug.LogWarning($"[CustomerWorldUI] 먹기 UI가 null입니다!");
                }
                break;

            case UIType.Payment:
                if (paymentUI != null)
                {
                    paymentUI.SetActive(true);
                    currentActiveUI = paymentUI;
                    Debug.Log($"[CustomerWorldUI] 계산 UI 표시됨");
                }
                else
                {
                    Debug.LogWarning($"[CustomerWorldUI] 계산 UI가 null입니다!");
                }
                break;

            case UIType.None:
            default:
                currentActiveUI = null;
                Debug.Log($"[CustomerWorldUI] 모든 UI 숨김");
                break;
        }
    }

    public void HideAllUI()
    {
        if (breadCollectionUI != null) breadCollectionUI.SetActive(false);
        if (eatingUI != null) eatingUI.SetActive(false);
        if (paymentUI != null) paymentUI.SetActive(false);
        currentActiveUI = null;
    }

    public void SetUIHeightOffset(float height)
    {
        uiHeightOffset = height;
        if (worldCanvas != null)
        {
            // 월드 위치로 업데이트
            Vector3 targetPosition = transform.position + Vector3.up * uiHeightOffset;
            worldCanvas.transform.position = targetPosition;
        }
    }

    public void SetUIScale(Vector3 scale)
    {
        uiScale = scale;
        if (worldCanvas != null)
        {
            worldCanvas.transform.localScale = uiScale;
        }
    }

    // 외부에서 특정 UI 프리팹 설정
    public void SetBreadCollectionUIPrefab(GameObject prefab)
    {
        breadCollectionUIPrefab = prefab;
        if (breadCollectionUI != null)
        {
            Destroy(breadCollectionUI);
            breadCollectionUI = CreateUIElement(prefab, "BreadCollectionUI", "🍞");
        }
    }

    public void SetEatingUIPrefab(GameObject prefab)
    {
        eatingUIPrefab = prefab;
        if (eatingUI != null)
        {
            Destroy(eatingUI);
            eatingUI = CreateUIElement(prefab, "EatingUI", "🍽️");
        }
    }

    public void SetPaymentUIPrefab(GameObject prefab)
    {
        paymentUIPrefab = prefab;
        if (paymentUI != null)
        {
            Destroy(paymentUI);
            paymentUI = CreateUIElement(prefab, "PaymentUI", "💳");
        }
    }

    private void OnDestroy()
    {
        // 정리
        if (worldCanvas != null)
        {
            Destroy(worldCanvas.gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // UI 위치 표시
        Gizmos.color = Color.cyan;
        Vector3 uiPosition = transform.position + Vector3.up * uiHeightOffset;
        Gizmos.DrawWireCube(uiPosition, Vector3.one * 0.5f);

        // 연결선 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, uiPosition);

        // 실제 캔버스 위치 표시 (런타임 시)
        if (Application.isPlaying && worldCanvas != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(worldCanvas.transform.position, 0.3f);

            // 디버그 정보 표시
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(uiPosition + Vector3.up * 0.5f,
                $"UI Height: {uiHeightOffset}\nCanvas Pos: {worldCanvas.transform.position}");
            #endif
        }
    }
}