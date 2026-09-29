using UnityEngine;

public static class DalsuSaveManager
{
    private const string CollectedPrefix = "Dalsu_Collected_";
    private const string CountPrefix = "Dalsu_Count_";

    // 달수 획득
    public static void AcquireDalsu(DalsuData dalsu)
    {
        // 1. 도감 등록
        PlayerPrefs.SetInt(
            CollectedPrefix + dalsu.id,
            1
        );

        // 2. 현재 보유 개수 +1
        int currentCount = GetOwnedCount(dalsu);

        PlayerPrefs.SetInt(
            CountPrefix + dalsu.id,
            currentCount + 1
        );

        PlayerPrefs.Save();

        Debug.Log(
            dalsu.dalsuName +
            " 획득! 현재 " +
            (currentCount + 1) +
            "개 보유"
        );
    }

    // 한 번이라도 획득했는지
    public static bool IsCollected(DalsuData dalsu)
    {
        return PlayerPrefs.GetInt(
            CollectedPrefix + dalsu.id,
            0
        ) == 1;
    }

    // 현재 몇 개 보유 중인지
    public static int GetOwnedCount(DalsuData dalsu)
    {
        return PlayerPrefs.GetInt(
            CountPrefix + dalsu.id,
            0
        );
    }

    // 현재 보유 여부
    public static bool IsOwned(DalsuData dalsu)
    {
        return GetOwnedCount(dalsu) > 0;
    }

    // 달수 사용
    public static bool UseDalsu(DalsuData dalsu)
    {
        int count = GetOwnedCount(dalsu);

        if (count <= 0)
        {
            return false;
        }

        PlayerPrefs.SetInt(
            CountPrefix + dalsu.id,
            count - 1
        );

        PlayerPrefs.Save();

        return true;
    }
}