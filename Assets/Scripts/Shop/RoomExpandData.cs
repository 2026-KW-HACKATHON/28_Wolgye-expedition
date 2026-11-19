// RoomExpandData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewRoomExpand", menuName = "Shop/Room Expand Data")]
public class RoomExpandData : ShopItemBase
{
    [Tooltip("몇 단계 상품인지 (1부터 시작, 순서대로 구매)")]
    [SerializeField, Min(1)] private int step = 1;

    public int Step => step;
    public override ShopCategory Category => ShopCategory.RoomExpand;
}