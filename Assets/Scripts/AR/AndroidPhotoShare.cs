
using System;
using System.IO;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public static class AndroidPhotoShare
{
    public static void ShareImage(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) ||
            !File.Exists(imagePath))
        {
            Debug.LogError("[DALSU] 공유할 사진이 없습니다.");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR

        AndroidApplication.InvokeOnUIThread(() =>
        {
            try
            {
                AndroidJavaObject activity =
                    AndroidApplication.currentActivity;

                if (activity == null)
                {
                    Debug.LogError("[DALSU] Android Activity가 없습니다.");
                    return;
                }

                string authority =
                    activity.Call<string>("getPackageName")
                    + ".dalsoo.fileprovider";

                // Keep the shared copy even when Save/Discard deletes the original.
                using var cache = activity.Call<AndroidJavaObject>("getCacheDir");
                string directory = Path.Combine(cache.Call<string>("getAbsolutePath"), "dalsoo-share");
                Directory.CreateDirectory(directory);
                foreach (string previous in Directory.GetFiles(directory, "*.png"))
                {
                    if (File.GetLastWriteTimeUtc(previous) < DateTime.UtcNow.AddDays(-7))
                        File.Delete(previous);
                }
                string sharedPath = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".png");
                File.Copy(imagePath, sharedPath);

                using (AndroidJavaClass provider =
                    new AndroidJavaClass(
                        "androidx.core.content.FileProvider"))
                using (AndroidJavaClass intentClass =
                    new AndroidJavaClass("android.content.Intent"))
                using (AndroidJavaObject file =
                    new AndroidJavaObject("java.io.File", sharedPath))
                {
                    using (AndroidJavaObject uri =
                        provider.CallStatic<AndroidJavaObject>(
                            "getUriForFile",
                            activity,
                            authority,
                            file))
                    using (AndroidJavaObject intent =
                        new AndroidJavaObject(
                            "android.content.Intent",
                            "android.intent.action.SEND"))
                    {
                        intent.Call<AndroidJavaObject>(
                            "setType", "image/png");

                        intent.Call<AndroidJavaObject>(
                            "putExtra",
                            "android.intent.extra.STREAM",
                            uri);

                        intent.Call<AndroidJavaObject>(
                            "addFlags",
                            intentClass.GetStatic<int>(
                                "FLAG_GRANT_READ_URI_PERMISSION"));

                        using (var clipDataClass = new AndroidJavaClass("android.content.ClipData"))
                        using (var clipData = clipDataClass.CallStatic<AndroidJavaObject>(
                            "newRawUri", "Dalsu photo", uri))
                        {
                            intent.Call("setClipData", clipData);
                        }

                        using (AndroidJavaObject chooser =
                            intentClass.CallStatic<AndroidJavaObject>(
                                "createChooser",
                                intent,
                                "달수 사진 공유하기"))
                        {
                            activity.Call("startActivity", chooser);
                        }

                        Debug.Log("[DALSU] 공유창 열기 요청 완료");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[DALSU] 공유 실패: " + e);
            }
        });

#else
        Debug.Log("[DALSU] 공유 기능은 Android 기기에서 실행됩니다.");
#endif
    }
}
