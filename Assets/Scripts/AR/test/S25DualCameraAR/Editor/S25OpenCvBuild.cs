#if UNITY_EDITOR && UNITY_ANDROID
using System.IO;
using UnityEditor.Android;
using UnityEditor.Build;
// Adds the official Android artifact to generated Gradle output only.
public sealed class S25OpenCvBuild : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1000;
    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string file=Path.Combine(path,"build.gradle");
        if(!File.Exists(file)) throw new BuildFailedException("S25: generated unityLibrary/build.gradle not found.");
        string text=File.ReadAllText(file);
        if(!text.Contains("org.opencv:opencv:4.12.0"))
            File.AppendAllText(file,"\n// S25 front-map prototype\ndependencies { implementation 'org.opencv:opencv:4.12.0' }\n");
        // Native merging ultimately happens in launcher, so configure BOTH modules.
        string launcher=Path.GetFullPath(Path.Combine(path,"..","launcher","build.gradle"));
        if(!File.Exists(launcher))
            throw new BuildFailedException("S25: launcher/build.gradle not found for libc++ packaging fix: " + launcher);
        AddCppPackagingRule(file);
        AddCppPackagingRule(launcher);
        string rules=Path.Combine(path,"proguard-user.txt");
        const string keep="\n-keep class com.example.s25dualcamera.** { *; }\n-keep class org.opencv.** { *; }\n";
        if(File.Exists(rules) && !File.ReadAllText(rules).Contains("-keep class org.opencv.**"))File.AppendAllText(rules,keep);
    }

    private static void AddCppPackagingRule(string file)
    {
        const string marker = "// S25_CPP_SHARED_PICK_FIRST_V1";
        if(File.ReadAllText(file).Contains(marker)) return;
        // Keep one shared C++ runtime per ABI. Do not exclude all copies or match other .so files.
        File.AppendAllText(file, "\n" + marker + "\n" +
            "android {\n" +
            "    packagingOptions {\n" +
            "        jniLibs {\n" +
            "            pickFirsts += ['**/libc++_shared.so']\n" +
            "        }\n" +
            "    }\n" +
            "}\n");
    }
}
#endif
