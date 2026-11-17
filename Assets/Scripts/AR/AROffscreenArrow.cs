using UnityEngine;

/// <summary>
/// 화면 밖의 달수 위치를 가리키는 Canvas UI.
/// 화살표 이미지는 위쪽(↑)을 향하고, anchor/pivot은 중앙이어야 합니다.
/// </summary>
public class AROffscreenArrow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform arrowRect;

    [Header("Arrow")]
    [SerializeField] private float edgeMargin = 80f;
    [SerializeField] private float targetHeight = 0.5f;
    [Range(0f, 0.3f)]
    [SerializeField] private float viewportInset = 0.05f;

    private Transform target;
    private bool discovered;

    private void Awake()
    {
        if (arCamera == null)
            arCamera = Camera.main;

        SetArrowVisible(false);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        discovered = false;
        SetArrowVisible(false);
    }

    public void MarkFound()
    {
        discovered = true;
        SetArrowVisible(false);
    }

    private void LateUpdate()
    {
        if (discovered || target == null || arCamera == null ||
            canvasRect == null || arrowRect == null)
        {
            SetArrowVisible(false);
            return;
        }

        Vector3 worldTarget = target.position + Vector3.up * targetHeight;
        Vector3 viewport = arCamera.WorldToViewportPoint(worldTarget);

        bool onScreen = viewport.z > 0f &&
            viewport.x >= viewportInset && viewport.x <= 1f - viewportInset &&
            viewport.y >= viewportInset && viewport.y <= 1f - viewportInset;

        if (onScreen)
        {
            SetArrowVisible(false);
            return;
        }

        Vector2 direction = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);

        // 목표물이 카메라 뒤에 있을 때는 반대 방향으로 안내한다.
        if (viewport.z <= 0f)
            direction = -direction;

        // 정확히 뒤에 있는 경우에는 오른쪽 회전을 유도한다.
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;

        direction.Normalize();

        // Canvas 중앙 기준 좌표. Arrow는 canvasRect의 자식으로 배치한다.
        Vector2 half = canvasRect.rect.size * 0.5f;
        half.x = Mathf.Max(1f, half.x - edgeMargin);
        half.y = Mathf.Max(1f, half.y - edgeMargin);

        float tx = Mathf.Abs(direction.x) > 0.0001f
            ? half.x / Mathf.Abs(direction.x) : float.PositiveInfinity;
        float ty = Mathf.Abs(direction.y) > 0.0001f
            ? half.y / Mathf.Abs(direction.y) : float.PositiveInfinity;

        arrowRect.anchoredPosition = direction * Mathf.Min(tx, ty);

        // 이미지가 기본적으로 ↑ 방향을 가리킨다고 가정한다.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        arrowRect.localRotation = Quaternion.Euler(0f, 0f, angle);
        SetArrowVisible(true);
    }

    private void SetArrowVisible(bool visible)
    {
        if (arrowRect != null && arrowRect.gameObject.activeSelf != visible)
            arrowRect.gameObject.SetActive(visible);
    }
}
