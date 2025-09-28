using UnityEngine;
using UnityEngine.Events;

public class CollectionManager : MonoBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private int totalBreadCount = 0;
    [SerializeField] private int maxBreadCapacity = 10;

    [Header("Events")]
    public UnityEvent<int> OnBreadCountChanged;
    public UnityEvent OnCapacityReached;


    // Singleton pattern
    public static CollectionManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 초기 UI 업데이트
        OnBreadCountChanged?.Invoke(totalBreadCount);
    }

    public void CollectBread(int amount = 1)
    {
        if (totalBreadCount >= maxBreadCapacity)
        {
            OnCapacityReached?.Invoke();
            return;
        }

        int oldCount = totalBreadCount;
        totalBreadCount = Mathf.Min(totalBreadCount + amount, maxBreadCapacity);
        int actualCollected = totalBreadCount - oldCount;

        // 이벤트 발동
        OnBreadCountChanged?.Invoke(totalBreadCount);

        // 용량 도달 시 이벤트
        if (totalBreadCount >= maxBreadCapacity)
        {
            OnCapacityReached?.Invoke();
        }
    }

    public bool CanCollectBread(int amount = 1)
    {
        return totalBreadCount + amount <= maxBreadCapacity;
    }

    public void UseBread(int amount)
    {
        if (amount <= 0) return;

        int oldCount = totalBreadCount;
        totalBreadCount = Mathf.Max(0, totalBreadCount - amount);
        int actualUsed = oldCount - totalBreadCount;

        OnBreadCountChanged?.Invoke(totalBreadCount);
    }

    public void SellAllBread()
    {
        int soldAmount = totalBreadCount;
        totalBreadCount = 0;

        OnBreadCountChanged?.Invoke(totalBreadCount);
    }

    public void SetMaxCapacity(int newCapacity)
    {
        maxBreadCapacity = Mathf.Max(0, newCapacity);

        // 현재 개수가 새 용량을 초과하면 조정
        if (totalBreadCount > maxBreadCapacity)
        {
            totalBreadCount = maxBreadCapacity;
            OnBreadCountChanged?.Invoke(totalBreadCount);
        }
    }

    // Getter 메서드들
    public int GetBreadCount()
    {
        return totalBreadCount;
    }

    public int GetMaxCapacity()
    {
        return maxBreadCapacity;
    }

    public float GetCapacityPercentage()
    {
        if (maxBreadCapacity <= 0) return 0f;
        return (float)totalBreadCount / maxBreadCapacity;
    }

    public bool IsCapacityFull()
    {
        return totalBreadCount >= maxBreadCapacity;
    }

    public int GetRemainingCapacity()
    {
        return Mathf.Max(0, maxBreadCapacity - totalBreadCount);
    }

    // 저장/로드 시스템 (PlayerPrefs 사용)
    public void SaveData()
    {
        PlayerPrefs.SetInt("BreadCount", totalBreadCount);
        PlayerPrefs.SetInt("BreadCapacity", maxBreadCapacity);
        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        totalBreadCount = PlayerPrefs.GetInt("BreadCount", 0);
        maxBreadCapacity = PlayerPrefs.GetInt("BreadCapacity", 10);

        OnBreadCountChanged?.Invoke(totalBreadCount);
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveData();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveData();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SaveData();
        }
    }
}