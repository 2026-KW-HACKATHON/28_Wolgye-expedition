using UnityEngine;
using UnityEngine.UI;

public class DalsuCollectionSlot : MonoBehaviour
{
    public DalsuData dalsuData;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        image = GetComponent<Image>();

        if (image == null)
            return;

        if (dalsuData == null)
            return;

        if (DalsuSaveManager.IsCollected(dalsuData))
        {
            // 획득함 → 원래 색
            image.color = Color.white;
        }
        else
        {
            // 미획득 → 검은색
            image.color = Color.black;
        }
    }
}