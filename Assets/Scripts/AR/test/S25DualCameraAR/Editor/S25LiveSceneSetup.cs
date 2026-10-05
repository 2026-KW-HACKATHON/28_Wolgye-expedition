#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public static class S25LiveSceneSetup
{
    [MenuItem("Tools/S25/Use Live Scene Experiment (U7)")]
    public static void Setup(){
        if(Object.FindFirstObjectByType<ARCameraManager>()==null || Object.FindFirstObjectByType<ARSession>()==null){
            EditorUtility.DisplayDialog("S25","Open your existing AR scene with AR Session and AR Camera Manager first.","OK");return;}
        Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Install S25 Live Scene");
        Disable<S25JointMapRuntime>();Disable<S25FrontMapProbe>();Disable<S25DualCameraAR>();Disable<S25UnifiedCapture>();
        var live=Object.FindFirstObjectByType<S25LiveSceneAR>(FindObjectsInactive.Include);
        if(live==null){var go=new GameObject("S25 Live Scene");Undo.RegisterCreatedObjectUndo(go,"Create S25 live scene");live=Undo.AddComponent<S25LiveSceneAR>(go);}
        Undo.RecordObject(live,"Enable S25 live scene");live.enabled=true;live.gameObject.SetActive(true);Selection.activeGameObject=live.gameObject;
        EditorSceneManager.MarkSceneDirty(live.gameObject.scene);
        Debug.Log("S25 U7 installed. Save scene, Build And Run. Start, scan static furniture, Place monster, Show FRONT.");
    }
    static void Disable<T>() where T:MonoBehaviour{
        foreach(var item in Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None)){Undo.RecordObject(item,"Disable older S25 controller");item.enabled=false;EditorUtility.SetDirty(item);}
    }
}
#endif
