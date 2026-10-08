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


    // =========================================================
    // Spawned Dalsu
    // =========================================================

    // 현재 월드에 존재하는 Dalsu
    // instanceId -> SpawnedDalsuData
    private readonly Dictionary<string, SpawnedDalsuData> _spawnedDalsu = new();


    // 현재 플레이어가 들어가 있는 SpawnArea
    private readonly HashSet<string> _spawnAreaEntered = new();


    // =========================================================
    // Singleton
    // =========================================================

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


    /// <summary>
    /// 새로운 Dalsu를 현재 월드 상태에 추가한다.
    /// </summary>
    public void AddDalsu(
        SpawnedDalsuData dalsu)
    {
        if (dalsu == null)
            return;

        if (string.IsNullOrEmpty(dalsu.instanceId))
            return;

        if (_spawnedDalsu.ContainsKey(dalsu.instanceId))
            return;

        _spawnedDalsu.Add(
            dalsu.instanceId,
            dalsu
        );
    }


    /// <summary>
    /// instanceId로 현재 살아있는 Dalsu를 가져온다.
    /// </summary>
    public bool TryGetDalsu(
        string instanceId,
        out SpawnedDalsuData dalsu)
    {
        return _spawnedDalsu.TryGetValue(
            instanceId,
            out dalsu
        );
    }


    /// <summary>
    /// Dalsu를 현재 월드 상태에서 제거한다.
    /// Catch 또는 Despawn 시 호출한다.
    /// </summary>
    public bool RemoveDalsu(
        string instanceId)
    {
        return _spawnedDalsu.Remove(
            instanceId
        );
    }


    // =========================================================
    // SpawnArea
    // =========================================================

    public bool IsAreaEntered(
        string areaId)
    {
        if (string.IsNullOrEmpty(areaId))
            return false;

        return _spawnAreaEntered.Contains(
            areaId
        );
    }


    public void EnterArea(
        string areaId)
    {
        if (string.IsNullOrEmpty(areaId))
            return;

        _spawnAreaEntered.Add(
            areaId
        );
    }


    public void ExitArea(
        string areaId)
    {
        if (string.IsNullOrEmpty(areaId))
            return;

        _spawnAreaEntered.Remove(
            areaId
        );
    }


    // =========================================================
    // Area Dalsu
    // =========================================================

    /// <summary>
    /// 특정 SpawnArea에 존재하는
    /// 현재 살아있는 Dalsu들의 instanceId를 반환한다.
    /// </summary>
    public List<string> GetDalsuIdsByArea(
        string areaId)
    {
        List<string> ids = new();

        if (string.IsNullOrEmpty(areaId))
            return ids;

        foreach (
            KeyValuePair<string, SpawnedDalsuData> pair
            in _spawnedDalsu)
        {
            if (pair.Value == null)
                continue;

            if (pair.Value.areaId != areaId)
                continue;

            ids.Add(pair.Key);
        }

        return ids;
    }
}