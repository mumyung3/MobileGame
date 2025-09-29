using UnityEngine;

public class Bread : MonoBehaviour, IInteractable
{
    [Header("Bread Settings")]
    [SerializeField] private float collectRadius = 1f;
    [SerializeField] private bool autoCollectOnTouch = true;

    [Header("Physics Settings")]
    [SerializeField] private float spawnForce = 1f;
    [SerializeField] private float spawnTorque = 2f;
    [SerializeField] private Vector3 forceDirection = new Vector3(0, 0, 0.5f);
    [SerializeField] private float dragAmount = 2f;
    [SerializeField] private float angularDragAmount = 5f;


    private OvenManager ovenManager;
    private bool isCollected = false;
    private bool isCarriedByCustomer = false; // 고객이 들고 있는지 여부
    private bool isOnDisplay = false; // 진열대에 놓인 빵인지 여부 (플레이어가 다시 수집 불가)
    private Rigidbody breadRigidbody;

    private void Start()
    {
        InitializePhysics();
        ApplySpawnForce();
    }

    private void InitializePhysics()
    {
        // Rigidbody 컴포넌트 찾기 또는 추가
        breadRigidbody = GetComponent<Rigidbody>();
        if (breadRigidbody == null)
        {
            breadRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        // 물리 설정 조정 (너무 세지 않게)
        breadRigidbody.mass = 0.5f; // 가벼운 빵
        breadRigidbody.drag = dragAmount; // 공기 저항
        breadRigidbody.angularDrag = angularDragAmount; // 회전 저항
        breadRigidbody.useGravity = true;

        // Collider 확인 및 추가
        Collider breadCollider = GetComponent<Collider>();
        if (breadCollider == null)
        {
            // 기본 Box Collider 추가
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = false; // 물리 충돌용
        }

        // 자동 수집용 Trigger Collider 추가
        if (autoCollectOnTouch)
        {
            GameObject triggerChild = new GameObject("CollectTrigger");
            triggerChild.transform.SetParent(transform);
            triggerChild.transform.localPosition = Vector3.zero;
            triggerChild.transform.localScale = Vector3.one;

            SphereCollider triggerCollider = triggerChild.AddComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = collectRadius;

            // 트리거 감지용 스크립트 추가
            BreadTrigger triggerScript = triggerChild.AddComponent<BreadTrigger>();
            triggerScript.SetBread(this);
        }
    }

    private void ApplySpawnForce()
    {
        if (breadRigidbody == null) return;

        // 랜덤 방향으로 약간 변화 (더 부드럽게)
        Vector3 randomDirection = forceDirection + new Vector3(
            Random.Range(-0.1f, 0.1f),
            Random.Range(-0.05f, 0.05f),
            Random.Range(-0.1f, 0.1f)
        );

        // 힘 적용 (위로 튀어나가면서 앞으로)
        //breadRigidbody.AddForce(randomDirection * spawnForce, ForceMode.Impulse);

        // 회전 토크 적용 (더 부드럽게)
        Vector3 randomTorque = new Vector3(
            Random.Range(-spawnTorque, spawnTorque),
            Random.Range(-spawnTorque * 0.5f, spawnTorque * 0.5f),
            Random.Range(-spawnTorque, spawnTorque)
        );
        breadRigidbody.AddTorque(randomTorque, ForceMode.Impulse);
    }

    public void SetOvenManager(OvenManager manager)
    {
        ovenManager = manager;
    }

    public void OnInteract()
    {
        CollectBread();
    }

    private void CollectBread()
    {
        if (isCollected) return;

        // 수집 가능한지 확인
        if (CollectionManager.Instance != null && !CollectionManager.Instance.CanCollectBread())
        {
            return;
        }

        isCollected = true;

        // CollectionManager에 수집 알림
        if (CollectionManager.Instance != null)
        {
            CollectionManager.Instance.CollectBread(1);
        }

        // OvenManager에게 수집됨을 알림 (스폰 카운트 관리)
        if (ovenManager != null)
        {
            ovenManager.OnBreadCollected();
        }

        // 빵 수집 효과나 애니메이션 추가 가능
        PlayCollectEffect();

        // 빵 오브젝트 제거
        Destroy(gameObject);
    }

    private void PlayCollectEffect()
    {
        // 나중에 파티클이나 사운드 효과 추가 가능
    }

    // 고객이 빵을 들고 있는 상태 설정
    public void SetCarriedByCustomer(bool carried)
    {
        isCarriedByCustomer = carried;
    }

    // 고객이 들고 있는지 확인
    public bool IsCarriedByCustomer()
    {
        return isCarriedByCustomer;
    }

    // 진열대에 놓인 빵 상태 설정
    public void SetOnDisplay(bool onDisplay)
    {
        isOnDisplay = onDisplay;
    }

    // 진열대에 놓인 빵인지 확인
    public bool IsOnDisplay()
    {
        return isOnDisplay;
    }

    public void OnPlayerEnterTrigger(PlayerController player)
    {
        if (!autoCollectOnTouch || isCollected || isCarriedByCustomer || isOnDisplay) return;

        CollectBread();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, collectRadius);
    }
}