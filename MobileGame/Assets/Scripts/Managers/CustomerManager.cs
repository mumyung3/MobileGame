using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerManager : MonoBehaviour
{
    [Header("Customer Spawning")]
    [SerializeField] private GameObject[] customerPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform[] exitPoints;
    [SerializeField] private int maxCustomers = 5;
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private float spawnVariation = 3f;

    [Header("Store References")]
    [SerializeField] private BucketManager[] bucketManagers;
    [SerializeField] private Transform counterPosition;
    [SerializeField] private bool autoFindBuckets = true;
    [SerializeField] private bool autoFindCounter = true;

    [Header("Spawn Control")]
    [SerializeField] private bool autoStartSpawning = true;
    [SerializeField] private float initialSpawnDelay = 5f;
    [SerializeField] private int maxCustomersWithoutBread = 2;

    [Header("Counter Queue Settings")]
    [SerializeField] private float queueSpacing = 2f;
    [SerializeField] private Vector3 queueDirection = Vector3.back; // z축 음의 방향으로 줄 세우기

    private List<Customer> activeCustomers = new List<Customer>();
    private List<Customer> counterQueueCustomers = new List<Customer>(); // 도착 순서대로 정렬된 대기열
    private Coroutine spawnCoroutine;
    private bool isSpawning = false;

    private void Start()
    {
        InitializeManager();

        if (autoStartSpawning)
        {
            StartCoroutine(DelayedStartSpawning());
        }
    }

    private void InitializeManager()
    {
        // 자동으로 버킷들 찾기
        if (autoFindBuckets && (bucketManagers == null || bucketManagers.Length == 0))
        {
            bucketManagers = FindObjectsOfType<BucketManager>();
        }

        // 자동으로 카운터 찾기
        if (autoFindCounter && counterPosition == null)
        {
            GameObject counter = GameObject.FindGameObjectWithTag("Counter");
            if (counter != null)
            {
                counterPosition = counter.transform;
            }
        }

        // 스폰 포인트가 없으면 자동 생성
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            CreateDefaultSpawnPoints();
        }

        // 출구 포인트가 없으면 자동 생성
        if (exitPoints == null || exitPoints.Length == 0)
        {
            CreateDefaultExitPoints();
        }

        ValidateSetup();
    }

    private void CreateDefaultSpawnPoints()
    {
        GameObject spawnContainer = new GameObject("CustomerSpawnPoints");
        spawnContainer.transform.SetParent(transform);

        List<Transform> spawns = new List<Transform>();

        // 기본 스폰 포인트 4개 생성 (상점 입구 쪽)
        for (int i = 0; i < 4; i++)
        {
            GameObject spawnPoint = new GameObject($"SpawnPoint_{i}");
            spawnPoint.transform.SetParent(spawnContainer.transform);

            // 일렬로 배치
            spawnPoint.transform.localPosition = new Vector3(
                (i - 1.5f) * 2f, // X: -3, -1, 1, 3
                0,
                -8f // 상점 앞쪽
            );

            spawns.Add(spawnPoint.transform);
        }

        spawnPoints = spawns.ToArray();
    }

    private void CreateDefaultExitPoints()
    {
        GameObject exitContainer = new GameObject("CustomerExitPoints");
        exitContainer.transform.SetParent(transform);

        List<Transform> exits = new List<Transform>();

        // 기본 출구 포인트 2개 생성
        for (int i = 0; i < 2; i++)
        {
            GameObject exitPoint = new GameObject($"ExitPoint_{i}");
            exitPoint.transform.SetParent(exitContainer.transform);

            exitPoint.transform.localPosition = new Vector3(
                (i == 0) ? -5f : 5f, // 좌우 출구
                0,
                -10f // 상점 밖
            );

            exits.Add(exitPoint.transform);
        }

        exitPoints = exits.ToArray();
    }

    private void ValidateSetup()
    {
        if (customerPrefabs == null || customerPrefabs.Length == 0)
        {
            Debug.LogWarning("CustomerManager: No customer prefabs assigned!");
        }

        if (bucketManagers == null || bucketManagers.Length == 0)
        {
            Debug.LogWarning("CustomerManager: No bucket managers found!");
        }

        if (counterPosition == null)
        {
            Debug.LogWarning("CustomerManager: Counter position not set!");
        }
    }

    private IEnumerator DelayedStartSpawning()
    {
        yield return new WaitForSeconds(initialSpawnDelay);
        StartSpawning();
    }

    public void StartSpawning()
    {
        if (isSpawning) return;

        isSpawning = true;
        spawnCoroutine = StartCoroutine(SpawnCustomersRoutine());
    }

    public void StopSpawning()
    {
        if (!isSpawning) return;

        isSpawning = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnCustomersRoutine()
    {
        while (isSpawning)
        {
            // 랜덤 간격으로 대기
            float waitTime = spawnInterval + Random.Range(-spawnVariation, spawnVariation);
            yield return new WaitForSeconds(waitTime);

            // 최대 고객 수 체크
            if (activeCustomers.Count < maxCustomers && CanSpawnCustomer())
            {
                SpawnCustomer();
            }
        }
    }

    private bool CanSpawnCustomer()
    {
        // 기본 조건 확인
        if (customerPrefabs == null || customerPrefabs.Length == 0 ||
            spawnPoints == null || spawnPoints.Length == 0)
        {
            return false;
        }

        // 빵이 있으면 무조건 스폰 가능
        if (HasBreadInBuckets())
        {
            return true;
        }

        // 빵이 없어도 최대 지정된 수까지는 스폰 가능
        return activeCustomers.Count < maxCustomersWithoutBread;
    }

    private bool HasBreadInBuckets()
    {
        if (bucketManagers == null) return false;

        foreach (BucketManager bucket in bucketManagers)
        {
            if (bucket != null && bucket.GetCurrentBreadCount() > 0)
            {
                return true;
            }
        }
        return false;
    }

    private void SpawnCustomer()
    {
        // 랜덤 고객 프리팹 선택
        GameObject customerPrefab = customerPrefabs[Random.Range(0, customerPrefabs.Length)];

        // 랜덤 스폰 포인트 선택
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // 빵이 있는 버킷 선택 (빵이 없어도 랜덤 버킷 선택)
        BucketManager targetBucket = GetAvailableBucket();

        if (targetBucket == null)
        {
            // 빵이 없어도 아무 버킷이나 선택 (고객이 기다리게 됨)
            targetBucket = GetRandomBucket();
        }

        if (targetBucket == null) return;

        // 고객 생성
        GameObject customerObj = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
        Customer customer = customerObj.GetComponent<Customer>();

        if (customer == null)
        {
            customer = customerObj.AddComponent<Customer>();
        }

        // 고객 초기화
        customer.Initialize(this, targetBucket, counterPosition);
        activeCustomers.Add(customer);

        // 네비메시 에이전트가 있는지 확인
        UnityEngine.AI.NavMeshAgent agent = customerObj.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent == null)
        {
            agent = customerObj.AddComponent<UnityEngine.AI.NavMeshAgent>();
        }
    }

    private BucketManager GetAvailableBucket()
    {
        List<BucketManager> availableBuckets = new List<BucketManager>();

        foreach (BucketManager bucket in bucketManagers)
        {
            if (bucket != null && bucket.GetCurrentBreadCount() > 0)
            {
                availableBuckets.Add(bucket);
            }
        }

        if (availableBuckets.Count > 0)
        {
            return availableBuckets[Random.Range(0, availableBuckets.Count)];
        }

        return null;
    }

    private BucketManager GetRandomBucket()
    {
        if (bucketManagers != null && bucketManagers.Length > 0)
        {
            return bucketManagers[Random.Range(0, bucketManagers.Length)];
        }
        return null;
    }

    public void OnCustomerLeft(Customer customer)
    {
        if (activeCustomers.Contains(customer))
        {
            activeCustomers.Remove(customer);
        }
    }

    public Transform GetRandomExitPoint()
    {
        if (exitPoints != null && exitPoints.Length > 0)
        {
            return exitPoints[Random.Range(0, exitPoints.Length)];
        }
        return null;
    }

    // 고객이 대기열에 참가 요청 (빵을 집은 후 호출) - 예약만 하고 위치는 나중에
    public Vector3 RequestJoinQueue(Customer customer)
    {
        if (counterPosition == null) return Vector3.zero;

        Debug.Log($"[CustomerManager] 고객 {customer.name}이 대기열 참가 요청");

        // 예약만 하고 실제 위치는 도착했을 때 결정
        // 현재는 대략적인 위치만 반환 (실제 순서는 도착 시 결정)
        int tempIndex = counterQueueCustomers.Count; // 현재 대기열 뒤쪽 예상 위치
        Vector3 tempPosition = CalculateValidQueuePosition(tempIndex);

        Debug.Log($"[CustomerManager] 고객 {customer.name}에게 임시 목표 위치 제공: {tempPosition}");
        return tempPosition;
    }

    // 고객이 실제로 대기열 위치에 도착했을 때 호출 - 도착 순서대로 배치
    public Vector3 OnCustomerArrivedAtQueue(Customer customer)
    {
        Debug.Log($"[CustomerManager] 고객 {customer.name}이 대기열에 실제 도착 - 도착 순서 기록");

        // 도착한 순서대로 대기열에 추가
        if (!counterQueueCustomers.Contains(customer))
        {
            counterQueueCustomers.Add(customer);
            Debug.Log($"[CustomerManager] 고객 {customer.name}을 대기열 {counterQueueCustomers.Count}번째 위치에 추가");
        }

        // 도착 순서에 따른 실제 위치 계산
        int actualQueueIndex = counterQueueCustomers.IndexOf(customer);
        Vector3 finalPosition = CalculateValidQueuePosition(actualQueueIndex);

        Debug.Log($"[CustomerManager] 고객 {customer.name}의 최종 대기열 위치: {actualQueueIndex + 1}번째, 위치: {finalPosition}");

        // 이미 대기열에 있던 다른 고객들의 위치도 업데이트 (밀려날 수 있음)
        UpdateAllQueuePositions();

        return finalPosition;
    }

    // NavMesh에 맞는 유효한 대기열 위치 계산
    private Vector3 CalculateValidQueuePosition(int queueIndex)
    {
        if (counterPosition == null) return Vector3.zero;

        // 기본 위치 계산
        Vector3 rawPosition = counterPosition.position +
            counterPosition.TransformDirection(queueDirection) * ((queueIndex + 1) * queueSpacing);

        // NavMesh에서 유효한 위치 찾기
        UnityEngine.AI.NavMeshHit hit;
        float searchRadius = 3f; // 검색 반경

        if (UnityEngine.AI.NavMesh.SamplePosition(rawPosition, out hit, searchRadius, UnityEngine.AI.NavMesh.AllAreas))
        {
            Debug.Log($"[CustomerManager] 대기열 위치 {queueIndex + 1}: 원래 위치 {rawPosition} → 유효한 위치 {hit.position}");
            return hit.position;
        }
        else
        {
            Debug.LogWarning($"[CustomerManager] 대기열 위치 {queueIndex + 1}에서 유효한 NavMesh 위치를 찾을 수 없음! 원래 위치 사용: {rawPosition}");
            return rawPosition;
        }
    }

    // 고객이 대기열을 떠날 때 호출 - 뒤의 모든 고객들이 한 칸씩 앞으로 이동
    public void RemoveFromCounterQueue(Customer customer)
    {
        if (counterQueueCustomers.Contains(customer))
        {
            int customerIndex = counterQueueCustomers.IndexOf(customer);
            Debug.Log($"[CustomerManager] 고객 {customer.name}이 대기열 위치 {customerIndex + 1}에서 떠남");

            counterQueueCustomers.Remove(customer);

            Debug.Log($"[CustomerManager] 남은 대기열 고객 수: {counterQueueCustomers.Count}");

            // 뒤에 있던 모든 고객들에게 위치 업데이트 알림 (한 칸씩 앞으로)
            if (counterQueueCustomers.Count > 0)
            {
                Debug.Log($"[CustomerManager] 남은 고객들을 한 칸씩 앞으로 이동시킴");
                UpdateAllQueuePositions();
                PrintQueueStatus();
            }
            else
            {
                Debug.Log($"[CustomerManager] 대기열이 비었습니다");
            }
        }
        else
        {
            Debug.LogWarning($"[CustomerManager] 대기열에 없는 고객 {customer?.name}을 제거하려고 시도");
        }
    }

    // 모든 대기열 고객들의 위치 업데이트 - 한 칸씩 앞으로 이동
    private void UpdateAllQueuePositions()
    {
        Debug.Log($"[CustomerManager] 대기열 위치 업데이트 시작. 총 {counterQueueCustomers.Count}명의 고객");

        for (int i = 0; i < counterQueueCustomers.Count; i++)
        {
            Customer customer = counterQueueCustomers[i];
            if (customer != null)
            {
                // NavMesh에 맞는 유효한 새 위치 계산
                Vector3 validNewPosition = CalculateValidQueuePosition(i);

                Debug.Log($"[CustomerManager] 고객 {i}: 유효한 새 위치 {validNewPosition}로 이동 지시");

                // 고객에게 새로운 위치로 이동하라고 알림
                customer.UpdateQueuePosition(validNewPosition);
            }
            else
            {
                Debug.LogWarning($"[CustomerManager] 대기열 인덱스 {i}에 null 고객 발견!");
            }
        }

        Debug.Log($"[CustomerManager] 대기열 위치 업데이트 완료");
    }

    // 대기열 상태 디버그 출력
    public void PrintQueueStatus()
    {
        Debug.Log($"=== 대기열 상태 ===");
        Debug.Log($"총 대기열 고객 수: {counterQueueCustomers.Count}");

        for (int i = 0; i < counterQueueCustomers.Count; i++)
        {
            Customer customer = counterQueueCustomers[i];
            if (customer != null)
            {
                Vector3 validPosition = CalculateValidQueuePosition(i);
                Vector3 currentPosition = customer.transform.position;
                float distance = Vector3.Distance(currentPosition, validPosition);

                Debug.Log($"위치 {i + 1}: {customer.name}");
                Debug.Log($"  - 현재 위치: {currentPosition}");
                Debug.Log($"  - 목표 위치: {validPosition}");
                Debug.Log($"  - 거리: {distance:F2}m");
            }
            else
            {
                Debug.Log($"위치 {i + 1}: [빈 자리]");
            }
        }
        Debug.Log($"==================");
    }

    // Getter 메서드들
    public int GetActiveCustomerCount()
    {
        return activeCustomers.Count;
    }

    public bool IsSpawning()
    {
        return isSpawning;
    }

    public void SetMaxCustomers(int newMax)
    {
        maxCustomers = Mathf.Max(0, newMax);
    }

    public void SetSpawnInterval(float newInterval)
    {
        spawnInterval = Mathf.Max(1f, newInterval);
    }

    private void OnDestroy()
    {
        StopSpawning();
    }

    private void OnDrawGizmosSelected()
    {
        // 스폰 포인트 표시
        if (spawnPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (Transform spawn in spawnPoints)
            {
                if (spawn != null)
                {
                    Gizmos.DrawWireSphere(spawn.position, 0.5f);
                    Gizmos.DrawRay(spawn.position, spawn.forward * 2f);
                }
            }
        }

        // 출구 포인트 표시
        if (exitPoints != null)
        {
            Gizmos.color = Color.red;
            foreach (Transform exit in exitPoints)
            {
                if (exit != null)
                {
                    Gizmos.DrawWireSphere(exit.position, 0.5f);
                }
            }
        }

        // 카운터 위치 표시
        if (counterPosition != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(counterPosition.position, Vector3.one);

            // 카운터 대기열 위치 디버그 표시
            DrawCounterQueueDebug();
        }
    }

    private void DrawCounterQueueDebug()
    {
        if (counterPosition == null) return;

        // 대기열 위치들 (모든 고객이 카운터 뒤에 일렬로) - NavMesh 유효 위치 표시
        for (int i = 0; i < 5; i++) // 최대 5명까지 미리보기
        {
            Vector3 queuePos = CalculateValidQueuePosition(i);

            // 현재 예약된 고객이 있으면 다른 색상
            bool hasCustomer = i < counterQueueCustomers.Count && counterQueueCustomers[i] != null;

            if (i == 0)
            {
                // 첫 번째 고객 (가장 앞) - 노란색
                Gizmos.color = hasCustomer ? Color.yellow : Color.gray;
                Gizmos.DrawWireSphere(queuePos, 0.8f);
                Gizmos.DrawCube(queuePos, Vector3.one * 0.3f);
            }
            else
            {
                // 나머지 고객들
                Gizmos.color = hasCustomer ? Color.cyan : Color.gray;
                Gizmos.DrawWireSphere(queuePos, 0.6f);
            }

            #if UNITY_EDITOR
            string label = hasCustomer ? $"Queue[{i}] - 예약됨" : $"Queue[{i}] - 빈자리";
            UnityEditor.Handles.Label(queuePos + Vector3.up * 1.5f, label);
            #endif
        }

        // 예약된 대기열 연결선 그리기
        if (counterQueueCustomers.Count > 1)
        {
            Gizmos.color = Color.magenta;

            for (int i = 0; i < counterQueueCustomers.Count - 1 && i < 4; i++)
            {
                Vector3 currentPos = counterPosition.position + counterPosition.TransformDirection(queueDirection) * ((i + 1) * queueSpacing);
                Vector3 nextPos = counterPosition.position + counterPosition.TransformDirection(queueDirection) * ((i + 2) * queueSpacing);
                Gizmos.DrawLine(currentPos, nextPos);
            }
        }

        // 카운터 방향 표시
        Gizmos.color = Color.white;
        Gizmos.DrawRay(counterPosition.position, counterPosition.TransformDirection(Vector3.forward) * 2f);
        Gizmos.DrawRay(counterPosition.position, counterPosition.TransformDirection(queueDirection) * 6f);

        #if UNITY_EDITOR
        UnityEditor.Handles.Label(counterPosition.position + Vector3.up * 2f, $"대기열 고객 수: {counterQueueCustomers.Count}");
        UnityEditor.Handles.Label(counterPosition.position + Vector3.up * 2.5f, "도착 순서 기반 대기열 시스템");
        #endif
    }
}