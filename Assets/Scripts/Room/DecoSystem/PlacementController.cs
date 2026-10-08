using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// 가구 배치 입력 처리.
/// 가구 선택: UI 버튼 (소지한 가구 하나당 버튼 하나) / R: 시계 방향 회전 / Q: 반시계 방향 회전
/// 좌클릭: 배치 / 우클릭: 제거

// 소지한 가구 데이터 (furniture_data.json). DalsuSpawnManager의 OwnDalsus/OwnDalsu 패턴과 동일하다.
public class OwnFurnitures
{
    public List<OwnFurniture> furnitures;
}
public class OwnFurniture
{
    public string id;

    // 소지한 수량. 이 수량만큼만 동시에 배치할 수 있다.
    public int count;
}

// 배치된 가구 상태 (placed_furniture.json). 세션이 끝나도 배치 상태가 유지되도록 저장/복원한다.
public class PlacedFurnitureSave
{
    public List<PlacedFurnitureEntry> items = new List<PlacedFurnitureEntry>();
}
public class PlacedFurnitureEntry
{
    public string id;
    public int x;
    public int y;
    public int rotation;
}

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

    [Header("UI")]
    [Tooltip("방 꾸미기 화면이 열려 있을 때만 배치 입력을 받는다.")]
    [SerializeField] private DalsuRoom_UIManager uiManager;

    [Tooltip("가구 선택 버튼 프리팹 (Button + 자식에 TextMeshProUGUI, 아이콘용 Image는 자식 이름 \"Icon\")")]
    [SerializeField] private Button furnitureButtonPrefab;
    [Tooltip("가구 선택 버튼들이 생성될 부모")]
    [SerializeField] private Transform furnitureButtonParent;

    [Tooltip("가구를 선택했을 때만 나타나는 회전 버튼 (R키와 동일하게 동작)")]
    [SerializeField] private Button rotateButton;
    [Tooltip("가구를 선택했을 때만 나타나는 ui 패널")]
    [SerializeField] private GameObject decoControlPanel;

    [Tooltip("decoControlPanel이 속한 Canvas. 비워두면 decoControlPanel에서 자동으로 찾는다.")]
    [SerializeField] private Canvas decoControlCanvas;
    [Tooltip("고스트 프리뷰 위치에서 얼마나 떨어진 곳에 패널을 띄울지 (월드 좌표 오프셋)")]
    [SerializeField] private Vector3 decoControlPanelOffset = new Vector3(0f, 1.5f, 0f);
    [Tooltip("집어든 가구를 보관함으로 돌려보내는 회수 버튼 (아직 배치 전인 선택을 취소할 때도 쓰인다)")]
    [SerializeField] private Button retrieveButton;
    [Tooltip("누르고 있는 동안에만 선택한 가구가 마우스를 따라 움직이는 '이동' 버튼")]
    [SerializeField] private PressHoldButton moveButton;

    [Header("Dalsu")]
    [Tooltip("달수 캐릭터와 겹치는 칸에는 가구를 배치할 수 없게 막는다. (NavMesh Obstacle이 NavMesh Agent와 겹쳐 위치가 튀는 문제 방지)")]
    [SerializeField] private DalsuSpawnManager spawnManager;

    private OwnFurnitures ownFurnitures;

    // furnitureList 인덱스 → 그 가구의 선택 버튼. 남은 수량이 없으면 버튼을 숨긴다.
    private readonly Dictionary<int, Button> furnitureButtons = new Dictionary<int, Button>();
    private bool furnitureButtonsDirty;
    private RectTransform decoControlPanelRect;

    private int selectedIndex = -1;
    private int rotation;

    // 이미 배치되어 있다가 클릭으로 선택한 가구. null이 아니면 선택된 상태 (아직 집어든 것은 아닐 수 있다).
    private PlacedFurniture editingFurniture;

    // '이동' 버튼을 누르고 있어서, 실제로 고스트/하이라이트가 마우스를 따라다니는 중인지.
    private bool isMoving;

    // 팔레트에서 새 가구를 선택한 시점의 마우스 위치. '이동' 버튼을 누르기 전까지는 이 자리에 고정해서 보여준다.
    private Vector2Int newItemPreviewOrigin;

    private FurnitureData Selected =>
        editingFurniture != null
            ? editingFurniture.Data
            : (furnitureList != null && selectedIndex >= 0 && selectedIndex < furnitureList.Length
                ? furnitureList[selectedIndex]
                : null);

    /// 현재 선택된 가구의 회전 반영 크기
    private Vector2Int CurrentSize =>
        Selected != null ? GridRotation.RotateSize(Selected.Size, rotation) : Vector2Int.one;

    private void Start()
    {
        if (cam == null) cam = Camera.main;

        ReadOwnFurnitureJson();
        CreateFurnitureButtons();
        LoadPlacedFurnitureJson();

        if (rotateButton != null && decoControlPanel != null)
        {
            rotateButton.onClick.AddListener(RotateClockwise);
            decoControlPanel.SetActive(false);
        }

        if (retrieveButton != null)
            retrieveButton.onClick.AddListener(RetrieveSelected);

        if (moveButton != null)
        {
            moveButton.Pressed += OnMovePressed;
            moveButton.Released += OnMoveReleased;
        }

        if (decoControlPanel != null)
        {
            decoControlPanelRect = decoControlPanel.GetComponent<RectTransform>();
            if (decoControlCanvas == null)
                decoControlCanvas = decoControlPanel.GetComponentInParent<Canvas>();
        }
    }

    // 소지한 가구 목록(furniture_data.json)을 읽어온다.
    private void ReadOwnFurnitureJson()
    {
        string path = Path.Combine(Application.persistentDataPath, "furniture_data.json");

        if (File.Exists(path))
        {
            string jsonString = File.ReadAllText(path);
            ownFurnitures = JsonConvert.DeserializeObject<OwnFurnitures>(jsonString);
        }
        else
        {
            CreateDefaultFurnitureJson(path);
        }

        if (ownFurnitures == null) ownFurnitures = new OwnFurnitures();
        if (ownFurnitures.furnitures == null) ownFurnitures.furnitures = new List<OwnFurniture>();
    }

    // furniture_data.json이 없을 때: furnitureList 앞쪽 가구 5개를 1개씩 소지한 상태로 파일을 만든다.
    // 아무것도 배치되지 않은 상태로 시작해야 하므로, 이전에 남아 있던 placed_furniture.json은 지운다.
    private void CreateDefaultFurnitureJson(string path)
    {
        const int defaultFurnitureCount = 5;

        ownFurnitures = new OwnFurnitures { furnitures = new List<OwnFurniture>() };

        if (furnitureList != null)
        {
            foreach (FurnitureData data in furnitureList)
            {
                if (ownFurnitures.furnitures.Count >= defaultFurnitureCount) break;
                if (data == null) continue;

                ownFurnitures.furnitures.Add(new OwnFurniture { id = data.Id, count = 1 });
            }
        }

        if (ownFurnitures.furnitures.Count < defaultFurnitureCount)
            Debug.LogWarning($"furnitureList에 가구가 {ownFurnitures.furnitures.Count}개뿐이라 {defaultFurnitureCount}개를 채우지 못했습니다.");

        string jsonString = JsonConvert.SerializeObject(ownFurnitures, Formatting.Indented);
        File.WriteAllText(path, jsonString);

        string placedPath = Path.Combine(Application.persistentDataPath, "placed_furniture.json");
        if (File.Exists(placedPath)) File.Delete(placedPath);

        Debug.Log($"가구 JSON 파일이 없어 새로 생성했습니다: {path}");
    }

    // id로 FurnitureData를 찾는다.
    private FurnitureData FindFurnitureData(string id)
    {
        if (furnitureList == null) return null;

        foreach (FurnitureData data in furnitureList)
        {
            if (data != null && data.Id == id)
                return data;
        }
        return null;
    }

    // 지금 grid에 배치되어 있는 상태를 placed_furniture.json에 저장한다.
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

        string path = Path.Combine(Application.persistentDataPath, "placed_furniture.json");
        string jsonString = JsonConvert.SerializeObject(save, Formatting.Indented);
        File.WriteAllText(path, jsonString);

        // 배치 상태가 바뀌었으니 다음 Update에서 버튼 목록을 갱신한다.
        // (회수 시 Destroy는 프레임 끝에 처리되므로, 바로 세지 않고 다음 프레임에 센다)
        furnitureButtonsDirty = true;
    }

    // placed_furniture.json에 저장된 배치 상태를 grid에 복원한다.
    private void LoadPlacedFurnitureJson()
    {
        if (grid == null || furnitureList == null) return;

        string path = Path.Combine(Application.persistentDataPath, "placed_furniture.json");
        if (!File.Exists(path)) return;

        string jsonString = File.ReadAllText(path);
        PlacedFurnitureSave save = JsonConvert.DeserializeObject<PlacedFurnitureSave>(jsonString);
        if (save == null || save.items == null) return;

        foreach (PlacedFurnitureEntry entry in save.items)
        {
            if (entry == null) continue;

            FurnitureData data = FindFurnitureData(entry.id);
            if (data == null) continue;

            grid.PlaceFurniture(data, new Vector2Int(entry.x, entry.y), entry.rotation);
        }

        RefreshFurnitureButtons();
    }

    // 소지하고 있는 수량을 반환한다. 소지하지 않았으면 0.
    private int GetOwnedCount(FurnitureData data)
    {
        if (data == null || ownFurnitures?.furnitures == null) return 0;

        foreach (OwnFurniture owned in ownFurnitures.furnitures)
        {
            if (owned != null && owned.id == data.Id)
                return owned.count;
        }
        return 0;
    }

    // 지금 씬에 배치되어 있는 수량을 센다 (grid에 실제로 놓여 있는 개수 기준).
    private int CountPlaced(FurnitureData data)
    {
        if (data == null || grid == null) return 0;

        int count = 0;
        foreach (PlacedFurniture placed in grid.AllFurniture)
        {
            if (placed != null && placed.Data != null && placed.Data.Id == data.Id)
                count++;
        }
        return count;
    }

    // 소지한 가구 하나당 버튼을 하나씩 만들어 클릭으로 선택하게 한다.
    private void CreateFurnitureButtons()
    {
        if (furnitureButtonPrefab == null || furnitureButtonParent == null || furnitureList == null) return;

        for (int i = 0; i < furnitureList.Length; i++)
        {
            FurnitureData data = furnitureList[i];
            if (GetOwnedCount(data) <= 0) continue; // 소지 수량이 없으면 버튼을 만들지 않는다.

            int index = i; // 람다 캡처용 지역 변수

            Button button = Instantiate(furnitureButtonPrefab, furnitureButtonParent);

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = data.DisplayName;

            Transform iconTransform = button.transform.Find("Icon");
            if (iconTransform != null)
            {
                Image icon = iconTransform.GetComponent<Image>();
                if (icon != null)
                    icon.sprite = data.Icon;
            }

            button.onClick.AddListener(() => SelectFurniture(index));
            furnitureButtons[index] = button;
        }

        RefreshFurnitureButtons();
    }

    // 남은 수량(소지 수량 - 배치된 수량)이 있는 가구의 버튼만 보이게 한다.
    private void RefreshFurnitureButtons()
    {
        furnitureButtonsDirty = false;

        foreach (KeyValuePair<int, Button> pair in furnitureButtons)
        {
            if (pair.Value == null) continue;

            FurnitureData data = furnitureList[pair.Key];
            bool hasRemaining = CountPlaced(data) < GetOwnedCount(data);
            pair.Value.gameObject.SetActive(hasRemaining);
        }
    }

    // UI 버튼 OnClick: 가구 선택
    private void SelectFurniture(int index)
    {
        if (furnitureList == null || index < 0 || index >= furnitureList.Length) return;

        ResetSelection(); // 다른 걸 고르기 전에, 기존에 선택/이동 중이던 것부터 정리한다.
        selectedIndex = index;
        newItemPreviewOrigin = ComputeGridCenterOrigin();
        Debug.Log($"Selected: {Selected.DisplayName}");
    }

    // 그리드 한가운데(비슷한 위치)를, 현재 선택된 가구 크기 기준 footprint origin으로 변환한다.
    private Vector2Int ComputeGridCenterOrigin()
    {
        Vector2 centerPoint = new Vector2(grid.Width * 0.5f, grid.Height * 0.5f);
        return grid.GetFootprintOrigin(centerPoint, CurrentSize);
    }

    private void Update()
    {
        if (furnitureButtonsDirty) RefreshFurnitureButtons();

        if (uiManager != null && !uiManager.IsDecoratePanelActive)
        {
            ResetSelection();
            HidePreview();
            SetRotateButtonActive(false);
            return;
        }

        SetRotateButtonActive(Selected != null);

        Pointer pointer = Pointer.current;
        if (pointer == null) return;

        HandleKeys(Keyboard.current);

        bool hasPoint = grid.TryGetGridPointFromScreen(pointer.position.ReadValue(), cam, out Vector2 point);
        Vector2Int hoveredCell = hasPoint
            ? new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y))
            : default;
        bool hoveredInBounds = hasPoint && grid.InBounds(hoveredCell);

        UpdatePreview(point, hoveredInBounds);

        if (pointer.press.wasPressedThisFrame)
        {
            if (Selected == null && hoveredInBounds) SelectPlacedFurniture(hoveredCell);
        }
        else if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)  // 우클릭은 마우스 전용
        {
            if (editingFurniture != null || selectedIndex >= 0) ResetSelection();
            else if (hoveredInBounds) TryRemove(hoveredCell);
        }
    }

    // 고스트/하이라이트/패널 미리보기 갱신. '이동' 버튼을 누르고 있을 때만 실제로 마우스를 따라간다.
    private void UpdatePreview(Vector2 point, bool hoveredInBounds)
    {
        if (editingFurniture != null && !isMoving)
        {
            // 선택만 된 상태: 가구는 원래 자리에 그대로 두고, 패널만 그 위치 근처에 띄운다.
            HidePreview();
            UpdateDecoControlPanel(grid.GetFootprintCenter(editingFurniture.Origin, editingFurniture.Size));
            return;
        }

        if (selectedIndex >= 0 && Selected != null && !isMoving)
        {
            // 새로 선택한 가구: 선택한 시점의 위치에 고정해서 보여준다 ('이동' 버튼을 눌러야 움직이기 시작한다).
            Vector2Int previewSize = CurrentSize;
            bool previewHasRemaining = CountPlaced(Selected) < GetOwnedCount(Selected);
            bool previewValid = previewHasRemaining
                && grid.CanPlace(newItemPreviewOrigin, previewSize);

            UpdateHighlight(newItemPreviewOrigin, previewSize, previewValid);
            UpdateGhost(newItemPreviewOrigin, previewSize, previewValid);
            return;
        }

        if (!isMoving || !hoveredInBounds)
        {
            HidePreview();
            return;
        }

        Vector2Int size = CurrentSize;
        Vector2Int origin = grid.GetFootprintOrigin(point, size);
        bool hasRemaining = Selected != null && CountPlaced(Selected) < GetOwnedCount(Selected);
        bool valid = hasRemaining && grid.CanPlace(origin, size);

        UpdateHighlight(origin, size, valid);
        UpdateGhost(origin, size, valid);
    }

    private void HandleKeys(Keyboard keyboard)
    {
        if (keyboard == null) return;

        if (keyboard.rKey.wasPressedThisFrame) RotateClockwise();
        if (keyboard.qKey.wasPressedThisFrame) rotation = GridRotation.Normalize(rotation - 1);
    }

    // 회전 버튼 OnClick / R키: 시계 방향으로 90도 회전.
    // 이미 배치된 가구가(이동 중이 아닌 채로) 선택되어 있을 때는, 화면에 보이는 고스트가 없으니
    // 회전해도 눈에 보이는 변화가 없으므로, 실제로 놓인 가구 자체를 그 자리에서 바로 회전시킨다.
    private void RotateClockwise()
    {
        int newRotation = GridRotation.Normalize(rotation + 1);

        if (editingFurniture != null && !isMoving)
        {
            TryRotateInPlace(newRotation);
            return;
        }

        rotation = newRotation;
    }

    // 제자리(마우스를 따라 이동 중이 아닌)에서 회전을 시도한다.
    // 회전한 크기가 옆 가구나 그리드 경계와 겹쳐서 들어갈 자리가 없으면, 원래 회전값으로 되돌린다.
    private void TryRotateInPlace(int newRotation)
    {
        PlacedFurniture furniture = editingFurniture;
        Vector2Int origin = furniture.Origin;
        int oldRotation = furniture.Rotation;

        grid.PickUp(furniture);

        if (grid.Drop(furniture, origin, newRotation))
        {
            rotation = newRotation;
            SavePlacedFurnitureJson();
        }
        else
        {
            grid.Drop(furniture, origin, oldRotation); // 회전이 안 되면 회전하기 전의 자리로 되돌림
            Debug.Log($"Cannot rotate {furniture.Data.DisplayName}: 공간이 부족합니다.");
        }
    }

    // 아무것도 선택되어 있지 않을 때 배치된 가구를 클릭하면 선택 상태로 전환한다.
    // 아직 그리드에서 들어올리지는 않는다 - 제자리에 그대로 둔 채 주변에 조작 UI만 띄운다.
    private void SelectPlacedFurniture(Vector2Int cell)
    {
        PlacedFurniture target = grid.GetFurnitureAt(cell);
        if (target == null) return;

        editingFurniture = target;
        selectedIndex = -1;
        rotation = target.Rotation;
    }

    // '이동' 버튼을 누르기 시작했을 때: 기존에 배치돼 있던 가구라면 이제서야 그리드에서 들어올린다.
    private void OnMovePressed()
    {
        if (Selected == null) return;

        isMoving = true;

        if (editingFurniture != null)
        {
            grid.PickUp(editingFurniture);
            editingFurniture.gameObject.SetActive(false); // 실제 오브젝트는 숨기고, 고스트 프리뷰가 대신 보여준다.
        }
    }

    // '이동' 버튼에서 손을 뗐을 때: 그 시점의 마우스 위치에 배치/이동을 확정한다.
    private void OnMoveReleased()
    {
        if (!isMoving) return;
        isMoving = false;

        Vector2Int size = CurrentSize;
        bool canCommit = false;
        Vector2Int origin = default;

        if (Pointer.current != null &&
            grid.TryGetGridPointFromScreen(Pointer.current.position.ReadValue(), cam, out Vector2 point))
        {
            Vector2Int hoveredCell = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
            if (grid.InBounds(hoveredCell))
            {
                origin = grid.GetFootprintOrigin(point, size);
                canCommit = true;
            }
        }

        if (editingFurniture != null)
        {
            PlacedFurniture furniture = editingFurniture;
            bool moved = canCommit && grid.Drop(furniture, origin, rotation);

            if (!moved)
            {
                Debug.Log($"Cannot move {furniture.Data.DisplayName} to {origin} (rotation {rotation})");
                grid.Drop(furniture, furniture.Origin, furniture.Rotation); // 실패하면 원래 자리로 복원
            }

            furniture.gameObject.SetActive(true);
            editingFurniture = null;
            SavePlacedFurnitureJson();
        }
        else if (selectedIndex >= 0 && Selected != null)
        {
            FurnitureData data = Selected;

            if (!canCommit)
            {
                Debug.Log($"Cannot place {data.DisplayName}: 그리드 밖입니다.");
            }
            else if (CountPlaced(data) >= GetOwnedCount(data))
            {
                Debug.Log($"{data.DisplayName}: 소지 수량을 모두 배치했습니다.");
            }
            else if (grid.PlaceFurniture(data, origin, rotation) == null)
            {
                Debug.Log($"Cannot place {data.DisplayName} at {origin} (rotation {rotation})");
            }
            else
            {
                SavePlacedFurnitureJson();
            }

            selectedIndex = -1;
        }

        HidePreview();
    }

    // 선택/이동 상태를 완전히 초기화한다. 이동(집어든 상태) 중이었다면 원래 자리로 되돌린다.
    private void ResetSelection()
    {
        if (isMoving && editingFurniture != null)
        {
            grid.Drop(editingFurniture, editingFurniture.Origin, editingFurniture.Rotation);
            editingFurniture.gameObject.SetActive(true);
        }

        isMoving = false;
        editingFurniture = null;
        selectedIndex = -1;
    }

    // 회수 버튼 OnClick: 선택된 가구를 보관함으로 돌려보낸다 (팔레트 선택 중이었다면 선택만 취소).
    private void RetrieveSelected()
    {
        if (editingFurniture != null)
        {
            PlacedFurniture furniture = editingFurniture;

            if (isMoving)
                Destroy(furniture.gameObject); // 이미 그리드에서 들어올려진 상태이므로 바로 파괴
            else
                grid.RemoveFurniture(furniture); // 아직 그리드에 그대로 있으므로 정식으로 제거

            isMoving = false;
            editingFurniture = null;
            SavePlacedFurnitureJson();
        }
        else
        {
            selectedIndex = -1;
        }
    }

    // 가구가 선택되어 있을 때만 회전 버튼을 보여준다.
    private void SetRotateButtonActive(bool active)
    {
        if (decoControlPanel != null)
        {
            decoControlPanel.gameObject.SetActive(active);
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

        UpdateDecoControlPanel(pos);
    }

    // decoControlPanel을 고스트 프리뷰 위치(pos + 오프셋) 근처의 화면 위치로 옮긴다.
    // Canvas의 Render Mode(Overlay / Camera / World Space)에 상관없이 동작하도록 처리한다.
    private void UpdateDecoControlPanel(Vector3 worldPos)
    {
        if (decoControlPanelRect == null || cam == null) return;


        if (decoControlCanvas != null && decoControlCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            decoControlPanel.transform.position = worldPos + decoControlPanelOffset;
        }

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

    private void TryRemove(Vector2Int cell)
    {
        PlacedFurniture target = grid.GetFurnitureAt(cell);
        if (target != null)
        {
            grid.RemoveFurniture(target);
            SavePlacedFurnitureJson();
        }
    }
}