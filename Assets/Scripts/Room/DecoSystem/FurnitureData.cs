// FurnitureData.cs
using UnityEngine;

/// <summary>가구인지 소품인지 구분 (상점 서브탭, 꾸미기 탭 분류용)</summary>
public enum FurnitureKind
{
    Furniture, // 가구
    Prop       // 소품
}

/// <summary>
/// 방 안에 놓을 수 있는 가구 데이터. (실제 놓인 가구가 아니라 "원본")
/// Project 창 우클릭 > Create > Room > Furniture Data 로 생성합니다.
/// </summary>
[CreateAssetMenu(fileName = "NewFurniture", menuName = "Room/Furniture Data")]
public class FurnitureData : ShopItemBase
{
    [Tooltip("피벗 규칙: footprint 중앙, 바닥 기준(y=0)")]
    [SerializeField] private GameObject prefab;

    [Tooltip("차지하는 칸 수 (x = 그리드 가로, y = 그리드 세로)")]
    [SerializeField] private Vector2Int size = Vector2Int.one;

    [Tooltip("가구 / 소품 구분. 상점 서브탭 분류에 쓰입니다.")]
    [SerializeField] private FurnitureKind kind = FurnitureKind.Furniture;   // ★ 추가

    public GameObject Prefab => prefab;
    public Vector2Int Size => size;
    public FurnitureKind Kind => kind;                                       // ★ 추가

    public override ShopCategory Category => ShopCategory.Furniture;

    protected override void OnValidate()
    {
        base.OnValidate();
        size = Vector2Int.Max(size, Vector2Int.one);
    }
}