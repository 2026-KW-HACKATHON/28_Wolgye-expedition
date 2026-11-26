using UnityEngine;

public class CharacterEquipment : MonoBehaviour
{
    [Header("Equipment")]
    [SerializeField] private Transform hatPosition;

    [Header("Character")]
    [SerializeField] private Renderer bodyRenderer;

    private GameObject currentHat;

    // 원래 Material 저장
    private Material defaultSkinMaterial;

    private void Awake()
    {
        defaultSkinMaterial = bodyRenderer.sharedMaterial;
    }

    // 모자 장착
    public void EquipHat(EquipmentData equipment)
    {
        UnequipHat();

        currentHat = Instantiate(
            equipment.prefab,
            hatPosition
        );

        currentHat.transform.localPosition =
            equipment.localPosition;

        currentHat.transform.localRotation =
            Quaternion.Euler(equipment.localRotation);

        currentHat.transform.localScale =
            equipment.localScale;
    }

    // 모자 해제
    public void UnequipHat()
    {
        if (currentHat != null)
        {
            Destroy(currentHat);
            currentHat = null;
        }
    }

    // 피부색 변경
    public void SetSkinColor(Color color)
    {
        bodyRenderer.material.color = color;
    }

    // 원래 Material 복원
    public void ResetSkinColor()
    {
        bodyRenderer.material = defaultSkinMaterial;
    }
}