using System.Collections;
using System.IO;
using UnityEngine;

public class DalsooCaptureManager : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera arCamera;

    [Header("UI")]
    [Tooltip("촬영 순간 사진에서 숨길 UI")]
    [SerializeField] private GameObject captureUI;

    [Tooltip("잡기 성공 후 띄울 UI. 아직 없으면 비워둬도 됨.")]
    [SerializeField] private CatchResultUI catchResultUI;


    [Header("Catch Condition")]
    [Range(0f, 0.4f)]
    [SerializeField] private float screenMargin = 0.08f;

    [SerializeField] private float maxCatchDistance = 5f;


    private bool isCapturing = false;

    // 방금 잡은 사진의 임시 경로
    private string tempPhotoPath;

    public string TempPhotoPath => tempPhotoPath;

    public bool HasPendingPhoto =>
        !string.IsNullOrEmpty(tempPhotoPath) &&
        File.Exists(tempPhotoPath);


    private void Start()
    {
        if (arCamera == null)
            arCamera = Camera.main;

        DeleteTempPhoto();
    }


    // ==============================
    // 촬영 버튼에서 호출
    // ==============================

    public void TakePhoto()
    {
        if (isCapturing)
            return;

        StartCoroutine(CaptureRoutine());
    }


    private IEnumerator CaptureRoutine()
    {
        isCapturing = true;

        if (arCamera == null)
            arCamera = Camera.main;


        DalsooCatchable dalsoo =
            DalsooCatchable.Current;


        if (dalsoo == null)
        {
            Debug.Log("현재 잡을 달수가 없습니다.");

            isCapturing = false;
            yield break;
        }


        // 촬영 버튼 등 숨기기
        if (captureUI != null)
            captureUI.SetActive(false);


        // UI가 사라진 상태로 실제 화면이 렌더링될 때까지 기다림
        yield return new WaitForEndOfFrame();


        // 촬영하는 바로 그 순간 잡기 조건 검사
        bool canCatch =
            IsDalsooCatchable(dalsoo);


        // ==================================
        // 실제 AR 화면 캡처
        // ==================================

        Texture2D capturedTexture =
            ScreenCapture.CaptureScreenshotAsTexture();


        // UI 다시 표시
        if (captureUI != null)
            captureUI.SetActive(true);


        // ==================================
        // 달수를 제대로 못 찍음
        // ==================================

        if (!canCatch)
        {
            Debug.Log("📷 달수를 제대로 찍지 못했습니다.");

            Destroy(capturedTexture);

            isCapturing = false;
            yield break;
        }


        // ==================================
        // 잡기 성공
        // ==================================

        Debug.Log("📸 달수 포착 성공!");


        // 이전 임시 사진 제거
        DeleteTempPhoto();


        // 임시 PNG 생성
        byte[] pngBytes =
            capturedTexture.EncodeToPNG();


        tempPhotoPath =
            Path.Combine(
                Application.temporaryCachePath,
                "last_dalsoo_capture.png"
            );


        File.WriteAllBytes(
            tempPhotoPath,
            pngBytes
        );


        Destroy(capturedTexture);


        Debug.Log(
            "임시 사진 저장 완료: " +
            tempPhotoPath
        );


        // ★ 사진 저장을 끝낸 뒤 달수를 잡는다.
        dalsoo.Catch();


        // 잡기 성공 UI 표시
        if (catchResultUI != null)
        {
            catchResultUI.Show(tempPhotoPath);
        }


        isCapturing = false;
    }


    // ==============================
    // 달수가 화면 안에 있는지 확인
    // ==============================

    private bool IsDalsooCatchable(
        DalsooCatchable dalsoo
    )
    {
        if (dalsoo == null || arCamera == null)
            return false;


        Vector3 dalsooPosition =
            dalsoo.transform.position;


        float distance =
            Vector3.Distance(
                arCamera.transform.position,
                dalsooPosition
            );


        if (distance > maxCatchDistance)
            return false;


        Vector3 viewport =
            arCamera.WorldToViewportPoint(
                dalsooPosition
            );


        // 카메라 뒤쪽
        if (viewport.z <= 0f)
            return false;


        // 화면 가장자리보다 조금 안쪽에 있어야 함
        bool insideScreen =
            viewport.x >= screenMargin &&
            viewport.x <= 1f - screenMargin &&
            viewport.y >= screenMargin &&
            viewport.y <= 1f - screenMargin;


        return insideScreen;
    }


    // ==============================
    // 임시 사진 삭제
    // ==============================

    public void DeleteTempPhoto()
    {
        if (!string.IsNullOrEmpty(tempPhotoPath))
        {
            if (File.Exists(tempPhotoPath))
                File.Delete(tempPhotoPath);
        }


        // 혹시 앱 실행 전에 만들어진 파일도 제거
        string defaultTempPath =
            Path.Combine(
                Application.temporaryCachePath,
                "last_dalsoo_capture.png"
            );


        if (File.Exists(defaultTempPath))
            File.Delete(defaultTempPath);


        tempPhotoPath = null;
    }
}