using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CatchResultUI : MonoBehaviour
{
    [Header("Photo")]
    [SerializeField] private RawImage photoPreview;

    [Header("Dalsu Info")]
    [SerializeField] private TMP_Text caughtText;
    [SerializeField] private Image dalsuIcon;

    private string currentTempPhotoPath;
    private Texture2D previewTexture;

    private DalsuData caughtDalsuData;


    public void Show(
        string photoPath,
        DalsuData data)
    {
        currentTempPhotoPath =
            photoPath;

        caughtDalsuData =
            data;


        LoadPreview(photoPath);

        UpdateDalsuInfo();


        gameObject.SetActive(true);
    }


    private void UpdateDalsuInfo()
    {
        if (caughtDalsuData == null)
        {
            if (caughtText != null)
            {
                caughtText.text =
                    "달수를 잡았다!";
            }

            return;
        }


        if (caughtText != null)
        {
            caughtText.text =
                $"{caughtDalsuData.dalsuName}을(를) 잡았다!";
        }


        if (dalsuIcon != null)
        {
            dalsuIcon.sprite =
                caughtDalsuData.icon;

            dalsuIcon.enabled =
                caughtDalsuData.icon != null;
        }
    }


    private void LoadPreview(
        string path)
    {
        if (
            string.IsNullOrEmpty(path)
            || !File.Exists(path))
        {
            Debug.LogError(
                "[DALSU] 결과 사진 파일이 없습니다."
            );

            return;
        }


        if (previewTexture != null)
        {
            Destroy(previewTexture);
        }


        byte[] bytes =
            File.ReadAllBytes(path);


        previewTexture =
            new Texture2D(2, 2);


        bool success =
            previewTexture.LoadImage(bytes);


        if (!success)
        {
            Debug.LogError(
                "[DALSU] 사진 Preview 로드 실패"
            );

            return;
        }


        if (photoPreview != null)
        {
            photoPreview.texture =
                previewTexture;
        }
    }


    public void OnSaveButton()
    {
        if (
            string.IsNullOrEmpty(
                currentTempPhotoPath))
        {
            return;
        }


        bool success =
            AndroidGallerySaver.SaveImage(
                currentTempPhotoPath
            );


        if (!success)
        {
            Debug.LogError(
                "[DALSU] 갤러리 저장 실패"
            );

            return;
        }


        DeleteTempPhoto();

        Close();
    }


    public void OnDiscardButton()
    {
        DeleteTempPhoto();

        Close();
    }


    private void DeleteTempPhoto()
    {
        if (
            !string.IsNullOrEmpty(
                currentTempPhotoPath)
            &&
            File.Exists(
                currentTempPhotoPath))
        {
            File.Delete(
                currentTempPhotoPath
            );
        }


        currentTempPhotoPath = null;
    }


    private void Close()
    {
        if (previewTexture != null)
        {
            Destroy(previewTexture);
            previewTexture = null;
        }


        if (photoPreview != null)
        {
            photoPreview.texture = null;
        }


        caughtDalsuData = null;

        gameObject.SetActive(false);
    }
}