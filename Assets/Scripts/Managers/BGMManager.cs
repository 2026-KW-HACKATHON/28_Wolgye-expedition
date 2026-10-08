using UnityEngine;

/// <summary>
/// 게임 전체 BGM. 씬이 바뀌어도 파괴되지 않고 계속 재생된다.
/// 여러 씬에 이 오브젝트를 놓아도 처음 생성된 하나만 남고 나머지는 스스로 삭제된다.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [SerializeField] private AudioClip bgmClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;

    private AudioSource audioSource;

    private void Awake()
    {
        // 이미 다른 씬에서 넘어온 BGMManager가 있으면 이 오브젝트는 삭제 (중복 재생 방지)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.clip = bgmClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D 사운드 (카메라 위치와 상관없이 같은 크기)
        audioSource.volume = volume;
        audioSource.Play();
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
        audioSource.volume = volume;
    }

    /// <summary>다른 곡으로 바꿀 때. 같은 곡이면 처음부터 다시 틀지 않고 그대로 둔다.</summary>
    public void Play(AudioClip clip)
    {
        if (clip == null || audioSource.clip == clip) return;

        audioSource.clip = clip;
        audioSource.Play();
    }
}