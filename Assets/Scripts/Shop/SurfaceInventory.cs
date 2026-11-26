// SurfaceInventory.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public static class SurfaceInventory
{
    private const string OwnedKey = "Dalsu_OwnedSurfaces";

    public static event Action InventoryChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents() => InventoryChanged = null;

    private static HashSet<string> Load()
    {
        string raw = PlayerPrefs.GetString(OwnedKey, "");
        return string.IsNullOrEmpty(raw) ? new HashSet<string>() : new HashSet<string>(raw.Split(','));
    }

    public static bool Has(string key) => Load().Contains(key);

    public static bool Add(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;

        HashSet<string> owned = Load();
        if (!owned.Add(key)) return false;

        PlayerPrefs.SetString(OwnedKey, string.Join(",", owned));
        PlayerPrefs.Save();
        InventoryChanged?.Invoke();
        return true;
    }
}