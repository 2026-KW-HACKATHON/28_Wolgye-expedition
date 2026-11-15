using System;
using UnityEngine;

[Serializable]
public class SettingsData
{
    public int version = 1;

    public AudioSettingsData audio = new();
    public GraphicsSettingsData graphics = new();
}

[Serializable]
public class AudioSettingsData
{
    [Range(0f, 1f)]
    public float bgmVolume = 1f;

    [Range(0f, 1f)]
    public float sfxVolume = 1f;
}

[Serializable]
public class GraphicsSettingsData
{
    // QualitySettings의 품질 단계 인덱스
    public int qualityLevel = 0;
}