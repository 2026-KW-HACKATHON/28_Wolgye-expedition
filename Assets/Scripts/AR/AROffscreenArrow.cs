using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 달수가 화면 밖에 있을 때 화면 가장자리에 조용한 빛만 표시합니다.
/// - 좌측 밖: 왼쪽 edge glow
/// - 우측 밖: 오른쪽 edge glow
/// - 위쪽 밖: 위쪽 edge glow
/// - 좌상단 밖: 왼쪽 + 위쪽 glow (동시에 표시)
/// - 카메라 뒤: 좌/우 glow로 회전 방향 안내
/// - 화면 안: 모두 사라짐
/// 화살표 위치 이동/회전은 사용하지 않습니다.
/// AROffscreenArrow라는 이름 및 SetTarget/MarkFound는 기존 Spawner와 호환됩니다.
/// </summary>
public class AROffscreenArrow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private RectTransform canvasRect;

    [Header("Old Arrow (Automatically Hidden)")]
    [Tooltip("기존 ArrowImage를 연결해 둔 경우 자동으로 꺼집니다. 새 이미지를 만들 필요 없습니다.")]
    [SerializeField] private RectTransform arrowRect;

    [Header("Edge Glow Style")]
    [SerializeField] private Color glowColor = new Color(0.58f, 0.83f, 1f, 1f);
    [Range(0.05f, 1f)]
    [SerializeField] private float maxOpacity = 0.32f;
    [Range(12f, 120f)]
    [SerializeField] private float glowThickness = 30f;
    [Range(0f, 60f)]
    [SerializeField] private float edgeInset = 0f;
    [Range(1f, 8f)]
    [SerializeField] private float pulsePeriod = 2.8f;
    [Range(1f, 12f)]
    [SerializeField] private float fadeSpeed = 4f;

    [Header("Tiny Edge Sparkles")]
    [Tooltip("고정된 작은 별빛을 가장자리에 추가합니다. 이동하지 않고 밝기만 바뀝니다.")]
    [SerializeField] private bool enableSparkles = true;
    [Range(0, 200)]
    [SerializeField] private int sparklesPerEdge = 30;
    [Range(5f, 28f)]
    [SerializeField] private float sparkleSize = 24f;
    [Range(0f, 30f)]
    [Tooltip("반짝이가 시작하는 테두리로부터의 최소 거리 (Canvas 단위)")]
    [SerializeField] private float sparkleEdgeOffset = 5f;

    [Range(0f, 160f)]
    [Tooltip("반짝이가 테두리에서 화면 안쪽으로 퍼질 수 있는 최대 추가 거리 (Canvas 단위)")]
    [SerializeField] private float sparkleSpread = 70f;

    [Range(1f, 6f)]
    [Tooltip("클수록 테두리 바로 근처에 더 촘촘하게 모이고, 안쪽에는 적게 분포합니다. 권장: 3")]
    [SerializeField] private float sparkleNearEdgeBias = 3f;
    [Range(0f, 1f)]
    [SerializeField] private float sparkleMaxOpacity = 0.9f;
    [Range(0.6f, 6f)]
    [SerializeField] private float sparklePeriod = 1.9f;
    [SerializeField] private Color sparkleColor = new Color(0.95f, 0.98f, 1f, 1f);

    [Header("Visibility Detection")]
    [SerializeField] private float targetHeight = 0.5f;
    [Range(0f, 0.2f)]
    [SerializeField] private float viewportInset = 0.05f;
    [Range(0.05f, 0.5f)]
    [Tooltip("화면 밖으로 나간 정도에 따라 빛이 서서히 진해지는 범위")]
    [SerializeField] private float edgeResponseRange = 0.2f;

    [Header("Testing")]
    [Tooltip("Play 모드에서 왼쪽 + 위쪽 Glow를 미리 보고 싶을 때만 켜세요.")]
    [SerializeField] private bool previewTopLeft = false;

    private Transform target;
    private Image leftGlow;
    private Image rightGlow;
    private Image topGlow;

    private float leftStrength;
    private float rightStrength;
    private float topStrength;

    private readonly Texture2D[] runtimeTextures = new Texture2D[3];
    private readonly Sprite[] runtimeSprites = new Sprite[3];

    private Texture2D sparkleTexture;
    private Sprite sparkleSprite;
    private readonly Image[][] sparkleImages = new Image[3][];
    private readonly float[][] sparklePhases = new float[3][];
    private readonly float[][] sparkleDepthOpacities = new float[3][];

    private enum Edge { Left, Right, Top }

    private void Awake()
    {
        if (arCamera == null)
            arCamera = Camera.main;

        if (canvasRect == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                canvasRect = canvas.GetComponent<RectTransform>();
        }

        // 기존 화살표 UI를 재사용하지 않고 비활성화합니다.
        // 이 스크립트는 ArrowImage가 아닌 활성 상태의 ArrowManager에 붙이세요.
        if (arrowRect != null && arrowRect.gameObject != gameObject)
            arrowRect.gameObject.SetActive(false);

        if (canvasRect == null)
        {
            Debug.LogWarning("[DALSU] AROffscreenArrow: Canvas Rect를 연결해야 Edge Glow를 표시할 수 있습니다.", this);
            return;
        }

        leftGlow = CreateGlowImage("DalsuEdgeGlow_Left", Edge.Left, 0);
        rightGlow = CreateGlowImage("DalsuEdgeGlow_Right", Edge.Right, 1);
        topGlow = CreateGlowImage("DalsuEdgeGlow_Top", Edge.Top, 2);
        CreateEdgeSparkles();
        HideAllImmediately();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        HideAllImmediately();
    }

    /// <summary>
    /// 기존 ARRandomSpawner가 호출하는 함수이므로 유지합니다.
    /// "최초 발견"과 무관하게 이후 화면 밖으로 가면 Edge Glow가 다시 나타나야 하므로
    /// 이 메서드에서는 표시 상태를 영구 변경하지 않습니다.
    /// </summary>
    public void MarkFound() { }

    private void LateUpdate()
    {
        float wantLeft = 0f;
        float wantRight = 0f;
        float wantTop = 0f;

        if (previewTopLeft)
        {
            wantLeft = 1f;
            wantTop = 1f;
        }
        else if (target != null && arCamera != null)
        {
            Vector3 worldPoint = target.position + Vector3.up * targetHeight;
            Vector3 localPoint = arCamera.transform.InverseTransformPoint(worldPoint);

            if (localPoint.z > 0.01f)
            {
                Vector3 view = arCamera.WorldToViewportPoint(worldPoint);
                float rightThreshold = 1f - viewportInset;

                // 위치를 이동하지 않고, 정해진 edge의 밝기만 변화시킵니다.
                // 대각선 밖이면 각 축이 독립적으로 켜져 두 edge가 함께 빛납니다.
                wantLeft = Mathf.Clamp01((viewportInset - view.x) / edgeResponseRange);
                wantRight = Mathf.Clamp01((view.x - rightThreshold) / edgeResponseRange);
                wantTop = Mathf.Clamp01((view.y - rightThreshold) / edgeResponseRange);
            }
            else
            {
                // 목표가 카메라 뒤에 있으면 화면 투영 좌표가 뒤집히므로
                // 카메라의 로컬 좌/우 관계로 회전 힌트를 줍니다.
                float horizontalMagnitude = new Vector2(localPoint.x, localPoint.z).magnitude;
                float side = horizontalMagnitude > 0.0001f
                    ? localPoint.x / horizontalMagnitude : 0f;

                wantLeft = Mathf.Clamp01(-side * 3f);
                wantRight = Mathf.Clamp01(side * 3f);

                // 정확히 뒤쪽에 있을 때 좌/우가 급히 뒤집히지 않도록
                // 양쪽을 약하게 밝힙니다.
                float straightBack = (1f - Mathf.Clamp01(Mathf.Abs(side) / 0.18f)) * 0.55f;
                wantLeft = Mathf.Max(wantLeft, straightBack);
                wantRight = Mathf.Max(wantRight, straightBack);
            }
        }

        float blend = 1f - Mathf.Exp(-fadeSpeed * Time.unscaledDeltaTime);
        leftStrength = Mathf.Lerp(leftStrength, wantLeft, blend);
        rightStrength = Mathf.Lerp(rightStrength, wantRight, blend);
        topStrength = Mathf.Lerp(topStrength, wantTop, blend);

        // 이미지의 위치는 고정하고, 전체 밝기만 2~3초 주기로 은은하게 호흡합니다.
        float cycle = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / Mathf.Max(0.1f, pulsePeriod));
        float breathing = 0.78f + 0.22f * cycle; // 0.56 ~ 1.00

        ApplyGlow(leftGlow, leftStrength * breathing);
        ApplyGlow(rightGlow, rightStrength * breathing);
        ApplyGlow(topGlow, topStrength * breathing);

        // 별빛은 2차원으로 분포하되 움직이지 않고, 밝기만 각기 다르게 반짝입니다.
        ApplySparkles(0, leftStrength);
        ApplySparkles(1, rightStrength);
        ApplySparkles(2, topStrength);
    }

    private Image CreateGlowImage(string objectName, Edge edge, int index)
    {
        GameObject child = new GameObject(objectName,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.transform.SetParent(canvasRect, false);
        child.transform.SetAsFirstSibling(); // 다른 버튼/텍스트를 가리지 않도록 아래쪽에 배치

        RectTransform rt = child.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        if (edge == Edge.Left)
        {
            rt.anchorMin = new Vector2(0f, 0.12f);
            rt.anchorMax = new Vector2(0f, 0.88f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(glowThickness, 0f);
            rt.anchoredPosition = new Vector2(edgeInset, 0f);
        }
        else if (edge == Edge.Right)
        {
            rt.anchorMin = new Vector2(1f, 0.12f);
            rt.anchorMax = new Vector2(1f, 0.88f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(glowThickness, 0f);
            rt.anchoredPosition = new Vector2(-edgeInset, 0f);
        }
        else // Top
        {
            rt.anchorMin = new Vector2(0.1f, 1f);
            rt.anchorMax = new Vector2(0.9f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, glowThickness);
            rt.anchoredPosition = new Vector2(0f, -edgeInset);
        }

        int width = edge == Edge.Top ? 256 : 48;
        int height = edge == Edge.Top ? 48 : 256;
        Texture2D texture = CreateFeatheredTexture(width, height, edge);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f));
        runtimeTextures[index] = texture;
        runtimeSprites[index] = sprite;

        Image img = child.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.raycastTarget = false;
        img.maskable = false;
        img.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
        return img;
    }

    private void CreateEdgeSparkles()
    {
        if (sparklesPerEdge <= 0)
            return;

        sparkleTexture = CreateSparkleTexture(48);
        sparkleSprite = Sprite.Create(
            sparkleTexture,
            new Rect(0f, 0f, sparkleTexture.width, sparkleTexture.height),
            new Vector2(0.5f, 0.5f));

        CreateSparklesOnEdge(leftGlow, Edge.Left, 0);
        CreateSparklesOnEdge(rightGlow, Edge.Right, 1);
        CreateSparklesOnEdge(topGlow, Edge.Top, 2);
    }

    private void CreateSparklesOnEdge(Image glow, Edge edge, int edgeIndex)
    {
        if (glow == null || sparkleSprite == null || canvasRect == null)
            return;

        sparkleImages[edgeIndex] = new Image[sparklesPerEdge];
        sparklePhases[edgeIndex] = new float[sparklesPerEdge];
        sparkleDepthOpacities[edgeIndex] = new float[sparklesPerEdge];

        // UnityEngine.Random 전역 시드를 변경하지 않는 독립 난수.
        // 한 줄 배치 대신 가장자리 근처에 밀집한 2차원 분포를 사용합니다.
        System.Random rng = new System.Random(20261117 + edgeIndex * 104729);

        for (int i = 0; i < sparklesPerEdge; i++)
        {
            GameObject star = new GameObject(
                "DalsuTinySparkle_" + edge + "_" + i,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            // IMPORTANT: Glow의 자식으로 만들지 않습니다.
            // Canvas의 최상단 형제에 두어 다른 UI 이미지에 가려지는 현상을 줄입니다.
            // 레이캐스트는 비활성화하므로 버튼 터치를 방해하지 않습니다.
            star.transform.SetParent(canvasRect, false);
            star.transform.SetAsLastSibling();

            RectTransform rt = star.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 0.5f);

            // 길이 방향은 균등한 구간에 약간의 랜덤을 주어 고르게 퍼지게 합니다.
            // 안쪽 거리(depth)는 제곱 분포를 적용해 대부분이 edge 근처에 모입니다.
            float along = (i + 0.15f + 0.7f * (float)rng.NextDouble()) / sparklesPerEdge;
            float u = (float)rng.NextDouble();
            float depth = sparkleSpread * Mathf.Pow(u, sparkleNearEdgeBias);
            float inset = sparkleEdgeOffset + depth;

            if (edge == Edge.Left)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0f, Mathf.Lerp(0.12f, 0.88f, along));
                rt.anchoredPosition = new Vector2(inset, 0f);
            }
            else if (edge == Edge.Right)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(1f, Mathf.Lerp(0.12f, 0.88f, along));
                rt.anchoredPosition = new Vector2(-inset, 0f);
            }
            else
            {
                rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Lerp(0.1f, 0.9f, along), 1f);
                rt.anchoredPosition = new Vector2(0f, -inset);
            }

            // 안쪽의 성긴 반짝이는 조금 더 작고 흐리게 표현합니다.
            float normalizedDepth = sparkleSpread > 0.001f ? depth / sparkleSpread : 0f;
            float sizeVariation = Mathf.Lerp(0.70f, 1.15f, (float)rng.NextDouble());
            float distanceSize = Mathf.Lerp(1f, 0.78f, normalizedDepth);
            rt.sizeDelta = Vector2.one * sparkleSize * sizeVariation * distanceSize;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Lerp(-15f, 15f, (float)rng.NextDouble()));
            sparkleDepthOpacities[edgeIndex][i] = Mathf.Lerp(1f, 0.38f, normalizedDepth);

            Image img = star.GetComponent<Image>();
            img.sprite = sparkleSprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget = false;
            img.maskable = false;
            img.color = new Color(sparkleColor.r, sparkleColor.g, sparkleColor.b, 0f);

            sparkleImages[edgeIndex][i] = img;
            sparklePhases[edgeIndex][i] = (float)rng.NextDouble() * Mathf.PI * 2f;
        }
    }

    private void ApplySparkles(int edgeIndex, float edgeStrength)
    {
        Image[] images = sparkleImages[edgeIndex];
        if (images == null)
            return;

        float angle = Time.unscaledTime * Mathf.PI * 2f / Mathf.Max(0.1f, sparklePeriod);
        // 목표가 가장자리에서 조금만 벗어나도 별빛은 식별할 수 있도록 상승 곡선을 완화합니다.
        float active = Mathf.Clamp01(Mathf.Pow(Mathf.Clamp01(edgeStrength), 0.65f) * 1.55f);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null)
                continue;

            float wave = Mathf.Sin(angle + sparklePhases[edgeIndex][i]);
            // 이전 버전은 3제곱+높은 threshold로 대부분의 프레임에서 알파가 거의 0이었습니다.
            // 이제 빛이 좀 더 오래 유지되면서 다른 타이밍에 반짝입니다.
            float twinkle = Mathf.Pow(Mathf.Clamp01((wave + 0.3f) / 1.3f), 1.25f);
            float alpha = enableSparkles
                ? Mathf.Clamp01(active * sparkleMaxOpacity * sparkleColor.a
                    * sparkleDepthOpacities[edgeIndex][i]
                    * (0.08f + 0.92f * twinkle))
                : 0f;

            images[i].color = new Color(sparkleColor.r, sparkleColor.g, sparkleColor.b, alpha);
        }
    }

    private static Texture2D CreateSparkleTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            float ny = ((y + 0.5f) / size - 0.5f) * 2f;
            for (int x = 0; x < size; x++)
            {
                float nx = ((x + 0.5f) / size - 0.5f) * 2f;
                float ax = Mathf.Abs(nx);
                float ay = Mathf.Abs(ny);
                float radius = Mathf.Sqrt(nx * nx + ny * ny);

                // 중앙 핵 + 4방향 빛살 + 아주 약한 후광
                float core = Mathf.Pow(Mathf.Clamp01(1f - radius / 0.32f), 0.85f);
                float horizontal = Mathf.Pow(Mathf.Clamp01(1f - ay / 0.19f), 1.6f)
                    * Mathf.Pow(Mathf.Clamp01(1f - ax / 0.94f), 1.5f);
                float vertical = Mathf.Pow(Mathf.Clamp01(1f - ax / 0.19f), 1.6f)
                    * Mathf.Pow(Mathf.Clamp01(1f - ay / 0.94f), 1.5f);
                float halo = Mathf.Pow(Mathf.Clamp01(1f - radius / 0.78f), 3f) * 0.28f;
                float alpha = Mathf.Clamp01(Mathf.Max(core, Mathf.Max(horizontal, vertical) * 0.95f, halo));

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply(false, true);
        return texture;
    }

    private static Texture2D CreateFeatheredTexture(int width, int height, Edge edge)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            float v = (float)y / (height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / (width - 1);

                // 화면 바깥쪽 가장 진하고 화면 안쪽으로 부드럽게 사라지는 빛
                float distanceFromEdge = edge == Edge.Left ? u
                    : edge == Edge.Right ? 1f - u : 1f - v;
                float lengthPosition = edge == Edge.Top ? u : v;
                float inwardFade = Mathf.Pow(1f - distanceFromEdge, 3.0f);
                float endFade = Mathf.Pow(Mathf.Max(0f,
                    Mathf.Sin(Mathf.PI * lengthPosition)), 0.85f);
                byte alpha = (byte)Mathf.RoundToInt(255f * inwardFade * endFade);
                texture.SetPixel(x, y, new Color32(255, 255, 255, alpha));
            }
        }

        texture.Apply(false, true);
        return texture;
    }

    private void ApplyGlow(Image image, float strength)
    {
        if (image == null)
            return;
        float alpha = Mathf.Clamp01(maxOpacity * glowColor.a * strength);
        image.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
    }

    private void HideAllImmediately()
    {
        leftStrength = rightStrength = topStrength = 0f;
        ApplyGlow(leftGlow, 0f);
        ApplyGlow(rightGlow, 0f);
        ApplyGlow(topGlow, 0f);
        ApplySparkles(0, 0f);
        ApplySparkles(1, 0f);
        ApplySparkles(2, 0f);
    }

    private void OnDestroy()
    {
        if (sparkleSprite != null)
            Destroy(sparkleSprite);
        if (sparkleTexture != null)
            Destroy(sparkleTexture);

        // 런타임에 생성한 Sprite/Texture만 직접 정리합니다.
        for (int i = 0; i < runtimeSprites.Length; i++)
        {
            if (runtimeSprites[i] != null)
                Destroy(runtimeSprites[i]);
            if (runtimeTextures[i] != null)
                Destroy(runtimeTextures[i]);
        }
    }
}
