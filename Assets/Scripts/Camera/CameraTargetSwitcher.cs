using UnityEngine;
using Unity.Cinemachine; // Cinemachine 3.x 네임스페이스 (2.x의 "Cinemachine"과 다름)

/// <summary>
/// 버튼 클릭으로 CinemachineCamera의 Follow / LookAt 대상을 바꾼다.
/// 전환 시 부드러움은 CinemachineFollow / RotationComposer의 Damping 값으로 조절.
/// </summary>
public class CameraTargetSwitcher : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private Transform[] targets;
    [SerializeField] private bool alsoLookAt = true;

    private int currentIndex;

    private void Start()
    {
        if (targets != null && targets.Length > 0)
            FocusOnIndex(0);
    }

    // 버튼 OnClick: 다음 타겟으로 순환
    public void NextTarget()
    {
        if (targets.Length == 0) return;
        FocusOnIndex((currentIndex + 1) % targets.Length);
    }

    // 버튼 OnClick: 이전 타겟으로 순환
    public void PreviousTarget()
    {
        if (targets.Length == 0) return;
        FocusOnIndex((currentIndex - 1 + targets.Length) % targets.Length);
    }

    // 버튼 OnClick: 인덱스로 지정 (Inspector에서 int 값 입력)
    public void FocusOnIndex(int index)
    {
        if (index < 0 || index >= targets.Length) return;
        currentIndex = index;
        FocusOn(targets[index]);
    }

    // 버튼 OnClick: Transform을 직접 드래그해서 지정
    public void FocusOn(Transform target)
    {
        if (cinemachineCamera == null || target == null) return;

        cinemachineCamera.Follow = target;
        if (alsoLookAt)
            cinemachineCamera.LookAt = target;
    }
}