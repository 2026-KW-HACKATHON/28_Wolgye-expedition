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

    [Header("전체 아이템 데이터")]
    [SerializeField] private List<ShopItemBase> allItems;

    private readonly List<ShopSlot> spawnedSlots = new List<ShopSlot>();

    private void Start()
    {
        foreach (var tab in tabs)
        {
            var captured = tab;
            captured.toggle.onValueChanged.AddListener(isOn =>
            {
                RefreshTabVisual(captured, isOn);
                if (isOn) ShowCategory(captured.category);
            });

            // 시작 시 모두 비선택 상태로 초기화
            RefreshTabVisual(captured, false);
        }

        // 첫 번째 탭을 기본 선택
        tabs[0].toggle.SetIsOnWithoutNotify(false);
        tabs[0].toggle.isOn = true;
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
            slot.Setup(item);
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
}