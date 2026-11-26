using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 버튼 4개(체크 / 보관 / 이동 / 회전) + 편집 완료 버튼으로 동작하는 가구 배치 시스템.
/// PlacementController를 응용했으며 GridManager, PlacementGhost, PressHoldButton,
/// 저장 파일(furniture_data.json / placed_furniture.json)을 그대로 함께 쓴다.
///
/// 흐름
///  [대기]  배치 UI(가구 목록 + 편집 완료 버튼)가 보인다.
///          ├ 가구 목록 버튼을 누르면 → 새 가구를 들고 [편집] 상태로
///          └ 바닥에 놓인 가구를 탭하면 → 그 가구를 들어올려 [편집] 상태로
///  [편집]  배치 UI는 숨기고, 가구 근처에 조작 패널(체크/보관/이동/회전)을 띄운다.
///          가구는 고스트로 보이고, 위치는 체크를 누르기 전까지 '임시' 위치다.
///          ├ 이동(꾹 누른 채 드래그) : 드래그한 만큼 가구가 따라 움직인다.
///          ├ 회전 : 90도 시계 방향 회전.
///          ├ 체크 : 임시 위치를 확정하고 배치 UI를 다시 켠다. (놓을 수 없는 자리면 버튼 비활성)
///          └ 보관 : 가구를 보관함으로 돌려보내 씬에서 제거한다.
///  [편집 완료] 편집 중이던 가구는 확정(놓을 수 없으면 원래 자리로 되돌림)하고 방 꾸미기 화면을 닫는다.
///
/// PlacementController와 같은 저장 파일을 다루므로, 한 씬에는 둘 중 하나만 켜 두세요.
/// </summary>
public class FurnitureEditController : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private GridManager grid;
    [SerializeField] private Camera cam;
    [SerializeField] private FurnitureData[] furnitureList;

    [Header("Preview")]
    [SerializeField] private PlacementGhost ghost;
    [Tooltip("footprint를 표시할 Quad의 Renderer (선택). 회전/크기는 코드에서 맞춘다.")]
    [SerializeField] private Renderer highlight;
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;

    [Header("Grid Border")]
    [Tooltip("Grid 오브젝트에 붙인 GridBorderRenderer. 편집 모드일 때만 테두리를 보여준다.")]
    [SerializeField] private GridBorderRenderer gridBorder;
    [Tooltip("DecorateScreen: 방 꾸미기 화면이 열려 있는 동안 표시\nEditingFurniture: 가구 하나를 들고 편집하는 동안만 표시")]
    [SerializeField] private BorderVisibility borderVisibility = BorderVisibility.DecorateScreen;

    [Header("Deco Screen")]
    [Tooltip("방 꾸미기 화면이 열려 있을 때만 입력을 받는다. 편집 완료 시 이 매니저로 화면을 닫는다.")]
    [SerializeField] private DalsuRoom_UIManager uiManager;

    [Header("배치 UI (대기 상태에서 보이는 UI)")]
    [Tooltip("가구 목록 등. 편집 중에는 숨기고 체크 버튼을 누르면 다시 켠다.")]
    [SerializeField] private GameObject placementUI;
    [Tooltip("가구 선택 버튼 프리팹 (Button + 자식 TextMeshProUGUI, 아이콘 Image는 자식 이름 \"Icon\")")]
    [SerializeField] private Button furnitureButtonPrefab;
    [Tooltip("가구 선택 버튼들이 생성될 부모")]
    [SerializeField] private Transform furnitureButtonParent;
    [Tooltip("가구 편집 상태를 종료하는 버튼")]
    [SerializeField] private Button editCompleteButton;

    [Header("조작 패널 (편집 상태에서 보이는 UI)")]
    [SerializeField] private GameObject controlPanel;
    [SerializeField] private Button checkButton;
    [SerializeField] private Button storeButton;
    [SerializeField] private PressHoldButton moveButton;
    [SerializeField] private Button rotateButton;
    [Tooltip("비워두면 controlPanel에서 자동으로 찾는다.")]
    [SerializeField] private Canvas controlPanelCanvas;
    [Tooltip("가구 중심에서 얼마나 떨어진 곳에 패널을 띄울지 (월드 좌표 오프셋)")]
    [SerializeField] private Vector3 controlPanelOffset = new Vector3(0f, 1.5f, 0f);

    // ───────── 저장 데이터 ─────────
    private const string OwnFileName = "furniture_data.json";
    private const string PlacedFileName = "placed_furniture.json";

    private OwnFurnitures ownFurnitures;
    private readonly Dictionary<int, Button> furnitureButtons = new Dictionary<int, Button>();
    private bool furnitureButtonsDirty;
    private RectTransform controlPanelRect;

    // ───────── 편집 상태 ─────────
    private FurnitureData editData;          // null이 아니면 편집 중
    private PlacedFurniture editTarget;      // 이미 배치돼 있던 가구를 편집 중이면 그 가구 (새 가구면 null)
    private Vector2Int editOrigin;           // 임시 위치 (footprint의 (0,0) 칸)
    private int editRotation;                // 임시 회전 (0~3)

    // ───────── 드래그(이동 버튼) 상태 ─────────
    private bool isDragging;
    private Vector2 dragStartPoint;          // 이동 버튼을 누른 순간 포인터가 가리키던 그리드 좌표
    private Vector2 dragStartCenter;         // 그 순간의 가구 footprint 중심 (그리드 좌표)

    private bool decoActive;

    public enum BorderVisibility { DecorateScreen, EditingFurniture }

    private bool IsEditing => editData != null;
    private Vector2Int EditSize => GridRotation.RotateSize(editData.Size, editRotation);

    // ════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════

    private void Start()
    {
        if (cam == null) cam = Camera.main;
        if (gridBorder == null && grid != null) gridBorder = grid.GetComponent<GridBorderRenderer>();

        ReadOwnFurnitureJson();
        CreateFurnitureButtons();
        LoadPlacedFurnitureJson();

        if (checkButton != null) checkButton.onClick.AddListener(OnCheck);
        if (storeButton != null) storeButton.onClick.AddListener(OnStore);
        if (rotateButton != null) rotateButton.onClick.AddListener(OnRotate);
        if (editCompleteButton != null) editCompleteButton.onClick.AddListener(OnEditComplete);

        if (moveButton != null)
        {
            moveButton.Pressed += OnMovePressed;
            moveButton.Released += OnMoveReleased;
        }

        if (controlPanel != null)
        {
            controlPanelRect = controlPanel.GetComponent<RectTransform>();
            if (controlPanelCanvas == null)
                controlPanelCanvas = controlPanel.GetComponentInParent<Canvas>(true);
        }

        ShowIdleUI();
        HidePreview();
    }

    private void OnDestroy()
    {
        if (moveButton != null)
        {
            moveButton.Pressed -= OnMovePressed;
            moveButton.Released -= OnMoveReleased;
        }
    }

    // ════════════════════════════════════════════
    //  매 프레임
    // ════════════════════════════════════════════

    private void Update()
    {
        if (furnitureButtonsDirty) RefreshFurnitureButtons();

        bool active = uiManager == null || uiManager.IsDecoratePanelActive;
        if (active != decoActive)
        {
            decoActive = active;
            if (!active) CancelEdit();   // 다른 경로로 꾸미기 화면이 닫혀도 들고 있던 가구는 원래 자리로
            ShowIdleUI();
        }

        UpdateGridBorder(active);

        if (!active)
        {
            HidePreview();
            return;
        }

        if (IsEditing)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) OnRotate(); // 에디터 테스트용
            if (isDragging) UpdateDrag();
            UpdatePreview();
        }
        else
        {
            HandleSelectTap();
        }
    }

    // 편집 모드일 때만 그리드 테두리를 보여준다. (SetVisible은 값이 바뀔 때만 실제로 처리한다)
    private void UpdateGridBorder(bool decoScreenActive)
    {
        if (gridBorder == null) return;

        bool show = decoScreenActive &&
                    (borderVisibility == BorderVisibility.DecorateScreen || IsEditing);
        gridBorder.SetVisible(show);
    }

    // 대기 상태: 화면(UI가 아닌 곳)을 탭하면 그 칸에 놓인 가구를 편집 상태로 들어올린다.
    private void HandleSelectTap()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (!grid.TryGetCellFromScreen(pointer.position.ReadValue(), cam, out Vector2Int cell)) return;

        PlacedFurniture target = grid.GetFurnitureAt(cell);
        if (target != null) BeginEditExisting(target);
    }

    // ════════════════════════════════════════════
    //  편집 시작 / 종료
    // ════════════════════════════════════════════

    // 가구 목록 버튼 OnClick: 보관함의 가구를 새로 꺼내 편집을 시작한다.
    private void BeginEditNew(int index)
    {
        if (IsEditing || furnitureList == null || index < 0 || index >= furnitureList.Length) return;

        FurnitureData data = furnitureList[index];
        if (data == null || RemainingCount(data) <= 0) return;

        editData = data;
        editTarget = null;
        editRotation = 0;
        editOrigin = FindStartOrigin(EditSize);

        ShowEditUI();
    }

    // 배치돼 있던 가구를 그리드에서 들어올려 편집을 시작한다. 실제 오브젝트는 숨기고 고스트로 보여준다.
    private void BeginEditExisting(PlacedFurniture target)
    {
        if (IsEditing || target == null) return;

        editData = target.Data;
        editTarget = target;
        editOrigin = target.Origin;
        editRotation = target.Rotation;

        grid.PickUp(target);
        target.gameObject.SetActive(false);

        ShowEditUI();
    }

    // 임시 위치에 확정한다. 성공하면 true.
    private bool CommitEdit()
    {
        if (!IsEditing || !CanCommit()) return false;

        if (editTarget != null)
        {
            if (!grid.Drop(editTarget, editOrigin, editRotation)) return false;
            editTarget.gameObject.SetActive(true);
        }
        else
        {
            if (grid.PlaceFurniture(editData, editOrigin, editRotation) == null) return false;
        }

        SavePlacedFurnitureJson();
        ClearEditState();
        return true;
    }

    // 편집을 취소한다. 기존 가구는 들어올리기 전 자리로, 새 가구는 보관함 그대로.
    private void CancelEdit()
    {
        if (!IsEditing) return;

        if (editTarget != null)
        {
            // PickUp은 Origin/Rotation을 바꾸지 않으므로 그대로 원래 자리다.
            grid.Drop(editTarget, editTarget.Origin, editTarget.Rotation);
            editTarget.gameObject.SetActive(true);
        }

        ClearEditState();
    }

    private void ClearEditState()
    {
        editData = null;
        editTarget = null;
        isDragging = false;
        HidePreview();
    }

    // ════════════════════════════════════════════
    //  버튼 핸들러
    // ════════════════════════════════════════════

    // 1. 체크 : 위치 확정 + 배치 UI 활성화
    private void OnCheck()
    {
        if (!CommitEdit())
        {
            Debug.Log("이 자리에는 가구를 놓을 수 없습니다.");
            return;
        }
        ShowIdleUI();
    }

    // 2. 보관 : 보관함에 넣고 씬에서 제거
    private void OnStore()
    {
        if (!IsEditing) return;

        if (editTarget != null)
        {
            // 이미 PickUp으로 그리드/목록에서 빠져 있으므로 오브젝트만 파괴하면 된다.
            Destroy(editTarget.gameObject);
            editTarget = null;
            SavePlacedFurnitureJson();
        }
        // 새로 꺼낸 가구는 아직 씬에 만들어지지 않았으므로 상태만 정리하면 보관함으로 돌아간 셈이다.

        ClearEditState();
        ShowIdleUI();
    }

    // 3. 이동 : 버튼을 누른 순간부터 드래그한 거리만큼 가구를 옮긴다.
    //    (손가락 위치로 순간이동시키지 않고 상대 이동이라, 버튼이 가구를 가리지 않는다)
    private void OnMovePressed()
    {
        if (!IsEditing || !moveButton.HasHoldPosition) return;
        if (!grid.TryGetGridPointFromScreen(moveButton.HoldPosition, cam, out dragStartPoint)) return;

        dragStartCenter = OriginToCenter(editOrigin, EditSize);
        isDragging = true;
    }

    private void OnMoveReleased()
    {
        if (isDragging && moveButton.HasHoldPosition) UpdateDrag(); // 손을 뗀 위치까지 반영
        isDragging = false;
        // 위치는 아직 임시 상태. 체크 버튼을 눌러야 확정된다.
    }

    private void UpdateDrag()
    {
        if (!grid.TryGetGridPointFromScreen(moveButton.HoldPosition, cam, out Vector2 point)) return;

        Vector2 center = dragStartCenter + (point - dragStartPoint);
        editOrigin = ClampOrigin(grid.GetFootprintOrigin(center, EditSize), EditSize);
    }

    // 4. 회전 : 90도 시계 방향. 중심을 유지한 채 돌리고, 그리드 밖으로 나가면 안쪽으로 밀어 넣는다.
    private void OnRotate()
    {
        if (!IsEditing) return;

        Vector2 center = OriginToCenter(editOrigin, EditSize);
        editRotation = GridRotation.Normalize(editRotation + 1);
        editOrigin = ClampOrigin(grid.GetFootprintOrigin(center, EditSize), EditSize);

        // 드래그 도중에 회전했다면 기준점을 다시 잡아 가구가 튀지 않게 한다.
        if (isDragging && grid.TryGetGridPointFromScreen(moveButton.HoldPosition, cam, out Vector2 p))
        {
            dragStartPoint = p;
            dragStartCenter = OriginToCenter(editOrigin, EditSize);
        }
    }

    // 편집 완료 : 들고 있던 가구는 놓을 수 있으면 확정, 아니면 원래대로 되돌린 뒤 꾸미기 화면을 닫는다.
    private void OnEditComplete()
    {
        if (IsEditing && !CommitEdit()) CancelEdit();

        ShowIdleUI();
        HidePreview();
        if (gridBorder != null) gridBorder.SetVisible(false); // 닫기 애니메이션을 기다리지 않고 바로 숨긴다

        if (uiManager != null) uiManager.CloseDecorate();
    }

    // ════════════════════════════════════════════
    //  UI / 미리보기
    // ════════════════════════════════════════════

    private void ShowIdleUI()
    {
        if (placementUI != null) placementUI.SetActive(true);
        if (controlPanel != null) controlPanel.SetActive(false);
    }

    private void ShowEditUI()
    {
        if (placementUI != null) placementUI.SetActive(false);
        if (controlPanel != null) controlPanel.SetActive(true);
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        Vector2Int size = EditSize;
        bool valid = CanCommit();

        Vector3 pos = grid.GetFootprintCenter(editOrigin, size);
        Quaternion rot = grid.transform.rotation * GridRotation.ToQuaternion(editRotation);

        if (ghost != null) ghost.Show(editData, pos, rot, valid);
        UpdateHighlight(editOrigin, size, valid);

        if (checkButton != null) checkButton.interactable = valid;
        PlaceControlPanel(pos);
    }

    private void HidePreview()
    {
        if (highlight != null) highlight.gameObject.SetActive(false);
        if (ghost != null) ghost.Hide();
    }

    private void UpdateHighlight(Vector2Int origin, Vector2Int size, bool valid)
    {
        if (highlight == null) return;

        float cs = grid.CellSize;
        Transform t = highlight.transform;
        t.gameObject.SetActive(true);
        t.rotation = grid.transform.rotation * Quaternion.Euler(90f, 0f, 0f);
        t.position = grid.GetFootprintCenter(origin, size) + grid.transform.up * 0.01f;
        t.localScale = new Vector3(size.x * cs, size.y * cs, 1f);
        highlight.sharedMaterial = valid ? validMaterial : invalidMaterial;
    }

    // 조작 패널을 가구 위치 근처로 옮긴다. Canvas Render Mode(Overlay / Camera / World Space) 모두 지원.
    private void PlaceControlPanel(Vector3 furnitureWorldPos)
    {
        if (controlPanelRect == null || cam == null) return;

        Vector3 target = furnitureWorldPos + controlPanelOffset;

        if (controlPanelCanvas == null || controlPanelCanvas.renderMode == RenderMode.WorldSpace)
        {
            controlPanelRect.position = target;
            return;
        }

        Vector2 screen = cam.WorldToScreenPoint(target);

        if (controlPanelCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            controlPanelRect.position = screen;
        }
        else if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                     (RectTransform)controlPanelCanvas.transform, screen,
                     controlPanelCanvas.worldCamera, out Vector3 world))
        {
            controlPanelRect.position = world;
        }
    }

    // ════════════════════════════════════════════
    //  배치 계산
    // ════════════════════════════════════════════

    private bool CanCommit()
    {
        if (!IsEditing) return false;
        if (editTarget == null && RemainingCount(editData) <= 0) return false;
        return grid.CanPlace(editOrigin, EditSize);
    }

    private static Vector2 OriginToCenter(Vector2Int origin, Vector2Int size)
    {
        return new Vector2(origin.x + size.x * 0.5f, origin.y + size.y * 0.5f);
    }

    // footprint가 그리드 밖으로 나가지 않게 origin을 안쪽으로 밀어 넣는다.
    private Vector2Int ClampOrigin(Vector2Int origin, Vector2Int size)
    {
        return new Vector2Int(
            Mathf.Clamp(origin.x, 0, Mathf.Max(0, grid.Width - size.x)),
            Mathf.Clamp(origin.y, 0, Mathf.Max(0, grid.Height - size.y)));
    }

    // 새 가구를 꺼냈을 때 처음 보여줄 자리: 그리드 중앙에서 가장 가까운 빈 자리. 없으면 중앙.
    private Vector2Int FindStartOrigin(Vector2Int size)
    {
        Vector2 gridCenter = new Vector2(grid.Width * 0.5f, grid.Height * 0.5f);
        Vector2Int fallback = ClampOrigin(grid.GetFootprintOrigin(gridCenter, size), size);

        Vector2Int best = fallback;
        float bestDist = float.MaxValue;

        for (int x = 0; x <= grid.Width - size.x; x++)
            for (int y = 0; y <= grid.Height - size.y; y++)
            {
                Vector2Int origin = new Vector2Int(x, y);
                if (!grid.CanPlace(origin, size)) continue;

                float dist = (OriginToCenter(origin, size) - gridCenter).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = origin;
                }
            }

        return best;
    }

    // ════════════════════════════════════════════
    //  가구 목록 버튼
    // ════════════════════════════════════════════

    private void CreateFurnitureButtons()
    {
        if (furnitureButtonPrefab == null || furnitureButtonParent == null || furnitureList == null) return;

        for (int i = 0; i < furnitureList.Length; i++)
        {
            FurnitureData data = furnitureList[i];
            if (data == null || GetOwnedCount(data) <= 0) continue;

            int index = i;
            Button button = Instantiate(furnitureButtonPrefab, furnitureButtonParent);

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = data.DisplayName;

            Transform iconTransform = button.transform.Find("Icon");
            if (iconTransform != null && iconTransform.TryGetComponent(out Image icon))
                icon.sprite = data.Icon;

            button.onClick.AddListener(() => BeginEditNew(index));
            furnitureButtons[index] = button;
        }

        RefreshFurnitureButtons();
    }

    // 보관함에 남은 수량이 있는 가구의 버튼만 보이게 한다.
    private void RefreshFurnitureButtons()
    {
        furnitureButtonsDirty = false;

        foreach (KeyValuePair<int, Button> pair in furnitureButtons)
        {
            if (pair.Value == null) continue;
            pair.Value.gameObject.SetActive(RemainingCount(furnitureList[pair.Key]) > 0);
        }
    }

    private int RemainingCount(FurnitureData data) => GetOwnedCount(data) - CountPlaced(data);

    private int GetOwnedCount(FurnitureData data)
    {
        if (data == null || ownFurnitures?.furnitures == null) return 0;

        foreach (OwnFurniture owned in ownFurnitures.furnitures)
            if (owned != null && owned.id == data.Id)
                return owned.count;
        return 0;
    }

    // 씬에 놓여 있는 수량 (편집 중이라 들어올린 가구는 포함되지 않는다)
    private int CountPlaced(FurnitureData data)
    {
        if (data == null || grid == null) return 0;

        int count = 0;
        foreach (PlacedFurniture placed in grid.AllFurniture)
            if (placed != null && placed.Data != null && placed.Data.Id == data.Id)
                count++;
        return count;
    }

    // ════════════════════════════════════════════
    //  저장 / 불러오기 (PlacementController와 같은 파일 형식)
    // ════════════════════════════════════════════

    private void ReadOwnFurnitureJson()
    {
        string path = Path.Combine(Application.persistentDataPath, OwnFileName);

        if (File.Exists(path))
            ownFurnitures = JsonConvert.DeserializeObject<OwnFurnitures>(File.ReadAllText(path));
        else
            CreateDefaultFurnitureJson(path);

        if (ownFurnitures == null) ownFurnitures = new OwnFurnitures();
        if (ownFurnitures.furnitures == null) ownFurnitures.furnitures = new List<OwnFurniture>();
    }

    // furniture_data.json이 없을 때: furnitureList 앞쪽 가구 5개를 1개씩 소지한 상태로 만든다.
    private void CreateDefaultFurnitureJson(string path)
    {
        const int defaultFurnitureCount = 5;

        ownFurnitures = new OwnFurnitures { furnitures = new List<OwnFurniture>() };
        if (furnitureList != null)
        {
            foreach (FurnitureData data in furnitureList)
            {
                if (ownFurnitures.furnitures.Count >= defaultFurnitureCount) break;
                if (data != null) ownFurnitures.furnitures.Add(new OwnFurniture { id = data.Id, count = 1 });
            }
        }

        File.WriteAllText(path, JsonConvert.SerializeObject(ownFurnitures, Formatting.Indented));

        string placedPath = Path.Combine(Application.persistentDataPath, PlacedFileName);
        if (File.Exists(placedPath)) File.Delete(placedPath);

        Debug.Log($"가구 JSON 파일이 없어 새로 생성했습니다: {path}");
    }

    private FurnitureData FindFurnitureData(string id)
    {
        if (furnitureList == null) return null;
        foreach (FurnitureData data in furnitureList)
            if (data != null && data.Id == id) return data;
        return null;
    }

    private void SavePlacedFurnitureJson()
    {
        if (grid == null) return;

        PlacedFurnitureSave save = new PlacedFurnitureSave();
        foreach (PlacedFurniture placed in grid.AllFurniture)
        {
            if (placed == null || placed.Data == null) continue;
            save.items.Add(new PlacedFurnitureEntry
            {
                id = placed.Data.Id,
                x = placed.Origin.x,
                y = placed.Origin.y,
                rotation = placed.Rotation
            });
        }

        string path = Path.Combine(Application.persistentDataPath, PlacedFileName);
        File.WriteAllText(path, JsonConvert.SerializeObject(save, Formatting.Indented));

        furnitureButtonsDirty = true; // Destroy는 프레임 끝에 처리되므로 다음 프레임에 버튼을 갱신한다.
    }

    private void LoadPlacedFurnitureJson()
    {
        if (grid == null || furnitureList == null) return;

        string path = Path.Combine(Application.persistentDataPath, PlacedFileName);
        if (!File.Exists(path)) return;

        PlacedFurnitureSave save = JsonConvert.DeserializeObject<PlacedFurnitureSave>(File.ReadAllText(path));
        if (save?.items == null) return;

        foreach (PlacedFurnitureEntry entry in save.items)
        {
            if (entry == null) continue;
            FurnitureData data = FindFurnitureData(entry.id);
            if (data != null) grid.PlaceFurniture(data, new Vector2Int(entry.x, entry.y), entry.rotation);
        }

        RefreshFurnitureButtons();
    }
}
