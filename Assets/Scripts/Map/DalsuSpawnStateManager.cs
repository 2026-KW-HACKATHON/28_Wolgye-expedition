using System.Collections.Generic;
using UnityEngine;

public class DalsuSpawnStateManager : MonoBehaviour
{
    public static DalsuSpawnStateManager Instance { get; private set; }

    // 현재 Spawn된 Dalsu
    private readonly HashSet<string> _spawnedDalsu = new();

    // 잡힌 Dalsu
    private readonly HashSet<string> _caughtDalsu = new();

    // 현재 Spawn 가능 구역에 들어갔는지
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

    public bool IsSpawned(string id)
    {
        return _spawnedDalsu.Contains(id);
    }

    public void AddSpawnedDalsu(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        _spawnedDalsu.Add(id);
    }

    public void RemoveSpawnedDalsu(string id)
    {
        _spawnedDalsu.Remove(id);
    }


    // =========================================================
    // Caught Dalsu
    // =========================================================

    public bool IsCaught(string id)
    {
        return _caughtDalsu.Contains(id);
    }

    public void AddCaughtDalsu(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        _caughtDalsu.Add(id);

        // 잡힌 Dalsu는 Spawn 상태에서도 제거
        _spawnedDalsu.Remove(id);
    }


    // =========================================================
    // Spawn Area
    // =========================================================

    public bool IsAreaEntered(string id)
    {
        return _spawnAreaEntered.Contains(id);
    }

    public void EnterArea(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        _spawnAreaEntered.Add(id);
    }

    public void ExitArea(string id)
    {
        _spawnAreaEntered.Remove(id);
    }


    // =========================================================
    // Area 초기화
    // =========================================================

    public void ResetArea(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        _spawnAreaEntered.Remove(id);

        _spawnedDalsu.Remove(id);
    }
}