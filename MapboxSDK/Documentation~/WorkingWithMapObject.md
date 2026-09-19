## Working with the Map Object

When you want to interact with the map (querying elevation, getting the latitude and longitude from the mouse position, or changing the map’s location) you first need to access the map object.

The map system has two parts:  
A MonoBehaviour script that manages the setup in Unity, and the underlying map object that performs the actual operations.  
All interaction from the Unity side begins through the MonoBehaviour. From there, you can retrieve a reference to the map object and store it for later use.

A common approach to obtain and cache the map object is shown below:

```csharp
public class CustomScript : MonoBehaviour
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
}
```


If you plan to use the map object in the Update method, it’s good practice to check its current state before performing operations.
This ensures that your logic only runs once the map is properly initialized and ready.

```csharp
private void Update()
{
    // Ready to work with submodules and their settings
    if (_map != null && _map.Status >= InitializationStatus.Initialized)
    {
        // Example: access map data or modify settings
    }
    
    // Initial view is loaded and ready for user interaction
    if (_map != null && _map.Status >= InitializationStatus.ReadyForUpdates)
    {
        // Example: perform runtime updates or interactions
    }
}
```

This setup guarantees that any scripts depending on the map will only run when the system is prepared, preventing null references and timing issues during initialization.