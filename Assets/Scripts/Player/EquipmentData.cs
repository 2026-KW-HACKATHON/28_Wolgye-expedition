using UnityEngine;

public enum EquipmentType
{
    Hat,
    SkinColor
}

[CreateAssetMenu(fileName = "EquipmentData", menuName = "Equipment/Data")]
public class EquipmentData : ShopItemBase
{
    [Header("Equipment")]
    public EquipmentType equipmentType;

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Hat Transform")]
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localRotation = new Vector3(-90f, 0f, 0f);
    public Vector3 localScale = Vector3.one * 0.2f;

    [Header("Skin Color")]
    public Color skinColor = Color.white;

    public override ShopCategory Category => ShopCategory.Character;
}