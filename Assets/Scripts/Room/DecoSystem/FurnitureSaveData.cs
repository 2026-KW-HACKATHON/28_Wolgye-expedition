using System.Collections.Generic;

// 가구 저장 데이터 클래스 모음.
// PlacementController / FurnitureEditController가 같은 JSON 파일을 공유하므로 별도 파일로 분리했다.

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
