using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    private const string HAT_KEY = "EquippedHatId";

    [Header("Database")]
    [SerializeField] private EquipmentDatabase equipmentDatabase;

    [Header("Character")]
    [SerializeField] private CharacterEquipment characterEquipment;

    private string equippedHatId;

    private void Start()
    {
        LoadEquipment();
    }

    // 모자 장착
    public void EquipHat(string equipmentId)
    {
        EquipmentData equipment =
            equipmentDatabase.GetEquipment(equipmentId);

        if (equipment == null)
        {
            Debug.LogWarning(
                $"장비를 찾을 수 없습니다: {equipmentId}"
            );
            return;
        }

        // 캐릭터에 실제 장착
        characterEquipment.EquipHat(equipment);

        // 장착 정보 저장
        equippedHatId = equipmentId;

        PlayerPrefs.SetString(HAT_KEY, equippedHatId);
        PlayerPrefs.Save();

        Debug.Log($"모자 장착 완료: {equipment.equipmentName}");
    }

    // 모자 해제
    public void UnequipHat()
    {
        characterEquipment.UnequipHat();

        equippedHatId = "";

        PlayerPrefs.DeleteKey(HAT_KEY);
        PlayerPrefs.Save();
    }

    // 저장된 장착 정보 불러오기
    private void LoadEquipment()
    {
        equippedHatId = PlayerPrefs.GetString(HAT_KEY, "");

        if (string.IsNullOrEmpty(equippedHatId))
            return;

        EquipmentData equipment =
            equipmentDatabase.GetEquipment(equippedHatId);

        if (equipment == null)
        {
            Debug.LogWarning(
                $"저장된 장비를 Database에서 찾을 수 없습니다: {equippedHatId}"
            );
            return;
        }

        characterEquipment.EquipHat(equipment);

        Debug.Log($"저장된 모자 적용: {equipment.equipmentName}");
    }
}