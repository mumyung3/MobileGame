using UnityEngine;
using TMPro;

public class SimpleCustomerUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private float uiHeightOffset = 2.5f;
    [SerializeField] private GameObject breadCollectionUIPrefab;
    [SerializeField] private GameObject eatingUIPrefab;
    [SerializeField] private GameObject paymentUIPrefab;
    [SerializeField] private GameObject markerUIPrefab; // 마커용 UI 프리팹

    private GameObject currentUI;
    private Camera mainCamera;

    // UI 위치 반환 (다른 스크립트에서 참조용)
    public Vector3 GetCurrentUIPosition()
    {
        if (currentUI != null)
        {
            return currentUI.transform.position;
        }
        return transform.position + Vector3.up * uiHeightOffset; // 기본 UI 위치
    }

    // 현재 UI GameObject 반환 (참조용)
    public GameObject GetCurrentUI()
    {
        return currentUI;
    }

    public enum UIType
    {
        None,
        BreadCollection,
        Eating,
        Payment
    }

    private void Start()
    {
        // 메인 카메라 찾기
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
    }

    private void Update()
    {
        // UI가 항상 카메라를 바라보도록 설정
        if (currentUI != null && mainCamera != null)
        {
            Vector3 lookDirection = mainCamera.transform.position - currentUI.transform.position;
            currentUI.transform.rotation = Quaternion.LookRotation(-lookDirection);
        }
    }

    public void ShowUI(UIType uiType)
    {
        Debug.Log($"[SimpleCustomerUI] ShowUI 호출됨: {uiType}");

        // 기존 UI 제거
        HideUI();

        GameObject prefabToInstantiate = null;

        switch (uiType)
        {
            case UIType.BreadCollection:
                prefabToInstantiate = breadCollectionUIPrefab;
                break;
            case UIType.Eating:
                prefabToInstantiate = eatingUIPrefab;
                break;
            case UIType.Payment:
                prefabToInstantiate = paymentUIPrefab;
                break;
        }

        if (prefabToInstantiate != null)
        {
            // 프리팹을 고객의 자식으로 인스턴스화
            currentUI = Instantiate(prefabToInstantiate, transform);

            // 머리 위 위치 설정
            currentUI.transform.localPosition = Vector3.up * uiHeightOffset;

            Debug.Log($"[SimpleCustomerUI] UI 생성됨: {uiType}, 위치: {currentUI.transform.position}");
        }
        else
        {
            Debug.LogWarning($"[SimpleCustomerUI] {uiType} UI 프리팹이 없습니다!");
        }
    }

    public void HideUI()
    {
        if (currentUI != null)
        {
            Destroy(currentUI);
            currentUI = null;
            Debug.Log("[SimpleCustomerUI] UI 숨김");
        }
    }

    public void SetUIHeightOffset(float height)
    {
        uiHeightOffset = height;
        if (currentUI != null)
        {
            currentUI.transform.localPosition = Vector3.up * uiHeightOffset;
        }
    }

    // 외부에서 프리팹 설정
    public void SetBreadCollectionUIPrefab(GameObject prefab)
    {
        breadCollectionUIPrefab = prefab;
    }

    public void SetEatingUIPrefab(GameObject prefab)
    {
        eatingUIPrefab = prefab;
    }

    public void SetPaymentUIPrefab(GameObject prefab)
    {
        paymentUIPrefab = prefab;
    }

    public void SetMarkerUIPrefab(GameObject prefab)
    {
        markerUIPrefab = prefab;
    }

    // 빵 수량을 UI에 업데이트
    public void UpdateBreadCount(int breadCount)
    {
        if (currentUI != null)
        {
            // UI에서 TextMeshPro 컴포넌트 찾기
            TextMeshProUGUI textComponent = currentUI.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = breadCount.ToString();
                Debug.Log($"[SimpleCustomerUI] 빵 수량 업데이트: {breadCount}");
            }
            else
            {
                Debug.LogWarning("[SimpleCustomerUI] UI에서 TextMeshPro 컴포넌트를 찾을 수 없습니다!");
            }
        }
    }

    // 마커 UI 생성
    public void ShowMarkerUI()
    {
        // 기존 UI 제거
        HideUI();

        if (markerUIPrefab != null)
        {
            // 마커 프리팹을 고객의 자식으로 인스턴스화
            currentUI = Instantiate(markerUIPrefab, transform);

            // 머리 위 위치 설정
            currentUI.transform.localPosition = Vector3.up * uiHeightOffset;

            Debug.Log($"[SimpleCustomerUI] 마커 UI 생성됨, 위치: {currentUI.transform.position}");
        }
        else
        {
            Debug.LogWarning("[SimpleCustomerUI] 마커 UI 프리팹이 없습니다!");
        }
    }

    private void OnDestroy()
    {
        HideUI();
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

        // 실제 UI 위치 표시 (런타임 시)
        if (Application.isPlaying && currentUI != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentUI.transform.position, 0.3f);
        }
    }
}