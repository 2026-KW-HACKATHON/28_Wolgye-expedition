// CharacterInventory.cs
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class CharacterSave
{
    public List<string> owned = new List<string>();
}

/// <summary>상점에서 구매한 캐릭터 장비(모자/피부색)를 character_data.json에 저장한다. 장착은 EquipmentManager가 담당.</summary>
public static class CharacterInventory
{
    private const string FileName = "character_data.json";

    public static event Action InventoryChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents() => InventoryChanged = null;

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    private static CharacterSave Load()
    {
        CharacterSave data = null;
        if (File.Exists(FilePath))
            data = JsonConvert.DeserializeObject<CharacterSave>(File.ReadAllText(FilePath));

        if (data == null) data = new CharacterSave();
        if (data.owned == null) data.owned = new List<string>();
        return data;
    }

    public static bool Has(string id) => Load().owned.Contains(id);

    public static bool Add(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        CharacterSave data = Load();
        if (data.owned.Contains(id)) return false;

        data.owned.Add(id);
        File.WriteAllText(FilePath, JsonConvert.SerializeObject(data, Formatting.Indented));
        InventoryChanged?.Invoke();
        return true;
    }
}