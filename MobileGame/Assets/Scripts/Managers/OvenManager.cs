using System.Collections;
using UnityEngine;

public class OvenManager : MonoBehaviour
{
    [Header("Bread Spawning")]
    [SerializeField] private GameObject breadPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxBreadCount = 10;

    [Header("Spawn Settings")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;


    private int currentBreadCount = 0;
    private bool isSpawning = false;
    private Coroutine spawnCoroutine;

    private void Start()
    {
        ValidateReferences();

        if (autoStart)
        {
            StartSpawning();
        }
    }

    private void ValidateReferences()
    {
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }
    }

    public void StartSpawning()
    {
        if (isSpawning)
        {
            return;
        }

        isSpawning = true;
        spawnCoroutine = StartCoroutine(SpawnBreadRoutine());
    }

    public void StopSpawning()
    {
        if (!isSpawning)
        {
            return;
        }

        isSpawning = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnBreadRoutine()
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (CanSpawnBread())
            {
                SpawnBread();
            }
        }
    }

    private bool CanSpawnBread()
    {
        return breadPrefab != null && currentBreadCount < maxBreadCount;
    }

    private void SpawnBread()
    {
        Vector3 spawnPosition = spawnPoint.position + spawnOffset;
        GameObject newBread = Instantiate(breadPrefab, spawnPosition, spawnPoint.rotation);

        currentBreadCount++;

        // 빵에 OvenManager 참조 추가 (빵이 수집되면 카운트 감소용)
        Bread breadComponent = newBread.GetComponent<Bread>();
        if (breadComponent != null)
        {
            breadComponent.SetOvenManager(this);
        }
    }

    public void OnBreadCollected()
    {
        if (currentBreadCount > 0)
        {
            currentBreadCount--;
        }
    }

    public void SetSpawnInterval(float newInterval)
    {
        spawnInterval = Mathf.Max(0.1f, newInterval);
    }

    public void SetMaxBreadCount(int newMaxCount)
    {
        maxBreadCount = Mathf.Max(0, newMaxCount);
    }

    public int GetCurrentBreadCount()
    {
        return currentBreadCount;
    }

    public int GetMaxBreadCount()
    {
        return maxBreadCount;
    }

    public bool IsSpawning()
    {
        return isSpawning;
    }

    public float GetSpawnInterval()
    {
        return spawnInterval;
    }

    private void OnDisable()
    {
        StopSpawning();
    }

    private void OnDestroy()
    {
        StopSpawning();
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnPoint.position + spawnOffset, 0.3f);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(spawnPoint.position, spawnPoint.forward);
        }
    }
}