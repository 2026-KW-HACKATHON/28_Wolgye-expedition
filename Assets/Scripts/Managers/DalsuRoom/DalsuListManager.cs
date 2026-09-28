using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DalsuListManager : MonoBehaviour
{
    [SerializeField] private GameObject listPrefab;
    [SerializeField] private Transform listParent;
    [SerializeField] private CameraTargetSwitcher cameraTargetSwitcher;
    [SerializeField] private CameraModeSwitcher cameraModeSwitcher;

    public void AddDalsuList(OwnDalsuData data)
    {
        GameObject element = Instantiate(listPrefab, listParent);

        element.transform.Find("DalsuImage").GetComponent<Image>().sprite = data.dalsuData.icon;
        element.GetComponentInChildren<TextMeshProUGUI>().text = data.dalsuData.dalsuName;

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
}
