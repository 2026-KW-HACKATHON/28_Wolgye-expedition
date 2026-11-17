using System;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    // 현재 설정 데이터
    public SettingsData CurrentSettings { get; private set; }

    // 설정 변경 이벤트
    public event Action<SettingsData> OnSettingsChanged;

    private void Awake()
    {
        // Singleton 처리
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    // 설정 초기화
    private void Initialize()
    {
        // 저장된 설정 불러오기
        CurrentSettings = SettingsRepository.Load();

        // 데이터 유효성 검사
        ValidateSettings();

        // 초기 설정 적용
        ApplyAllSettings();

        Debug.Log("[Settings] Initialized");
    }

    // 설정값 유효성 검사
    private void ValidateSettings()
    {
        CurrentSettings.audio.bgmVolume =
            Mathf.Clamp01(CurrentSettings.audio.bgmVolume);

        CurrentSettings.audio.sfxVolume =
            Mathf.Clamp01(CurrentSettings.audio.sfxVolume);

        int maxQualityLevel = QualitySettings.names.Length - 1;

        CurrentSettings.graphics.qualityLevel =
            Mathf.Clamp(
                CurrentSettings.graphics.qualityLevel,
                0,
                Mathf.Max(0, maxQualityLevel)
            );
    }

    // 전체 설정 적용
    private void ApplyAllSettings()
    {
        ApplyGraphicsSettings();

        OnSettingsChanged?.Invoke(CurrentSettings);
    }

    // 그래픽 설정 적용
    private void ApplyGraphicsSettings()
    {
        QualitySettings.SetQualityLevel(
            CurrentSettings.graphics.qualityLevel,
            true
        );
    }

    // BGM 볼륨 변경
    public void SetBgmVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (Mathf.Approximately(
            CurrentSettings.audio.bgmVolume, volume))
            return;

        CurrentSettings.audio.bgmVolume = volume;

        NotifySettingsChanged();
    }

    // 효과음 볼륨 변경
    public void SetSfxVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (Mathf.Approximately(
            CurrentSettings.audio.sfxVolume, volume))
            return;

        CurrentSettings.audio.sfxVolume = volume;

        NotifySettingsChanged();
    }

    // 그래픽 품질 변경
    public void SetQualityLevel(int level)
    {
        int maxLevel = QualitySettings.names.Length - 1;

        level = Mathf.Clamp(level, 0, Mathf.Max(0, maxLevel));

        if (CurrentSettings.graphics.qualityLevel == level)
            return;

        CurrentSettings.graphics.qualityLevel = level;

        ApplyGraphicsSettings();

        NotifySettingsChanged();
    }

    // 설정 변경 알림
    private void NotifySettingsChanged()
    {
        OnSettingsChanged?.Invoke(CurrentSettings);

        SaveSettings();
    }

    // 설정 저장
    public void SaveSettings()
    {
        SettingsRepository.Save(CurrentSettings);
    }

    // 기본 설정으로 초기화
    public void ResetSettings()
    {
        CurrentSettings = new SettingsData();

        ValidateSettings();

        ApplyAllSettings();

        SaveSettings();
    }

    // 앱이 백그라운드로 이동할 때 저장
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveSettings();
        }
    }

    // 앱 종료 시 저장
    private void OnApplicationQuit()
    {
        SaveSettings();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
