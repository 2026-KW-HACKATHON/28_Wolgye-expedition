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

    private Button button;
    private ShopItemBase data;
    private Action<ShopItemBase> onClicked;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void Setup(ShopItemBase item, Action<ShopItemBase> clickCallback)
    {
        data = item;
        onClicked = clickCallback;

        if (button == null) button = GetComponent<Button>();

        nameText.text = item.DisplayName;
        iconImage.sprite = item.Icon;
        priceText.text = item.Price.ToString("N0");

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClicked?.Invoke(data));
    }
}