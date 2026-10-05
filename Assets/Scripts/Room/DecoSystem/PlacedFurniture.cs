using UnityEngine;

/// <summary>
/// 씬에 실제로 배치된 가구 하나. GridManager가 생성 시 자동으로 붙여줍니다.
/// 여러 칸짜리 가구는 차지한 모든 GridCell이 같은 인스턴스를 가리킵니다.
/// </summary>
public class PlacedFurniture : MonoBehaviour
{
    public FurnitureData Data { get; private set; }

    /// <summary>footprint의 (0,0) 쪽 모서리 칸</summary>
    public Vector2Int Origin { get; private set; }

    /// <summary>0~3 (90도 단위)</summary>
    public int Rotation { get; private set; }

    /// <summary>회전이 반영된 실제 점유 크기</summary>
    public Vector2Int Size => GridRotation.RotateSize(Data.Size, Rotation);

    public void Initialize(FurnitureData data, Vector2Int origin, int rotation)
    {
        Data = data;
        Origin = origin;
        Rotation = GridRotation.Normalize(rotation);
        gameObject.name = $"{data.DisplayName} ({origin.x},{origin.y}) r{Rotation}";
    }
}