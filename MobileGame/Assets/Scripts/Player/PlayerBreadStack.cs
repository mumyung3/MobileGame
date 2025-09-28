using System.Collections.Generic;
using UnityEngine;

public class PlayerBreadStack : MonoBehaviour
{
    [Header("Stack Settings")]
    [SerializeField] private Transform stackPoint;
    [SerializeField] private GameObject stackedBreadPrefab;
    [SerializeField] private int maxStackCount = 10;
    [SerializeField] private float stackHeight = 0.3f;
    [SerializeField] private Vector3 stackOffset = Vector3.zero;

    [Header("Visual Effects")]
    [SerializeField] private float stackAnimationDuration = 0.5f;
    [SerializeField] private AnimationCurve stackCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);


    private List<GameObject> stackedBreads = new List<GameObject>();
    private CollectionManager collectionManager;

    private void Start()
    {
        // Stack Point 설정
        if (stackPoint == null)
        {
            // Stack Point가 없으면 자동 생성
            GameObject stackPointObj = new GameObject("BreadStackPoint");
            stackPointObj.transform.SetParent(transform);
            stackPointObj.transform.localPosition = Vector3.up * 2f; // 플레이어 머리 위
            stackPoint = stackPointObj.transform;

        }

        // CollectionManager 이벤트 구독
        collectionManager = CollectionManager.Instance;
        if (collectionManager != null)
        {
            collectionManager.OnBreadCountChanged.AddListener(UpdateBreadStack);
        }
    }

    private void UpdateBreadStack(int newBreadCount)
    {
        int targetStackCount = Mathf.Min(newBreadCount, maxStackCount);


        // 빵이 늘어났을 때
        while (stackedBreads.Count < targetStackCount)
        {
            AddBreadToStack();
        }

        // 빵이 줄어들었을 때
        while (stackedBreads.Count > targetStackCount)
        {
            RemoveBreadFromStack();
        }
    }

    private void AddBreadToStack()
    {
        if (stackedBreadPrefab == null)
        {
            return;
        }

        int stackIndex = stackedBreads.Count;

        GameObject newBread = Instantiate(stackedBreadPrefab);
        newBread.transform.SetParent(stackPoint, false);
        newBread.transform.localPosition = GetStackPosition(stackIndex);
        newBread.transform.localRotation = Quaternion.identity;
        newBread.name = $"StackedBread_{stackIndex}";

        // 물리 비활성화 (쌓인 빵은 물리 적용 안함)
        Rigidbody breadRigidbody = newBread.GetComponent<Rigidbody>();
        if (breadRigidbody != null)
        {
            breadRigidbody.isKinematic = true;
        }

        // 수집 기능 비활성화
        Bread breadScript = newBread.GetComponent<Bread>();
        if (breadScript != null)
        {
            breadScript.enabled = false;
        }

        // 애니메이션 효과
        StartCoroutine(AnimateStackBread(newBread, GetStackPosition(stackIndex)));

        stackedBreads.Add(newBread);

    }

    private void RemoveBreadFromStack()
    {
        if (stackedBreads.Count == 0) return;

        int lastIndex = stackedBreads.Count - 1;
        GameObject breadToRemove = stackedBreads[lastIndex];

        if (breadToRemove != null)
        {
            Destroy(breadToRemove);
        }

        stackedBreads.RemoveAt(lastIndex);

    }

    private Vector3 GetStackPosition(int stackIndex)
    {
        Vector3 localPosition = stackOffset + Vector3.up * (stackIndex * stackHeight);
        return localPosition;
    }

    private System.Collections.IEnumerator AnimateStackBread(GameObject bread, Vector3 targetLocalPosition)
    {
        Vector3 startLocalPosition = targetLocalPosition + Vector3.up * 2f; // 위에서 시작
        bread.transform.localPosition = startLocalPosition;

        float elapsedTime = 0f;
        while (elapsedTime < stackAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / stackAnimationDuration;
            float curveValue = stackCurve.Evaluate(progress);

            bread.transform.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, curveValue);
            yield return null;
        }

        bread.transform.localPosition = targetLocalPosition;
    }

    public void ClearAllBread()
    {
        for (int i = stackedBreads.Count - 1; i >= 0; i--)
        {
            if (stackedBreads[i] != null)
            {
                Destroy(stackedBreads[i]);
            }
        }

        stackedBreads.Clear();

    }

    public int GetStackedCount()
    {
        return stackedBreads.Count;
    }

    public bool IsStackFull()
    {
        return stackedBreads.Count >= maxStackCount;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (collectionManager != null)
        {
            collectionManager.OnBreadCountChanged.RemoveListener(UpdateBreadStack);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (stackPoint == null) return;

        Gizmos.color = Color.yellow;

        // 스택 포인트 표시
        Gizmos.DrawWireSphere(stackPoint.position + stackOffset, 0.2f);

        // 스택 높이 표시
        for (int i = 0; i < maxStackCount; i++)
        {
            Vector3 stackPos = GetStackPosition(i);
            Gizmos.color = Color.Lerp(Color.yellow, Color.red, (float)i / maxStackCount);
            Gizmos.DrawWireCube(stackPos, Vector3.one * 0.2f);
        }
    }
}