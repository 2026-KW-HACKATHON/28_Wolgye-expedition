using UnityEngine;
using UnityEngine.UI;

public class StampManager : MonoBehaviour
{
    [Header("스탬프 UI")]
    public Image[] stampImages;          // 스탬프 이미지 3개
    public Sprite emptyStampSprite;      // 빈 스탬프
    public Sprite stampedSprite;         // 찍힌 스탬프

    [Header("쿠폰 UI")]
    public Transform couponContent;      // Scroll View의 Content
    public GameObject couponPrefab;      // 쿠폰 UI 프리팹

    private const string STAMP_KEY = "StampCount";
    private const string COUPON_KEY = "CouponCount";

    private const int MAX_STAMP = 3;


    private void Start()
    {
        UpdateStampUI();
        LoadCoupons();
    }


    // =========================
    // 스탬프 +1
    // =========================

    public void AddStamp()
    {
        int stampCount = PlayerPrefs.GetInt(STAMP_KEY, 0);

        stampCount++;

        Debug.Log("스탬프 +1 / 현재: " + stampCount);


        // =========================
        // 3개 모았으면 쿠폰 발급
        // =========================

        if (stampCount >= MAX_STAMP)
        {
            IssueCoupon();

            // 다시 0으로 초기화
            stampCount = 0;

            Debug.Log("스탬프 3개 완성 → 0으로 초기화");
        }


        PlayerPrefs.SetInt(STAMP_KEY, stampCount);
        PlayerPrefs.Save();

        UpdateStampUI();
    }


    // =========================
    // 쿠폰 발급
    // =========================

    private void IssueCoupon()
    {
        int couponCount = PlayerPrefs.GetInt(COUPON_KEY, 0);

        couponCount++;

        PlayerPrefs.SetInt(COUPON_KEY, couponCount);
        PlayerPrefs.Save();

        // Scroll View에 쿠폰 하나 생성
        CreateCoupon();

        Debug.Log("쿠폰 발급!");
        Debug.Log("현재 쿠폰 개수: " + couponCount);
    }


    // =========================
    // 쿠폰 UI 생성
    // =========================

    private void CreateCoupon()
    {
        if (couponContent == null)
        {
            Debug.LogError("Coupon Content가 연결되지 않았습니다.");
            return;
        }

        if (couponPrefab == null)
        {
            Debug.LogError("Coupon Prefab이 연결되지 않았습니다.");
            return;
        }

        Instantiate(couponPrefab, couponContent);

        Debug.Log("쿠폰 UI 생성 완료");
    }


    // =========================
    // 저장된 쿠폰 불러오기
    // =========================

    private void LoadCoupons()
    {
        int couponCount = PlayerPrefs.GetInt(COUPON_KEY, 0);

        for (int i = 0; i < couponCount; i++)
        {
            CreateCoupon();
        }

        Debug.Log("저장된 쿠폰: " + couponCount + "개");
    }


    // =========================
    // 스탬프 UI 갱신
    // =========================

    private void UpdateStampUI()
    {
        int stampCount = PlayerPrefs.GetInt(STAMP_KEY, 0);

        for (int i = 0; i < stampImages.Length; i++)
        {
            if (i < stampCount)
            {
                stampImages[i].sprite = stampedSprite;
            }
            else
            {
                stampImages[i].sprite = emptyStampSprite;
            }
        }
    }


    // =========================
    // 현재 스탬프 개수
    // =========================

    public int GetStampCount()
    {
        return PlayerPrefs.GetInt(STAMP_KEY, 0);
    }


    // =========================
    // 현재 쿠폰 개수
    // =========================

    public int GetCouponCount()
    {
        return PlayerPrefs.GetInt(COUPON_KEY, 0);
    }


    // =========================
    // 테스트용 전체 초기화
    // =========================

    public void ResetData()
    {
        PlayerPrefs.SetInt(STAMP_KEY, 0);
        PlayerPrefs.SetInt(COUPON_KEY, 0);
        PlayerPrefs.Save();

        UpdateStampUI();

        // 생성된 쿠폰 UI 제거
        if (couponContent != null)
        {
            foreach (Transform child in couponContent)
            {
                Destroy(child.gameObject);
            }
        }

        Debug.Log("스탬프 / 쿠폰 전체 초기화");
    }
}