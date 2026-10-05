using UnityEngine;

/// <summary>
/// 그리드의 칸 하나. 순수 데이터 클래스.
/// </summary>
public class GridCell
{
    public Vector2Int Position { get; }
    public PlacedFurniture Occupant { get; private set; }
    public bool IsOccupied => Occupant != null;

    public GridCell(Vector2Int position)
    {
        Position = position;
    }

    public void SetOccupant(PlacedFurniture occupant) => Occupant = occupant;
    public void Clear() => Occupant = null;
}