// PurchasePopup.cs
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PurchasePopup : MonoBehaviour
{
    [SerializeField] private GameObject root;         // 팝업 전체 (켜고 끌 오브젝트)
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image iconImage;         // 선택 (없으면 비워둠)
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("안내 모드용 (확인 버튼 1개)")]
    [SerializeField] private Button okButton;         // 안내 전용 확인 버튼

    private Action onConfirm;

    private void Awake()
    {
        yesButton.onClick.AddListener(OnYes);
        noButton.onClick.AddListener(Close);
        if (okButton != null) okButton.onClick.AddListener(Close);
        root.SetActive(false);
    }

    /// <summary>구매 확인 팝업 (네 / 아니오)</summary>
    public void Open(ShopItemBase item, Action confirmCallback)
    {
        onConfirm = confirmCallback;

        messageText.text = $"{item.DisplayName}을(를)\n 구매하시겠습니까?";
        SetIcon(item.Icon);
        SetButtons(confirmMode: true);

        root.SetActive(true);
    }

    /// <summary>안내 팝업 (확인 버튼만)</summary>
    public void OpenNotice(string message, Sprite icon = null)
    {
        onConfirm = null;

        messageText.text = message;
        SetIcon(icon);
        SetButtons(confirmMode: false);

        root.SetActive(true);
    }

    private void SetIcon(Sprite sprite)
    {
        if (iconImage == null) return;
        iconImage.sprite = sprite;
        iconImage.enabled = sprite != null;
    }

    private void SetButtons(bool confirmMode)
    {
        yesButton.gameObject.SetActive(confirmMode);
        noButton.gameObject.SetActive(confirmMode);

        // okButton이 없으면 안내 모드에서도 noButton을 확인 버튼처럼 사용
        if (okButton != null)
            okButton.gameObject.SetActive(!confirmMode);
        else if (!confirmMode)
            noButton.gameObject.SetActive(true);
    }

    private void OnYes()
    {
        var callback = onConfirm;
        Close();
        callback?.Invoke();
    }

    public void Close()
    {
        onConfirm = null;
        root.SetActive(false);
    }
}