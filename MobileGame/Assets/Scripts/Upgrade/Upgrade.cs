using System.Collections.Generic;
using UnityEngine;

public class Upgrade : MonoBehaviour
{
    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("Upgrade Settings")]
    [SerializeField] private bool activateOnce = true; // 한 번만 활성화할지
    [SerializeField] private bool requireMoney = false; // 돈이 필요한지
    [SerializeField] private int upgradeCost = 100; // 업그레이드 비용
    [SerializeField] private bool autoFindMoneyManager = true;

    [Header("Child Activation")]
    [SerializeField] private bool activateAllChildren = false; // 모든 자식 활성화 여부
    [SerializeField] private List<GameObject> specificChildren = new List<GameObject>(); // 특정 자식들만 활성화

    [Header("Child Deactivation")]
    [SerializeField] private List<GameObject> deactivateChildren = new List<GameObject>(); // 비활성화할 객체들


    private bool hasBeenActivated = false;
    private MoneyManager moneyManager;

    private void Start()
    {
        InitializeUpgrade();
    }

    private void InitializeUpgrade()
    {
        // MoneyManager 찾기
        if (autoFindMoneyManager && requireMoney)
        {
            moneyManager = FindObjectOfType<MoneyManager>();
            if (moneyManager == null)
            {
                Debug.LogWarning("[Upgrade] MoneyManager를 찾을 수 없습니다!");
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
        if (other.CompareTag(playerTag))
        {
            TryActivateUpgrade();
        }
    }

    private void TryActivateUpgrade()
    {
        // 이미 활성화되었고 한 번만 활성화하는 경우
        if (hasBeenActivated && activateOnce)
        {
            return;
        }

        // 돈이 필요한 경우 체크
        if (requireMoney)
        {
            if (moneyManager == null)
            {
                return;
            }

            if (!moneyManager.SpendPlayerMoney(upgradeCost))
            {
                return;
            }

        }

        // 업그레이드 실행
        DeactivateChildren();
        ActivateChildren();
        hasBeenActivated = true;

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
                // 프리팹인 경우 인스턴스화
                if (child.scene.name == null || child.scene.name == "")
                {
                    GameObject instance = Instantiate(child, transform);
                    instance.SetActive(true);
                    activatedCount++;
                }
                // 씬의 비활성화된 객체인 경우
                else if (!child.activeInHierarchy)
                {
                    child.SetActive(true);
                    activatedCount++;
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

}