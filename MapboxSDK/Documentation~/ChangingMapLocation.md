## How to change map location

To jump to a specific location during runtime, use the ChangeView method from the MapboxMap class:

`public void ChangeView(LatitudeLongitude? latlng = null, float? zoom = null, float? pitch = null, float? bearing = null)`

This method updates the map with the provided parameters and triggers a redraw.

The first step is to get the map object. You can learn more about that in the short tutorial [Working with map object](WorkingWithMapObject.md).

Once you have access to the map object, you can call the method directly. A sample script is shown below:

```csharp
public class ChangeLocation : MonoBehaviour
{
    public MapBehaviourCore Core;
    private MapboxMap _map;
    
    private void Awake()
    {
        Core.Initialized += map =>
        {
            _map = map;
        };
    }
    
    public void ChangeLocationTo(string location)
    {
        _map.ChangeView(Conversions.StringToLatLon(location));
    }
}
