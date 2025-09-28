using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    [Header("Money Spawn Settings")]
    [SerializeField] private GameObject moneyPrefab;
    [SerializeField] private Transform moneySpawnPoint; // 돈이 쌓일 기준 위치
    [SerializeField] private bool autoFindSpawnPoint = true;

    [Header("Grid Settings")]
    [SerializeField] private int gridSize = 3; // 3x3 그리드
    [SerializeField] private float moneySpacing = 0.2f; // 돈 사이 간격
    [SerializeField] private float layerHeight = 0.1f; // 층 높이

    [Header("Animation Settings")]
    [SerializeField] private float spawnAnimationDuration = 1f;
    [SerializeField] private AnimationCurve spawnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Player Collection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float collectionRadius = 2f;
    [SerializeField] private int moneyValuePerObject = 10;

    [Header("UI")]
    [SerializeField] private TMPro.TextMeshProUGUI moneyText;
    [SerializeField] private bool autoFindMoneyText = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // 돈 관리
    private List<GameObject> spawnedMoney = new List<GameObject>();
    private bool[,,] gridOccupied; // [layer][x][z] 그리드 점유 상태
    private int maxLayers = 10; // 최대 층수
    private int totalMoney = 0; // 플레이어가 획득한 총 돈
    private SphereCollider collectionCollider;

    private void Start()
    {
        InitializeMoneyManager();
    }

    private void InitializeMoneyManager()
    {
        // 그리드 점유 상태 배열 초기화
        gridOccupied = new bool[maxLayers, gridSize, gridSize];

        // 자동으로 스폰 포인트 찾기
        if (autoFindSpawnPoint && moneySpawnPoint == null)
        {
            GameObject spawnPointObj = GameObject.FindGameObjectWithTag("MoneySpawn");
            if (spawnPointObj != null)
            {
                moneySpawnPoint = spawnPointObj.transform;
                DebugLog("[MoneyManager] MoneySpawn 태그로 스폰 포인트 자동 설정");
            }
            else
            {
                // 스폰 포인트가 없으면 기본 위치에 생성
                CreateDefaultSpawnPoint();
            }
        }

        ValidateSetup();
        SetupCollectionCollider();
        SetupMoneyText();
    }

    private void SetupCollectionCollider()
    {
        // 기존 콜라이더가 있는지 확인
        collectionCollider = GetComponent<SphereCollider>();

        // 없으면 새로 생성
        if (collectionCollider == null)
        {
            collectionCollider = gameObject.AddComponent<SphereCollider>();
            DebugLog("[MoneyManager] 수집용 SphereCollider 자동 생성");
        }

        // 콜라이더 설정
        collectionCollider.isTrigger = true;
        collectionCollider.radius = collectionRadius;

        // MoneySpawnPoint가 있으면 그 위치를 중심으로 콜라이더 배치
        if (moneySpawnPoint != null)
        {
            collectionCollider.center = transform.InverseTransformPoint(moneySpawnPoint.position);
        }

        DebugLog("[MoneyManager] 플레이어 감지용 콜라이더 설정 완료");
    }

    private void SetupMoneyText()
    {
        // 자동으로 Money Text UI 찾기
        if (autoFindMoneyText && moneyText == null)
        {
            // "MoneyText" 이름을 가진 TextMeshProUGUI 찾기
            GameObject moneyTextObj = GameObject.Find("MoneyText");
            if (moneyTextObj != null)
            {
                moneyText = moneyTextObj.GetComponent<TMPro.TextMeshProUGUI>();
                if (moneyText != null)
                {
                    DebugLog("[MoneyManager] MoneyText UI 자동 연결 완료");
                }
            }

            // 찾지 못했으면 태그로 시도
            if (moneyText == null)
            {
                GameObject taggedObj = GameObject.FindGameObjectWithTag("MoneyUI");
                if (taggedObj != null)
                {
                    moneyText = taggedObj.GetComponent<TMPro.TextMeshProUGUI>();
                    if (moneyText != null)
                    {
                        DebugLog("[MoneyManager] MoneyUI 태그로 MoneyText 자동 연결 완료");
                    }
                }
            }

            // 여전히 찾지 못했으면 첫 번째 TextMeshProUGUI 사용
            if (moneyText == null)
            {
                TMPro.TextMeshProUGUI[] allTexts = FindObjectsOfType<TMPro.TextMeshProUGUI>();
                if (allTexts.Length > 0)
                {
                    moneyText = allTexts[0];
                    DebugLog($"[MoneyManager] 첫 번째 TextMeshProUGUI({moneyText.name})를 MoneyText로 사용");
                }
            }
        }

        // 초기 텍스트 업데이트
        UpdateMoneyText();
    }

    private void CreateDefaultSpawnPoint()
    {
        GameObject spawnPointObj = new GameObject("MoneySpawnPoint");
        spawnPointObj.transform.SetParent(transform);
        spawnPointObj.transform.localPosition = Vector3.zero;
        spawnPointObj.tag = "MoneySpawn";

        moneySpawnPoint = spawnPointObj.transform;
        DebugLog("[MoneyManager] 기본 돈 스폰 포인트 생성");
    }

    private void ValidateSetup()
    {
        if (moneyPrefab == null)
        {
            Debug.LogWarning("[MoneyManager] Money Prefab이 설정되지 않았습니다!");
        }

        if (moneySpawnPoint == null)
        {
            Debug.LogWarning("[MoneyManager] Money Spawn Point가 설정되지 않았습니다!");
        }
    }

    // 빵 개수에 따라 돈 스폰 (Customer에서 호출)
    public void SpawnMoneyForBread(int breadCount)
    {
        if (breadCount <= 0) return;

        DebugLog($"[MoneyManager] {breadCount}개 빵에 대한 돈 스폰 시작");

        for (int i = 0; i < breadCount; i++)
        {
            StartCoroutine(SpawnSingleMoney(i * 0.2f)); // 0.2초 간격으로 스폰
        }
    }

    // 단일 돈 객체 스폰
    private IEnumerator SpawnSingleMoney(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (moneyPrefab == null || moneySpawnPoint == null)
        {
            DebugLog("[MoneyManager] Prefab 또는 SpawnPoint가 없어서 돈 스폰 실패");
            yield break;
        }

        // 다음 사용 가능한 그리드 위치 찾기
        Vector3 gridPosition = FindNextAvailableGridPosition(out int layer, out int x, out int z);

        if (gridPosition == Vector3.zero)
        {
            DebugLog("[MoneyManager] 사용 가능한 그리드 위치가 없어서 돈 스폰 취소");
            yield break;
        }

        // 돈 객체 생성 (위에서 떨어뜨리기 위해 위쪽에서 시작, Y축으로 90도 회전)
        Quaternion rotation = Quaternion.Euler(0, 90, 0);
        GameObject moneyObj = Instantiate(moneyPrefab, moneySpawnPoint);

        // 자식으로 설정하고 로컬 좌표계로 위치 설정
        moneyObj.transform.SetParent(moneySpawnPoint, false);
        moneyObj.transform.localPosition = Vector3.up * 5f; // 로컬 좌표에서 위로 5f
        moneyObj.transform.localRotation = rotation;
        Money moneyScript = moneyObj.GetComponent<Money>();

        if (moneyScript == null)
        {
            moneyScript = moneyObj.AddComponent<Money>();
        }

        // 돈 초기화
        moneyScript.Initialize();
        spawnedMoney.Add(moneyObj);

        // 그리드 위치 점유 표시
        OccupyGridPosition(layer, x, z);

        // 돈 객체에 그리드 정보 저장 (나중에 제거할 때 필요)
        MoneyGridInfo gridInfo = moneyObj.AddComponent<MoneyGridInfo>();
        gridInfo.SetGridPosition(layer, x, z);

        // 그리드 위치를 로컬 좌표로 변환
        Vector3 localGridPosition = moneySpawnPoint.InverseTransformPoint(gridPosition);

        // 그리드 위치로 애니메이션
        yield return StartCoroutine(AnimateMoneyToGrid(moneyObj, localGridPosition));

        DebugLog($"[MoneyManager] 돈 스폰 완료. 총 돈 개수: {spawnedMoney.Count}");
    }

    // 플레이어 감지 시 돈 수집
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            DebugLog($"[MoneyManager] 플레이어 감지 - 모든 돈 수집 시작");
            CollectAllMoneyByPlayer();
        }
    }

    // 플레이어가 모든 돈 수집
    private void CollectAllMoneyByPlayer()
    {
        int collectedCount = spawnedMoney.Count;

        if (collectedCount <= 0)
        {
            DebugLog("[MoneyManager] 수집할 돈이 없습니다");
            return;
        }

        DebugLog($"[MoneyManager] {collectedCount}개의 돈 수집 시작");

        // 각 돈 객체마다 moneyValuePerObject 만큼 추가
        int earnedMoney = collectedCount * moneyValuePerObject;
        totalMoney += earnedMoney;

        // 모든 돈 객체 파괴
        foreach (GameObject money in spawnedMoney)
        {
            if (money != null)
            {
                // 그리드 정보 가져와서 해당 위치 해제
                MoneyGridInfo gridInfo = money.GetComponent<MoneyGridInfo>();
                if (gridInfo != null)
                {
                    FreeGridPosition(gridInfo.GetLayer(), gridInfo.GetX(), gridInfo.GetZ());
                }

                Destroy(money);
            }
        }

        spawnedMoney.Clear();

        DebugLog($"[MoneyManager] 돈 수집 완료! 획득: {earnedMoney}, 총 보유: {totalMoney}");

        // UI 텍스트 업데이트
        UpdateMoneyText();

        // TODO: 여기서 게임 매니저에 돈 전달
        // GameManager.Instance.AddMoney(earnedMoney);
    }

    // 돈 텍스트 UI 업데이트
    private void UpdateMoneyText()
    {
        if (moneyText != null)
        {
            moneyText.text = totalMoney.ToString();
            DebugLog($"[MoneyManager] UI 텍스트 업데이트: {totalMoney}");
        }
        else
        {
            DebugLog("[MoneyManager] MoneyText UI가 설정되지 않았습니다");
        }
    }

    // 다음 사용 가능한 그리드 위치 찾기
    private Vector3 FindNextAvailableGridPosition(out int layer, out int x, out int z)
    {
        if (moneySpawnPoint == null)
        {
            layer = x = z = 0;
            return Vector3.zero;
        }

        // 0층부터 시작해서 빈 자리 찾기
        for (int currentLayer = 0; currentLayer < maxLayers; currentLayer++)
        {
            for (int currentZ = 0; currentZ < gridSize; currentZ++)
            {
                for (int currentX = 0; currentX < gridSize; currentX++)
                {
                    if (!gridOccupied[currentLayer, currentX, currentZ])
                    {
                        // 빈 자리 발견
                        layer = currentLayer;
                        x = currentX;
                        z = currentZ;

                        return CalculateGridPosition(currentLayer, currentX, currentZ);
                    }
                }
            }
        }

        // 모든 자리가 찾! (이론적으로는 발생하지 않아야 함)
        DebugLog("[MoneyManager] 경고: 모든 그리드 위치가 점유됨!");
        layer = x = z = 0;
        return moneySpawnPoint.position;
    }

    // 특정 그리드 좌표의 월드 위치 계산
    private Vector3 CalculateGridPosition(int layer, int x, int z)
    {
        if (moneySpawnPoint == null) return Vector3.zero;

        // 그리드 오프셋 계산 (중앙 정렬)
        float offsetX = (gridSize - 1) * moneySpacing * 0.5f;
        float offsetZ = (gridSize - 1) * moneySpacing * 0.5f;

        Vector3 gridPos = moneySpawnPoint.position;
        gridPos.x += (x * moneySpacing) - offsetX;
        gridPos.z += (z * moneySpacing) - offsetZ;
        gridPos.y += layer * layerHeight;

        return gridPos;
    }

    // 그리드 위치 점유 표시
    private void OccupyGridPosition(int layer, int x, int z)
    {
        if (layer < maxLayers && x < gridSize && z < gridSize)
        {
            gridOccupied[layer, x, z] = true;
            DebugLog($"[MoneyManager] 그리드 위치 점유: 층{layer} ({x},{z})");
        }
    }

    // 그리드 위치 해제
    private void FreeGridPosition(int layer, int x, int z)
    {
        if (layer < maxLayers && x < gridSize && z < gridSize)
        {
            gridOccupied[layer, x, z] = false;
            DebugLog($"[MoneyManager] 그리드 위치 해제: 층{layer} ({x},{z})");
        }
    }

    // 돈을 그리드 위치로 이동시키는 애니메이션 (로컬 좌표계)
    private IEnumerator AnimateMoneyToGrid(GameObject money, Vector3 targetLocalPosition)
    {
        if (money == null) yield break;

        Vector3 startLocalPosition = money.transform.localPosition;
        float elapsedTime = 0f;

        while (elapsedTime < spawnAnimationDuration)
        {
            if (money == null) yield break;

            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / spawnAnimationDuration;
            float curveValue = spawnCurve.Evaluate(progress);

            money.transform.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, curveValue);

            yield return null;
        }

        if (money != null)
        {
            money.transform.localPosition = targetLocalPosition;
        }
    }

    // 돈 수집 (외부에서 직접 호출용 - 레거시)
    public int CollectAllMoney()
    {
        int collectedCount = spawnedMoney.Count;

        DebugLog($"[MoneyManager] 외부 호출 - 모든 돈 수집: {collectedCount}개");

        foreach (GameObject money in spawnedMoney)
        {
            if (money != null)
            {
                Money moneyScript = money.GetComponent<Money>();
                if (moneyScript != null)
                {
                    moneyScript.CollectMoney();
                }
                else
                {
                    Destroy(money);
                }
            }
        }

        spawnedMoney.Clear();
        ResetGrid();

        return collectedCount;
    }

    // 특정 돈 객체 제거 (Money 스크립트에서 호출)
    public void RemoveMoney(GameObject money)
    {
        if (spawnedMoney.Contains(money))
        {
            // 그리드 정보 가져와서 해당 위치 해제
            MoneyGridInfo gridInfo = money.GetComponent<MoneyGridInfo>();
            if (gridInfo != null)
            {
                FreeGridPosition(gridInfo.GetLayer(), gridInfo.GetX(), gridInfo.GetZ());
            }

            spawnedMoney.Remove(money);
            DebugLog($"[MoneyManager] 돈 제거. 남은 돈: {spawnedMoney.Count}개");
        }
    }

    // 그리드 초기화
    private void ResetGrid()
    {
        // 모든 그리드 위치 해제
        for (int layer = 0; layer < maxLayers; layer++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    gridOccupied[layer, x, z] = false;
                }
            }
        }
        DebugLog("[MoneyManager] 모든 그리드 위치 초기화");
    }

    // Getter 메서드들
    public int GetTotalMoneyCount()
    {
        return spawnedMoney.Count;
    }

    public int GetPlayerTotalMoney()
    {
        return totalMoney;
    }

    public Vector3 GetSpawnPoint()
    {
        return moneySpawnPoint != null ? moneySpawnPoint.position : Vector3.zero;
    }

    // Setter 메서드들
    public void SetGridSize(int newSize)
    {
        gridSize = Mathf.Max(1, newSize);
    }

    public void SetMoneySpacing(float newSpacing)
    {
        moneySpacing = Mathf.Max(0.1f, newSpacing);
    }

    public void SetLayerHeight(float newHeight)
    {
        layerHeight = Mathf.Max(0.05f, newHeight);
    }

    public void SetCollectionRadius(float newRadius)
    {
        collectionRadius = Mathf.Max(0.5f, newRadius);
        if (collectionCollider != null)
        {
            collectionCollider.radius = collectionRadius;
        }
    }

    public void SetMoneyValuePerObject(int newValue)
    {
        moneyValuePerObject = Mathf.Max(1, newValue);
    }

    // 플레이어 돈 추가/차감 (외부에서 사용)
    public void AddPlayerMoney(int amount)
    {
        totalMoney += amount;
        UpdateMoneyText();
        DebugLog($"[MoneyManager] 플레이어 돈 추가: +{amount}, 총 보유: {totalMoney}");
    }

    public bool SpendPlayerMoney(int amount)
    {
        if (totalMoney >= amount)
        {
            totalMoney -= amount;
            UpdateMoneyText();
            DebugLog($"[MoneyManager] 플레이어 돈 차감: -{amount}, 총 보유: {totalMoney}");
            return true;
        }
        else
        {
            DebugLog($"[MoneyManager] 돈 부족! 필요: {amount}, 보유: {totalMoney}");
            return false;
        }
    }

    // 외부에서 돈 텍스트 수동 업데이트 (필요시)
    public void RefreshMoneyText()
    {
        UpdateMoneyText();
    }

    // MoneyText UI 설정 (외부에서 직접 설정)
    public void SetMoneyText(TMPro.TextMeshProUGUI newMoneyText)
    {
        moneyText = newMoneyText;
        UpdateMoneyText();
        DebugLog("[MoneyManager] MoneyText UI 수동 설정 완료");
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
        if (moneySpawnPoint == null) return;

        // 스폰 포인트 표시
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(moneySpawnPoint.position, 0.5f);

        // 플레이어 감지 범위 표시
        if (collectionCollider != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 colliderCenter = transform.position + collectionCollider.center;
            Gizmos.DrawWireSphere(colliderCenter, collectionRadius);
        }

        // 3x3 그리드 표시
        float offsetX = (gridSize - 1) * moneySpacing * 0.5f;
        float offsetZ = (gridSize - 1) * moneySpacing * 0.5f;

        for (int layer = 0; layer <= 2; layer++) // 최대 3층까지 미리보기
        {
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    Vector3 gridPos = CalculateGridPosition(layer, x, z);

                    // 점유 상태에 따라 색상 변경
                    bool isOccupied = gridOccupied != null && layer < maxLayers && gridOccupied[layer, x, z];

                    if (isOccupied)
                    {
                        Gizmos.color = Color.red; // 점유된 위치
                        Gizmos.DrawWireCube(gridPos, Vector3.one * 0.1f);
                    }
                    else
                    {
                        Gizmos.color = Color.yellow; // 빈 위치
                        Gizmos.DrawWireCube(gridPos, Vector3.one * 0.05f);
                    }
                }
            }
        }

        #if UNITY_EDITOR
        int occupiedCount = 0;
        if (gridOccupied != null)
        {
            for (int layer = 0; layer < maxLayers; layer++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    for (int z = 0; z < gridSize; z++)
                    {
                        if (gridOccupied[layer, x, z]) occupiedCount++;
                    }
                }
            }
        }

        string info = $"현재 돈 개수: {spawnedMoney.Count}\n점유된 그리드: {occupiedCount}\n최대 수용량: {gridSize * gridSize * maxLayers}\n플레이어 보유 돈: {totalMoney}";
        UnityEditor.Handles.Label(moneySpawnPoint.position + Vector3.up * 2f, info);
        #endif
    }
}