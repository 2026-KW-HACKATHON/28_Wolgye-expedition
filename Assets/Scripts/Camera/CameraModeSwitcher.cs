using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 전경(Overview) 카메라와 Follow 카메라를 Priority로 전환한다.
/// 전환 애니메이션은 Main Camera의 CinemachineBrain > Default Blend 설정을 따른다.
/// </summary>
public class CameraModeSwitcher : MonoBehaviour
{
    [SerializeField] private CinemachineCamera overviewCamera;
    [SerializeField] private CinemachineCamera followCamera;

    private const int ActivePriority = 10;
    private const int InactivePriority = 0;

    public bool IsFollowing { get; private set; }

    private void Start()
    {
        ShowOverview(); // 시작은 전경 카메라
    }

    // 버튼 OnClick: 전경 → 팔로우
    public void ShowFollow()
    {
        overviewCamera.Priority = InactivePriority;
        followCamera.Priority = ActivePriority;
        IsFollowing = true;
    }

    // 버튼 OnClick: 팔로우 → 전경
    public void ShowOverview()
    {
        followCamera.Priority = InactivePriority;
        overviewCamera.Priority = ActivePriority;
        IsFollowing = false;
    }

    // 버튼 하나로 왔다 갔다 할 때
    public void Toggle()
    {
        if (IsFollowing) ShowOverview();
        else ShowFollow();
    }

    // 특정 오브젝트를 따라가며 팔로우 모드로 진입 (버튼에 Transform 지정)
    public void FollowTarget(Transform target)
    {
        if (target == null) return;
        followCamera.Follow = target;
        followCamera.LookAt = target;
        ShowFollow();
    }
}