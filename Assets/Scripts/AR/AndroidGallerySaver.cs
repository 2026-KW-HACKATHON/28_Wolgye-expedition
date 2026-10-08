using System;
using System.IO;
using UnityEngine;

public static class AndroidGallerySaver
{
    public static bool SaveImage(string sourceFilePath)
    {
#if UNITY_ANDROID && !UNITY_EDITOR

        try
        {
            byte[] imageBytes =
                File.ReadAllBytes(sourceFilePath);


            using AndroidJavaClass unityPlayer =
                new AndroidJavaClass(
                    "com.unity3d.player.UnityPlayer"
                );


            using AndroidJavaObject activity =
                unityPlayer.GetStatic<AndroidJavaObject>(
                    "currentActivity"
                );


            using AndroidJavaObject resolver =
                activity.Call<AndroidJavaObject>(
                    "getContentResolver"
                );


            using AndroidJavaObject values =
                new AndroidJavaObject(
                    "android.content.ContentValues"
                );


            string fileName =
                "Dalsoo_" +
                DateTime.Now.ToString("yyyyMMdd_HHmmss") +
                ".png";


            values.Call(
                "put",
                "_display_name",
                fileName
            );

            values.Call(
                "put",
                "mime_type",
                "image/png"
            );

            values.Call(
                "put",
                "relative_path",
                "Pictures/Dalsoo"
            );

            using AndroidJavaObject pending1 = new AndroidJavaObject("java.lang.Integer", 1);
            values.Call("put", "is_pending", pending1);


            using AndroidJavaClass media =
                new AndroidJavaClass(
                    "android.provider.MediaStore$Images$Media"
                );


            using AndroidJavaObject externalUri =
                media.GetStatic<AndroidJavaObject>(
                    "EXTERNAL_CONTENT_URI"
                );


            using AndroidJavaObject imageUri =
                resolver.Call<AndroidJavaObject>(
                    "insert",
                    externalUri,
                    values
                );


            if (imageUri == null)
            {
                Debug.LogError(
                    "MediaStore insert 실패"
                );

                return false;
            }


            using AndroidJavaObject outputStream =
                resolver.Call<AndroidJavaObject>(
                    "openOutputStream",
                    imageUri
                );


            if (outputStream == null)
            {
                Debug.LogError(
                    "OutputStream 생성 실패"
                );

                return false;
            }


            outputStream.Call(
                "write",
                imageBytes
            );

            outputStream.Call("flush");
            outputStream.Call("close");


            // 저장 완료 처리
            values.Call("clear");

            using AndroidJavaObject pending0 = new AndroidJavaObject("java.lang.Integer", 0);
            values.Call("put", "is_pending", pending0);


            resolver.Call<int>(
                "update",
                imageUri,
                values,
                null,
                null
            );


            Debug.Log(
                "Pictures/Dalsoo/" +
                fileName +
                " 저장 완료"
            );


            return true;
        }

        catch (Exception e)
        {
            Debug.LogError(
                "갤러리 저장 오류: " +
                e
            );

            return false;
        }

#else

        Debug.Log(
            "Android 기기에서만 갤러리에 저장됩니다."
        );

        return false;

#endif
    }
}