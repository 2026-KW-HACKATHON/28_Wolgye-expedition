using System.Collections;
using System.IO;
using UnityEngine;

public class DalsooCaptureManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera arCamera;

    [SerializeField] private GameObject captureUI;

    [SerializeField] private CatchResultUI catchResultUI;


    [Header("Catch Condition")]
    [Range(0f, 0.4f)]
    [SerializeField] private float screenMargin = 0.15f;

    [SerializeField] private float maxCatchDistance = 5f;


    private bool isCapturing = false;


    private void Awake()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }
    }


    public void Capture()
    {
        if (isCapturing)
            return;

        StartCoroutine(
            CaptureRoutine()
        );
    }


    private IEnumerator CaptureRoutine()
    {
        isCapturing = true;


        DalsooCatchable catchable =
            DalsooCatchable.Current;


        if (catchable == null)
        {
            Debug.Log(
                "[DALSU] 현재 잡을 수 있는 달수가 없습니다."
            );

            isCapturing = false;
            yield break;
        }


        // 달수가 화면 중앙 조건을 만족하는지
        bool catchSuccess =
            IsCatchSuccess(catchable);


        // 잡힌 달수의 데이터는
        // Destroy 전에 미리 보관
        DalsuData caughtData =
            catchable.Data;


        // 촬영 버튼 등 UI 숨기기
        if (captureUI != null)
        {
            captureUI.SetActive(false);
        }


        // UI가 실제 화면에서 사라진 뒤 캡처
        yield return new WaitForEndOfFrame();


        Texture2D screenshot =
            ScreenCapture.CaptureScreenshotAsTexture();


        // ======================================
        // 실패
        // ======================================

        if (!catchSuccess)
        {
            Debug.Log(
                "[DALSU] 사진 촬영했지만 잡기 실패"
            );


            if (screenshot != null)
            {
                Destroy(screenshot);
            }


            if (captureUI != null)
            {
                captureUI.SetActive(true);
            }


            isCapturing = false;

            yield break;
        }


        // ======================================
        // 성공
        // ======================================

        string tempPhotoPath =
            SaveTemporaryScreenshot(
                screenshot
            );


        if (screenshot != null)
        {
            Destroy(screenshot);
        }


        if (string.IsNullOrEmpty(tempPhotoPath))
        {
            Debug.LogError(
                "[DALSU] 임시 사진 저장 실패"
            );


            if (captureUI != null)
            {
                captureUI.SetActive(true);
            }


            isCapturing = false;

            yield break;
        }


        Debug.Log(
            caughtData != null
                ? $"[DALSU] 사진 잡기 성공 / {caughtData.id}"
                : "[DALSU] 사진 잡기 성공"
        );


        // ======================================
        // 사진 저장 후에 달수 제거
        // ======================================

        catchable.Catch();


        // ======================================
        // 결과 UI
        // ======================================

        if (catchResultUI != null)
        {
            catchResultUI.Show(
                tempPhotoPath,
                caughtData
            );
        }


        isCapturing = false;
    }


    // ==================================================
    // 잡기 판정
    // ==================================================

    private bool IsCatchSuccess(
        DalsooCatchable catchable)
    {
        if (
            catchable == null
            || arCamera == null)
        {
            return false;
        }


        Vector3 targetPosition =
            catchable.transform.position;


        Vector3 viewport =
            arCamera.WorldToViewportPoint(
                targetPosition
            );


        // 카메라 뒤쪽
        if (viewport.z <= 0f)
        {
            return false;
        }


        // 화면 안쪽 판정
        bool insideScreen =
            viewport.x >= screenMargin
            &&
            viewport.x <= 1f - screenMargin
            &&
            viewport.y >= screenMargin
            &&
            viewport.y <= 1f - screenMargin;


        if (!insideScreen)
        {
            return false;
        }


        // 거리 판정
        float distance =
            Vector3.Distance(
                arCamera.transform.position,
                targetPosition
            );


        if (distance > maxCatchDistance)
        {
            return false;
        }


        return true;
    }


    // ==================================================
    // 임시 사진 저장
    // ==================================================

    private string SaveTemporaryScreenshot(
        Texture2D screenshot)
    {
        if (screenshot == null)
        {
            return null;
        }


        try
        {
            byte[] pngBytes =
                screenshot.EncodeToPNG();


            string path =
                Path.Combine(
                    Application.temporaryCachePath,
                    "last_dalsoo_capture.png"
                );


            File.WriteAllBytes(
                path,
                pngBytes
            );


            Debug.Log(
                $"[DALSU] 임시 사진 저장 완료: {path}"
            );


            return path;
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"[DALSU] 임시 사진 저장 실패: {e.Message}"
            );

            return null;
        }
    }
}