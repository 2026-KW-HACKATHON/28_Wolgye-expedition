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

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Transform")]
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localRotation = new Vector3(-90f, 0f, 0f);
    public Vector3 localScale = Vector3.one * 0.2f;

}