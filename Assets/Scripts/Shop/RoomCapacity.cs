// RoomCapacity.cs
using System;
using UnityEngine;

public static class RoomCapacity
{
    private const string LevelKey = "Dalsu_RoomExpandLevel";
    public const int BaseCapacity = 3;   // 기본 수용량 (달수 3마리)

    public static event Action<int> CapacityChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents() => CapacityChanged = null;

    /// <summary>구매 완료한 최대 단계 (0 = 아무것도 안 샀음)</summary>
    public static int ExpandLevel => PlayerPrefs.GetInt(LevelKey, 0);

    /// <summary>방에 둘 수 있는 최대 달수 수</summary>
    public static int MaxDalsu => BaseCapacity + ExpandLevel;

    /// <summary>이미 산 단계인지</summary>
    public static bool IsPurchased(int step) => step <= ExpandLevel;

    /// <summary>지금 살 수 있는 단계인지 (바로 다음 단계만)</summary>
    public static bool IsNextStep(int step) => step == ExpandLevel + 1;

    public static void SetLevel(int step)
    {
        if (step <= ExpandLevel) return;
        PlayerPrefs.SetInt(LevelKey, step);
        PlayerPrefs.Save();
        CapacityChanged?.Invoke(MaxDalsu);
    }
}