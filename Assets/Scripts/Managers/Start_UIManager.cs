using UnityEngine;
using UnityEngine.SceneManagement;

public class Start_UIManager : MonoBehaviour
{
    [SerializeField] private string mainScene = "Map";
    public void ClickStartBtn()
    {
        SceneManager.LoadScene(mainScene);
    }
}
