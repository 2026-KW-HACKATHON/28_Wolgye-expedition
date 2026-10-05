using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 꾹 누르고 있는 동안(Pointer Down ~ Up)을 감지하는 버튼.
/// 방 꾸미기의 '이동' 버튼처럼, 누르고 있는 동안에만 어떤 동작이 계속되고
/// 손을 떼는 순간 그 동작을 확정/종료해야 하는 UI에 사용한다.
/// 일반 Button과 함께 같은 오브젝트에 붙여도 되고, 이 컴포넌트만 써도 된다.
/// </summary>
public class PressHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public event Action Pressed;
    public event Action Released;

    private bool isPressed;

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        Pressed?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed) return;

        isPressed = false;
        Released?.Invoke();
    }

    private void OnDisable()
    {
        // 패널이 닫히는 등으로 버튼이 눌린 채 비활성화되면, 눌림 상태가 풀리지 않고 남는 것을 방지한다.
        if (isPressed)
        {
            isPressed = false;
            Released?.Invoke();
        }
    }
}
