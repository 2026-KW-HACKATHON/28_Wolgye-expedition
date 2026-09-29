using UnityEngine;

/// <summary>
/// 가구 한 종류의 정의 데이터. (씬에 놓인 개별 가구가 아니라 "종류")
/// Project 창 우클릭 > Create > Room > Furniture Data 로 생성합니다.
/// </summary>
[CreateAssetMenu(fileName = "NewFurniture", menuName = "Room/Furniture Data")]
public class FurnitureData : ScriptableObject
{
    [Tooltip("저장/불러오기에 쓰이는 고유 ID. 한 번 정하면 바꾸지 마세요.")]
    [SerializeField] private string id;
    [SerializeField] private string displayName;

    [Tooltip("피벗 규칙: footprint 중앙, 바닥 높이(y=0)")]
    [SerializeField] private GameObject prefab;

    [Tooltip("차지하는 칸 수 (x = 그리드 가로, y = 그리드 세로)")]
    [SerializeField] private Vector2Int size = Vector2Int.one;

    public string Id => id;
    public string DisplayName => displayName;
    public GameObject Prefab => prefab;
    public Vector2Int Size => size;

    private void OnValidate()
    {
        size = Vector2Int.Max(size, Vector2Int.one);
        if (string.IsNullOrEmpty(id)) id = name;
    }
}