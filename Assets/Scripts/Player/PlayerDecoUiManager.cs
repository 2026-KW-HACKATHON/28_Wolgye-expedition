
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Category Toggles")]
    [SerializeField] private Toggle hatToggle;
    [SerializeField] private Toggle skinColorToggle;

    [Header("Equipment List")]
    [SerializeField] private EquipmentListReturnManager equipmentListManager;

    [Header("MainScene")]
    [SerializeField] private string mainScene;

    private void Awake()
    {
        Prepare(decoList);
    }

    private void Start()
    {
        OpenDecoList();

        // Toggle 이벤트 등록
        hatToggle.onValueChanged.AddListener(OnHatToggle);
        skinColorToggle.onValueChanged.AddListener(OnSkinColorToggle);

        // 초기 카테고리 설정
        if (hatToggle.isOn)
            equipmentListManager.ChangeCategory(EquipmentType.Hat);
        else if (skinColorToggle.isOn)
            equipmentListManager.ChangeCategory(EquipmentType.SkinColor);

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

    // 모자 카테고리 선택
    private void OnHatToggle(bool isOn)
    {
        if (!isOn) return;

        equipmentListManager.ChangeCategory(EquipmentType.Hat);
    }

    // 피부색 카테고리 선택
    private void OnSkinColorToggle(bool isOn)
    {
        if (!isOn) return;

        equipmentListManager.ChangeCategory(EquipmentType.SkinColor);
    }

    public void PrevBtn()
    {
        SceneLoader.Instance.Load(mainScene, waitForSceneReady: true);
    }

    private void OnDestroy()
    {
        if (hatToggle != null)
            hatToggle.onValueChanged.RemoveListener(OnHatToggle);

        if (skinColorToggle != null)
            skinColorToggle.onValueChanged.RemoveListener(OnSkinColorToggle);
    }
}
