using System.Collections.Generic;
using UnityEngine;
using System;

public class Upgrade : MonoBehaviour
{
    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("Upgrade Settings")]
    [SerializeField] private bool activateOnce = true; // 한 번만 활성화할지
    [SerializeField] private bool requireMoney = false; // 돈이 필요한지
    [SerializeField] private int upgradeCost = 100; // 업그레이드 비용
    [SerializeField] private bool autoFindMoneyManager = true;
    [SerializeField] private MoneyManager specificMoneyManager; // 특정 MoneyManager 직접 설정
    [SerializeField] private string moneyManagerTag = ""; // MoneyManager 태그로 찾기
    [SerializeField] private string moneyManagerName = ""; // MoneyManager 이름으로 찾기

    [Header("Child Activation")]
    [SerializeField] private bool activateAllChildren = false; // 모든 자식 활성화 여부
    [SerializeField] private List<GameObject> specificChildren = new List<GameObject>(); // 특정 자식들만 활성화

    [Header("Child Deactivation")]
    [SerializeField] private List<GameObject> deactivateChildren = new List<GameObject>(); // 비활성화할 객체들

    [Header("Chair Upgrade")]
    [SerializeField] private string chairPrefabName = "Chair"; // Chair 프리팹 이름

    // Chair 활성화 시 실행될 델리게이트
    public static event Action<Transform> OnChairActivated;

    // 3번째 고객이 돈을 두기 위한 MoneyManager 위치 제공
    public static Transform GetThirdCustomerMoneyPosition()
    {
        // Chair를 활성화한 Upgrade 컴포넌트 찾기
        Upgrade[] upgrades = FindObjectsOfType<Upgrade>();
        foreach (Upgrade upgrade in upgrades)
        {
            if (upgrade.hasBeenActivated && upgrade.moneyManager != null)
            {
                return upgrade.moneyManager.transform;
            }
        }
        return null;
    }


    private bool hasBeenActivated = false;
    private MoneyManager moneyManager;

    private void Start()
    {
        InitializeUpgrade();
    }

    private void InitializeUpgrade()
    {
        // MoneyManager 찾기
        if (requireMoney)
        {
            // 직접 설정된 MoneyManager가 있으면 우선 사용
            if (specificMoneyManager != null)
            {
                moneyManager = specificMoneyManager;
            }
            // 자동 찾기가 활성화되어 있고 직접 설정된 것이 없으면 찾기
            else if (autoFindMoneyManager)
            {
                moneyManager = FindMoneyManagerByCondition();
                if (moneyManager == null)
                {
                    Debug.LogWarning("[Upgrade] MoneyManager를 찾을 수 없습니다!");
                }
            }
            else
            {
                Debug.LogWarning("[Upgrade] MoneyManager가 설정되지 않았습니다!");
            }
        }

        // 초기 상태 확인
        ValidateChildren();

    }


    private void ValidateChildren()
    {
        if (activateAllChildren)
        {
        }
        else
        {
            // null 제거
            specificChildren.RemoveAll(child => child == null);
        }

        // 비활성화 리스트에서도 null 제거
        deactivateChildren.RemoveAll(child => child == null);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Upgrade {name}] 콜라이더 감지: {other.name} (태그: {other.tag})");

        if (other.CompareTag(playerTag))
        {
            Debug.Log($"[Upgrade {name}] 플레이어 감지됨 - 업그레이드 시도");
            TryActivateUpgrade();
        }
        else
        {
            Debug.Log($"[Upgrade {name}] 플레이어가 아님 - 무시 (필요한 태그: {playerTag})");
        }
    }

    private void TryActivateUpgrade()
    {
        Debug.Log($"[Upgrade {name}] TryActivateUpgrade 시작");

        // 이미 활성화되었고 한 번만 활성화하는 경우
        if (hasBeenActivated && activateOnce)
        {
            Debug.Log($"[Upgrade {name}] 이미 활성화됨 - 무시");
            return;
        }

        // 돈이 필요한 경우 체크
        if (requireMoney)
        {
            Debug.Log($"[Upgrade {name}] 돈 필요 - 비용: {upgradeCost}");

            if (moneyManager == null)
            {
                Debug.LogWarning($"[Upgrade {name}] MoneyManager가 없어서 업그레이드 불가");
                return;
            }

            Debug.Log($"[Upgrade {name}] 현재 보유 돈: {moneyManager.GetPlayerTotalMoney()}");

            if (!moneyManager.SpendPlayerMoney(upgradeCost))
            {
                Debug.Log($"[Upgrade {name}] 돈 부족 - 업그레이드 취소");
                return;
            }

            Debug.Log($"[Upgrade {name}] 돈 차감 완료");
        }

        // 업그레이드 실행
        Debug.Log($"[Upgrade {name}] 업그레이드 실행 시작");
        DeactivateChildren();
        ActivateChildren();
        hasBeenActivated = true;
        Debug.Log($"[Upgrade {name}] 업그레이드 완료!");

    }

    private void DeactivateChildren()
    {
        foreach (GameObject child in deactivateChildren)
        {
            if (child != null && child.activeInHierarchy)
            {
                child.SetActive(false);
            }
        }
    }

    private void ActivateChildren()
    {
        if (activateAllChildren)
        {
            // 모든 직계 자식 활성화
            ActivateAllDirectChildren();
        }
        else
        {
            // 특정 자식들만 활성화
            ActivateSpecificChildren();
        }
    }

    private void ActivateAllDirectChildren()
    {
        int activatedCount = 0;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.gameObject != null && !child.gameObject.activeInHierarchy)
            {
                child.gameObject.SetActive(true);
                activatedCount++;
            }
        }

    }

    private void ActivateSpecificChildren()
    {
        int activatedCount = 0;

        foreach (GameObject child in specificChildren)
        {
            if (child != null)
            {
                GameObject activatedObject = null;

                // 프리팹인 경우 인스턴스화
                if (child.scene.name == null || child.scene.name == "")
                {
                    activatedObject = Instantiate(child, transform);
                    activatedObject.SetActive(true);
                    activatedCount++;
                }
                // 씬의 비활성화된 객체인 경우
                else if (!child.activeInHierarchy)
                {
                    child.SetActive(true);
                    activatedObject = child;
                    activatedCount++;
                }

                // Chair 프리팹이 활성화된 경우 델리게이트 호출
                if (activatedObject != null && activatedObject.name.Contains(chairPrefabName))
                {
                    OnChairActivated?.Invoke(activatedObject.transform);
                }
            }
        }

    }

    // 수동 업그레이드 실행 (외부에서 호출 가능)
    public void ManualActivateUpgrade()
    {
        TryActivateUpgrade();
    }

    // 업그레이드 상태 초기화 (테스트용)
    public void ResetUpgrade()
    {
        hasBeenActivated = false;
    }

    // 특정 자식 추가/제거
    public void AddSpecificChild(GameObject child)
    {
        if (child != null && !specificChildren.Contains(child))
        {
            specificChildren.Add(child);
        }
    }

    public void RemoveSpecificChild(GameObject child)
    {
        if (specificChildren.Contains(child))
        {
            specificChildren.Remove(child);
        }
    }

    // 비활성화 자식 추가/제거
    public void AddDeactivateChild(GameObject child)
    {
        if (child != null && !deactivateChildren.Contains(child))
        {
            deactivateChildren.Add(child);
        }
    }

    public void RemoveDeactivateChild(GameObject child)
    {
        if (deactivateChildren.Contains(child))
        {
            deactivateChildren.Remove(child);
        }
    }

    // Getter/Setter 메서드들
    public bool HasBeenActivated()
    {
        return hasBeenActivated;
    }

    public void SetUpgradeCost(int newCost)
    {
        upgradeCost = Mathf.Max(0, newCost);
    }

    public int GetUpgradeCost()
    {
        return upgradeCost;
    }

    public void SetRequireMoney(bool require)
    {
        requireMoney = require;
    }

    // 조건에 따라 MoneyManager 찾기
    private MoneyManager FindMoneyManagerByCondition()
    {
        // 1. 태그로 찾기
        if (!string.IsNullOrEmpty(moneyManagerTag))
        {
            GameObject taggedObject = GameObject.FindGameObjectWithTag(moneyManagerTag);
            if (taggedObject != null)
            {
                MoneyManager manager = taggedObject.GetComponent<MoneyManager>();
                if (manager != null)
                {
                    return manager;
                }
            }
        }

        // 2. 이름으로 찾기
        if (!string.IsNullOrEmpty(moneyManagerName))
        {
            GameObject namedObject = GameObject.Find(moneyManagerName);
            if (namedObject != null)
            {
                MoneyManager manager = namedObject.GetComponent<MoneyManager>();
                if (manager != null)
                {
                    return manager;
                }
            }
        }

        // 3. 기본: 첫 번째 MoneyManager 찾기
        return FindObjectOfType<MoneyManager>();
    }

}