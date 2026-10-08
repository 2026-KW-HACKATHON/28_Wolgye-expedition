using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine; // Cinemachine 3.x 네임스페이스 (2.x의 "Cinemachine"과 다름)

/// <summary>
/// 버튼 클릭으로 CinemachineCamera의 Follow / LookAt 대상을 바꾼다.
/// 전환 시 부드러움은 CinemachineFollow / RotationComposer의 Damping 값으로 조절.
/// </summary>
public class CameraTargetSwitcher : MonoBehaviour
{
    public List<Transform> targets = new List<Transform>();

    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private bool alsoLookAt = true;

    private int currentIndex;

    /// 지금 카메라가 따라가고 있는 대상. (CameraOcclusionFader 등 다른 스크립트가 참조)
    public Transform CurrentTarget { get; private set; }

    /// 대상을 바꾸는 시네머신 카메라.
    public CinemachineCamera TargetCamera => cinemachineCamera;

    private void Start()
    {
        if (targets != null && targets.Count > 0)
            FocusOnIndex(0);
    }

    // 버튼 OnClick: 다음 타겟으로 순환
    public void NextTarget()
    {
        if (targets.Count == 0) return;
        FocusOnIndex((currentIndex + 1) % targets.Count);
    }

    // 버튼 OnClick: 이전 타겟으로 순환
    public void PreviousTarget()
    {
        if (targets.Count == 0) return;
        FocusOnIndex((currentIndex - 1 + targets.Count) % targets.Count);
    }

    // 버튼 OnClick: 인덱스로 지정 (Inspector에서 int 값 입력)
    public void FocusOnIndex(int index)
    {
        if (index < 0 || index >= targets.Count) return;
        currentIndex = index;
        FocusOn(targets[index]);
    }

    // 버튼 OnClick: Transform을 직접 드래그해서 지정
    public void FocusOn(Transform target)
    {
        if (cinemachineCamera == null || target == null) return;

        CurrentTarget = target;
        cinemachineCamera.Follow = target;
        if (alsoLookAt)
            cinemachineCamera.LookAt = target;
    }
}