using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ScrollView의 Content가 자식 요소 수에 맞춰 세로로 자동으로 늘어나게 해주는 헬퍼.
///
/// - Content를 위쪽 기준(anchor 상단 + 좌우 stretch, pivot 상단)으로 바꿔서 아래로만 자라게 한다.
/// - ContentSizeFitter(Vertical = Preferred Size)를 붙여, GridLayoutGroup 등이 계산한 높이를 그대로 쓰게 한다.
///   이후에는 요소를 Instantiate/Destroy할 때마다 레이아웃 시스템이 알아서 높이를 다시 계산한다.
/// - 여러 번 호출해도 안전하다 (이미 설정돼 있으면 아무것도 하지 않는다).
/// </summary>
public static class ScrollContentUtil
{
    public static void EnableAutoHeight(Transform content)
    {
        if (!(content is RectTransform rt)) return;
        if (rt.TryGetComponent(out ContentSizeFitter existing) &&
            existing.verticalFit == ContentSizeFitter.FitMode.PreferredSize)
            return;

        // 위쪽에 붙이고 가로는 Viewport 폭에 맞춘다. (높이는 ContentSizeFitter가 정한다)
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(rt.pivot.x, 1f);
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 0f);
        rt.sizeDelta = new Vector2(0f, rt.sizeDelta.y);

        ContentSizeFitter fitter = existing != null ? existing : rt.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutRebuilder.MarkLayoutForRebuild(rt);
    }
}
