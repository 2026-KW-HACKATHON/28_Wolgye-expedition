using Mapbox.BaseModule.Data.Vector2d;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DalsuSpawnStateManager : MonoBehaviour
{
    public static DalsuSpawnStateManager Instance { get; private set; }

    [Serializable]
    public class SpawnedDalsuData
    {
        public string instanceId;
        public DalsuData data;
        public string areaId;
        public LatitudeLongitude location;
    }


    // instanceId -> SpawnedDalsuData
    private readonly Dictionary<string, SpawnedDalsuData> _spawnedDalsu = new();

    // 현재 플레이어가 들어가 있는 SpawnArea
    private readonly HashSet<string> _spawnAreaEntered = new();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }


    // =========================================================
    // Spawned Dalsu
    // =========================================================

    public IReadOnlyDictionary<string, SpawnedDalsuData> SpawnedDalsu
    {
        get
        {
            return _spawnedDalsu;
        }
    }


    public void AddDalsu(
        SpawnedDalsuData dalsu)
    {
        if (dalsu == null)
        {
            return;
        }

        _spawnedDalsu.Add(
            dalsu.instanceId,
            dalsu
        );
    }


    public bool TryGetDalsu(
        string instanceId,
        out SpawnedDalsuData dalsu)
    {
        return _spawnedDalsu.TryGetValue(
            instanceId,
            out dalsu
        );
    }


    public void RemoveDalsu(
        string instanceId)
    {
        _spawnedDalsu.Remove(
            instanceId
        );
    }


    // =========================================================
    // SpawnArea
    // =========================================================

    public bool IsAreaEntered(
        string areaId)
    {
        return _spawnAreaEntered.Contains(
            areaId
        );
    }


    public void EnterArea(
        string areaId)
    {
        _spawnAreaEntered.Add(
            areaId
        );
    }


    public void ExitArea(
        string areaId)
    {
        _spawnAreaEntered.Remove(
            areaId
        );
    }


    // =========================================================
    // Area Dalsu Remove
    // =========================================================

    public List<string> GetDalsuIdsByArea(
        string areaId)
    {
        List<string> ids = new();

        foreach (KeyValuePair<string, SpawnedDalsuData> pair
                 in _spawnedDalsu)
        {
            if (pair.Value.areaId == areaId)
            {
                ids.Add(pair.Key);
            }
        }

        return ids;
    }
}