using UnityEngine;

[CreateAssetMenu(fileName = "DalsuData", menuName = "Dalsu/Dalsu Data")]
public class DalsuData : ScriptableObject
{
    [Header("기본 정보")]
    public string id;
    public string dalsuName;
    public string type;
    [TextArea]
    public string description;
    public int rarity;

    [Header("에셋")]
    public Sprite icon;
    public GameObject prefab;

    [Header("등장 위치")]
    public double latitude;
    public double longitude;
    public float spawnRadius = 30f;
}