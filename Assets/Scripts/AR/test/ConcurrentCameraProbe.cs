using System;
using System.Text;
using UnityEngine;

// Attach to an empty GameObject and run on an Android device.
// Queries capabilities only: does not open cameras or prove ARCore coexistence.
public sealed class ConcurrentCameraProbe : MonoBehaviour
{
    private string report = "Press Run probe.";
    private Vector2 scroll;

    private void Start() => RunProbe();

    public void RunProbe()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var text = new StringBuilder();
        try
        {
            using var version = new AndroidJavaClass("android.os.Build$VERSION");
            int sdk = version.GetStatic<int>("SDK_INT");
            text.AppendLine($"Device: {SystemInfo.deviceModel}\nAndroid API: {sdk}");
            if (sdk < 30)
            {
                report = text + "Concurrent camera query requires API 30+.";
                return;
            }

            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            using var manager = activity.Call<AndroidJavaObject>("getSystemService", "camera");
            using var characteristicsClass = new AndroidJavaClass("android.hardware.camera2.CameraCharacteristics");
            using var facingKey = characteristicsClass.GetStatic<AndroidJavaObject>("LENS_FACING");

            string[] ids = manager.Call<string[]>("getCameraIdList");
            text.AppendLine("\nAvailable cameras:");
            foreach (string id in ids)
                text.AppendLine($"  {id}: {Facing(manager, facingKey, id)}");

            using var sets = manager.Call<AndroidJavaObject>("getConcurrentCameraIds");
            using var groups = sets.Call<AndroidJavaObject>("iterator");
            int count = 0;
            bool frontBack = false;
            text.AppendLine("\nAdvertised concurrent groups:");
            while (groups.Call<bool>("hasNext"))
            {
                using var group = groups.Call<AndroidJavaObject>("next");
                using var cameras = group.Call<AndroidJavaObject>("iterator");
                bool front = false, back = false;
                text.Append($"  Group {++count}: ");
                while (cameras.Call<bool>("hasNext"))
                {
                    using var idObject = cameras.Call<AndroidJavaObject>("next");
                    string id = idObject.Call<string>("toString");
                    string facing = Facing(manager, facingKey, id);
                    front |= facing == "FRONT";
                    back |= facing == "BACK";
                    text.Append($"{id}({facing}) ");
                }
                text.AppendLine();
                frontBack |= front && back;
            }

            if (count == 0) text.AppendLine("  NONE");
            text.AppendLine($"\nFRONT + BACK advertised: {frontBack}");
            text.AppendLine("\nThis is a capability query, NOT an ARCore + front-camera test.");
            text.AppendLine("Camera IDs are device-specific. Do not hardcode 0/1.");
            report = text.ToString();
        }
        catch (Exception e)
        {
            report = text + "\nProbe error:\n" + e;
        }
#else
        report = "Build and run on your Galaxy S25. Editor cannot query Android cameras.";
#endif
        Debug.Log("[ConcurrentCameraProbe]\n" + report);
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static string Facing(AndroidJavaObject manager, AndroidJavaObject key, string id)
    {
        using var info = manager.Call<AndroidJavaObject>("getCameraCharacteristics", id);
        using var value = info.Call<AndroidJavaObject>("get", key);
        if (value == null) return "UNKNOWN";
        return value.Call<int>("intValue") switch
        {
            0 => "FRONT",
            1 => "BACK",
            2 => "EXTERNAL",
            _ => "UNKNOWN"
        };
    }
#endif

    private void OnGUI()
    {
        float scale = Mathf.Max(1f, Screen.width / 600f);
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        GUILayout.BeginArea(new Rect(12, 12, Screen.width / scale - 24, Screen.height / scale - 24), GUI.skin.box);
        if (GUILayout.Button("Run probe", GUILayout.Height(44))) RunProbe();
        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.Label(report, new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true });
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        GUI.matrix = previous;
    }
}
