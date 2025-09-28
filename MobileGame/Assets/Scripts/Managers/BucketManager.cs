using System.Collections.Generic;
using UnityEngine;

public class BucketManager : MonoBehaviour
{
    [Header("Bucket Settings")]
    [SerializeField] private int maxBreadCapacity = 8;
    [SerializeField] private GameObject displayBreadPrefab;

    public GameObject GetDisplayBreadPrefab() { return displayBreadPrefab; }

    [Header("Bread Display Positions")]
    [SerializeField] private Transform[] breadDisplayPoints = new Transform[8];

    [Header("Trigger Zone")]
    [SerializeField] private Collider triggerZone;
    [SerializeField] private bool autoCreateTrigger = true;
    [SerializeField] private Vector3 triggerSize = new Vector3(3f, 2f, 3f);

    [Header("Animation Settings")]
    [SerializeField] private float dropHeight = 2f;
    [SerializeField] private float dropDuration = 0.8f;
    [SerializeField] private float scaleAnimationDuration = 0.5f;
    [SerializeField] private AnimationCurve dropCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private List<GameObject> displayedBreads = new List<GameObject>();
    private int currentBreadCount = 0;

    private void Start()
    {
        ValidateComponents();
        SetupTriggerZone();
    }

    private void ValidateComponents()
    {
        // Display points 검증
        if (breadDisplayPoints.Length != maxBreadCapacity)
        {
            System.Array.Resize(ref breadDisplayPoints, maxBreadCapacity);
        }

        // 빈 포인트들을 자동 생성
        for (int i = 0; i < breadDisplayPoints.Length; i++)
        {
            if (breadDisplayPoints[i] == null)
            {
                GameObject point = new GameObject($"BreadPoint_{i}");
                point.transform.SetParent(transform);

                // 2x4 그리드로 배치
                int row = i / 4;
                int col = i % 4;
                point.transform.localPosition = new Vector3(
                    (col - 1.5f) * 0.5f,  // X: -0.75, -0.25, 0.25, 0.75
                    0.2f,                  // Y: 바닥에서 약간 위
                    (row - 0.5f) * 0.5f   // Z: -0.25, 0.25
                );

                breadDisplayPoints[i] = point.transform;
            }
        }
    }

    private void SetupTriggerZone()
    {
        if (triggerZone == null && autoCreateTrigger)
        {
            // Trigger Zone 자동 생성
            GameObject triggerObj = new GameObject("TriggerZone");
            triggerObj.transform.SetParent(transform);
            triggerObj.transform.localPosition = Vector3.zero;

            BoxCollider boxCollider = triggerObj.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = triggerSize;

            triggerZone = boxCollider;
        }

        if (triggerZone != null)
        {
            // BucketTrigger 컴포넌트 추가
            BucketTrigger bucketTrigger = triggerZone.GetComponent<BucketTrigger>();
            if (bucketTrigger == null)
            {
                bucketTrigger = triggerZone.gameObject.AddComponent<BucketTrigger>();
            }
            bucketTrigger.SetBucketManager(this);
        }
    }

    public void OnPlayerEnterZone(PlayerController player)
    {
        if (CollectionManager.Instance == null) return;

        int playerBreadCount = CollectionManager.Instance.GetBreadCount();
        int breadsToDisplay = Mathf.Min(playerBreadCount, maxBreadCapacity - currentBreadCount);

        if (breadsToDisplay > 0)
        {
            // 플레이어의 빵을 사용
            CollectionManager.Instance.UseBread(breadsToDisplay);

            // 빵 진열 (애니메이션과 함께)
            StartCoroutine(DisplayBreadsWithAnimation(breadsToDisplay));
        }
    }

    private System.Collections.IEnumerator DisplayBreadsWithAnimation(int amount)
    {
        for (int i = 0; i < amount && currentBreadCount < maxBreadCapacity; i++)
        {
            int displayIndex = currentBreadCount;

            if (displayBreadPrefab != null && breadDisplayPoints[displayIndex] != null)
            {
                GameObject newBread = Instantiate(displayBreadPrefab);
                newBread.transform.SetParent(breadDisplayPoints[displayIndex], false);
                newBread.transform.localRotation = Quaternion.identity;
                newBread.name = $"DisplayBread_{displayIndex}";

                // 물리 비활성화
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

                displayedBreads.Add(newBread);
                currentBreadCount++;

                // 드롭 애니메이션 시작
                StartCoroutine(AnimateDropAndScale(newBread, Vector3.zero));

                // 각 빵 사이에 짧은 딜레이
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private System.Collections.IEnumerator AnimateDropAndScale(GameObject bread, Vector3 targetLocalPosition)
    {
        // 빵 오브젝트가 파괴되었는지 확인
        if (bread == null) yield break;

        // 시작 위치 (위쪽에서 시작)
        Vector3 startLocalPosition = targetLocalPosition + Vector3.up * dropHeight;
        bread.transform.localPosition = startLocalPosition;

        // 시작 스케일 (작게 시작)
        Vector3 originalScale = bread.transform.localScale;
        bread.transform.localScale = Vector3.zero;

        float elapsedTime = 0f;

        // 드롭과 스케일 애니메이션을 동시에 실행
        while (elapsedTime < Mathf.Max(dropDuration, scaleAnimationDuration))
        {
            // 매 프레임마다 빵이 아직 존재하는지 확인
            if (bread == null) yield break;

            elapsedTime += Time.deltaTime;

            // 드롭 애니메이션
            if (elapsedTime < dropDuration)
            {
                float dropProgress = elapsedTime / dropDuration;
                float dropCurveValue = dropCurve.Evaluate(dropProgress);
                bread.transform.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, dropCurveValue);
            }
            else
            {
                bread.transform.localPosition = targetLocalPosition;
            }

            // 스케일 애니메이션
            if (elapsedTime < scaleAnimationDuration)
            {
                float scaleProgress = elapsedTime / scaleAnimationDuration;
                float scaleCurveValue = scaleCurve.Evaluate(scaleProgress);

                // 0 -> 1.2 -> 1.0 스케일 애니메이션
                float scaleMultiplier;
                if (scaleCurveValue < 0.7f)
                {
                    // 0에서 1.2로
                    scaleMultiplier = Mathf.Lerp(0f, 1.2f, scaleCurveValue / 0.7f);
                }
                else
                {
                    // 1.2에서 1.0으로
                    scaleMultiplier = Mathf.Lerp(1.2f, 1.0f, (scaleCurveValue - 0.7f) / 0.3f);
                }

                bread.transform.localScale = originalScale * scaleMultiplier;
            }
            else
            {
                bread.transform.localScale = originalScale;
            }

            yield return null;
        }

        // 최종 위치와 스케일 보장 (빵이 아직 존재한다면)
        if (bread != null)
        {
            bread.transform.localPosition = targetLocalPosition;
            bread.transform.localScale = originalScale;
        }
    }

    public void ClearAllDisplayedBreads()
    {
        for (int i = displayedBreads.Count - 1; i >= 0; i--)
        {
            if (displayedBreads[i] != null)
            {
                Destroy(displayedBreads[i]);
            }
        }

        displayedBreads.Clear();
        currentBreadCount = 0;
    }

    public void SellAllBreads()
    {
        if (currentBreadCount > 0)
        {
            // 판매 로직 (나중에 추가 가능)
            ClearAllDisplayedBreads();
        }
    }

    // Getter 메서드들
    public int GetCurrentBreadCount()
    {
        return currentBreadCount;
    }

    public int GetMaxCapacity()
    {
        return maxBreadCapacity;
    }

    public bool IsFull()
    {
        return currentBreadCount >= maxBreadCapacity;
    }

    public int GetRemainingCapacity()
    {
        return maxBreadCapacity - currentBreadCount;
    }

    public bool RemoveOneBread()
    {
        if (currentBreadCount > 0 && displayedBreads.Count > 0)
        {
            // 마지막 빵 제거
            int lastIndex = displayedBreads.Count - 1;
            GameObject breadToRemove = displayedBreads[lastIndex];

            if (breadToRemove != null)
            {
                // 모든 코루틴을 정지하여 애니메이션 참조 오류 방지
                StopAllCoroutines();
                Destroy(breadToRemove);
            }

            displayedBreads.RemoveAt(lastIndex);
            currentBreadCount--;

            return true;
        }

        return false;
    }

    public GameObject GetLastBread()
    {
        if (displayedBreads.Count > 0)
        {
            return displayedBreads[displayedBreads.Count - 1];
        }
        return null;
    }

    public bool RemoveSpecificBread(GameObject breadToRemove)
    {
        if (breadToRemove != null && displayedBreads.Contains(breadToRemove))
        {
            displayedBreads.Remove(breadToRemove);
            currentBreadCount--;
            return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        // Trigger Zone 표시
        if (triggerZone != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.matrix = triggerZone.transform.localToWorldMatrix;
            if (triggerZone is BoxCollider boxCollider)
            {
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            }
            Gizmos.matrix = Matrix4x4.identity;
        }

        // Bread Display Points 표시
        for (int i = 0; i < breadDisplayPoints.Length; i++)
        {
            if (breadDisplayPoints[i] != null)
            {
                Gizmos.color = i < currentBreadCount ? Color.yellow : Color.white;
                Gizmos.DrawWireCube(breadDisplayPoints[i].position, Vector3.one * 0.2f);

                // 인덱스 표시
                Gizmos.color = Color.black;
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(breadDisplayPoints[i].position + Vector3.up * 0.3f, i.ToString());
                #endif
            }
        }
    }
}