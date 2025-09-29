using UnityEngine;

[System.Serializable]
public class SFXClip
{
    public string name;
    public AudioClip clip;
}

public class SoundManager : MonoBehaviour
{
    [Header("SFX Settings")]
    [SerializeField] private SFXClip[] sfxClips;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float defaultVolume = 1f;

    public static SoundManager Instance { get; private set; }

    // SFX 타입 열거형
    public enum SFXType
    {
        CostMoney,      // Cost_Money.wav - 돈 지불/차감시
        GetObject,      // Get_Object.wav - 아이템 획득시
        PutObject,      // Put_Object.wav - 아이템 배치시
        Success,        // Success.wav - 성공/완료시
        Cash,           // cash.wav - 돈 관련 액션
        Trash           // trash.mp3 - 쓰레기 청소시
    }

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSoundManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSoundManager()
    {
        // AudioSource가 없다면 추가
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.volume = defaultVolume;

        Debug.Log("[SoundManager] 초기화 완료");
    }

    // SFX 재생 메서드
    public void PlaySFX(SFXType sfxType, float volume = -1f)
    {
        if (audioSource == null) return;

        string targetName = GetSFXName(sfxType);
        AudioClip clipToPlay = GetClipByName(targetName);

        if (clipToPlay != null)
        {
            float playVolume = volume > 0 ? volume : defaultVolume;
            audioSource.PlayOneShot(clipToPlay, playVolume);
            Debug.Log($"[SoundManager] SFX 재생: {targetName}");
        }
        else
        {
            Debug.LogWarning($"[SoundManager] SFX를 찾을 수 없음: {targetName}");
        }
    }

    // SFX 이름으로 직접 재생
    public void PlaySFXByName(string sfxName, float volume = -1f)
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = GetClipByName(sfxName);

        if (clipToPlay != null)
        {
            float playVolume = volume > 0 ? volume : defaultVolume;
            audioSource.PlayOneShot(clipToPlay, playVolume);
            Debug.Log($"[SoundManager] SFX 재생: {sfxName}");
        }
        else
        {
            Debug.LogWarning($"[SoundManager] SFX를 찾을 수 없음: {sfxName}");
        }
    }

    private string GetSFXName(SFXType sfxType)
    {
        switch (sfxType)
        {
            case SFXType.CostMoney: return "Cost_Money";
            case SFXType.GetObject: return "Get_Object";
            case SFXType.PutObject: return "Put_Object";
            case SFXType.Success: return "Success";
            case SFXType.Cash: return "cash";
            case SFXType.Trash: return "trash";
            default: return "";
        }
    }

    private AudioClip GetClipByName(string clipName)
    {
        foreach (SFXClip sfxClip in sfxClips)
        {
            if (sfxClip.name.Equals(clipName, System.StringComparison.OrdinalIgnoreCase))
            {
                return sfxClip.clip;
            }
        }
        return null;
    }

    // 볼륨 설정
    public void SetVolume(float volume)
    {
        defaultVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = defaultVolume;
        }
    }

    // 특정 사운드가 재생 중인지 확인
    public bool IsPlaying()
    {
        return audioSource != null && audioSource.isPlaying;
    }

    // 모든 사운드 정지
    public void StopAllSounds()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    // 게임 이벤트별 사운드 재생 메서드들
    public static class GameSounds
    {
        // 돈 관련
        public static void PlayMoneySpend() => Instance?.PlaySFX(SFXType.CostMoney);
        public static void PlayCashRegister() => Instance?.PlaySFX(SFXType.Cash);

        // 아이템 관련
        public static void PlayItemPickup() => Instance?.PlaySFX(SFXType.GetObject);
        public static void PlayItemPlace() => Instance?.PlaySFX(SFXType.PutObject);

        // 성공/완료
        public static void PlaySuccess() => Instance?.PlaySFX(SFXType.Success);

        // 청소
        public static void PlayTrashClean() => Instance?.PlaySFX(SFXType.Trash);
    }

    private void OnValidate()
    {
        // Inspector에서 수정시 자동 검증
        if (sfxClips != null)
        {
            foreach (SFXClip sfxClip in sfxClips)
            {
                if (sfxClip.clip == null && !string.IsNullOrEmpty(sfxClip.name))
                {
                    Debug.LogWarning($"[SoundManager] {sfxClip.name} 클립이 설정되지 않았습니다.");
                }
            }
        }
    }
}