
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentListReturnManager : MonoBehaviour
{
    private class Entry
    {
        public GameObject element;
        public EquipmentData data;
        public Image rootImage;
        public Color normalColor;
        public bool selected;
        public bool isUnequipped;
    }

    [Header("UI")]
    [SerializeField] private GameObject listPrefab;
    [SerializeField] private Transform listParent;

    [Header("Equipment")]
    [SerializeField] private EquipmentDatabase equipmentDatabase;
    [SerializeField] private EquipmentManager equipmentManager;

    [Header("Unequipped")]
    [SerializeField] private Sprite unequippedIcon;

    [Header("Selection")]
    [SerializeField]
    private Color selectedColor =
        new Color(0.6f, 0.8f, 1f, 1f);

    private readonly List<Entry> entries = new List<Entry>();

    private Entry selectedEntry;

    private EquipmentType currentType;

    // 외부에서 카테고리 변경
    public void ChangeCategory(EquipmentType type)
    {
        currentType = type;

        // 기존 리스트 제거
        foreach (Entry entry in entries)
        {
            Destroy(entry.element);
        }

        entries.Clear();
        selectedEntry = null;

        // 새 리스트 생성
        InitializeEquipmentList();
    }

    private void InitializeEquipmentList()
    {
        if (equipmentDatabase == null)
        {
            Debug.LogWarning("EquipmentDatabase가 연결되지 않았습니다.");
            return;
        }

        AddUnequippedList();

        IReadOnlyList<EquipmentData> equipments =
            equipmentDatabase.GetAllEquipments();

        foreach (EquipmentData equipment in equipments)
        {
            if (equipment.equipmentType != currentType)
                continue;

            AddEquipmentList(equipment);
        }
    }

    private void AddUnequippedList()
    {
        GameObject element = Instantiate(listPrefab, listParent);

        element.transform.Find("Icon")
            .GetComponentInChildren<Image>().sprite = unequippedIcon;

        element.GetComponentInChildren<TextMeshProUGUI>().text =
            currentType == EquipmentType.Hat
                ? "미장착"
                : "기본 피부색";

        Entry entry = new Entry
        {
            element = element,
            rootImage = element.GetComponent<Image>(),
            isUnequipped = true
        };

        RegisterEntry(entry);
    }

    public void AddEquipmentList(EquipmentData data)
    {
        if (data == null || data.equipmentType != currentType)
            return;

        GameObject element = Instantiate(listPrefab, listParent);

        element.transform.Find("Icon")
            .GetComponentInChildren<Image>().sprite = data.icon;

        element.GetComponentInChildren<TextMeshProUGUI>().text =
            data.equipmentName;

        Entry entry = new Entry
        {
            element = element,
            data = data,
            rootImage = element.GetComponent<Image>()
        };

        RegisterEntry(entry);
    }

    private void RegisterEntry(Entry entry)
    {
        if (entry.rootImage != null)
            entry.normalColor = entry.rootImage.color;

        entries.Add(entry);

        Button button = entry.element.GetComponent<Button>();

        if (button == null)
            button = entry.element.GetComponentInChildren<Button>();

        if (button != null)
            button.onClick.AddListener(() => ToggleSelect(entry));
    }

    private void ToggleSelect(Entry entry)
    {
        if (equipmentManager == null)
            return;

        if (entry.isUnequipped)
        {
            if (currentType == EquipmentType.Hat)
                equipmentManager.UnequipHat();
            else if (currentType == EquipmentType.SkinColor)
                equipmentManager.UnequipSkinColor();
        }
        else
        {
            equipmentManager.Equip(entry.data.equipmentId);
        }

        if (selectedEntry != null)
        {
            selectedEntry.selected = false;

            if (selectedEntry.rootImage != null)
                selectedEntry.rootImage.color =
                    selectedEntry.normalColor;
        }

        selectedEntry = entry;
        entry.selected = true;

        if (entry.rootImage != null)
            entry.rootImage.color = selectedColor;
    }
}
