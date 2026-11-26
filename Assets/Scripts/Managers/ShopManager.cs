using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum ShopSubType { Furniture, Prop, Wallpaper, Floor }

public class ShopManager : MonoBehaviour
{
    [System.Serializable]
    public class TabEntry
    {
        public ShopCategory category;
        public Toggle toggle;

        [Header("선택 표시")]
        public Image backgroundImage;      // 탭 배경 Image
        public Sprite selectedSprite;      // 선택됐을 때
        public Sprite normalSprite;        // 선택 안 됐을 때
    }

    [System.Serializable]
    public class SubTabEntry
    {
        public ShopSubType subType;
        public Toggle toggle;

        [Header("선택 표시")]
        public Image backgroundImage;      // 서브탭 배경 Image
        public Sprite selectedSprite;      // 선택됐을 때
        public Sprite normalSprite;        // 선택 안 됐을 때
    }

    [Header("탭")]
    [SerializeField] private List<TabEntry> tabs;

    [Header("가구 서브탭")]
    [SerializeField] private GameObject subTabRoot;
    [SerializeField] private List<SubTabEntry> subTabs;

    [Header("목록")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private ShopSlot slotPrefab;

    [Header("목록 여백")]
    [SerializeField] private int paddingTopNormal = 20;      // 서브탭 없는 탭
    [SerializeField] private int paddingTopWithSubTab = 80;  // 가구 탭 (서브탭 높이 + 여유)

    [Header("구매")]
    [SerializeField] private PurchasePopup purchasePopup;

    [Header("전체 아이템 데이터")]
    [SerializeField] private List<ShopItemBase> allItems;

    private readonly List<ShopSlot> spawnedSlots = new List<ShopSlot>();
    private ShopCategory currentCategory = ShopCategory.Furniture;
    private ShopSubType currentSubType = ShopSubType.Furniture;

    private void Awake()
    {
        foreach (var tab in tabs)
        {
            var captured = tab;
            captured.toggle.onValueChanged.AddListener(isOn =>
            {
                RefreshTabVisual(captured, isOn);
                if (isOn)
                {
                    // 큰 탭을 바꾸면 서브탭은 "가구"로 초기화
                    currentSubType = ShopSubType.Furniture;
                    RefreshSubTabVisual();
                    ShowCategory(captured.category);
                }
            });
        }

        foreach (var sub in subTabs)
        {
            var captured = sub;
            captured.toggle.onValueChanged.AddListener(isOn =>
            {
                if (!isOn) return;
                currentSubType = captured.subType;
                RefreshSubTabVisual();
                ShowCategory(ShopCategory.Furniture);
            });
        }
    }

    private void OnEnable()
    {
        SelectCategory(ShopCategory.Furniture);
    }

    /// <summary>탭 선택 상태와 목록을 강제로 맞춘다.</summary>
    private void SelectCategory(ShopCategory category)
    {
        currentSubType = ShopSubType.Furniture;
        RefreshSubTabVisual();

        foreach (var tab in tabs)
        {
            bool isTarget = tab.category == category;
            tab.toggle.SetIsOnWithoutNotify(isTarget);
            RefreshTabVisual(tab, isTarget);
        }

        ShowCategory(category);
    }

    private void RefreshTabVisual(TabEntry tab, bool isOn)
    {
        if (tab.backgroundImage != null)
            tab.backgroundImage.sprite = isOn ? tab.selectedSprite : tab.normalSprite;
    }

    private void RefreshSubTabVisual()
    {
        foreach (var sub in subTabs)
        {
            bool isOn = sub.subType == currentSubType;
            sub.toggle.SetIsOnWithoutNotify(isOn);

            if (sub.backgroundImage != null)
                sub.backgroundImage.sprite = isOn ? sub.selectedSprite : sub.normalSprite;
        }
    }

    private void ShowCategory(ShopCategory category)
    {
        currentCategory = category;

        // 가구 탭일 때만 서브탭 표시
        if (subTabRoot != null) subTabRoot.SetActive(category == ShopCategory.Furniture);

        // 가구 탭만 목록을 서브탭 아래로 내림
        var grid = contentRoot.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.padding.top = category == ShopCategory.Furniture ? paddingTopWithSubTab : paddingTopNormal;
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)contentRoot);
        }

        ClearSlots();

        foreach (var item in allItems)
        {
            if (item == null || item.Category != category) continue;

            // 가구 탭이면 선택한 서브탭 종류만 표시
            if (category == ShopCategory.Furniture && GetSubType(item) != currentSubType) continue;

            bool locked = false;
            bool owned = false;

            if (item is RoomExpandData expand)
            {
                owned = RoomCapacity.IsPurchased(expand.Step);
                locked = !owned && !RoomCapacity.IsNextStep(expand.Step);
            }

            ShopSlot slot = Instantiate(slotPrefab, contentRoot);
            slot.Setup(item, OnSlotClicked, locked, owned);
            spawnedSlots.Add(slot);
        }

        var scroll = contentRoot.GetComponentInParent<ScrollRect>();
        if (scroll != null) scroll.verticalNormalizedPosition = 1f;
    }

    private void ClearSlots()
    {
        foreach (var slot in spawnedSlots)
            if (slot != null) Destroy(slot.gameObject);
        spawnedSlots.Clear();
    }

    private void OnSlotClicked(ShopItemBase item)
    {
        if (item is SurfaceShopItem s && SurfaceInventory.Has(s.OwnedKey))
        {
            purchasePopup.OpenNotice("이미 보유한 아이템입니다.");
            return;
        }

        if (item is RoomExpandData expand)
        {
            if (RoomCapacity.IsPurchased(expand.Step))
            {
                purchasePopup.OpenNotice("이미 구매한 확장입니다.");
                return;
            }
            if (!RoomCapacity.IsNextStep(expand.Step))
            {
                purchasePopup.OpenNotice("이전 단계를 먼저 구매해주세요.");
                return;
            }
        }

        if (item.Category == ShopCategory.Character && CharacterInventory.Has(item.Id))
        {
            purchasePopup.OpenNotice("이미 보유한 아이템입니다.");
            return;
        }

        if (!CurrencyManager.CanAfford(item.Price))
        {
            int shortage = item.Price - CurrencyManager.Balance;
            purchasePopup.OpenNotice($"코인이 부족합니다.\n({shortage:N0} 코인 더 필요해요)");
            return;
        }

        purchasePopup.Open(item, () => Purchase(item));
    }

    private void Purchase(ShopItemBase item)
    {
        if (!CurrencyManager.TrySpend(item.Price)) return;

        switch (item.Category)
        {
            case ShopCategory.Furniture:
                if (item is SurfaceShopItem s)
                    SurfaceInventory.Add(s.OwnedKey);     // 벽지/바닥: 1회성 보유
                else
                    FurnitureInventory.Add(item.Id, 1);   // 가구/소품: 수량 +1
                break;

            case ShopCategory.Character:
                CharacterInventory.Add(item.Id);
                break;

            case ShopCategory.RoomExpand:
                RoomCapacity.SetLevel(((RoomExpandData)item).Step);
                ShowCategory(ShopCategory.RoomExpand);
                break;
        }

        purchasePopup.OpenNotice($"{item.DisplayName} 구매 완료!", item.Icon);
    }

    private static ShopSubType GetSubType(ShopItemBase item)
    {
        if (item is SurfaceShopItem s)
            return s.Slot == SurfaceSlot.Wall ? ShopSubType.Wallpaper : ShopSubType.Floor;

        if (item is FurnitureData f && f.Kind == FurnitureKind.Prop)
            return ShopSubType.Prop;

        return ShopSubType.Furniture;
    }
}