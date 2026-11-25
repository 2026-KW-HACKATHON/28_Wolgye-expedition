
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentListReturnManager : MonoBehaviour
{
    // 리스트에 추가된 장비 하나
    private class Entry
    {
        public GameObject element;
        public EquipmentData data;
        public Image rootImage;
        public Color normalColor;
        public bool selected;
    }

    [Header("UI")]
    [SerializeField] private GameObject listPrefab;
    [SerializeField] private Transform listParent;

    [Header("Equipment")]
    [SerializeField] private EquipmentDatabase equipmentDatabase;
    [SerializeField] private EquipmentManager equipmentManager;

    [Header("Selection")]
    [SerializeField]
    private Color selectedColor =
        new Color(0.6f, 0.8f, 1f, 1f);

    private readonly List<Entry> entries = new List<Entry>();

    private Entry selectedEntry;

    private void Start()
    {
        InitializeEquipmentList();
    }

    // Database의 전체 장비를 UI에 등록
    private void InitializeEquipmentList()
    {
        if (equipmentDatabase == null)
        {
            Debug.LogWarning("EquipmentDatabase가 연결되지 않았습니다.");
            return;
        }

        IReadOnlyList<EquipmentData> equipments =
            equipmentDatabase.GetAllEquipments();

        foreach (EquipmentData equipment in equipments)
        {
            AddEquipmentList(equipment);
        }
    }

    // 장비 리스트에 항목 추가
    public void AddEquipmentList(EquipmentData data)
    {
        if (data == null) return;

        GameObject element = Instantiate(listPrefab, listParent);

        element.transform.Find("Icon").GetComponentInChildren<Image>().sprite =
            data.icon;

        element.GetComponentInChildren<TextMeshProUGUI>().text =
            data.equipmentName;

        Entry entry = new Entry
        {
            element = element,
            data = data,
            rootImage = element.GetComponent<Image>()
        };

        if (entry.rootImage != null)
            entry.normalColor = entry.rootImage.color;

        entries.Add(entry);

        Button button = element.GetComponent<Button>();

        if (button == null)
            button = element.GetComponentInChildren<Button>();

        if (button != null)
            button.onClick.AddListener(() => ToggleSelect(entry));
    }

    // 항목 클릭: 선택 및 즉시 장착
    private void ToggleSelect(Entry entry)
    {
        if (equipmentManager == null || entry.data == null)
            return;

        // 실제 장비 장착
        equipmentManager.EquipHat(entry.data.equipmentId);

        // 기존 선택 항목 색상 복구
        if (selectedEntry != null)
        {
            selectedEntry.selected = false;

            if (selectedEntry.rootImage != null)
                selectedEntry.rootImage.color = selectedEntry.normalColor;
        }

        // 새로운 항목 선택
        selectedEntry = entry;
        entry.selected = true;

        if (entry.rootImage != null)
            entry.rootImage.color = selectedColor;
    }
}
