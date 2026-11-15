using UnityEngine;

public static class SettingsRepository
{
    private const string BGM_VOLUME_KEY = "Settings_BGMVolume";
    private const string SFX_VOLUME_KEY = "Settings_SFXVolume";
    private const string QUALITY_LEVEL_KEY = "Settings_QualityLevel";

    public static SettingsData Load()
    {
        SettingsData data = new SettingsData();

        data.audio.bgmVolume =
            PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);

        data.audio.sfxVolume =
            PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);

        data.graphics.qualityLevel =
            PlayerPrefs.GetInt(QUALITY_LEVEL_KEY, QualitySettings.GetQualityLevel());

        return data;
    }

    public static void Save(SettingsData data)
    {
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, data.audio.bgmVolume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, data.audio.sfxVolume);

        PlayerPrefs.SetInt(QUALITY_LEVEL_KEY, data.graphics.qualityLevel);

        PlayerPrefs.Save();
    }
}