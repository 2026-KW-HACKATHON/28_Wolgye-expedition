using UnityEngine;

[CreateAssetMenu(
    fileName = "EquipmentData",
    menuName = "Equipment/Data"
)]
public class EquipmentData : ScriptableObject
{
    [Header("Basic Info")]
    public string equipmentId;
    public string equipmentName;
    public EquipmentType equipmentType;
    public int price;
    public Sprite icon;

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Hat Transform")]
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localRotation = new Vector3(-90f, 0f, 0f);
    public Vector3 localScale = Vector3.one * 0.2f;

    [Header("Skin Color")]
    public Color skinColor = Color.white;
}

public enum EquipmentType
{
    Hat,
    SkinColor
}