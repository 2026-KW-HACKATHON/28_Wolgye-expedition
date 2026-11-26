// SurfaceShopItem.cs
using UnityEngine;


[CreateAssetMenu(fileName = "SurfaceShopItem", menuName = "Shop/Surface Shop Item")]
public class SurfaceShopItem : ShopItemBase
{
    [SerializeField] private SurfaceOption surface;   // 기존 SO 참조
    [SerializeField] private SurfaceSlot slot;        // 벽지로 팔지, 바닥으로 팔지

    public SurfaceOption Surface => surface;
    public SurfaceSlot Slot => slot;

    // 보유 저장용 키: 같은 SO라도 벽/바닥을 따로 구분
    public string OwnedKey => $"{surface.Id}_{slot}";

    // 상점 카테고리는 가구 탭 하위 분류로 쓴다 (아래 참고)
    public override ShopCategory Category => ShopCategory.Furniture;
}