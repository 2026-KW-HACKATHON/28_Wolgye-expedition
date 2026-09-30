using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReturnListManager : MonoBehaviour
{
    // 리스트에 추가된 항목 하나 (UI 엘리먼트 + 원본 데이터 + 선택 상태)
    private class Entry
    {
        public GameObject element;
        public OwnDalsuData data;
        public Image rootImage;
        public Color normalColor;
        public bool selected;
    }

    [SerializeField] private DalsuRoom_UIManager uiManager;

    [SerializeField] private GameObject listPrefab;
    [SerializeField] private Transform listParent;
    [SerializeField] private CameraTargetSwitcher cameraTargetSwitcher;
    [SerializeField] private CameraModeSwitcher cameraModeSwitcher;

    [Tooltip("data.json에서 실제로 데이터를 지우는 매니저")]
    [SerializeField] private DalsuSpawnManager spawnManager;

    [Tooltip("달수 리스트 화면. 삭제된 달수의 항목을 여기서도 같이 지운다.")]
    [SerializeField] private DalsuListManager dalsuListManager;

    [Tooltip("선택된 항목에 표시할 강조 색상")]
    [SerializeField] private Color selectedColor = new Color(0.6f, 0.8f, 1f, 1f);

    private readonly List<Entry> entries = new List<Entry>();

    public void AddReturnList(OwnDalsuData data)
    {
        GameObject element = Instantiate(listPrefab, listParent);

        element.transform.Find("DalsuImage").GetComponent<Image>().sprite = data.dalsuData.icon;
        element.GetComponentInChildren<TextMeshProUGUI>().text = data.dalsuData.dalsuName;

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
        if (button == null) button = element.GetComponentInChildren<Button>();

        if (button != null)
            button.onClick.AddListener(() => ToggleSelect(entry));
    }

    // 항목 클릭: 선택/선택 해제를 토글한다. (여러 개 동시 선택 가능)
    private void ToggleSelect(Entry entry)
    {
        entry.selected = !entry.selected;

        if (entry.rootImage != null)
            entry.rootImage.color = entry.selected ? selectedColor : entry.normalColor;
    }

    // 확인 버튼 OnClick: 선택된 달수들을 data.json에서 삭제하고 화면/씬에서 제거한다.
    public void ConfirmReturn()
    {
        if (uiManager == null) return;

        bool isSelected = false;
        foreach(Entry entry in entries)
        {
            if (entry.selected)
            {
                isSelected = true;
                break;
            }
        }
        if (!isSelected) return;

        uiManager.OpenReturnComplete();

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry entry = entries[i];
            if (!entry.selected) continue;

            if (spawnManager != null && entry.data != null)
                spawnManager.RemoveOwnDalsu(entry.data.ownData);

            if (dalsuListManager != null)
                dalsuListManager.RemoveEntry(entry.data); // 달수 리스트 화면의 항목도 같이 제거

            // 카메라 타겟 목록에서 자리를 비워, 남은 항목들의 listIndex가 밀리지 않게 한다.
            if (cameraTargetSwitcher != null && entry.data != null)
            {
                int index = entry.data.listIndex;
                if (index >= 0 && index < cameraTargetSwitcher.targets.Count)
                    cameraTargetSwitcher.targets[index] = null;
            }

            if (entry.data != null)
                Destroy(entry.data.gameObject); // 방(3D 씬)의 달수 캐릭터 제거

            Destroy(entry.element); // 리스트 UI 항목 제거
            entries.RemoveAt(i);
        }

        if (cameraModeSwitcher != null)
            cameraModeSwitcher.ShowOverview();
    }
}
