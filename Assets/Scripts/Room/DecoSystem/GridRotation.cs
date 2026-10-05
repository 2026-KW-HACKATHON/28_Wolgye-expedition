using UnityEngine;

/// <summary>
/// 그리드 가구의 90도 단위 회전 계산.
/// 회전값 규칙: 0 = 0°, 1 = 90°, 2 = 180°, 3 = 270° (위에서 봤을 때 시계 방향)
/// 프리팹 규칙: 회전 0일 때 가구의 정면이 +Z, size.x가 X축, size.y가 Z축 방향.
/// </summary>
public static class GridRotation
{
    /// <summary>임의의 정수를 0~3 범위로 정규화 (음수도 처리)</summary>
    public static int Normalize(int rotation)
    {
        return ((rotation % 4) + 4) % 4;
    }

    /// <summary>회전을 반영한 footprint 크기. 90°, 270°에서 가로세로가 바뀜.</summary>
    public static Vector2Int RotateSize(Vector2Int size, int rotation)
    {
        return Normalize(rotation) % 2 == 1 ? new Vector2Int(size.y, size.x) : size;
    }

    /// <summary>그리드 로컬 기준 회전 (Y축)</summary>
    public static Quaternion ToQuaternion(int rotation)
    {
        return Quaternion.Euler(0f, Normalize(rotation) * 90f, 0f);
    }
}