
using System;
using System.Collections;
using UnityEngine;

public class PlayerDecoUiManager : MonoBehaviour
{
    [Serializable]
    public class PanelUI
    {
        public GameObject panel;

        [Tooltip("비워두면 panel에 붙은 Animator를 사용합니다.")]
        public Animator animator;

        [Tooltip("열기 애니메이션을 실행하는 Trigger 파라미터 이름")]
        public string openTrigger = "Open";
    }

    [Header("Panels")]
    [SerializeField] private PanelUI decoList = new PanelUI();

    [Header("Camera")]
    [SerializeField] private Transform mainCamera;

    [SerializeField] private Vector3 cameraStartPosition;
    [SerializeField] private Vector3 cameraTargetPosition;

    [SerializeField] private float cameraMoveDuration = 1f;


    [Header("MainScene")]
    [SerializeField] private string mainScene;

    private void Awake()
    {
        Prepare(decoList);
    }

    private void Start()
    {
        OpenDecoList();

        if (mainCamera != null)
            StartCoroutine(PlayCameraMove());
    }

    private void Prepare(PanelUI ui)
    {
        if (ui.panel != null && ui.animator == null)
            ui.animator = ui.panel.GetComponent<Animator>();
    }

    public void OpenDecoList()
    {
        if (decoList.panel == null) return;

        if (decoList.panel.activeSelf) return;

        decoList.panel.SetActive(true);

        if (decoList.animator != null &&
            !string.IsNullOrEmpty(decoList.openTrigger))
        {
            decoList.animator.SetTrigger(decoList.openTrigger);
        }
    }

    private IEnumerator PlayCameraMove()
    {
        float elapsed = 0f;

        while (elapsed < cameraMoveDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / cameraMoveDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            mainCamera.position = Vector3.Lerp(
                cameraStartPosition,
                cameraTargetPosition,
                t
            );

            yield return null;
        }

        mainCamera.position = cameraTargetPosition;
    }

    public void PrevBtn()
    {
        SceneLoader.Instance.Load(mainScene, waitForSceneReady: true);
    }
}
