using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DalsooCaptureManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private GameObject captureUI;

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

        bool catchSuccess =
            IsCatchSuccess(catchable);

        // Destroy 전에 데이터 보관
        DalsuData caughtData =
            catchable.Data;

        if (captureUI != null)
        {
            captureUI.SetActive(false);
        }

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
        // 사진 저장
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
        // Context에 결과 정보 저장
        // ======================================

        DalsuSceneContext.SelectedDalsuId =
            caughtData.id;

        DalsuSceneContext.CapturedPhotoPath =
            tempPhotoPath;

        // ======================================
        // 달수 제거
        // ======================================

        catchable.Catch();

        // ======================================
        // Result 씬 이동
        // ======================================

        SceneManager.LoadScene("Reward");

        isCapturing = false;
    }

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

        if (viewport.z <= 0f)
        {
            return false;
        }

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


    //======================================
    // 이전 버튼
    //======================================
    public void ClickBackBtn()
    {
        SceneLoader.Instance.Load("Map", waitForSceneReady: true);
    }
}