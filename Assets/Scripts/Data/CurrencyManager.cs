using System;
using UnityEngine;

public static class CurrencyManager
{
    private const string BalanceKey = "Dalsu_Currency";
    public static event Action<int> BalanceChanged;
    public static int Balance => PlayerPrefs.GetInt(BalanceKey, 0);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents() => BalanceChanged = null;

    public static void Add(int amount)
    {
        if (amount <= 0) return;
        int balance = checked(Balance + amount);
        PlayerPrefs.SetInt(BalanceKey, balance);
        PlayerPrefs.Save();
        BalanceChanged?.Invoke(balance);
    }
}
