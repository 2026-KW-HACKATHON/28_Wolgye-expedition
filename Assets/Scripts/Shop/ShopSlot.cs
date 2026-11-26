// ShopSlot.cs
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class ShopSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text priceText;

    [Header("상태 표시")]
    [SerializeField] private GameObject lockOverlay;    // 자물쇠
    [SerializeField] private GameObject ownedOverlay;   // "구매 완료" 표시

    private Button button;
    private ShopItemBase data;
    private Action<ShopItemBase> onClicked;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void Setup(ShopItemBase item, Action<ShopItemBase> clickCallback,
                      bool locked = false, bool owned = false)
    {
        data = item;
        onClicked = clickCallback;

        if (button == null) button = GetComponent<Button>();

        nameText.text = item.DisplayName;
        iconImage.sprite = item.Icon;
        priceText.text = item.Price.ToString("N0");

        if (lockOverlay != null) lockOverlay.SetActive(locked);
        if (ownedOverlay != null) ownedOverlay.SetActive(owned);

        // 잠김/구매 완료 슬롯도 클릭은 받아서 안내 팝업을 띄운다.
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClicked?.Invoke(data));
    }
}