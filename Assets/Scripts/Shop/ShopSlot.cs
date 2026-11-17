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

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void Setup(ShopItemBase item)
    {
        data = item;
        nameText.text = item.DisplayName;
        iconImage.sprite = item.Icon;
        priceText.text = item.Price.ToString("N0");

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClickBuy);
    }

    private void OnClickBuy()
    {
        Debug.Log($"구매 시도: {data.DisplayName} / {data.Price}");
    }
}