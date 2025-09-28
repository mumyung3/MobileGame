using UnityEngine;

public class BreadTrigger : MonoBehaviour
{
    private Bread breadScript;

    public void SetBread(Bread bread)
    {
        breadScript = bread;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (breadScript == null) return;

        // 플레이어와 충돌 시 빵 수집
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            breadScript.OnPlayerEnterTrigger(player);
        }
    }
}