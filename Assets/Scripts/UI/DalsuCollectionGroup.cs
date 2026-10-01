using TMPro;
using UnityEngine;

public class DalsuCollectionGroup : MonoBehaviour
{
    public TMP_Text countText;
    public string type;
    public DalsuCollectionSlot[] slots;

    public void Refresh()
    {
        int collectedCount = 0;

        foreach (DalsuCollectionSlot slot in slots)
        {
            slot.Refresh();

            if (slot.dalsuData != null &&
                DalsuSaveManager.IsCollected(slot.dalsuData))
            {
                collectedCount++;
            }
        }

        countText.text = type + " " + collectedCount + "/" + slots.Length;
    }

    private void OnEnable()
    {
        Refresh();
    }
}