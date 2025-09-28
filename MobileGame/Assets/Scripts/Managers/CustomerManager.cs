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

    private List<Customer> activeCustomers = new List<Customer>();
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
        }
    }
}