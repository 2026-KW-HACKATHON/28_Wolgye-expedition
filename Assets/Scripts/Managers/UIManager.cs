using UnityEngine;
using UnityEngine.SceneManagement;
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Panels")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject collectionPanel;
    [SerializeField] private GameObject couponPanel;
    [SerializeField] private GameObject couponDetailPanel;
    [SerializeField] private GameObject qrScanPanel;

    [Header("Scenes")]
    [SerializeField] private string roomScene = "Room";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        shopPanel.SetActive(false);
        collectionPanel.SetActive(false);
        couponPanel.SetActive(false);
        couponDetailPanel.SetActive(false);
        qrScanPanel.SetActive(false);

        if (DalsuSceneContext.ShowStampOnMap)
        {
            couponPanel.SetActive(true);
            couponDetailPanel.SetActive(true);
            DalsuSceneContext.ShowStampOnMap = false;   // 한 번만 켜지도록 소비
        }
    }

    // =========================
    // 화면 열기
    // =========================

    public void ShowShop()
    {
        shopPanel.SetActive(true);
        shopPanel.transform.SetAsLastSibling();
    }

    public void ShowCollection()
    {
        collectionPanel.SetActive(true);
        collectionPanel.transform.SetAsLastSibling();
    }

    public void ShowCoupon()
    {
        couponPanel.SetActive(true);
        couponPanel.transform.SetAsLastSibling();
    }

    public void ShowCouponDetail()
    {
        couponDetailPanel.SetActive(true);
        couponDetailPanel.transform.SetAsLastSibling();
    }

    public void ShowQRScan()
    {
        qrScanPanel.SetActive(true);
        qrScanPanel.transform.SetAsLastSibling();
    }

    // =========================
    // 뒤로가기
    // =========================
    public void CloseShop()
    {
        shopPanel.SetActive(false);
    }

    public void CloseCollection()
    {
        collectionPanel.SetActive(false);
    }

    public void CloseCoupon()
    {
        couponPanel.SetActive(false);
    }

    public void CloseCouponDetail()
    {
        couponDetailPanel.SetActive(false);
    }

    public void CloseQRScan()
    {
        qrScanPanel.SetActive(false);
    }

    // =========================
    // 씬 전환
    // =========================

    public void ShowRoom()
    {
        SceneManager.LoadScene(roomScene);
    }
}