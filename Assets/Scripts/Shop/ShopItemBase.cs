// ShopItemBase.cs
using UnityEngine;

public enum ShopCategory
{
    Furniture,   // 가구
    Character,   // 캐릭터
    RoomExpand   // 방 확장
}

/// <summary>
/// 상점에 진열되는 모든 아이템의 공통 부모.
/// </summary>
public abstract class ShopItemBase : ScriptableObject
{
    [Tooltip("저장/불러오기에 쓰이는 고유 ID. 한 번 정하면 바꾸지 마세요.")]
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    [Tooltip("상점 판매 가격")]
    [SerializeField, Min(0)] private int price;

    public string Id => id;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public int Price => price;

    public abstract ShopCategory Category { get; }

    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(id)) id = name;
    }
}