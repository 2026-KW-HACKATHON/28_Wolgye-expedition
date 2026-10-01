using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 3D 바닥 그리드. 이 오브젝트의 위치가 (0,0) 칸의 모서리가 되고,
/// 로컬 X축이 그리드의 x, 로컬 Z축이 그리드의 y 방향입니다.
/// 오브젝트의 Scale은 (1,1,1)로 유지하세요. 칸 크기는 cellSize로 조절합니다.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private float cellSize = 1f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color occupiedColor = new Color(1f, 0.2f, 0.2f, 0.4f);

    [Header("Furniture")]
    [Tooltip("배치된 가구의 부모. 비워두면 이 오브젝트 아래에 생성됩니다.")]
    [SerializeField] private Transform furnitureRoot;

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;

    private GridCell[,] cells;
    private readonly List<PlacedFurniture> placedFurniture = new List<PlacedFurniture>();

    /// <summary>현재 배치된 모든 가구 (저장 기능에서 사용 예정)</summary>
    public IReadOnlyList<PlacedFurniture> AllFurniture => placedFurniture;

    private void Awake()
    {
        InitializeGrid();
    }

    public void InitializeGrid()
    {
        cells = new GridCell[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                cells[x, y] = new GridCell(new Vector2Int(x, y));
    }

    // ───────── 좌표 변환 ─────────

    /// <summary>월드 좌표 → 칸 좌표 (범위 밖일 수도 있으니 InBounds로 확인)</summary>
    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        Vector3 local = transform.InverseTransformPoint(worldPos);
        return new Vector2Int(
            Mathf.FloorToInt(local.x / cellSize),
            Mathf.FloorToInt(local.z / cellSize));
    }

    /// <summary>칸의 (0,0) 쪽 모서리 월드 좌표</summary>
    public Vector3 CellToWorld(Vector2Int cell)
    {
        return transform.TransformPoint(new Vector3(cell.x * cellSize, 0f, cell.y * cellSize));
    }

    /// <summary>칸 중앙의 월드 좌표</summary>
    public Vector3 GetCellCenter(Vector2Int cell)
    {
        return transform.TransformPoint(new Vector3(
            (cell.x + 0.5f) * cellSize, 0f, (cell.y + 0.5f) * cellSize));
    }

    // ───────── 조회 ─────────

    public bool InBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;
    }

    public GridCell GetCell(Vector2Int cell)
    {
        return InBounds(cell) ? cells[cell.x, cell.y] : null;
    }

    /// <summary>
    /// 화면 좌표(마우스 위치)에서 그리드 칸을 구합니다.
    /// 콜라이더 없이 수학적 평면과 교차 계산을 하므로 바닥 메시가 없어도 동작합니다.
    /// </summary>
    public bool TryGetCellFromScreen(Vector2 screenPos, Camera cam, out Vector2Int cell)
    {
        cell = default;
        if (!TryGetGridPointFromScreen(screenPos, cam, out Vector2 point))
            return false;

        cell = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
        return InBounds(cell);
    }

    /// <summary>
    /// 화면 좌표 → 그리드 위의 연속 좌표 (칸 단위, 소수점 포함).
    /// 예: (3.7, 1.2)는 3번 칸의 오른쪽 부분, 1번 칸의 아래쪽 부분.
    /// 범위 체크는 하지 않습니다.
    /// </summary>
    public bool TryGetGridPointFromScreen(Vector2 screenPos, Camera cam, out Vector2 gridPoint)
    {
        gridPoint = default;
        Ray ray = cam.ScreenPointToRay(screenPos);
        Plane plane = new Plane(transform.up, transform.position);

        if (!plane.Raycast(ray, out float distance))
            return false;

        Vector3 local = transform.InverseTransformPoint(ray.GetPoint(distance));
        gridPoint = new Vector2(local.x / cellSize, local.z / cellSize);
        return true;
    }

    /// <summary>
    /// 커서 위치가 footprint 중앙 근처에 오도록 원점 칸을 계산.
    /// 짝수 크기 가구는 커서에서 가장 가까운 격자선을 중심으로 스냅됩니다.
    /// </summary>
    public Vector2Int GetFootprintOrigin(Vector2 gridPoint, Vector2Int size)
    {
        return new Vector2Int(
            Mathf.RoundToInt(gridPoint.x - size.x * 0.5f),
            Mathf.RoundToInt(gridPoint.y - size.y * 0.5f));
    }

    // ───────── 가구 배치 ─────────

    /// <summary>origin 칸부터 size만큼의 영역이 모두 비어 있고 그리드 안에 있는지</summary>
    public bool CanPlace(Vector2Int origin, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int c = origin + new Vector2Int(x, y);
                if (!InBounds(c) || cells[c.x, c.y].IsOccupied) return false;
            }
        return true;
    }

    /// <summary>footprint 영역 중앙의 월드 좌표 (가구 프리팹 피벗이 놓일 위치)</summary>
    public Vector3 GetFootprintCenter(Vector2Int origin, Vector2Int size)
    {
        return transform.TransformPoint(new Vector3(
            (origin.x + size.x * 0.5f) * cellSize, 0f,
            (origin.y + size.y * 0.5f) * cellSize));
    }

    public PlacedFurniture GetFurnitureAt(Vector2Int cell)
    {
        return GetCell(cell)?.Occupant;
    }

    /// <summary>배치 성공 시 생성된 가구를, 실패 시 null을 반환</summary>
    public PlacedFurniture PlaceFurniture(FurnitureData data, Vector2Int origin, int rotation = 0)
    {
        if (data == null || data.Prefab == null) return null;

        rotation = GridRotation.Normalize(rotation);
        Vector2Int size = GridRotation.RotateSize(data.Size, rotation);
        if (!CanPlace(origin, size)) return null;

        // 피벗이 footprint 중앙이므로, 중앙을 축으로 회전시키면 어떤 크기든 칸에 딱 맞음
        Vector3 pos = GetFootprintCenter(origin, size);
        Quaternion rot = transform.rotation * GridRotation.ToQuaternion(rotation);
        Transform parent = furnitureRoot != null ? furnitureRoot : transform;
        GameObject go = Instantiate(data.Prefab, pos, rot, parent);

        if (!go.TryGetComponent(out PlacedFurniture placed))
            placed = go.AddComponent<PlacedFurniture>();

        placed.Initialize(data, origin, rotation);
        SetOccupant(origin, size, placed);
        placedFurniture.Add(placed);
        return placed;
    }

    public void RemoveFurniture(PlacedFurniture furniture)
    {
        if (furniture == null) return;

        SetOccupant(furniture.Origin, furniture.Size, null);
        placedFurniture.Remove(furniture);
        Destroy(furniture.gameObject);
    }

    /// <summary>
    /// 이미 배치된 가구를 파괴하지 않고 그리드에서만 들어올린다 (칸을 비우고 목록에서 뺌).
    /// 이동시키는 동안 임시로 빼두는 용도. 되돌리거나 다시 놓을 때는 Drop을 사용한다.
    /// </summary>
    public void PickUp(PlacedFurniture furniture)
    {
        if (furniture == null) return;

        SetOccupant(furniture.Origin, furniture.Size, null);
        placedFurniture.Remove(furniture);
    }

    /// <summary>
    /// PickUp으로 들어올린 가구를 새 위치에 내려놓는다. 자리가 없으면 false를 반환하고 아무 것도 바뀌지 않는다.
    /// </summary>
    public bool Drop(PlacedFurniture furniture, Vector2Int origin, int rotation)
    {
        if (furniture == null || furniture.Data == null) return false;

        rotation = GridRotation.Normalize(rotation);
        Vector2Int size = GridRotation.RotateSize(furniture.Data.Size, rotation);
        if (!CanPlace(origin, size)) return false;

        Vector3 pos = GetFootprintCenter(origin, size);
        Quaternion rot = transform.rotation * GridRotation.ToQuaternion(rotation);
        furniture.transform.SetPositionAndRotation(pos, rot);

        furniture.Initialize(furniture.Data, origin, rotation);
        SetOccupant(origin, size, furniture);
        placedFurniture.Add(furniture);
        return true;
    }

    private void SetOccupant(Vector2Int origin, Vector2Int size, PlacedFurniture occupant)
    {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int c = origin + new Vector2Int(x, y);
                if (InBounds(c)) cells[c.x, c.y].SetOccupant(occupant);
            }
    }

    // ───────── 시각화 ─────────

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        // 격자선
        Gizmos.color = gridColor;
        for (int x = 0; x <= width; x++)
            Gizmos.DrawLine(new Vector3(x * cellSize, 0f, 0f),
                            new Vector3(x * cellSize, 0f, height * cellSize));
        for (int y = 0; y <= height; y++)
            Gizmos.DrawLine(new Vector3(0f, 0f, y * cellSize),
                            new Vector3(width * cellSize, 0f, y * cellSize));

        // 점유된 칸 (플레이 중에만)
        if (cells == null) return;
        Gizmos.color = occupiedColor;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (cells[x, y].IsOccupied)
                    Gizmos.DrawCube(
                        new Vector3((x + 0.5f) * cellSize, 0.01f, (y + 0.5f) * cellSize),
                        new Vector3(cellSize * 0.9f, 0.02f, cellSize * 0.9f));
    }
}