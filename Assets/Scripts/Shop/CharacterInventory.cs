// CharacterInventory.cs
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class CharacterSave
{
    // 구매한 아이템 id 목록
    public List<string> owned = new List<string>();

    // 지금 장착 중인 아이템 id (없으면 null)
    public string equipped;
}

/// <summary>구매/장착한 캐릭터 아이템을 character_data.json에 저장한다.</summary>
public static class CharacterInventory
{
    private const string FileName = "character_data.json";

    public static event Action InventoryChanged;   // 구매 시
    public static event Action EquipChanged;       // 장착/해제 시

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents()
    {
        InventoryChanged = null;
        EquipChanged = null;
    }

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

    private static void Save(CharacterSave data)
    {
        File.WriteAllText(FilePath, JsonConvert.SerializeObject(data, Formatting.Indented));
    }

    public static bool Has(string id) => Load().owned.Contains(id);

    /// <summary>구매 처리. 이미 보유 중이면 false.</summary>
    public static bool Add(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        CharacterSave data = Load();
        if (data.owned.Contains(id)) return false;

        data.owned.Add(id);
        Save(data);
        InventoryChanged?.Invoke();
        return true;
    }

    /// <summary>지금 장착 중인 아이템 id (없으면 null)</summary>
    public static string GetEquipped() => Load().equipped;

    public static bool IsEquipped(string id) => Load().equipped == id;

    /// <summary>장착. 보유하지 않은 아이템은 장착할 수 없다. 기존 장착은 자동 교체.</summary>
    public static bool Equip(string id)
    {
        CharacterSave data = Load();
        if (!data.owned.Contains(id)) return false;

        data.equipped = id;
        Save(data);
        EquipChanged?.Invoke();
        return true;
    }

    public static void Unequip()
    {
        CharacterSave data = Load();
        if (data.equipped == null) return;

        data.equipped = null;
        Save(data);
        EquipChanged?.Invoke();
    }
}