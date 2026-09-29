using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DalsuListManager : MonoBehaviour
{
    [SerializeField] private GameObject listPrefab;
    [SerializeField] private Transform listParent;
    [SerializeField] private CameraTargetSwitcher cameraTargetSwitcher;
    [SerializeField] private CameraModeSwitcher cameraModeSwitcher;

    // 각 소유 달수(data)와 그 UI 엘리먼트를 추적한다. (돌려보내기 등에서 항목을 제거할 때 사용)
    private readonly Dictionary<OwnDalsuData, GameObject> entries = new Dictionary<OwnDalsuData, GameObject>();

    public void AddDalsuList(OwnDalsuData data)
    {
        GameObject element = Instantiate(listPrefab, listParent);

        element.transform.Find("DalsuImage").GetComponent<Image>().sprite = data.dalsuData.icon;
        element.GetComponentInChildren<TextMeshProUGUI>().text = data.dalsuData.dalsuName;

        entries[data] = element;

        // 클릭 시 Follow 카메라로 전환한 뒤, 해당 달수의 listIndex로 타겟 전환
        Button button = element.GetComponent<Button>();
        if (button == null) button = element.GetComponentInChildren<Button>();

        if (button != null)
        {
            int index = data.listIndex; // 람다 캡처용 지역 변수
            button.onClick.AddListener(() =>
            {
                cameraModeSwitcher.ShowFollow();
                cameraTargetSwitcher.FocusOnIndex(index);
            });
        }
    }

    // 돌려보내기 등에서 데이터가 삭제됐을 때, 이 리스트에 있는 해당 항목도 제거한다.
    public void RemoveEntry(OwnDalsuData data)
    {
        if (data == null) return;

        if (entries.TryGetValue(data, out GameObject element))
        {
            if (element != null)
                Destroy(element);

            entries.Remove(data);
        }
    }
}
