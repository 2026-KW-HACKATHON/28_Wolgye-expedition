using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class CurrencyDisplay : MonoBehaviour
{
    private TMP_Text label;

    private void OnEnable()
    {
        label = GetComponent<TMP_Text>();
        CurrencyManager.BalanceChanged += Refresh;
        Refresh(CurrencyManager.Balance);
    }

    private void OnDisable() => CurrencyManager.BalanceChanged -= Refresh;

    private void Refresh(int balance) => label.text = balance.ToString("N0");
}
