using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CatchResultUI : MonoBehaviour
{
    [Header("Dalsu Data")]
    [SerializeField]
    private DalsuData[] dalsuDatas;

    [Header("Photo")]
    [SerializeField]
    private RawImage photoPreview;

    [Header("Dalsu Info")]
    [SerializeField]
    private TMP_Text caughtText;

    [SerializeField]
    private Image dalsuIcon;

    private string currentTempPhotoPath;
    private Texture2D previewTexture;

    private DalsuData caughtDalsuData;

    private void Start()
    {
        LoadResult();
    }

    private void LoadResult()
    {
        string dalsuId =
            DalsuSceneContext.SelectedDalsuId;

        string photoPath =
            DalsuSceneContext.CapturedPhotoPath;

        caughtDalsuData =
            FindDalsuData(dalsuId);

        DalsuSaveManager.AcquireDalsu(caughtDalsuData);

        currentTempPhotoPath =
            photoPath;

        LoadPreview(photoPath);
        UpdateDalsuInfo();
    }

    private DalsuData FindDalsuData(string id)
    {
        if (dalsuDatas == null)
            return null;

        foreach (DalsuData data in dalsuDatas)
        {
            if (data == null)
                continue;

            if (data.id == id)
            {
                return data;
            }
        }

        Debug.LogError(
            $"[DALSU] DalsuData를 찾을 수 없습니다. id = {id}"
        );

        return null;
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

        // Context도 비워준다.
        DalsuSceneContext.Clear();

        gameObject.SetActive(false);
    }

    public void ClickOkBtn()
    {
        SceneManager.LoadScene("Map");
    }
}