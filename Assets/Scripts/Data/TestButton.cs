using UnityEngine;

public class TestButton : MonoBehaviour
{
    public DalsuData dalsuData;

    public void AcquireDalsuByButton()
    {
        if (dalsuData == null)
        {
            Debug.LogError("DalsuData가 연결되지 않았습니다.");
            return;
        }

        DalsuSaveManager.AcquireDalsu(dalsuData);
    }
}