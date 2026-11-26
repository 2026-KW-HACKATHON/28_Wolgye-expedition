// FurnitureInventory.cs
using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>상점에서 구매한 가구를 furniture_data.json에 기록한다. (PlacementController와 같은 파일/형식)</summary>
public static class FurnitureInventory
{
    private const string FileName = "furniture_data.json";

    /// <summary>구매로 수량이 바뀔 때 호출</summary>
    public static event Action InventoryChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents() => InventoryChanged = null;

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static void Add(string id, int amount = 1)
    {
        if (string.IsNullOrEmpty(id) || amount <= 0) return;

        OwnFurnitures data = null;
        if (File.Exists(FilePath))
            data = JsonConvert.DeserializeObject<OwnFurnitures>(File.ReadAllText(FilePath));

        if (data == null) data = new OwnFurnitures();
        if (data.furnitures == null) data.furnitures = new System.Collections.Generic.List<OwnFurniture>();

        OwnFurniture target = data.furnitures.Find(f => f != null && f.id == id);
        if (target == null)
            data.furnitures.Add(new OwnFurniture { id = id, count = amount });
        else
            target.count += amount;

        File.WriteAllText(FilePath, JsonConvert.SerializeObject(data, Formatting.Indented));
        InventoryChanged?.Invoke();
    }
}