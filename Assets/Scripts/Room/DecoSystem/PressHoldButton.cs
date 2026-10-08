using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 꾹 누르고 있는 동안(Pointer Down ~ Up)을 감지하는 버튼.
/// 방 꾸미기의 '이동' 버튼처럼, 누르고 있는 동안에만 어떤 동작이 계속되고
/// 손을 떼는 순간 그 동작을 확정/종료해야 하는 UI에 사용한다.
/// 일반 Button과 함께 같은 오브젝트에 붙여도 되고, 이 컴포넌트만 써도 된다.
///
/// 버튼을 누른 그 포인터(마우스 또는 손가락)의 화면 좌표를 HoldPosition으로 계속 추적한다.
/// 모바일에는 Mouse.current가 없고, 멀티터치일 때는 어느 손가락이 기준인지도 모호하므로
/// 버튼을 누른 포인터의 위치를 여기서 직접 넘겨준다.
/// </summary>
public class PressHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public event Action Pressed;
    public event Action Released;

    /// 지금 눌려 있는지.
    public bool IsPressed => isPressed;

    /// 버튼을 누른 포인터의 현재(또는 손을 뗀 순간의) 화면 좌표. HasHoldPosition이 false면 의미 없음.
    public Vector2 HoldPosition { get; private set; }

    /// HoldPosition이 유효한지. 손을 떼지 않고 버튼이 비활성화되어 Released가 불린 경우에는 false.
    public bool HasHoldPosition { get; private set; }

    private bool isPressed;
    private int pointerId;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isPressed) return; // 이미 다른 손가락으로 누르고 있으면 무시

        isPressed = true;
        pointerId = eventData.pointerId;
        HoldPosition = eventData.position;
        HasHoldPosition = true;
        Pressed?.Invoke();
    }

    // 버튼을 누른 채 손가락/마우스를 움직이는 동안 위치를 갱신한다.
    public void OnDrag(PointerEventData eventData)
    {
        if (!isPressed || eventData.pointerId != pointerId) return;
        HoldPosition = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed || eventData.pointerId != pointerId) return;

        isPressed = false;
        HoldPosition = eventData.position;
        HasHoldPosition = true;
        Released?.Invoke();
    }

    private void OnDisable()
    {
        // 패널이 닫히는 등으로 버튼이 눌린 채 비활성화되면, 눌림 상태가 풀리지 않고 남는 것을 방지한다.
        if (isPressed)
        {
            isPressed = false;
            HasHoldPosition = false; // 손을 뗀 위치가 아니므로 확정에 쓰면 안 된다.
            Released?.Invoke();
        }
    }
}
