using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class DalsuInputController : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField]
    private Camera _mainCamera;

    [SerializeField]
    private LayerMask _dalsuLayer;

    private void Awake()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        HandleTouch();

#if UNITY_EDITOR
        HandleMouse();
#endif
    }

    private void HandleTouch()
    {
        if (Touchscreen.current == null)
            return;

        TouchControl touch =
            Touchscreen.current.primaryTouch;

        if (!touch.press.wasPressedThisFrame)
            return;

        Vector2 screenPosition =
            touch.position.ReadValue();

        TryFindDalsu(screenPosition);
    }

    private void TryFindDalsu(
        Vector2 screenPosition)
    {
        // UI 위를 클릭/터치했다면
        // 뒤쪽의 Dalsu는 클릭하지 않는다.
        if (IsPointerOverUI(screenPosition))
        {
            Debug.Log(
                $"[DalsuInput] UI 위 터치: {screenPosition}"
            );
            return;
        }

        Ray ray =
            _mainCamera.ScreenPointToRay(
                screenPosition
            );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                Mathf.Infinity,
                _dalsuLayer))
        {
            return;
        }

        DalsuController dalsu =
            hit.collider.GetComponentInParent<DalsuController>();

        if (dalsu == null)
            return;

        Debug.Log(
            $"[DalsuInput] 달수 터치: {dalsu.name}"
        );

        dalsu.Catch();
    }

    private bool IsPointerOverUI(
    Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData eventData =
            new PointerEventData(EventSystem.current);

        eventData.position =
            screenPosition;

        List<RaycastResult> results =
            new List<RaycastResult>();

        EventSystem.current.RaycastAll(
            eventData,
            results);

        return results.Count > 0;
    }

#if UNITY_EDITOR
    private void HandleMouse()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 screenPosition =
            Mouse.current.position.ReadValue();

        TryFindDalsu(screenPosition);
    }
#endif
}