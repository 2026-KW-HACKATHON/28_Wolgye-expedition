
using UnityEngine;
using UnityEngine.Audio;

public class AudioSettingsApplier : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField]
    private AudioMixer audioMixer;

    private const string BGM_VOLUME = "BGMVolume";
    private const string SFX_VOLUME = "SFXVolume";

    private void Start()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("[AudioSettingsApplier] SettingsManager not found.");
            return;
        }

        // 설정 변경 이벤트 구독
        SettingsManager.Instance.OnSettingsChanged += ApplySettings;

        // 이미 로드되어 있는 설정값 최초 적용
        ApplySettings(SettingsManager.Instance.CurrentSettings);
    }

    private void OnDestroy()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged -= ApplySettings;
        }
    }

    private void ApplySettings(SettingsData settings)
    {
        ApplyVolume(BGM_VOLUME, settings.audio.bgmVolume);
        ApplyVolume(SFX_VOLUME, settings.audio.sfxVolume);
    }

    private void ApplyVolume(string parameter, float volume)
    {
        // 0~1 값을 dB로 변환
        float decibels = volume > 0.0001f
            ? Mathf.Log10(volume) * 20f
            : -80f;

        if (!audioMixer.SetFloat(parameter, decibels))
        {
            Debug.LogWarning(
                $"[AudioSettingsApplier] Failed to set {parameter}"
            );
        }
    }
}
