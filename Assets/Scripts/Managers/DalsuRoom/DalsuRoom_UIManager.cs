using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DalsuRoom_UIManager : MonoBehaviour
{
    // 화면 하나(패널 + 열기/닫기 애니메이션) 설정
    [Serializable]
    public class PanelUI
    {
        public GameObject panel;

        [Tooltip("비워두면 panel에 붙은 Animator를 사용합니다.")]
        public Animator animator;

        [Tooltip("열기 애니메이션을 실행하는 Trigger 파라미터 이름")]
        public string openTrigger = "Open";

        [Tooltip("닫기 애니메이션을 실행하는 Trigger 파라미터 이름")]
        public string closeTrigger = "Close";

        [Tooltip("닫기 애니메이션이 끝난 뒤 패널을 비활성화하기까지의 시간(초). Close 클립 길이와 맞추세요.")]
        public float closeDuration = 0.6f;

        [NonSerialized] public Coroutine closing;
        [NonSerialized] public bool isClosing;
    }

    [Header("Panels")]
    [SerializeField] private PanelUI decorate = new PanelUI();   // 방 꾸미기
    [SerializeField] private PanelUI dalsuList = new PanelUI();  // 달수 리스트
    [SerializeField] private PanelUI returnBack = new PanelUI(); // 돌려보내기
    [SerializeField] private GameObject returnCompletePanel;

    [Header("Camera")]
    [SerializeField] private CameraModeSwitcher cameraModeSwitcher;

    [Header("Deco")]
    [Tooltip("방 꾸미기 화면을 여는 동안 씬의 달수 캐릭터를 통째로 숨겼다가, 닫히면 빈 자리에 다시 불러온다.")]
    [SerializeField] private DalsuSpawnManager spawnManager;

    [Header("MainScene")]
    [SerializeField] private string mainScene;
    private void Awake()
    {
        Prepare(decorate);
        Prepare(dalsuList);
        Prepare(returnBack);
    }

    private void Start()
    {
        CloseAll(true);
    }

    // ── 방 꾸미기 ──
    public void OpenDecorate()
    {
        Open(decorate);

        // 배치 작업 중 겹침 문제가 생기지 않도록 달수 캐릭터를 통째로 숨기고, 위에서 수직으로 내려다보는 카메라로 전환
        if (spawnManager != null)
            spawnManager.SetDalsusActive(false);
        if (cameraModeSwitcher != null)
            cameraModeSwitcher.ShowDecorate();
    }

    public void CloseDecorate()
    {
        Close(decorate);

        // 숨겨뒀던 달수 캐릭터를 가구가 없는 빈 자리로 옮겨 다시 불러오고, Overview 카메라로 복귀
        if (spawnManager != null)
            spawnManager.SetDalsusActive(true);
        ShowOverview();
    }

    // 방 꾸미기 화면이 켜져 있는 동안에만 배치 입력을 받도록, PlacementController 등에서 참조한다.
    public bool IsDecoratePanelActive => decorate.panel != null && decorate.panel.activeSelf;

    // ── 달수 리스트 ──
    public void OpenDalsuList() => Open(dalsuList);
    public void CloseDalsuList()
    {
        // 달수 리스트를 닫으면 Overview 카메라로 복귀
        ShowOverview();
        Close(dalsuList);
    }

    // ── 돌려보내기 ──
    public void OpenReturn() => Open(returnBack);
    public void CloseReturn() => Close(returnBack);

    // ── 돌려보내기 완료 ──
    public void OpenReturnComplete()
    {
        CloseAll(true);
        SetActive(returnCompletePanel, true);
    }
    public void CloseReturnComplete()
    {
        SetActive(returnCompletePanel, false);
    }

    // 모든 화면 닫기 (버튼 OnClick용: 애니메이션 재생)
    public void CloseAll() => CloseAll(false);

    private void CloseAll(bool immediate)
    {
        Close(decorate, immediate);

        if (!immediate) ShowOverview(); // 시작 시(immediate)에는 CameraModeSwitcher가 알아서 Overview로 시작
        Close(dalsuList, immediate);

        Close(returnBack, immediate);
    }

    private void ShowOverview()
    {
        if (cameraModeSwitcher != null)
            cameraModeSwitcher.ShowOverview();
    }

    private void Prepare(PanelUI ui)
    {
        if (ui.panel != null && ui.animator == null)
            ui.animator = ui.panel.GetComponent<Animator>();
    }

    private void Open(PanelUI ui)
    {
        if (ui.panel == null) return;

        // 닫는 중이면 닫기 취소
        if (ui.closing != null)
        {
            StopCoroutine(ui.closing);
            ui.closing = null;
        }

        // 이미 완전히 열려 있으면 다시 재생하지 않음
        if (ui.panel.activeSelf && !ui.isClosing) return;
        ui.isClosing = false;

        // 비활성 상태에서는 Animator 트리거가 동작하지 않으므로 먼저 활성화
        ui.panel.SetActive(true);
        SetTrigger(ui, ui.openTrigger, ui.closeTrigger);
    }

    private void Close(PanelUI ui, bool immediate = false)
    {
        if (ui.panel == null) return;
        if (!ui.panel.activeSelf) return;

        if (ui.closing != null)
        {
            StopCoroutine(ui.closing);
            ui.closing = null;
        }

        // 애니메이션이 없거나 즉시 닫기인 경우
        if (immediate || ui.animator == null || string.IsNullOrEmpty(ui.closeTrigger))
        {
            ui.isClosing = false;
            ui.panel.SetActive(false);
            return;
        }

        ui.isClosing = true;
        SetTrigger(ui, ui.closeTrigger, ui.openTrigger);
        ui.closing = StartCoroutine(DeactivateAfter(ui));
    }

    private IEnumerator DeactivateAfter(PanelUI ui)
    {
        yield return new WaitForSecondsRealtime(ui.closeDuration);

        ui.panel.SetActive(false);
        ui.isClosing = false;
        ui.closing = null;
    }

    // 반대쪽 트리거를 지우고 원하는 트리거를 발동 (연타 시 트리거가 쌓이는 것 방지)
    private void SetTrigger(PanelUI ui, string trigger, string oppositeTrigger)
    {
        if (ui.animator == null || string.IsNullOrEmpty(trigger)) return;

        if (!string.IsNullOrEmpty(oppositeTrigger))
            ui.animator.ResetTrigger(oppositeTrigger);
        ui.animator.SetTrigger(trigger);
    }

    private void SetActive(GameObject gameObject, bool boolean)
    {
        gameObject.SetActive(boolean);
    }

    public void PrevBtn()
    {
        SceneLoader.Instance.Load(mainScene);
    }
}
