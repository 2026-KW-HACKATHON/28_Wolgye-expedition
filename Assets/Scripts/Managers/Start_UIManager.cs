using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Start_UIManager : MonoBehaviour
{
    [SerializeField] private string mainScene = "Map";
    public void ClickStartBtn()
    {
        SceneLoader.Instance.Load(mainScene, waitForSceneReady: true);
    }
}
