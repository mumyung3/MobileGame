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

    // 제자리 맴돌기 감지용
    private Vector3 lastPosition = Vector3.zero;
    private float stuckTimer = 0f;
    private float stuckCheckInterval = 0.5f; // 0.5초마다 체크
    private float maxStuckTime = 2f; // 2초간 같은 곳에 있으면 제자리 맴돌기로 판정
    private float stuckDistanceThreshold = 0.3f; // 0.3m 이내 움직임만 있으면 제자리 맴돌기로 판정

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
        navAgent.stoppingDistance = 0.5f; // 적당한 거리에서 멈추도록 설정
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

        // 목적지에 도착할 때까지 대기 (버킷은 후한 판정)
        while (!HasReachedBucketDestination(targetPosition))
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

        // 대기열 참가 요청
        if (customerManager != null)
        {
            customerManager.RequestJoinQueue(this);
        }

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

        // 새로 생성된 빵은 진열대 상태 해제 (고객이 가져갈 수 있도록)
        Bread newBreadScript = breadToCollect.GetComponent<Bread>();
        if (newBreadScript != null)
        {
            newBreadScript.SetCarriedByCustomer(false);
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

        // 수집 기능 비활성화 및 고객이 들고 있는 상태로 설정
        Bread breadScript = bread.GetComponent<Bread>();
        if (breadScript != null)
        {
            breadScript.SetCarriedByCustomer(true); // 플레이어가 회수하지 못하도록
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
        if (counterPosition == null || customerManager == null)
        {
            currentState = CustomerState.Leaving;
            yield break;
        }

        // 대기열 위치 요청
        Vector3 targetPosition = customerManager.RequestJoinQueue(this);
        navAgent.SetDestination(targetPosition);
        UpdateAnimation(true);

        while (!HasReachedCounterDestination(targetPosition))
        {
            // 제자리 맴돌기 감지 및 처리
            if (CheckAndHandleStuckMovement(targetPosition))
            {
                // 제자리 맴돌기 해결 중이면 잠시 대기
                yield return new WaitForSeconds(2f);
            }

            yield return new WaitForSeconds(0.1f);
        }

        // 강제 위치 이동 완료 후 애니메이션 정지
        UpdateAnimation(false);

        Debug.Log($"[Customer {name}] 대기열 근처 도착! 도착 순서 등록 중...");

        // 실제 도착했음을 CustomerManager에게 알리고 정확한 위치 받기
        if (customerManager != null)
        {
            Vector3 finalPosition = customerManager.OnCustomerArrivedAtQueue(this);

            // 최종 위치가 현재 위치와 다르면 이동
            if (Vector3.Distance(transform.position, finalPosition) > 0.5f)
            {
                Debug.Log($"[Customer {name}] 최종 위치로 미세 조정: {finalPosition}");
                navAgent.enabled = false;
                transform.position = finalPosition;
                navAgent.enabled = true;
            }
        }

        Debug.Log($"[Customer {name}] 대기열 최종 위치 도착! 현재 위치: {transform.position}");

        // 잠시 대기 후 카운터 방향으로 회전
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(LookAtCounter());

        Debug.Log($"[Customer {name}] 대기열 위치 정착 및 카운터 응시 완료");

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
                // 빵의 carried 상태 해제 (혹시 제거되기 전에)
                Bread breadScript = carriedBreads[i].GetComponent<Bread>();
                if (breadScript != null)
                {
                    breadScript.SetCarriedByCustomer(false);
                }

                Destroy(carriedBreads[i]);
            }
        }
        carriedBreads.Clear();

        // 빵을 놓은 후 애니메이션 업데이트
        UpdateAnimation(false);


        // 카운터 대기열에서 제거
        if (customerManager != null)
        {
            customerManager.RemoveFromCounterQueue(this);
        }

        // 돈 지급 (나중에 MoneyManager 연동)
        yield return new WaitForSeconds(2f);
        currentState = CustomerState.Leaving;
    }

    private IEnumerator LeaveStore()
    {
        // 먼저 대기열을 피해서 안전한 위치로 이동
        if (customerManager != null && counterPosition != null)
        {
            Vector3 safePosition = GetSafeExitPosition();
            if (safePosition != Vector3.zero)
            {
                navAgent.SetDestination(safePosition);
                UpdateAnimation(true);

                // 안전한 위치에 도달할 때까지 대기
                while (Vector3.Distance(transform.position, safePosition) > 1f)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }

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

    // 버킷 도달 판정 (후하게)
    private bool HasReachedBucketDestination(Vector3 targetPosition)
    {
        if (navAgent == null) return true;

        // 거리 체크 (후하게)
        float distance = Vector3.Distance(transform.position, targetPosition);
        bool isCloseEnough = distance <= stoppingDistance + 1.5f; // 여유를 많이 둠

        // 경로 계산이 끝났는지 체크
        bool isNotPending = !navAgent.pathPending;

        // NavMeshAgent가 목적지에 도달했다고 판단하는지 체크 (후하게)
        bool agentReached = !navAgent.hasPath || navAgent.remainingDistance < 1.5f;

        return isCloseEnough && isNotPending && agentReached;
    }

    // 카운터 도달 판정 - 가까워지면 강제 위치 이동
    private bool HasReachedCounterDestination(Vector3 targetPosition)
    {
        if (navAgent == null) return true;

        float distance = Vector3.Distance(transform.position, targetPosition);

        // 일정 거리 이하로 가까워지면 강제로 정확한 위치로 이동
        if (distance <= 1.5f)
        {
            Debug.Log($"[Customer {name}] 대기열 위치 근처 도달 - 강제 위치 이동: {distance:F2}m → 정확한 위치로");

            // NavMeshAgent 비활성화하고 강제 위치 이동
            navAgent.enabled = false;
            transform.position = targetPosition;
            navAgent.enabled = true;

            return true;
        }

        return false;
    }

    // 제자리 맴돌기 감지 및 처리
    private bool CheckAndHandleStuckMovement(Vector3 targetPosition)
    {
        float currentDistance = Vector3.Distance(transform.position, lastPosition);

        // 처음 위치 기록
        if (lastPosition == Vector3.zero)
        {
            lastPosition = transform.position;
            stuckTimer = 0f;
            return false;
        }

        // 0.5초마다 체크
        stuckTimer += Time.deltaTime;
        if (stuckTimer >= stuckCheckInterval)
        {
            // 거의 같은 위치에서 맴돌고 있는지 확인
            if (currentDistance <= stuckDistanceThreshold)
            {
                float timeSinceLastCheck = stuckTimer;

                // 2초 동안 제자리에 있었다면 재이동 처리
                if (timeSinceLastCheck >= maxStuckTime)
                {
                    Debug.LogWarning($"[Customer {name}] 제자리 맴돌기 감지! 거리: {currentDistance:F2}m, 시간: {timeSinceLastCheck:F1}초");
                    HandleStuckMovement(targetPosition);
                    return true;
                }
            }
            else
            {
                // 움직이고 있으면 타이머 리셋
                lastPosition = transform.position;
                stuckTimer = 0f;
            }
        }

        return false;
    }

    // 제자리 맴돌기 해결: 잠시 다른 곳으로 보낸 후 다시 목표 위치로
    private void HandleStuckMovement(Vector3 targetPosition)
    {
        StartCoroutine(HandleStuckMovementCoroutine(targetPosition));
    }

    private System.Collections.IEnumerator HandleStuckMovementCoroutine(Vector3 targetPosition)
    {
        Debug.Log($"[Customer {name}] 제자리 맴돌기 해결 시작 - 임시 위치로 이동");

        // 1단계: 근처의 임시 위치로 이동
        Vector3 tempPosition = GetTemporaryPosition(targetPosition);
        navAgent.SetDestination(tempPosition);

        // 임시 위치로 이동하거나 1초 대기
        float tempMoveStart = Time.time;
        while (Vector3.Distance(transform.position, tempPosition) > 1f && (Time.time - tempMoveStart) < 1f)
        {
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log($"[Customer {name}] 임시 위치 도달 - 이제 목표 위치로 재이동");

        // 2단계: 목표 위치로 다시 이동
        navAgent.SetDestination(targetPosition);

        // 상태 리셋
        lastPosition = Vector3.zero;
        stuckTimer = 0f;
    }

    // 임시 위치 계산 (목표 위치 근처의 안전한 위치)
    private Vector3 GetTemporaryPosition(Vector3 targetPosition)
    {
        // 목표 위치에서 2-3m 떨어진 랜덤한 위치 찾기
        for (int i = 0; i < 5; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere;
            randomDirection.y = 0; // Y축 제거
            randomDirection = randomDirection.normalized;

            Vector3 tempPos = targetPosition + randomDirection * Random.Range(2f, 3f);

            // NavMesh에서 유효한 위치인지 확인
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(tempPos, out hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        // 실패하면 목표 위치에서 뒤쪽으로 2m
        return targetPosition + Vector3.back * 2f;
    }

    // 대기열을 피해서 안전한 출구 위치 찾기
    private Vector3 GetSafeExitPosition()
    {
        if (counterPosition == null) return Vector3.zero;

        // 카운터 옆쪽으로 이동 (대기열과 반대 방향)
        Vector3 queueDirection = Vector3.back; // 대기열 방향
        Vector3 sideDirection = Vector3.Cross(queueDirection, Vector3.up).normalized; // 옆쪽 방향

        // 카운터에서 옆쪽으로 3f 떨어진 위치
        Vector3 safePosition = counterPosition.position + counterPosition.TransformDirection(sideDirection) * 3f;

        // NavMesh에서 유효한 위치 찾기
        NavMeshHit hit;
        if (NavMesh.SamplePosition(safePosition, out hit, 5f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        // 실패 시 반대편 시도
        safePosition = counterPosition.position + counterPosition.TransformDirection(-sideDirection) * 3f;
        if (NavMesh.SamplePosition(safePosition, out hit, 5f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        // 완전히 실패 시 카운터 앞쪽으로
        safePosition = counterPosition.position + counterPosition.TransformDirection(Vector3.forward) * 4f;
        if (NavMesh.SamplePosition(safePosition, out hit, 5f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return Vector3.zero; // 완전 실패
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


    // 카운터 방향으로 회전
    private IEnumerator LookAtCounter()
    {
        if (counterPosition == null) yield break;

        // NavMeshAgent 회전 비활성화 (수동으로 회전하기 위해)
        if (navAgent != null)
        {
            navAgent.updateRotation = false;
        }

        Vector3 directionToCounter = (counterPosition.position - transform.position).normalized;
        directionToCounter.y = 0f; // Y축 회전 제거

        if (directionToCounter.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToCounter);
            float rotationTime = 0f;
            float rotationDuration = 1f;
            Quaternion startRotation = transform.rotation;

            while (rotationTime < rotationDuration)
            {
                rotationTime += Time.deltaTime;
                float progress = rotationTime / rotationDuration;
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, progress);
                yield return null;
            }

            transform.rotation = targetRotation;
        }

        // NavMeshAgent 회전 다시 활성화
        if (navAgent != null)
        {
            navAgent.updateRotation = true;
        }
    }

    // 대기열 위치 업데이트 (다른 고객이 떠날 때 호출) - 한 칸씩 앞으로 이동
    public void UpdateQueuePosition(Vector3 newQueuePosition)
    {
        Debug.Log($"[Customer {name}] 새로운 대기열 위치 업데이트: {newQueuePosition}, 현재 상태: {currentState}");

        if (navAgent != null && (currentState == CustomerState.MovingToCounter || currentState == CustomerState.WaitingAtCounter))
        {
            if (currentState == CustomerState.MovingToCounter)
            {
                // 아직 이동 중이면 목적지 업데이트 (부드러운 경로 변경)
                Debug.Log($"[Customer {name}] 이동 중 - 목적지를 {newQueuePosition}로 변경");
                navAgent.SetDestination(newQueuePosition);
            }
            else if (currentState == CustomerState.WaitingAtCounter)
            {
                // 이미 대기 중이면 새 위치로 이동 시작 (한 칸 앞으로)
                Debug.Log($"[Customer {name}] 대기 중 - {newQueuePosition}로 한 칸 앞으로 이동 시작");
                StartCoroutine(MoveToNewQueuePosition(newQueuePosition));
            }
        }
        else
        {
            Debug.Log($"[Customer {name}] 대기열 위치 업데이트 무시 - 상태: {currentState}");
        }
    }

    // 대기 중에 새로운 큐 위치로 이동 - 한 칸씩 앞으로 부드럽게 이동
    private IEnumerator MoveToNewQueuePosition(Vector3 newPosition)
    {
        Debug.Log($"[Customer {name}] 한 칸 앞으로 이동 시작: 현재 위치 {transform.position} → 목표 위치 {newPosition}");

        // 새 위치로 이동 시작
        navAgent.SetDestination(newPosition);
        UpdateAnimation(true);

        // 새 위치에 도달할 때까지 대기 (강제 위치 이동 포함)
        float moveStartTime = Time.time;
        while (!HasReachedCounterDestination(newPosition))
        {
            // 제자리 맴돌기 감지 및 처리
            if (CheckAndHandleStuckMovement(newPosition))
            {
                // 제자리 맴돌기 해결 중이면 잠시 대기
                yield return new WaitForSeconds(2f);
            }

            // 너무 오래 걸리면 강제로 위치 설정
            if (Time.time - moveStartTime > 5f)
            {
                Debug.LogWarning($"[Customer {name}] 이동 시간 초과 - 즉시 강제 위치 이동");
                navAgent.enabled = false;
                transform.position = newPosition;
                navAgent.enabled = true;
                break;
            }
            yield return new WaitForSeconds(0.1f);
        }

        UpdateAnimation(false);
        Debug.Log($"[Customer {name}] 새 대기열 위치 정착 완료: {transform.position}");

        // 잠시 대기 후 카운터 방향으로 회전
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(LookAtCounter());

        Debug.Log($"[Customer {name}] 한 칸 앞으로 이동 완료");
    }

    // 카운터 목적지 업데이트 (레거시 - 호환성을 위해 유지)
    public void UpdateCounterDestination(Vector3 newDestination)
    {
        UpdateQueuePosition(newDestination);
    }

    // 결제 관련 메서드들
    public bool IsProcessingPayment()
    {
        return currentState == CustomerState.Paying;
    }

    public void StartPayment()
    {
        if (currentState == CustomerState.WaitingAtCounter)
        {
            currentState = CustomerState.Paying;
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