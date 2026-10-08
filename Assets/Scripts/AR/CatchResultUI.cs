using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CatchResultUI : MonoBehaviour
{
    [Header("Dalsu Data")]
    [SerializeField]
    private DalsuDatabase dalsuDatabase;

    [Header("Photo")]
    [SerializeField]
    private RawImage photoPreview;

    [Header("Dalsu Info")]
    [SerializeField]
    private TMP_Text caughtText;

    [SerializeField]
    private Image dalsuIcon;

    [SerializeField] private Transform dalsuParent;

    private string currentTempPhotoPath;
    private Texture2D previewTexture;
    private bool photoSaved;

    private DalsuData caughtDalsuData;

    private void Start()
    {
        LoadResult();
        SpawnDalsuPrefab();
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
        if (dalsuDatabase == null)
            return null;

        var dalsuData =
            dalsuDatabase.GetById(id);

        if (dalsuData == null)
        {
            Debug.LogError(
                $"[DALSU] DalsuData를 찾을 수 없습니다. id = {id}"
                );
        }


        return dalsuData;
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
                caughtDalsuData.dalsuName;
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
        if (photoSaved)
        {
            ShowSavedMessage();
            return;
        }

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

        photoSaved = true;
        ShowSavedMessage();
    }

    private void ShowSavedMessage()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        UnityEngine.Android.AndroidApplication.InvokeOnUIThread(() =>
        {
            using var toastClass = new AndroidJavaClass("android.widget.Toast");
            using var toast = toastClass.CallStatic<AndroidJavaObject>(
                "makeText", UnityEngine.Android.AndroidApplication.currentActivity,
                "사진이 저장되었습니다", toastClass.GetStatic<int>("LENGTH_SHORT"));
            toast.Call("show");
        });
#endif
    }

    private void OnDestroy()
    {
        // Keep the photo available for sharing until the result screen is left.
        DeleteTempPhoto();
        if (previewTexture != null)
            Destroy(previewTexture);
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

    //============================================

    public void ClickOkBtn()
    {
        SceneLoader.Instance.Load("Map", waitForSceneReady: true);
    }

    private void SpawnDalsuPrefab()
    {
        if (caughtDalsuData == null) return;
        GameObject dalsu = Instantiate(caughtDalsuData.prefab, dalsuParent);
    }


    public void OnShareButton()
    {
        if (string.IsNullOrEmpty(currentTempPhotoPath)
            || !File.Exists(currentTempPhotoPath))
        {
            Debug.LogError("[DALSU] 공유할 사진이 없습니다.");
            return;
        }

        AndroidPhotoShare.ShareImage(currentTempPhotoPath);
    }

}
