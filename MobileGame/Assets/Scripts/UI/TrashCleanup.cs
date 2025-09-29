using UnityEngine;

public class TrashCleanup : MonoBehaviour
{
    private GameObject cleanVFXPrefab;
    private float detectionRadius = 2f;
    private bool isPlayerNearby = false;

    public void Initialize(GameObject vfxPrefab, float radius)
    {
        cleanVFXPrefab = vfxPrefab;
        detectionRadius = radius;
    }

    private void Update()
    {
        CheckPlayerDistance();
    }

    private void CheckPlayerDistance()
    {
        // 플레이어 태그로 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance <= detectionRadius && !isPlayerNearby)
            {
                isPlayerNearby = true;
                CleanTrash();
            }
        }
    }

    private void CleanTrash()
    {
        Debug.Log("[TrashCleanup] 쓰레기 청소 시작!");

        // 쓰레기 청소 사운드
        SoundManager.GameSounds.PlayTrashClean();

        // VFX 재생
        if (cleanVFXPrefab != null)
        {
            GameObject vfxInstance = Instantiate(cleanVFXPrefab, transform.position, transform.rotation);

            // VFX 지속시간 후 제거
            ParticleSystem ps = vfxInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(vfxInstance, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(vfxInstance, 3f);
            }

            Debug.Log("[TrashCleanup] 청소 VFX 재생됨");
        }

        // 쓰레기 인스턴스 제거
        Customer.CurrentTrashInstance = null;
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // 감지 반경 시각화
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}