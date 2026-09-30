using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

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

    [Header("Dalsu")]
    [Tooltip("달수 캐릭터와 겹치는 칸에는 가구를 배치할 수 없게 막는다. (NavMesh Obstacle이 NavMesh Agent와 겹쳐 위치가 튀는 문제 방지)")]
    [SerializeField] private DalsuSpawnManager spawnManager;

    private OwnFurnitures ownFurnitures;

    private int selectedIndex = -1;
    private int rotation;

    private FurnitureData Selected =>
        furnitureList != null && selectedIndex >= 0 && selectedIndex < furnitureList.Length
            ? furnitureList[selectedIndex]
            : null;

    /// 현재 선택된 가구의 회전 반영 크기
    private Vector2Int CurrentSize =>
        Selected != null ? GridRotation.RotateSize(Selected.Size, rotation) : Vector2Int.one;

    private void Start()
    {
        if (cam == null) cam = Camera.main;

        ReadOwnFurnitureJson();
        CreateFurnitureButtons();
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
            Debug.Log("가구 JSON 파일이 존재하지 않습니다.");
        }

        if (ownFurnitures == null) ownFurnitures = new OwnFurnitures();
        if (ownFurnitures.furnitures == null) ownFurnitures.furnitures = new List<OwnFurniture>();
    }

    // 소지하고 있는 가구인지 확인한다.
    private bool IsOwned(FurnitureData data)
    {
        if (data == null) return false;

        foreach (OwnFurniture owned in ownFurnitures.furnitures)
        {
            if (owned != null && owned.id == data.Id)
                return true;
        }
        return false;
    }

    // 소지한 가구 하나당 버튼을 하나씩 만들어 클릭으로 선택하게 한다.
    private void CreateFurnitureButtons()
    {
        if (furnitureButtonPrefab == null || furnitureButtonParent == null || furnitureList == null) return;

        for (int i = 0; i < furnitureList.Length; i++)
        {
            FurnitureData data = furnitureList[i];
            if (!IsOwned(data)) continue; // 소지하지 않은 가구는 버튼을 만들지 않는다.

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
        }
    }

    // UI 버튼 OnClick: 가구 선택
    private void SelectFurniture(int index)
    {
        if (furnitureList == null || index < 0 || index >= furnitureList.Length) return;

        selectedIndex = index;
        Debug.Log($"Selected: {Selected.DisplayName}");
    }

    private void Update()
    {
        if (uiManager != null && !uiManager.IsDecoratePanelActive)
        {
            HidePreview();
            return;
        }

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
        bool valid = Selected != null && grid.CanPlace(origin, size) && !IsDalsuBlocking(origin, size);

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
    }

    // 달수(Navmesh Agent)가 현재 서 있는 칸에는 가구(Navmesh Obstacle)를 놓지 못하게 막는다.
    // 방 꾸미기 중에는 달수가 반투명해질 뿐 그대로 활성 상태로 돌아다니므로, 매 프레임 실제 위치로 확인한다.
    // (겹친 채로 놓으면, NavMeshObstacle이 carve될 때 NavMesh가 달수를
    //  가장 가까운 빈 자리로 강제로 옮겨버려서 위치가 튀는 것처럼 보인다.)
    private bool IsDalsuBlocking(Vector2Int origin, Vector2Int size)
    {
        if (spawnManager == null) return false;

        foreach (GameObject dalsu in spawnManager.SpawnedDalsus)
        {
            if (dalsu == null) continue;

            Vector2Int cell = grid.WorldToCell(dalsu.transform.position);
            if (cell.x >= origin.x && cell.x < origin.x + size.x &&
                cell.y >= origin.y && cell.y < origin.y + size.y)
            {
                return true;
            }
        }
        return false;
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
