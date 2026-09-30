using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 가구 배치 입력 처리.
/// 숫자키 1~9: 가구 선택 / R: 시계 방향 회전 / Q: 반시계 방향 회전
/// 좌클릭: 배치 / 우클릭: 제거
/// </summary>
public class PlacementController : MonoBehaviour
{
    [SerializeField] private GridManager grid;
    [SerializeField] private Camera cam;
    [SerializeField] private FurnitureData[] furnitureList;

    [Header("Footprint Highlight")]
    [Tooltip("Quad의 Renderer. 회전/크기는 코드에서 맞춰줍니다.")]
    [SerializeField] private Renderer highlight;
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;

    [Header("Ghost Preview")]
    [SerializeField] private PlacementGhost ghost;

    private int selectedIndex;
    private int rotation;

    private FurnitureData Selected =>
        furnitureList != null && furnitureList.Length > 0 ? furnitureList[selectedIndex] : null;

    /// <summary>현재 선택된 가구의 회전 반영 크기</summary>
    private Vector2Int CurrentSize =>
        Selected != null ? GridRotation.RotateSize(Selected.Size, rotation) : Vector2Int.one;

    private void Start()
    {
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        HandleKeys(Keyboard.current);

        if (!grid.TryGetGridPointFromScreen(mouse.position.ReadValue(), cam, out Vector2 point))
        {
            HidePreview();
            return;
        }

        Vector2Int hoveredCell = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
        if (!grid.InBounds(hoveredCell))
        {
            HidePreview();
            return;
        }

        Vector2Int size = CurrentSize;
        Vector2Int origin = grid.GetFootprintOrigin(point, size);
        bool valid = Selected != null && grid.CanPlace(origin, size);

        UpdateHighlight(origin, size, valid);
        UpdateGhost(origin, size, valid);

        if (mouse.leftButton.wasPressedThisFrame) TryPlace(origin);
        else if (mouse.rightButton.wasPressedThisFrame) TryRemove(hoveredCell);
    }

    private void HandleKeys(Keyboard keyboard)
    {
        if (keyboard == null) return;

        if (keyboard.rKey.wasPressedThisFrame) rotation = GridRotation.Normalize(rotation + 1);
        if (keyboard.qKey.wasPressedThisFrame) rotation = GridRotation.Normalize(rotation - 1);

        if (furnitureList == null) return;
        int count = Mathf.Min(9, furnitureList.Length);
        for (int i = 0; i < count; i++)
        {
            if (keyboard[Key.Digit1 + i].wasPressedThisFrame)
            {
                selectedIndex = i;
                Debug.Log($"Selected: {Selected.DisplayName}");
            }
        }
    }

    private void HidePreview()
    {
        highlight.gameObject.SetActive(false);
        if (ghost != null) ghost.Hide();
    }

    private void UpdateGhost(Vector2Int origin, Vector2Int size, bool valid)
    {
        if (ghost == null) return;

        Vector3 pos = grid.GetFootprintCenter(origin, size);
        Quaternion rot = grid.transform.rotation * GridRotation.ToQuaternion(rotation);
        ghost.Show(Selected, pos, rot, valid);
    }

    private void UpdateHighlight(Vector2Int origin, Vector2Int size, bool valid)
    {
        float cs = grid.CellSize;

        Transform t = highlight.transform;
        t.gameObject.SetActive(true);
        t.rotation = grid.transform.rotation * Quaternion.Euler(90f, 0f, 0f);
        t.position = grid.GetFootprintCenter(origin, size) + grid.transform.up * 0.01f;
        t.localScale = new Vector3(size.x * cs, size.y * cs, 1f);

        highlight.sharedMaterial = valid ? validMaterial : invalidMaterial;
    }

    private void TryPlace(Vector2Int origin)
    {
        if (Selected == null) return;

        if (grid.PlaceFurniture(Selected, origin, rotation) == null)
            Debug.Log($"Cannot place {Selected.DisplayName} at {origin} (rotation {rotation})");
    }

    private void TryRemove(Vector2Int cell)
    {
        PlacedFurniture target = grid.GetFurnitureAt(cell);
        if (target != null) grid.RemoveFurniture(target);
    }
}