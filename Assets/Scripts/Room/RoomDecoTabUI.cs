using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum DecoTab
{
    Furniture, // 가구
    Props,     // 소품
    Wall,      // 벽지
    Floor,     // 바닥
}

/// <summary>
/// 방 꾸미기 패널(Room_Deco)의 탭(Toggle_furniture / props / wall / floor) 전환과
/// 벽지·바닥 선택 버튼 생성을 담당한다. Room_Deco(또는 그 하위)에 붙여서 사용한다.
///
/// - 탭마다 버튼 목록 부모(Content)를 따로 두고, 선택한 탭의 Content만 켜서 ScrollRect에 연결한다.
/// - 가구 탭은 기존 Content(FurnitureEditController가 가구 버튼을 만드는 곳)를 그대로 쓴다.
/// - Content를 비워둔 탭은 기존 Content의 크기·레이아웃을 본떠 실행 시 자동으로 만든다.
/// - 벽지/바닥 버튼은 상점 아이템(SurfaceShopItem) 중 구매한 것만 만들고, 누르면 바로 적용·저장된다.
/// </summary>
public class RoomDecoTabUI : MonoBehaviour
{
    [Serializable]
    public class TabEntry
    {
        public DecoTab tab;
        public Toggle toggle;

        [Tooltip("이 탭의 버튼 목록 부모. 비워두면 가구 탭은 ScrollRect의 원래 Content를 쓰고, 나머지는 실행 시 만든다.")]
        public RectTransform content;
    }

    [Header("Tabs")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TabEntry[] tabs =
    {
        new TabEntry { tab = DecoTab.Furniture },
        new TabEntry { tab = DecoTab.Props },
        new TabEntry { tab = DecoTab.Wall },
        new TabEntry { tab = DecoTab.Floor },
    };

    [Header("Surface (벽지 / 바닥)")]
    [SerializeField] private RoomSurfaceManager surfaceManager;

    [Tooltip("선택 버튼 프리팹 (DecoList_prefab: Button + 자식 TextMeshProUGUI, 아이콘은 자식 이름 \"Icon\")")]
    [SerializeField] private Button optionButtonPrefab;

    [Tooltip("현재 적용된 벽지/바닥 버튼의 배경 색")]
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.45f, 1f);

    [Header("Surface Shop Items (상점 아이템)")]
    [SerializeField] private List<SurfaceShopItem> surfaceShopItems;

    private class OptionButton
    {
        public SurfaceOption option;
        public GameObject root;          // ★ 추가: 다시 만들 때 지우기 위한 참조
        public Image background;
        public Color normalColor;
    }

    private readonly Dictionary<SurfaceSlot, List<OptionButton>> optionButtons = new Dictionary<SurfaceSlot, List<OptionButton>>
    {
        { SurfaceSlot.Wall, new List<OptionButton>() },
        { SurfaceSlot.Floor, new List<OptionButton>() },
    };

    private RectTransform originalContent;
    private DecoTab currentTab;

    private void Awake()
    {
        if (scrollRect != null) originalContent = scrollRect.content;
        PrepareContents();

        // ★ 추가: 구매하면 바로 목록 갱신
        SurfaceInventory.InventoryChanged += RebuildOptionButtons;
    }

    private void Start()
    {
        BuildOptionButtons(SurfaceSlot.Wall, DecoTab.Wall);
        BuildOptionButtons(SurfaceSlot.Floor, DecoTab.Floor);

        if (surfaceManager != null)
            surfaceManager.SurfaceChanged += OnSurfaceChanged;
        RefreshSelection(SurfaceSlot.Wall);
        RefreshSelection(SurfaceSlot.Floor);

        foreach (TabEntry entry in tabs)
        {
            if (entry.toggle == null) continue;

            DecoTab tab = entry.tab;
            entry.toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn) ShowTab(tab);
            });
        }

        ShowTab(GetInitialTab());
    }

    private void OnDestroy()
    {
        if (surfaceManager != null)
            surfaceManager.SurfaceChanged -= OnSurfaceChanged;

        SurfaceInventory.InventoryChanged -= RebuildOptionButtons;   // ★ 추가
    }

    // ── 탭 ──

    /// 코드에서 탭을 바꿀 때 사용 (토글도 함께 맞춰준다).
    public void SelectTab(DecoTab tab)
    {
        TabEntry entry = FindEntry(tab);
        if (entry?.toggle != null && !entry.toggle.isOn)
            entry.toggle.isOn = true; // onValueChanged → ShowTab
        else
            ShowTab(tab);
    }

    private void ShowTab(DecoTab tab)
    {
        currentTab = tab;

        foreach (TabEntry entry in tabs)
        {
            if (entry.content == null) continue;
            entry.content.gameObject.SetActive(entry.tab == tab);
        }

        TabEntry selected = FindEntry(tab);
        if (scrollRect != null && selected?.content != null)
        {
            scrollRect.StopMovement();
            scrollRect.content = selected.content;
            selected.content.anchoredPosition = Vector2.zero; // 탭을 바꾸면 목록 처음부터 보여준다.
        }
    }

    private DecoTab GetInitialTab()
    {
        foreach (TabEntry entry in tabs)
            if (entry.toggle != null && entry.toggle.isOn) return entry.tab;

        return tabs.Length > 0 ? tabs[0].tab : DecoTab.Furniture;
    }

    private TabEntry FindEntry(DecoTab tab)
    {
        foreach (TabEntry entry in tabs)
            if (entry.tab == tab) return entry;
        return null;
    }

    // 탭마다 Content를 준비한다. 비어 있는 탭은 원래 Content를 본떠 새로 만든다.
    private void PrepareContents()
    {
        foreach (TabEntry entry in tabs)
        {
            if (entry.content != null) continue;

            if (entry.tab == DecoTab.Furniture)
                entry.content = originalContent;
            else if (originalContent != null)
                entry.content = CreateContentLike(originalContent, $"Content_{entry.tab}");
        }
    }

    private static RectTransform CreateContentLike(RectTransform template, string objectName)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.layer = template.gameObject.layer;

        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(template.parent, false);
        rt.anchorMin = template.anchorMin;
        rt.anchorMax = template.anchorMax;
        rt.pivot = template.pivot;
        rt.anchoredPosition = template.anchoredPosition;
        rt.sizeDelta = template.sizeDelta;
        rt.localScale = template.localScale;

        if (template.TryGetComponent(out HorizontalOrVerticalLayoutGroup srcLayout))
        {
            var layout = (HorizontalOrVerticalLayoutGroup)go.AddComponent(srcLayout.GetType());
            layout.padding = new RectOffset(srcLayout.padding.left, srcLayout.padding.right, srcLayout.padding.top, srcLayout.padding.bottom);
            layout.spacing = srcLayout.spacing;
            layout.childAlignment = srcLayout.childAlignment;
            layout.childControlWidth = srcLayout.childControlWidth;
            layout.childControlHeight = srcLayout.childControlHeight;
            layout.childForceExpandWidth = srcLayout.childForceExpandWidth;
            layout.childForceExpandHeight = srcLayout.childForceExpandHeight;
            layout.childScaleWidth = srcLayout.childScaleWidth;
            layout.childScaleHeight = srcLayout.childScaleHeight;
            layout.reverseArrangement = srcLayout.reverseArrangement;
        }

        if (template.TryGetComponent(out ContentSizeFitter srcFitter))
        {
            ContentSizeFitter fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = srcFitter.horizontalFit;
            fitter.verticalFit = srcFitter.verticalFit;
        }

        go.SetActive(false);
        return rt;
    }

    // ── 벽지 / 바닥 버튼 ──

    private void BuildOptionButtons(SurfaceSlot slot, DecoTab tab)
    {
        TabEntry entry = FindEntry(tab);
        if (surfaceManager == null || optionButtonPrefab == null || entry?.content == null) return;
        if (surfaceShopItems == null) return;

        foreach (SurfaceShopItem shopItem in surfaceShopItems)
        {
            if (shopItem == null || shopItem.Slot != slot) continue;      // 이 탭(벽/바닥)에 맞는 것만
            if (shopItem.Surface == null || shopItem.Surface.material == null) continue;
            if (!SurfaceInventory.Has(shopItem.OwnedKey)) continue;       // 산 것만

            SurfaceOption option = shopItem.Surface;

            Button button = Instantiate(optionButtonPrefab, entry.content);
            button.name = $"{slot}_{shopItem.Id}";

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.text = shopItem.DisplayName;

            Transform iconTransform = button.transform.Find("Icon");
            if (iconTransform != null && iconTransform.TryGetComponent(out Image icon))
            {
                icon.sprite = shopItem.Icon;
                icon.enabled = shopItem.Icon != null;
            }

            button.onClick.AddListener(() => surfaceManager.Select(slot, option));

            Image background = button.targetGraphic as Image ?? button.GetComponent<Image>();
            optionButtons[slot].Add(new OptionButton
            {
                option = option,
                root = button.gameObject,    // ★ 추가
                background = background,
                normalColor = background != null ? background.color : Color.white,
            });
        }
    }

    // ★ 추가: 구매 직후 호출. 기존 버튼을 지우고 보유 상태 기준으로 다시 만든다.
    private void RebuildOptionButtons()
    {
        foreach (var list in optionButtons.Values)
        {
            foreach (var ob in list)
                if (ob.root != null) Destroy(ob.root);
            list.Clear();
        }

        BuildOptionButtons(SurfaceSlot.Wall, DecoTab.Wall);
        BuildOptionButtons(SurfaceSlot.Floor, DecoTab.Floor);
        RefreshSelection(SurfaceSlot.Wall);
        RefreshSelection(SurfaceSlot.Floor);
    }

    private void OnSurfaceChanged(SurfaceSlot slot, SurfaceOption option) => RefreshSelection(slot);

    // 지금 적용된 옵션의 버튼만 selectedColor로 표시한다.
    private void RefreshSelection(SurfaceSlot slot)
    {
        if (surfaceManager == null) return;

        SurfaceOption current = surfaceManager.GetCurrent(slot);
        foreach (OptionButton ob in optionButtons[slot])
        {
            if (ob.background == null) continue;
            ob.background.color = ob.option == current ? selectedColor : ob.normalColor;
        }
    }
}