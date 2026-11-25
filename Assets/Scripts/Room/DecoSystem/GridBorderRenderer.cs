using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 그리드 테두리(+선택: 칸 구분선)를 런타임에 LineRenderer로 그린다.
/// GridManager의 기즈모는 에디터에서만 보이므로, 실제 게임 화면에 보여줄 때 이 컴포넌트를 쓴다.
/// GridManager와 같은 오브젝트에 붙이고, 보이기/숨기기는 SetVisible로 제어한다.
/// (FurnitureEditController가 편집 모드일 때만 켠다)
/// </summary>
[RequireComponent(typeof(GridManager))]
public class GridBorderRenderer : MonoBehaviour
{
    [Tooltip("선 머티리얼. URP라면 Unlit 계열(투명) 머티리얼을 권장. 비워두면 Sprites/Default로 생성한다.")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Color borderColor = new Color(1f, 1f, 1f, 0.9f);
    [Tooltip("테두리 두께 (월드 단위)")]
    [SerializeField] private float borderWidth = 0.15f;

    [Header("Cell Lines (선택)")]
    [Tooltip("칸 구분선도 함께 그릴지")]
    [SerializeField] private bool drawCellLines = false;
    [SerializeField] private Color cellLineColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private float cellLineWidth = 0.06f;

    [Tooltip("바닥과 겹쳐 깜빡이지 않도록 살짝 띄우는 높이")]
    [SerializeField] private float heightOffset = 0.02f;
    [Tooltip("시작할 때 보이게 할지")]
    [SerializeField] private bool visibleOnStart = false;

    private GridManager grid;
    private Transform root;
    private readonly List<LineRenderer> lines = new List<LineRenderer>();
    private bool visible;

    public bool IsVisible => visible;

    private void Awake()
    {
        grid = GetComponent<GridManager>();
        Rebuild();
        SetVisible(visibleOnStart, force: true);
    }

    public void SetVisible(bool show) => SetVisible(show, false);

    private void SetVisible(bool show, bool force)
    {
        if (!force && visible == show) return;
        visible = show;
        if (root != null) root.gameObject.SetActive(show);
    }

    /// <summary>그리드 크기/위치가 바뀌었을 때 다시 그린다.</summary>
    public void Rebuild()
    {
        if (grid == null) grid = GetComponent<GridManager>();

        if (root != null) Destroy(root.gameObject);
        lines.Clear();

        root = new GameObject("GridBorder").transform;
        root.SetParent(transform, false);

        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) lineMaterial = new Material(shader);
            else Debug.LogWarning("GridBorderRenderer: lineMaterial을 지정해 주세요.");
        }

        float cs = grid.CellSize;
        float w = grid.Width * cs;
        float h = grid.Height * cs;

        // 테두리 (닫힌 사각형)
        CreateLine("Border", borderColor, borderWidth, true,
            new Vector3(0f, 0f, 0f), new Vector3(w, 0f, 0f),
            new Vector3(w, 0f, h), new Vector3(0f, 0f, h));

        if (drawCellLines)
        {
            for (int x = 1; x < grid.Width; x++)
                CreateLine($"CellX{x}", cellLineColor, cellLineWidth, false,
                    new Vector3(x * cs, 0f, 0f), new Vector3(x * cs, 0f, h));
            for (int y = 1; y < grid.Height; y++)
                CreateLine($"CellY{y}", cellLineColor, cellLineWidth, false,
                    new Vector3(0f, 0f, y * cs), new Vector3(w, 0f, y * cs));
        }

        root.gameObject.SetActive(visible);
    }

    // gridLocalPoints: 그리드 로컬 좌표 (X = 그리드 x, Z = 그리드 y)
    private void CreateLine(string name, Color color, float width, bool loop, params Vector3[] gridLocalPoints)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root, false);
        // TransformZ 정렬 + 아래를 보게 회전 → 선이 카메라를 향하지 않고 바닥에 납작하게 깔린다.
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.alignment = LineAlignment.TransformZ;
        lr.loop = loop;
        lr.numCornerVertices = 2;
        lr.numCapVertices = loop ? 0 : 2;
        lr.widthMultiplier = width;
        lr.startColor = lr.endColor = color;
        lr.sharedMaterial = lineMaterial;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        // 그리드 로컬 → 이 선 오브젝트의 로컬 좌표로 변환
        Quaternion toLineLocal = Quaternion.Inverse(go.transform.localRotation);
        Vector3[] points = new Vector3[gridLocalPoints.Length];
        for (int i = 0; i < points.Length; i++)
            points[i] = toLineLocal * (gridLocalPoints[i] + Vector3.up * heightOffset);

        lr.positionCount = points.Length;
        lr.SetPositions(points);
        lines.Add(lr);
    }
}
