using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class CatchResultUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage photoPreview;

    private string currentTempPhotoPath;
    private Texture2D previewTexture;


    public void Show(string tempPhotoPath)
    {
        currentTempPhotoPath = tempPhotoPath;

        LoadPreview();

        gameObject.SetActive(true);
    }


    private void LoadPreview()
    {
        if (string.IsNullOrEmpty(currentTempPhotoPath))
            return;

        if (!File.Exists(currentTempPhotoPath))
            return;


        byte[] imageBytes =
            File.ReadAllBytes(currentTempPhotoPath);


        if (previewTexture != null)
        {
            Destroy(previewTexture);
        }


        previewTexture =
            new Texture2D(2, 2);

        previewTexture.LoadImage(imageBytes);


        if (photoPreview != null)
        {
            photoPreview.texture = previewTexture;
        }
    }


    // ============================
    // 저장 버튼
    // ============================

    public void OnSaveButton()
    {
        if (string.IsNullOrEmpty(currentTempPhotoPath))
            return;

        if (!File.Exists(currentTempPhotoPath))
            return;


        bool success =
            AndroidGallerySaver.SaveImage(
                currentTempPhotoPath
            );


        if (success)
        {
            Debug.Log("사진을 갤러리에 저장했습니다.");

            DeleteTempPhoto();
            CloseUI();
        }
        else
        {
            Debug.LogError("사진 저장 실패");
        }
    }


    // ============================
    // 안 함 버튼
    // ============================

    public void OnDiscardButton()
    {
        Debug.Log("사진을 저장하지 않습니다.");

        DeleteTempPhoto();

        CloseUI();
    }


    private void DeleteTempPhoto()
    {
        if (!string.IsNullOrEmpty(currentTempPhotoPath))
        {
            if (File.Exists(currentTempPhotoPath))
            {
                File.Delete(currentTempPhotoPath);
            }
        }

        currentTempPhotoPath = null;
    }


    private void CloseUI()
    {
        if (photoPreview != null)
            photoPreview.texture = null;


        if (previewTexture != null)
        {
            Destroy(previewTexture);
            previewTexture = null;
        }


        gameObject.SetActive(false);
    }
}