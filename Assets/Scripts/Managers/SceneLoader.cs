using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 로딩 화면을 띄우면서 씬을 비동기로 불러온다.
/// BGMManager처럼 씬이 바뀌어도 유지되며, 중복된 오브젝트는 스스로 삭제된다.
/// 사용: SceneLoader.Instance.Load("RoomScene");
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Tooltip("로딩 화면 전체를 감싸는 CanvasGroup (Canvas의 Sort Order를 가장 높게)")]
    [SerializeField] private CanvasGroup loadingScreen;
    [Tooltip("진행도 표시용 (Image Type = Filled). 없으면 비워둬도 된다.")]
    [SerializeField] private Image progressBar;

    [SerializeField] private float fadeDuration = 0.3f;
    [Tooltip("로딩이 너무 빨리 끝나 화면이 번쩍이는 것을 막기 위한 최소 표시 시간")]
    [SerializeField] private float minimumShowTime = 1f;
    [Tooltip("씬이 실제로 로드된 뒤, 로딩 화면을 걷어내기 전까지 기다리는 시간")]
    [SerializeField] private float delayAfterLoad = 0.5f;

    public bool IsLoading { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        SetScreenVisible(false, 0f);
    }

    public void Load(string sceneName)
    {
        if (IsLoading) return;
        StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        IsLoading = true;
        SetProgress(0f);

        yield return Fade(0f, 1f); // 로딩 화면으로 덮기

        float startTime = Time.unscaledTime;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false; // 다 불러와도 바로 넘어가지 않고 기다림

        // allowSceneActivation이 false면 progress는 0.9에서 멈춘다 → 0.9를 100%로 환산
        while (op.progress < 0.9f || Time.unscaledTime - startTime < minimumShowTime)
        {
            float loadRatio = Mathf.Clamp01(op.progress / 0.9f);
            float timeRatio = Mathf.Clamp01((Time.unscaledTime - startTime) / minimumShowTime);
            SetProgress(Mathf.Min(loadRatio, timeRatio));
            yield return null;
        }

        SetProgress(1f);
        op.allowSceneActivation = true;
        yield return new WaitUntil(() => op.isDone);

        // 새 씬의 Awake/Start(가구 생성 등)가 끝나고 화면이 안정될 때까지 로딩 화면을 유지
        if (delayAfterLoad > 0f)
            yield return new WaitForSecondsRealtime(delayAfterLoad);

        yield return Fade(1f, 0f); // 새 씬 보여주기
        IsLoading = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime; // Time.timeScale = 0인 상태에서도 동작하도록
            SetScreenVisible(true, Mathf.Lerp(from, to, t / fadeDuration));
            yield return null;
        }
        SetScreenVisible(to > 0f, to);
    }

    private void SetScreenVisible(bool visible, float alpha)
    {
        if (loadingScreen == null) return;
        loadingScreen.gameObject.SetActive(visible);
        loadingScreen.alpha = alpha;
        loadingScreen.blocksRaycasts = visible; // 로딩 중 뒤쪽 UI 클릭 방지
    }

    private void SetProgress(float value)
    {
        if (progressBar != null) progressBar.fillAmount = value;
    }
}