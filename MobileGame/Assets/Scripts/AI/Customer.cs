using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Customer : MonoBehaviour
{
    [Header("Customer Settings")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float waitTimeAtBucket = 2f;
    [SerializeField] private float waitTimeAtCounter = 3f;
    [SerializeField] private float despawnDelay = 1f;

    [Header("AI Navigation")]
    [SerializeField] private float stoppingDistance = 1f;
    [SerializeField] private float obstacleAvoidanceRadius = 1.5f;

    [Header("Bread Carry Point")]
    [SerializeField] private Transform customBreadCarryPoint;

    [Header("Bread Collection")]
    [SerializeField] private int minBreadCount = 1;
    [SerializeField] private int maxBreadCount = 4;
    [SerializeField] private float breadStackHeight = 0.15f;

    [Header("Collection Animation")]
    [SerializeField] private float collectAnimationDuration = 0.8f;
    [SerializeField] private AnimationCurve collectCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private NavMeshAgent navAgent;
    private Animator customerAnimator;
    private CustomerState currentState;
    private BucketManager targetBucket;
    private Transform counterPosition;
    private CustomerManager customerManager;

    // 빵 관련
    private List<GameObject> carriedBreads = new List<GameObject>();
    private Transform breadCarryPoint;
    private int targetBreadCount;

    public enum CustomerState
    {
        MovingToBucket,
        WaitingAtBucket,
        PickingUpBread,
        MovingToCounter,
        WaitingAtCounter,
        Paying,
        Leaving
    }

    private void Awake()
    {
        SetupComponents();
    }

    private void SetupComponents()
    {
        // NavMeshAgent 설정
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            navAgent = gameObject.AddComponent<NavMeshAgent>();
        }

        navAgent.speed = moveSpeed;
        navAgent.stoppingDistance = stoppingDistance;
        navAgent.radius = obstacleAvoidanceRadius;
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // Animator 찾기
        customerAnimator = GetComponent<Animator>();
        if (customerAnimator == null)
        {
            customerAnimator = GetComponentInChildren<Animator>();
        }

        // 빵 들고다닐 포인트 생성
        CreateBreadCarryPoint();
    }

    private void CreateBreadCarryPoint()
    {
        // 사용자가 지정한 Transform이 있으면 그것을 사용
        if (customBreadCarryPoint != null)
        {
            breadCarryPoint = customBreadCarryPoint;
        }
        else
        {
            // 없으면 기본 위치에 자동 생성
            GameObject carryPoint = new GameObject("BreadCarryPoint");
            carryPoint.transform.SetParent(transform);
            carryPoint.transform.localPosition = new Vector3(0, 1.2f, 0.3f); // 손 앞쪽
            breadCarryPoint = carryPoint.transform;
        }
    }

    public void Initialize(CustomerManager manager, BucketManager bucket, Transform counter)
    {
        customerManager = manager;
        targetBucket = bucket;
        counterPosition = counter;
        currentState = CustomerState.MovingToBucket;

        // 수집할 빵 개수 랜덤 결정
        targetBreadCount = Random.Range(minBreadCount, maxBreadCount + 1);

        StartCustomerBehavior();
    }

    private void StartCustomerBehavior()
    {
        StartCoroutine(CustomerBehaviorRoutine());
    }

    private System.Collections.IEnumerator CustomerBehaviorRoutine()
    {
        while (currentState != CustomerState.Leaving)
        {
            switch (currentState)
            {
                case CustomerState.MovingToBucket:
                    yield return MoveToBucket();
                    break;

                case CustomerState.WaitingAtBucket:
                    yield return WaitAtBucket();
                    break;

                case CustomerState.PickingUpBread:
                    yield return PickUpBread();
                    break;

                case CustomerState.MovingToCounter:
                    yield return MoveToCounter();
                    break;

                case CustomerState.WaitingAtCounter:
                    yield return WaitAtCounter();
                    break;

                case CustomerState.Paying:
                    yield return PayForBread();
                    break;
            }

            yield return null;
        }

        // 고객 퇴장
        yield return LeaveStore();
    }

    private IEnumerator MoveToBucket()
    {
        if (targetBucket == null)
        {
            currentState = CustomerState.Leaving;
            yield break;
        }

        // 버킷 근처로 이동
        Vector3 bucketPosition = targetBucket.transform.position;
        Vector3 targetPosition = GetNearbyPosition(bucketPosition, 2f);

        navAgent.SetDestination(targetPosition);
        UpdateAnimation(true);

        // 목적지에 도착할 때까지 대기
        while (!HasReachedDestination(targetPosition))
        {
            yield return new WaitForSeconds(0.1f);
        }

        UpdateAnimation(false);
        currentState = CustomerState.WaitingAtBucket;
    }

    private IEnumerator WaitAtBucket()
    {
        // 빵이 올 때까지 무한정 기다림
        while (targetBucket.GetCurrentBreadCount() <= 0)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 빵이 있으면 조금 더 기다린 후 픽업
        yield return new WaitForSeconds(waitTimeAtBucket);
        currentState = CustomerState.PickingUpBread;
    }

    private IEnumerator PickUpBread()
    {
        int breadCollected = 0;

        // 원하는 만큼 또는 버킷에 있는 만큼 빵 수집
        while (breadCollected < targetBreadCount && targetBucket.GetCurrentBreadCount() > 0)
        {
            // 버킷에서 빵 애니메이션과 함께 제거
            yield return StartCoroutine(CollectAndAnimateBread(breadCollected));
            breadCollected++;
        }

        // 빵을 집은 후 애니메이션 업데이트 (들고 있는 상태로)
        UpdateAnimation(false);

        // 빵을 집은 후 1초 대기
        yield return new WaitForSeconds(1f);

        currentState = CustomerState.MovingToCounter;
    }

    private IEnumerator CollectAndAnimateBread(int stackIndex)
    {
        // 버킷에서 빵이 있는지 확인하고 애니메이션이 끝날 때까지 대기
        while (targetBucket.GetCurrentBreadCount() <= 0)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // 추가 대기: 빵 진열 애니메이션이 완료될 때까지
        yield return new WaitForSeconds(0.3f);

        // 버킷에서 빵 하나 제거
        if (!targetBucket.RemoveOneBread())
        {
            yield break;
        }

        // 새 빵 오브젝트 생성 (애니메이션용)
        GameObject breadToCollect = Instantiate(targetBucket.GetDisplayBreadPrefab());
        if (breadToCollect == null)
        {
            yield break;
        }

        // 1단계: 버킷에서 역방향 애니메이션
        yield return StartCoroutine(AnimateBreadFromBucket(breadToCollect));

        // 2단계: 고객 손으로 이동 애니메이션
        yield return StartCoroutine(AnimateBreadToHand(breadToCollect, stackIndex));
    }

    private IEnumerator AnimateBreadFromBucket(GameObject breadToCollect)
    {
        // 버킷 위치에서 시작
        Vector3 startPosition = targetBucket.transform.position + Vector3.up * 0.5f;
        Vector3 endPosition = startPosition + Vector3.up * 2f;

        breadToCollect.transform.position = startPosition;
        Vector3 startScale = Vector3.one;
        breadToCollect.transform.localScale = startScale;

        float elapsedTime = 0f;

        // 역방향 애니메이션 (위로 올라가면서 작아짐)
        while (elapsedTime < collectAnimationDuration)
        {
            if (breadToCollect == null) yield break;

            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / collectAnimationDuration;
            float curveValue = collectCurve.Evaluate(progress);

            // 위로 올라가면서
            breadToCollect.transform.position = Vector3.Lerp(startPosition, endPosition, curveValue);

            // 작아지면서
            float scaleMultiplier = Mathf.Lerp(1f, 0f, curveValue);
            breadToCollect.transform.localScale = startScale * scaleMultiplier;

            yield return null;
        }
    }


    private IEnumerator AnimateBreadToHand(GameObject bread, int stackIndex)
    {
        if (bread == null || breadCarryPoint == null) yield break;

        // 물리 즉시 비활성화 (위치 문제 방지)
        Rigidbody breadRb = bread.GetComponent<Rigidbody>();
        if (breadRb != null)
        {
            breadRb.isKinematic = true;
        }

        // 수집 기능 비활성화
        Bread breadScript = bread.GetComponent<Bread>();
        if (breadScript != null)
        {
            breadScript.enabled = false;
        }

        // 빵을 고객 손에 부모 설정
        bread.transform.SetParent(breadCarryPoint, false);

        // 목표 위치 (스택 위치)
        Vector3 targetLocalPosition = Vector3.up * (stackIndex * breadStackHeight);
        Vector3 startLocalPosition = targetLocalPosition + Vector3.up * 2f;

        // 시작 위치와 스케일 설정 (강제로 설정)
        bread.transform.localPosition = startLocalPosition;
        bread.transform.localRotation = Quaternion.identity;
        bread.transform.localScale = Vector3.zero;

        // 한 프레임 대기 후 위치 재설정 (Transform 업데이트 보장)
        yield return null;
        bread.transform.localPosition = startLocalPosition;

        Vector3 targetScale = Vector3.one * 0.8f;

        float elapsedTime = 0f;

        while (elapsedTime < collectAnimationDuration)
        {
            if (bread == null) yield break;

            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / collectAnimationDuration;
            float curveValue = collectCurve.Evaluate(progress);

            // 아래로 내려오면서
            bread.transform.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, curveValue);

            // 커지면서 (0 -> 1.2 -> 0.8)
            float scaleMultiplier;
            if (curveValue < 0.7f)
            {
                scaleMultiplier = Mathf.Lerp(0f, 1.2f, curveValue / 0.7f);
            }
            else
            {
                scaleMultiplier = Mathf.Lerp(1.2f, 0.8f, (curveValue - 0.7f) / 0.3f);
            }

            bread.transform.localScale = targetScale * scaleMultiplier;

            yield return null;
        }

        // 최종 위치와 스케일
        if (bread != null)
        {
            bread.transform.localPosition = targetLocalPosition;
            bread.transform.localScale = targetScale;

            bread.name = $"CarriedBread_{stackIndex}";
            carriedBreads.Add(bread);
        }
    }


    private IEnumerator MoveToCounter()
    {
        if (counterPosition == null)
        {
            currentState = CustomerState.Leaving;
            yield break;
        }

        Vector3 targetPosition = GetNearbyPosition(counterPosition.position, 1.5f);
        navAgent.SetDestination(targetPosition);
        UpdateAnimation(true);

        while (!HasReachedDestination(targetPosition))
        {
            yield return new WaitForSeconds(0.1f);
        }

        UpdateAnimation(false);
        currentState = CustomerState.WaitingAtCounter;
    }

    private IEnumerator WaitAtCounter()
    {
        yield return new WaitForSeconds(waitTimeAtCounter);
        currentState = CustomerState.Paying;
    }

    private IEnumerator PayForBread()
    {
        // 결제 처리 - 모든 빵 제거
        for (int i = carriedBreads.Count - 1; i >= 0; i--)
        {
            if (carriedBreads[i] != null)
            {
                Destroy(carriedBreads[i]);
            }
        }
        carriedBreads.Clear();

        // 빵을 놓은 후 애니메이션 업데이트
        UpdateAnimation(false);

        // 돈 지급 (나중에 MoneyManager 연동)
        yield return new WaitForSeconds(2f);
        currentState = CustomerState.Leaving;
    }

    private IEnumerator LeaveStore()
    {
        // 출구로 이동
        if (customerManager != null)
        {
            Transform exitPoint = customerManager.GetRandomExitPoint();
            if (exitPoint != null)
            {
                navAgent.SetDestination(exitPoint.position);
                UpdateAnimation(true);

                while (Vector3.Distance(transform.position, exitPoint.position) > 1f)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }

        yield return new WaitForSeconds(despawnDelay);

        // CustomerManager에게 제거 알림
        if (customerManager != null)
        {
            customerManager.OnCustomerLeft(this);
        }

        Destroy(gameObject);
    }

    private bool HasReachedDestination(Vector3 targetPosition)
    {
        if (navAgent == null) return true;

        // 거리 체크 (더 후하게)
        float distance = Vector3.Distance(transform.position, targetPosition);
        bool isCloseEnough = distance <= stoppingDistance + 1.0f; // 여유를 더 많이 둠

        // 경로 계산이 끝났는지 체크
        bool isNotPending = !navAgent.pathPending;

        // NavMeshAgent가 목적지에 도달했다고 판단하는지 체크 (더 후하게)
        bool agentReached = !navAgent.hasPath || navAgent.remainingDistance < 1.0f;

        return isCloseEnough && isNotPending && agentReached;
    }

    private Vector3 GetNearbyPosition(Vector3 targetPos, float radius)
    {
        // 타겟 위치 근처의 랜덤한 위치 찾기
        for (int i = 0; i < 10; i++) // 최대 10번 시도
        {
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection.y = 0; // Y축 제거
            Vector3 targetPosition = targetPos + randomDirection;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPosition, out hit, radius, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return targetPos; // 실패 시 원래 위치 반환
    }

    private void UpdateAnimation(bool isMoving)
    {
        if (customerAnimator != null)
        {
            customerAnimator.SetBool("bIsRunning", isMoving);
            customerAnimator.SetBool("bIsGrabbed", carriedBreads.Count > 0);

            // 즉시 적용을 위해 강제 업데이트
            customerAnimator.Update(0f);
        }
    }

    // 즉시 애니메이션 상태 변경 (블렌딩 없이)
    public void ForceAnimationState(string stateName)
    {
        if (customerAnimator != null)
        {
            customerAnimator.Play(stateName, 0, 0f);
        }
    }

    // Getter 메서드들
    public CustomerState GetCurrentState()
    {
        return currentState;
    }

    public bool IsMoving()
    {
        return navAgent != null && navAgent.velocity.magnitude > 0.1f;
    }

    private void OnDrawGizmosSelected()
    {
        if (navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.yellow;
            Vector3[] path = navAgent.path.corners;
            for (int i = 0; i < path.Length - 1; i++)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }
        }

        // 현재 상태 표시
        Gizmos.color = Color.white;
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, currentState.ToString());
        #endif
    }
}