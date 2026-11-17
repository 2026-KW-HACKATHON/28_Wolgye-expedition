using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("탭")]
    [SerializeField] private List<TabEntry> tabs;
    //[SerializeField] private Color selectedTextColor = Color.white;
    //[SerializeField] private Color normalTextColor = Color.black;

    [Header("목록")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private ShopSlot slotPrefab;

    [Header("구매")]
    [SerializeField] private PurchasePopup purchasePopup;

    [Header("전체 아이템 데이터")]
    [SerializeField] private List<ShopItemBase> allItems;

    private readonly List<ShopSlot> spawnedSlots = new List<ShopSlot>();

    private void Awake()
    {
        foreach (var tab in tabs)
        {
            var captured = tab;
            captured.toggle.onValueChanged.AddListener(isOn =>
            {
                RefreshTabVisual(captured, isOn);
                if (isOn) ShowCategory(captured.category);
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
        foreach (var tab in tabs)
        {
            bool isTarget = tab.category == category;
            tab.toggle.SetIsOnWithoutNotify(isTarget);   // 이벤트 없이 상태만 변경
            RefreshTabVisual(tab, isTarget);             // 스프라이트 갱신
        }

        ShowCategory(category);                          // 목록 직접 표시
    }

    private void RefreshTabVisual(TabEntry tab, bool isOn)
    {
        if (tab.backgroundImage != null)
            tab.backgroundImage.sprite = isOn ? tab.selectedSprite : tab.normalSprite;
    }

    private void ShowCategory(ShopCategory category)
    {
        ClearSlots();

        foreach (var item in allItems)
        {
            if (item == null || item.Category != category) continue;

            ShopSlot slot = Instantiate(slotPrefab, contentRoot);
            slot.Setup(item, OnSlotClicked);
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
        Debug.Log($"슬롯 클릭됨: {item.DisplayName}");
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
                FurnitureInventory.Add(item.Id, 1);
                break;
            case ShopCategory.Character:
                Debug.Log($"캐릭터 구매: {item.Id}");   // TODO
                break;
            case ShopCategory.RoomExpand:
                Debug.Log($"방 확장 구매: {item.Id}");   // TODO
                break;
        }

        purchasePopup.OpenNotice($"{item.DisplayName} 구매 완료!", item.Icon);
    }
}