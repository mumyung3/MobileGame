using UnityEngine;

public class BucketTrigger : MonoBehaviour
{
    private BucketManager bucketManager;

    public void SetBucketManager(BucketManager manager)
    {
        bucketManager = manager;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (bucketManager == null) return;

        // 플레이어와 충돌 시 빵 진열
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            bucketManager.OnPlayerEnterZone(player);
        }
    }
}