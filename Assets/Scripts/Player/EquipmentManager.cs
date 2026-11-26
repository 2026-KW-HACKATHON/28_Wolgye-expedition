using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    private const string HAT_KEY = "EquippedHatId";
    private const string SKIN_COLOR_KEY = "EquippedSkinColorId";

    [Header("Database")]
    [SerializeField] private EquipmentDatabase equipmentDatabase;

    [Header("Character")]
    [SerializeField] private CharacterEquipment characterEquipment;

    private string equippedHatId;
    private string equippedSkinColorId;

    private void Start()
    {
        LoadEquipment();
    }

    // 장비 장착
    public void Equip(string equipmentId)
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

        switch (equipment.equipmentType)
        {
            case EquipmentType.Hat:
                EquipHat(equipment);
                break;

            case EquipmentType.SkinColor:
                EquipSkinColor(equipment);
                break;
        }
    }

    // 모자 장착
    private void EquipHat(EquipmentData equipment)
    {
        characterEquipment.EquipHat(equipment);

        equippedHatId = equipment.Id;

        PlayerPrefs.SetString(HAT_KEY, equippedHatId);
        PlayerPrefs.Save();

        Debug.Log($"모자 장착 완료: {equipment.DisplayName}");
    }

    // 피부색 변경
    private void EquipSkinColor(EquipmentData equipment)
    {
        characterEquipment.SetSkinColor(equipment.skinColor);

        equippedSkinColorId = equipment.Id;

        PlayerPrefs.SetString(
            SKIN_COLOR_KEY,
            equippedSkinColorId
        );

        PlayerPrefs.Save();

        Debug.Log($"피부색 변경 완료: {equipment.DisplayName}");
    }

    // 모자 해제
    public void UnequipHat()
    {
        characterEquipment.UnequipHat();

        equippedHatId = "";

        PlayerPrefs.DeleteKey(HAT_KEY);
        PlayerPrefs.Save();
    }

    public void UnequipSkinColor()
    {
        characterEquipment.ResetSkinColor();

        equippedSkinColorId = "";

        PlayerPrefs.DeleteKey(SKIN_COLOR_KEY);
        PlayerPrefs.Save();
    }

    // 저장된 장착 정보 불러오기
    private void LoadEquipment()
    {
        // 모자 불러오기
        equippedHatId = PlayerPrefs.GetString(HAT_KEY, "");

        if (!string.IsNullOrEmpty(equippedHatId))
        {
            EquipmentData hat =
                equipmentDatabase.GetEquipment(equippedHatId);

            if (hat != null &&
                hat.equipmentType == EquipmentType.Hat)
            {
                characterEquipment.EquipHat(hat);
            }
            else
            {
                Debug.LogWarning(
                    $"저장된 모자를 찾을 수 없습니다: {equippedHatId}"
                );
            }
        }

        // 피부색 불러오기
        equippedSkinColorId =
            PlayerPrefs.GetString(SKIN_COLOR_KEY, "");

        if (!string.IsNullOrEmpty(equippedSkinColorId))
        {
            EquipmentData skin =
                equipmentDatabase.GetEquipment(equippedSkinColorId);

            if (skin != null &&
                skin.equipmentType == EquipmentType.SkinColor)
            {
                characterEquipment.SetSkinColor(skin.skinColor);
            }
            else
            {
                Debug.LogWarning(
                    $"저장된 피부색을 찾을 수 없습니다: {equippedSkinColorId}"
                );
            }
        }
    }
}