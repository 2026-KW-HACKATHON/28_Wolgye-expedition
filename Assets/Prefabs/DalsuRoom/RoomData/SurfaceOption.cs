using UnityEngine;

/// <summary>
/// 벽지/바닥 선택지 하나. Project 창 우클릭 > Create > Room > Surface Data 로 생성한다.
///
/// DalsuHouse 머티리얼은 벽과 바닥이 한 장에 들어 있는 아틀라스이므로,
/// 같은 에셋이 '벽지' 목록(wallName/wallThumbnail)과 '바닥' 목록(floorName/floorThumbnail)에 모두 쓰인다.
/// - 벽지로 고르면 벽 파트(Column, Door, Knob, Wall, Window)에 material을 씌운다.
/// - 바닥으로 고르면 Floor에만 material을 씌운다.
///
/// 주의: ScriptableObject는 클래스 이름과 파일 이름이 같아야 에셋이 스크립트를 찾을 수 있어서
///       클래스 이름을 파일 이름(SurfaceOption)에 맞췄다. 메타 GUID는 그대로라 기존 SurfaceData 에셋은 그대로 동작한다.
/// </summary>
[CreateAssetMenu(fileName = "SurfaceData", menuName = "Room/Surface Data")]
public class SurfaceOption : ScriptableObject
{
    [Tooltip("저장/불러오기에 쓰이는 고유 ID. 비워두면 에셋 이름을 쓴다. 한 번 정하면 바꾸지 마세요.")]
    [SerializeField] private string id;

    public string wallName;
    public string floorName;

    public Sprite wallThumbnail;
    public Sprite floorThumbnail;

    public Material material;

    public string Id => string.IsNullOrEmpty(id) ? name : id;

    public string GetDisplayName(SurfaceSlot slot)
    {
        string displayName = slot == SurfaceSlot.Wall ? wallName : floorName;
        return string.IsNullOrEmpty(displayName) ? name : displayName;
    }

    public Sprite GetThumbnail(SurfaceSlot slot) => slot == SurfaceSlot.Wall ? wallThumbnail : floorThumbnail;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id)) id = name;
    }
}
