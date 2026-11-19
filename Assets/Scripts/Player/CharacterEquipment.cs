using UnityEngine;

public class CharacterEquipment : MonoBehaviour
{
    [SerializeField]
    private Transform headTopEnd;

    private GameObject currentHat;

    public void EquipHat(EquipmentData equipment)
    {
        // 기존 모자 제거
        if (currentHat != null)
        {
            Destroy(currentHat);
            currentHat = null;
        }

        if (equipment == null || equipment.prefab == null)
            return;

        // 모자 생성
        currentHat = Instantiate(
            equipment.prefab,
            headTopEnd
        );

        // 위치 및 회전 설정
        currentHat.transform.localPosition =
            equipment.localPosition;

        currentHat.transform.localRotation =
            Quaternion.Euler(equipment.localRotation);

        currentHat.transform.localScale =
            equipment.localScale;
    }

    public void UnequipHat()
    {
        if (currentHat != null)
        {
            Destroy(currentHat);
            currentHat = null;
        }
    }
}