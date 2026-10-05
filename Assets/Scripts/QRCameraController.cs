using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using ZXing;
using ZXing.Common;

public class QRCameraController : MonoBehaviour
{
    public RawImage cameraPreview;

    public StampManager stampManager;

    [Header("Scan Settings")]
    [Tooltip("스캔 간격(초)")]
    public float scanInterval = 0.3f;

    private WebCamTexture webCamTexture;
    private BarcodeReader barcodeReader;

    private bool isScanning = false;   // 카메라/ZXing 준비가 끝나면 true
    private float scanTimer = 0f;


    IEnumerator Start()
    {
        // =========================
        // 1. 카메라 권한 (사용자가 누를 때까지 최대 15초 대기)
        // =========================

        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);

            float permTimer = 0f;
            while (!Permission.HasUserAuthorizedPermission(Permission.Camera) && permTimer < 15f)
            {
                permTimer += Time.deltaTime;
                yield return null;
            }
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Debug.LogError("카메라 권한이 없습니다.");
            yield break;
        }

        Debug.Log("카메라 권한 OK");


        // =========================
        // 2. 카메라 찾기 (후면 우선)
        // =========================

        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices == null || devices.Length == 0)
        {
            Debug.LogError("사용 가능한 카메라가 없습니다.");
            yield break;
        }

        Debug.Log("카메라 개수: " + devices.Length);

        string cameraName = devices[0].name;

        foreach (WebCamDevice device in devices)
        {
            Debug.Log("카메라: " + device.name + " / 전면: " + device.isFrontFacing);

            if (!device.isFrontFacing)
            {
                cameraName = device.name;
                break;
            }
        }

        Debug.Log("선택된 카메라: " + cameraName);


        // =========================
        // 3. RawImage 확인
        // =========================

        if (cameraPreview == null)
        {
            Debug.LogError("Camera Preview가 연결되지 않았습니다.");
            yield break;
        }


        // =========================
        // 4. 카메라 실행 (해상도 요청: 1280x720)
        // =========================

        webCamTexture = new WebCamTexture(cameraName, 1280, 720);
        cameraPreview.texture = webCamTexture;
        webCamTexture.Play();

        Debug.Log("카메라 Play");


        // =========================
        // 5. 카메라 초기화 기다리기
        // =========================

        float timer = 0f;

        while (webCamTexture.width <= 16 && timer < 10f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (webCamTexture.width <= 16)
        {
            Debug.LogError("카메라 초기화 실패");
            yield break;
        }

        Debug.Log("카메라 해상도: " + webCamTexture.width + " x " + webCamTexture.height);


        // =========================
        // 6. 카메라 화면 회전
        // =========================

        cameraPreview.rectTransform.localEulerAngles =
            new Vector3(0, 0, -webCamTexture.videoRotationAngle);


        // =========================
        // 7. 화면 반전
        // =========================

        cameraPreview.rectTransform.localScale =
            webCamTexture.videoVerticallyMirrored
                ? new Vector3(1, -1, 1)
                : new Vector3(1, 1, 1);


        // =========================
        // 8. 화면 비율 맞추기
        // =========================

        AspectRatioFitter aspectFitter = cameraPreview.GetComponent<AspectRatioFitter>();

        if (aspectFitter == null)
        {
            aspectFitter = cameraPreview.gameObject.AddComponent<AspectRatioFitter>();
        }

        aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspectFitter.aspectRatio = (float)webCamTexture.width / webCamTexture.height;

        Debug.Log("카메라 화면 비율: " + aspectFitter.aspectRatio);


        // =========================
        // 9. ZXing 초기화 (인식률 향상 옵션)
        // =========================

        barcodeReader = new BarcodeReader
        {
            AutoRotate = true,      // 90도 누운 QR도 인식
            TryInverted = true,     // 색 반전 QR도 인식
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
            }
        };

        Debug.Log("ZXing 초기화 완료");


        // =========================
        // 10. QR 스캔 시작 (Update에서 처리)
        // =========================

        scanTimer = 0f;
        isScanning = true;
        Debug.Log("QR 스캔 시작");
    }


    void Update()
    {
        if (!isScanning) return;
        if (webCamTexture == null || !webCamTexture.isPlaying) return;
        if (webCamTexture.width <= 16) return;

        // 카메라 프레임이 새로 갱신됐을 때만 (검은 빈 프레임 방지)
        if (!webCamTexture.didUpdateThisFrame) return;

        // 일정 간격으로만 스캔 (성능)
        scanTimer += Time.deltaTime;
        if (scanTimer < scanInterval) return;
        scanTimer = 0f;

        try
        {
            Color32[] pixels = webCamTexture.GetPixels32();

            Result result = barcodeReader.Decode(
                pixels,
                webCamTexture.width,
                webCamTexture.height
            );

            if (result != null)
            {
                Debug.Log("====================");
                Debug.Log("QR 인식 성공!");
                Debug.Log("QR 내용: " + result.Text);
                Debug.Log("====================");

                QRDetected(result.Text);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("QR 스캔 오류: " + e.Message);
        }
    }


    // =========================
    // QR 인식 후 행동
    // =========================

    void QRDetected(string qrData)
    {
        isScanning = false;

        Debug.Log("QR 행동 실행: " + qrData);

        // TODO: 여기에 QR 내용(qrData)으로 할 행동 추가
        stampManager.AddStamp();

        // 카메라 종료
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            //webCamTexture.Stop();
        }

        // 카메라 화면 종료
        if (cameraPreview != null)
        {
            //cameraPreview.gameObject.SetActive(false);
        }

        Debug.Log("카메라 화면 종료");
    }


    // =========================
    // 종료
    // =========================

    private void OnDestroy()
    {
        isScanning = false;

        if (webCamTexture != null)
        {
            if (webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }

            Destroy(webCamTexture);
        }
    }
}
