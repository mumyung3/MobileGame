using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Counter : MonoBehaviour
{
    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool useColliderDetection = true;
    [SerializeField] private Vector3 detectionBoxSize = new Vector3(3f, 2f, 3f);
    [SerializeField] private Vector3 detectionBoxOffset = Vector3.zero;

    [Header("Payment Processing")]
    [SerializeField] private float paymentDuration = 2f;
    [SerializeField] private bool autoProcessPayment = true;

    [Header("References")]
    [SerializeField] private CustomerManager customerManager;
    [SerializeField] private bool autoFindCustomerManager = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private bool isPlayerNearby = false;
    private bool isProcessingPayment = false;
    private Coroutine paymentCoroutine;
    private BoxCollider detectionCollider;

    private void Start()
    {
        InitializeCounter();
    }

    private void InitializeCounter()
    {
        // 자동으로 CustomerManager 찾기
        if (autoFindCustomerManager && customerManager == null)
        {
            customerManager = FindObjectOfType<CustomerManager>();
            if (customerManager == null)
            {
                Debug.LogWarning("[Counter] CustomerManager를 찾을 수 없습니다!");
            }
            else
            {
                DebugLog("[Counter] CustomerManager 자동 연결 완료");
            }
        }

        // 카운터 태그 설정 확인
        if (!gameObject.CompareTag("Counter"))
        {
            gameObject.tag = "Counter";
            DebugLog("[Counter] 카운터 태그 자동 설정");
        }

        // 콜라이더 기반 감지 시스템 설정
        if (useColliderDetection)
        {
            SetupDetectionCollider();
        }
    }

    private void SetupDetectionCollider()
    {
        // 기존 감지용 콜라이더가 있는지 확인
        detectionCollider = GetComponent<BoxCollider>();

        // 없으면 새로 생성
        if (detectionCollider == null)
        {
            detectionCollider = gameObject.AddComponent<BoxCollider>();
            DebugLog("[Counter] 감지용 BoxCollider 자동 생성");
        }

        // 콜라이더 설정
        detectionCollider.isTrigger = true;
        detectionCollider.size = detectionBoxSize;
        detectionCollider.center = detectionBoxOffset;

        DebugLog("[Counter] 콜라이더 기반 플레이어 감지 시스템 활성화");
    }

    private void Update()
    {
        // 콜라이더 기반 감지를 사용하지 않는 경우에만 Update에서 체크
        if (!useColliderDetection)
        {
            CheckForPlayerManually();
        }

        // 플레이어가 근처에 있고 자동 결제가 활성화되어 있으면 결제 처리
        if (isPlayerNearby && autoProcessPayment && !isProcessingPayment)
        {
            TryProcessNextCustomerPayment();
        }
    }

    // 콜라이더 방식이 아닌 수동 감지 (fallback)
    private void CheckForPlayerManually()
    {
        bool wasPlayerNearby = isPlayerNearby;

        // 플레이어 감지 (Box 영역)
        Vector3 boxCenter = transform.position + transform.TransformDirection(detectionBoxOffset);
        Collider[] playersInRange = Physics.OverlapBox(boxCenter, detectionBoxSize * 0.5f, transform.rotation);
        isPlayerNearby = false;

        foreach (Collider col in playersInRange)
        {
            if (col.CompareTag(playerTag))
            {
                isPlayerNearby = true;
                break;
            }
        }

        // 플레이어 상태 변화 처리
        HandlePlayerStateChange(wasPlayerNearby);
    }

    // 트리거 기반 플레이어 감지
    private void OnTriggerEnter(Collider other)
    {
        if (!useColliderDetection) return;

        if (other.CompareTag(playerTag))
        {
            bool wasPlayerNearby = isPlayerNearby;
            isPlayerNearby = true;
            HandlePlayerStateChange(wasPlayerNearby);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!useColliderDetection) return;

        if (other.CompareTag(playerTag))
        {
            bool wasPlayerNearby = isPlayerNearby;
            isPlayerNearby = false;
            HandlePlayerStateChange(wasPlayerNearby);
        }
    }

    // 플레이어 상태 변화 처리
    private void HandlePlayerStateChange(bool wasPlayerNearby)
    {
        if (isPlayerNearby != wasPlayerNearby)
        {
            if (isPlayerNearby)
            {
                DebugLog("[Counter] 플레이어가 카운터에 접근했습니다");
            }
            else
            {
                DebugLog("[Counter] 플레이어가 카운터에서 떠났습니다");

                // 결제 처리 중이었다면 중단
                if (isProcessingPayment && paymentCoroutine != null)
                {
                    StopCoroutine(paymentCoroutine);
                    isProcessingPayment = false;
                    DebugLog("[Counter] 플레이어가 떠나서 결제 처리 중단");
                }
            }
        }
    }

    // 대기열 첫 번째 고객의 결제 처리 시도
    public void TryProcessNextCustomerPayment()
    {
        if (isProcessingPayment || customerManager == null)
        {
            return;
        }

        // 대기열에서 첫 번째 고객 가져오기 (CustomerManager에서 메서드 추가 필요)
        Customer firstCustomer = GetFirstCustomerInQueue();

        if (firstCustomer != null && CanProcessCustomerPayment(firstCustomer))
        {
            DebugLog($"[Counter] 고객 {firstCustomer.name}의 결제 처리 시작");
            paymentCoroutine = StartCoroutine(ProcessCustomerPayment(firstCustomer));
        }
    }

    // 수동 결제 처리 (플레이어가 직접 호출할 수 있음)
    public void ManualProcessPayment()
    {
        if (!isProcessingPayment)
        {
            TryProcessNextCustomerPayment();
        }
    }

    private Customer GetFirstCustomerInQueue()
    {
        if (customerManager == null) return null;

        return customerManager.GetFirstCustomerInQueue();
    }

    private bool CanProcessCustomerPayment(Customer customer)
    {
        if (customer == null) return false;

        // 고객이 WaitingAtCounter 또는 Paying 상태인지 확인
        Customer.CustomerState state = customer.GetCurrentState();
        return state == Customer.CustomerState.WaitingAtCounter || state == Customer.CustomerState.Paying;
    }

    private IEnumerator ProcessCustomerPayment(Customer customer)
    {
        isProcessingPayment = true;
        DebugLog($"[Counter] {customer.name} 결제 처리 시작");

        // 고객을 결제 상태로 변경
        customer.StartPayment();

        // 결제 처리 시간 대기
        float elapsedTime = 0f;
        while (elapsedTime < paymentDuration)
        {
            // 플레이어가 떠나면 결제 중단
            if (!isPlayerNearby)
            {
                DebugLog($"[Counter] 플레이어가 떠나서 {customer.name} 결제 중단");
                isProcessingPayment = false;
                yield break;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        DebugLog($"[Counter] {customer.name} 결제 완료");

        // 결제 완료 후 고객은 자동으로 Leaving 상태가 되고 대기열에서 제거됨
        // Customer.PayForBread()에서 자동으로 customerManager.RemoveFromCounterQueue(this) 호출

        isProcessingPayment = false;

        // 잠시 대기 후 다음 고객 처리 가능
        yield return new WaitForSeconds(0.5f);
    }

    // 공개 메서드들
    public bool IsPlayerNearby()
    {
        return isPlayerNearby;
    }

    public bool IsProcessingPayment()
    {
        return isProcessingPayment;
    }

    public void SetAutoProcessPayment(bool enabled)
    {
        autoProcessPayment = enabled;
    }

    public void SetDetectionBoxSize(Vector3 newSize)
    {
        detectionBoxSize = newSize;
        if (detectionCollider != null)
        {
            detectionCollider.size = detectionBoxSize;
        }
    }

    public void SetDetectionBoxOffset(Vector3 newOffset)
    {
        detectionBoxOffset = newOffset;
        if (detectionCollider != null)
        {
            detectionCollider.center = detectionBoxOffset;
        }
    }

    public void SetPaymentDuration(float duration)
    {
        paymentDuration = Mathf.Max(0.1f, duration);
    }

    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log(message);
        }
    }

    // Gizmos for debugging
    private void OnDrawGizmosSelected()
    {
        // 플레이어 감지 범위 표시 (Box)
        Gizmos.color = isPlayerNearby ? Color.green : Color.yellow;
        Vector3 boxCenter = transform.position + transform.TransformDirection(detectionBoxOffset);

        // Box 감지 영역 표시
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, detectionBoxSize);
        Gizmos.matrix = oldMatrix;

        // 카운터 표시
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 1.5f);

        #if UNITY_EDITOR
        // 상태 표시
        string status = $"플레이어: {(isPlayerNearby ? "감지됨" : "없음")}";
        if (isProcessingPayment)
        {
            status += "\n결제 처리 중";
        }
        status += $"\n감지 방식: {(useColliderDetection ? "콜라이더" : "수동")}";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, status);
        #endif
    }
}