using System.Collections;
using UnityEngine;

public class Money : MonoBehaviour
{
    [Header("Money Settings")]
    [SerializeField] private int moneyValue = 1;
    [SerializeField] private readonly float collectAnimationDuration = 0.8f;
    [SerializeField] private readonly AnimationCurve collectCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Collection")]
    [SerializeField] private readonly string playerTag = "Player";
    [SerializeField] private readonly float collectionTriggerRadius = 1.5f;
    [SerializeField] private bool autoCollectByPlayer = false; // 플레이어가 가까이 오면 자동 수집

    private bool isCollected = false;
    private MoneyManager moneyManager;

    public void Initialize(MoneyManager manager = null)
    {
        isCollected = false;

        // MoneyManager 참조 설정
        if (manager != null)
        {
            moneyManager = manager;
        }
        else if (moneyManager == null)
        {
            // 백업: MoneyManager가 전달되지 않은 경우에만 찾기
            moneyManager = FindObjectOfType<MoneyManager>();
        }

        // 자동 수집을 위한 Collider 설정
        if (autoCollectByPlayer)
        {
            SetupColliderForCollection();
        }

        Debug.Log($"[Money {name}] 초기화 완료");
    }

    private void SetupColliderForCollection()
    {
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            // 기본 Box Collider 추가
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = Vector3.one * collectionTriggerRadius;
        }
        else
        {
            collider.isTrigger = true;
        }
    }

    // 플레이어 자동 수집 감지
    private void OnTriggerEnter(Collider other)
    {
        if (!autoCollectByPlayer || isCollected) return;

        if (other.CompareTag(playerTag))
        {
            Debug.Log($"[Money {name}] 플레이어가 접근하여 자동 수집");
            CollectMoney();
        }
    }

    // 돈 수집 (외부에서 호출 가능)
    public void CollectMoney()
    {
        if (isCollected) return;

        isCollected = true;

        // 돈 수집 사운드
        SoundManager.GameSounds.PlayItemPickup();
        Debug.Log($"[Money {name}] 수집됨. 가치: {moneyValue}");

        // 수집 애니메이션 시작
        StartCoroutine(CollectionAnimation());
    }

    // 수집 애니메이션
    private IEnumerator CollectionAnimation()
    {
        Vector3 startPosition = transform.position;
        Vector3 startScale = transform.localScale;

        // 위로 올라가면서 사라지는 애니메이션
        Vector3 endPosition = startPosition + Vector3.up * 2f;
        Vector3 endScale = Vector3.zero;

        float elapsedTime = 0f;

        while (elapsedTime < collectAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / collectAnimationDuration;
            float curveValue = collectCurve.Evaluate(progress);

            // 위치와 스케일 애니메이션
            transform.position = Vector3.Lerp(startPosition, endPosition, curveValue);
            transform.localScale = Vector3.Lerp(startScale, endScale, curveValue);

            yield return null;
        }

        // MoneyManager에 제거 알림
        if (moneyManager != null)
        {
            moneyManager.RemoveMoney(gameObject);
        }

        // TODO: 여기서 플레이어 돈 추가 로직 호출
        // GameManager.Instance.AddMoney(moneyValue);

        // 객체 파괴
        Destroy(gameObject);
    }

    // 수동 수집 (플레이어가 클릭/터치할 때)
    public void OnPlayerInteract()
    {
        if (!isCollected)
        {
            Debug.Log($"[Money {name}] 플레이어가 수동으로 수집");
            CollectMoney();
        }
    }

    // Getter 메서드들
    public int GetMoneyValue()
    {
        return moneyValue;
    }

    public bool IsCollected()
    {
        return isCollected;
    }

    public void SetMoneyValue(int value)
    {
        moneyValue = Mathf.Max(0, value);
    }

    public void SetAutoCollect(bool autoCollect)
    {
        autoCollectByPlayer = autoCollect;
    }

    // Gizmos for debugging
    private void OnDrawGizmosSelected()
    {
        if (autoCollectByPlayer)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, collectionTriggerRadius);
        }

        #if UNITY_EDITOR
        string info = $"가치: {moneyValue}\n수집됨: {isCollected}";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, info);
        #endif
    }
}