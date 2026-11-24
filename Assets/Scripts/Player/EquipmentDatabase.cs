using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "EquipmentDatabase",
    menuName = "Equipment/Database"
)]
public class EquipmentDatabase : ScriptableObject
{
    [SerializeField]
    private List<EquipmentData> equipments = new();

    public EquipmentData GetEquipment(string equipmentId)
    {
        return equipments.Find(
            equipment => equipment.equipmentId == equipmentId
        );
    }

    // 전체 장비 목록 반환
    public IReadOnlyList<EquipmentData> GetAllEquipments()
    {
        return equipments;
    }
}