using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DalsuSpawnEntry
{
    public DalsuData dalsuData;
    public float weight = 1f;
}

[CreateAssetMenu(
    fileName = "DalsuSpawnArea",
    menuName = "Dalsu/Dalsu Spawn Area"
)]
public class DalsuSpawnArea : ScriptableObject
{
    [Header("구역 정보")]
    public string areaId;
    public string areaName;

    public double latitude;
    public double longitude;

    public float radius = 100f;

    [Header("Spawn")]
    public int maxSpawnCount = 5;

    public List<DalsuSpawnEntry> spawnEntries = new();
}