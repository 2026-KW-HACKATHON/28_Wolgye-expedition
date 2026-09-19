
# Accessing the Map Object in Your Own Scripts

If you want to use the map object in your own code (for example, to query tiles, add layers, or respond to map events), the recommended approach is to:

1. Reference the `MapBehaviourCore` component in your script.  
2. Subscribe to its `Initialized` event.  
3. Store the resulting `MapboxMap` instance when it becomes available.

Here’s a simple example:

```csharp
using UnityEngine;
using Mapbox.Unity.Map;

public class Test : MonoBehaviour
{
    public MapBehaviourCore MapCore;
    private MapboxMap _map;

    public void Awake()
    {
        MapCore.Initialized += (map) =>
        {
            _map = map;
            Debug.Log("Map is initialized");
        };
    }
}
```

## What This Does
- The script listens for the map’s initialization event.  
- When the map is ready, it assigns it to the private `_map` field.  
- You can then safely use `_map` for all your map-related logic.

---
