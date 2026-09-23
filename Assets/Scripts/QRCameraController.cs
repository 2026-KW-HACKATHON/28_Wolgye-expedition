using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class QRCameraController : MonoBehaviour
{
    public RawImage cameraPreview;

    private WebCamTexture webCamTexture;

    IEnumerator Start()
    {
        // 카메라 사용 권한 요청
        yield return Application.RequestUserAuthorization(
            UserAuthorization.WebCam
        );

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Debug.Log("카메라 권한이 없습니다.");
            yield break;
        }

        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.Log("사용 가능한 카메라가 없습니다.");
            yield break;
        }

        // 보통 모바일에서는 첫 번째 또는 후면 카메라 검색
        string cameraName = devices[0].name;

        foreach (WebCamDevice device in devices)
        {
            if (!device.isFrontFacing)
            {
                cameraName = device.name;
                break;
            }
        }

        webCamTexture = new WebCamTexture(
            cameraName,
            1280,
            720,
            30
        );

        cameraPreview.texture = webCamTexture;
        webCamTexture.Play();
    }

    private void OnDestroy()
    {
        if (webCamTexture != null &&
            webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }
    }
}