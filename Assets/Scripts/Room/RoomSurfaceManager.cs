using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public enum SurfaceSlot
{
    Wall,  // 벽지
    Floor, // 바닥
}

// 선택한 벽지/바닥 저장 데이터 (room_surface.json)
public class RoomSurfaceSave
{
    public string wallId;
    public string floorId;
}

/// <summary>
/// 방 모델(DalsuHome)의 벽지/바닥 머티리얼을 교체하고, 선택 상태를 저장/복원한다.
/// DalsuHome 오브젝트(또는 항상 켜져 있는 다른 오브젝트)에 붙여서 사용한다.
///
/// - '벽지' : wallPartNames의 파트들이 모두 같은 머티리얼을 공유하며 함께 바뀐다.
/// - '바닥' : floorPartNames의 파트(Floor)만 바뀐다.
/// - 미리 만들어 둔 머티리얼 에셋을 sharedMaterials로 끼우므로 머티리얼 인스턴스가 생기지 않는다.
/// </summary>
[DefaultExecutionOrder(-100)] // UI(RoomDecoTabUI)보다 먼저 저장된 선택을 적용해 둔다.
public class RoomSurfaceManager : MonoBehaviour
{
    private const string SaveFileName = "room_surface.json";

    [Header("Room Model")]
    [Tooltip("방 모델의 루트(DalsuHome). 비워두면 이 오브젝트를 쓴다.")]
    [SerializeField] private Transform roomRoot;

    [Tooltip("'벽지' 옵션에 따라 함께 바뀌는 파트 이름들 (roomRoot 하위에서 이름으로 찾는다)")]
    [SerializeField] private string[] wallPartNames = { "Column", "Door01", "Door02", "Knob", "Wall01", "Wall02", "Window" };

    [Tooltip("'바닥' 옵션에 따라 바뀌는 파트 이름들")]
    [SerializeField] private string[] floorPartNames = { "Floor" };

    [Header("Options")]
    [Tooltip("선택 가능한 벽지/바닥 목록. 같은 목록이 벽지 탭과 바닥 탭에 각각 나온다.")]
    [SerializeField] private SurfaceOption[] options;

    [Tooltip("저장된 선택이 없을 때 쓸 벽지. 비워두면 지금 모델에 씌워진 머티리얼을 그대로 둔다.")]
    [SerializeField] private SurfaceOption defaultWall;

    [Tooltip("저장된 선택이 없을 때 쓸 바닥. 비워두면 지금 모델에 씌워진 머티리얼을 그대로 둔다.")]
    [SerializeField] private SurfaceOption defaultFloor;

    private readonly List<Renderer> wallRenderers = new List<Renderer>();
    private readonly List<Renderer> floorRenderers = new List<Renderer>();

    public IReadOnlyList<SurfaceOption> Options => options ?? Array.Empty<SurfaceOption>();
    public SurfaceOption CurrentWall { get; private set; }
    public SurfaceOption CurrentFloor { get; private set; }

    /// 벽지/바닥이 바뀔 때 호출된다. (UI 선택 표시 갱신용)
    public event Action<SurfaceSlot, SurfaceOption> SurfaceChanged;

    private void Awake()
    {
        if (roomRoot == null) roomRoot = transform;

        CollectRenderers(wallPartNames, wallRenderers);
        CollectRenderers(floorPartNames, floorRenderers);

        LoadAndApply();
    }

    public SurfaceOption GetCurrent(SurfaceSlot slot) => slot == SurfaceSlot.Wall ? CurrentWall : CurrentFloor;

    // UI 버튼에서 호출: 선택을 적용하고 저장한다.
    public void Select(SurfaceSlot slot, SurfaceOption option) => Apply(slot, option, save: true);

    public void SelectWall(SurfaceOption option) => Select(SurfaceSlot.Wall, option);
    public void SelectFloor(SurfaceOption option) => Select(SurfaceSlot.Floor, option);

    private void Apply(SurfaceSlot slot, SurfaceOption option, bool save)
    {
        if (option == null || option.material == null)
        {
            Debug.LogWarning($"[RoomSurfaceManager] {slot}: 머티리얼이 지정되지 않은 옵션입니다.", option);
            return;
        }

        List<Renderer> targets = slot == SurfaceSlot.Wall ? wallRenderers : floorRenderers;
        foreach (Renderer r in targets)
        {
            if (r == null) continue;

            // sharedMaterials는 복사본을 돌려주므로, 바꾼 배열을 다시 대입해야 반영된다.
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = option.material;
            r.sharedMaterials = mats;
        }

        if (slot == SurfaceSlot.Wall) CurrentWall = option;
        else CurrentFloor = option;

        if (save) Save();

        SurfaceChanged?.Invoke(slot, option);
    }

    // ── 렌더러 수집 ──

    private void CollectRenderers(string[] partNames, List<Renderer> result)
    {
        result.Clear();
        if (partNames == null) return;

        foreach (string partName in partNames)
        {
            if (string.IsNullOrEmpty(partName)) continue;

            Transform part = FindDeep(roomRoot, partName);
            if (part != null && part.TryGetComponent(out Renderer r))
                result.Add(r);
            else
                Debug.LogWarning($"[RoomSurfaceManager] '{roomRoot.name}' 아래에서 렌더러가 있는 '{partName}'을(를) 찾지 못했습니다.", this);
        }
    }

    private static Transform FindDeep(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName) return child;

            Transform found = FindDeep(child, targetName);
            if (found != null) return found;
        }
        return null;
    }

    // ── 저장/불러오기 ──

    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private void LoadAndApply()
    {
        RoomSurfaceSave save = null;

        try
        {
            if (File.Exists(SavePath))
                save = JsonConvert.DeserializeObject<RoomSurfaceSave>(File.ReadAllText(SavePath));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RoomSurfaceManager] {SaveFileName}을(를) 읽지 못해 기본값을 씁니다: {e.Message}");
        }

        RestoreSlot(SurfaceSlot.Wall, save?.wallId, defaultWall, wallRenderers);
        RestoreSlot(SurfaceSlot.Floor, save?.floorId, defaultFloor, floorRenderers);
    }

    private void RestoreSlot(SurfaceSlot slot, string savedId, SurfaceOption fallback, List<Renderer> renderers)
    {
        SurfaceOption option = FindOption(savedId) ?? fallback;

        if (option != null)
        {
            Apply(slot, option, save: false);
            return;
        }

        // 저장된 것도 기본값도 없으면 모델은 그대로 두고, 지금 씌워진 머티리얼과 같은 옵션을 '현재 선택'으로 표시한다.
        Material current = renderers.Count > 0 && renderers[0] != null ? renderers[0].sharedMaterial : null;
        SurfaceOption matched = FindOptionByMaterial(current);
        if (slot == SurfaceSlot.Wall) CurrentWall = matched;
        else CurrentFloor = matched;
    }

    private void Save()
    {
        RoomSurfaceSave save = new RoomSurfaceSave
        {
            wallId = CurrentWall != null ? CurrentWall.Id : null,
            floorId = CurrentFloor != null ? CurrentFloor.Id : null,
        };

        try
        {
            File.WriteAllText(SavePath, JsonConvert.SerializeObject(save, Formatting.Indented));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RoomSurfaceManager] {SaveFileName} 저장 실패: {e.Message}");
        }
    }

    private SurfaceOption FindOption(string id)
    {
        if (string.IsNullOrEmpty(id) || options == null) return null;

        foreach (SurfaceOption option in options)
            if (option != null && option.Id == id) return option;
        return null;
    }

    private SurfaceOption FindOptionByMaterial(Material material)
    {
        if (material == null || options == null) return null;

        foreach (SurfaceOption option in options)
            if (option != null && option.material == material) return option;
        return null;
    }
}
