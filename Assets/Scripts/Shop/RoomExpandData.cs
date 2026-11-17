// RoomExpandData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewRoomExpand", menuName = "Shop/Room Expand Data")]
public class RoomExpandData : ShopItemBase
{
    [Tooltip("확장되는 칸 수")]
    [SerializeField] private Vector2Int expandSize = Vector2Int.one;

    public Vector2Int ExpandSize => expandSize;
    public override ShopCategory Category => ShopCategory.RoomExpand;

    protected override void OnValidate()
    {
        base.OnValidate();
        expandSize = Vector2Int.Max(expandSize, Vector2Int.one);
    }
}