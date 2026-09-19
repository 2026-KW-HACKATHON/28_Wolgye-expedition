
# Getting Started with Mapbox Map Script and Object

In this guide, you’ll learn how to set up and interact with the Mapbox Map object in Unity.  
We’ll explore how the map system works, how the related scripts are structured, and how to properly access the `MapboxMap` instance in your own code.

---

## Understanding the `MapboxMap` Object

The `MapboxMap` class is the root object that controls the map instance.  
It’s a plain C# class — meaning it does not inherit from Unity’s `MonoBehaviour`.  

Because of this, you cannot attach it directly to a GameObject or interact with it in the Unity Editor.

---

## Exposing the Map in Unity: `MapboxMapBehaviour`

To expose `MapboxMap` in Unity, a helper interface class is used: `MapboxMapBehaviour`.  

This class:
- Inherits from `MapBehaviourCore`,  
- Which in turn inherits from `MonoBehaviour`.

That means `MapboxMapBehaviour` can be attached to a Unity GameObject and used directly in the Unity Scene or Inspector.

---

## How It Works

The classes `MapBehaviourCore` and `MapboxMapBehaviour` are responsible for:

1. Creating a `MapboxMap` instance using the settings configured in the Unity Inspector.  
2. Exposing the map object after it’s created, allowing other scripts to access it later.

After initialization, these classes mainly serve as a reference to retrieve the existing `MapboxMap` instance.

---

## In the Scene

When you open a demo scene, you’ll notice a GameObject named `Map`.  
This object has a `MapboxMapBehaviour` script attached.

During scene startup (in Unity’s `Start()` lifecycle method), this script automatically:
- Creates the `MapboxMap` object,
- Initializes it based on your scene’s configuration,
- Makes it available through the property:  

```csharp
MapboxMapBehaviour.MapboxMap
```

---

## The Initialization Process

By default, map initialization starts during Unity’s built-in `Start()` phase.

However, you can change this behavior — for example, to manually control when the map initializes — by adjusting the initialization settings in the Inspector.

---

## Creating a Basic Map Setup

To create a basic map in your own Unity scene, follow these simple steps:

1. Create a new GameObject and add the `MapboxMapBehaviour` script.  
   This script is the core of the map system—it represents the map itself. However, it does not include any default visualization, so we’ll add a few modules to display it as desired.

2. Add a `StaticApiLayerModule` script to the same GameObject.  
   The Static API module works with image data, downloading server-rendered map images and draping them over the terrain (flat or elevated).  
   It includes several predefined style options, and you can also use your own custom styles created in Mapbox Studio.  
   Without changing any settings, running the application should display a basic map.  
   (Remember to adjust the Scene or Game view camera so the map is visible.)

3. Add a `TerrainLayerModule` script to the same GameObject.  
   The Terrain module also works with image data, but instead of draping it as imagery, it extracts elevation information and applies it to the base terrain mesh.  
   Running the application again with default settings will display small elevation details and terrain variation.

4. Add a `VectorLayerModule` script to the same GameObject.  
   The Vector module is different—it works with vector data rather than images.  
   By default, it uses the Mapbox `Streets-v8` tileset, which you can read more about here: [Mapbox Streets V8 Documentation](https://docs.mapbox.com/data/tilesets/reference/mapbox-streets-v8/).  
   Unlike other modules, the Vector module doesn’t create visuals on its own. If you run the map now, you won’t see any new features appear yet.  
   The Vector module needs subcomponents called `Layer Visualizers` to turn data into visuals. You can think of these as “styles” that define how vector data appears.  
   If you click the `+` button and add a `BuildingLayer` visualizer (available in the sample content), then run the application, you’ll see basic buildings appear on your map.


---

## Common Tasks

### Place a GameObject at a latitude / longitude

To position your own object (a marker, a character avatar, a custom POI) at a specific geographic coordinate, use `IMapInformation.ConvertLatLngToPosition`:

```csharp
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.Example.Scripts.Map;
using UnityEngine;

public class PlaceMarker : MonoBehaviour
{
    public MapboxMapBehaviour MapBehaviour;
    public Transform Marker;

    private void Start()
    {
        // Wait until the map is ready, then place the marker.
        MapBehaviour.MapboxMap.Initialized += OnMapReady;
    }

    private void OnMapReady()
    {
        var coord = new LatitudeLongitude(40.7484, -73.9857); // Empire State Building
        var localPos = MapBehaviour.MapboxMap.MapInformation.ConvertLatLngToPosition(coord);

        // Parent under the map root so the marker composes with any transform applied
        // to the map hierarchy (rotation, translation, AR anchor, etc.).
        Marker.SetParent(MapBehaviour.UnityContext.MapRoot, worldPositionStays: false);
        Marker.localPosition = localPos;
    }
}
```

**Important**: the value `ConvertLatLngToPosition` returns is in the **map's local coordinate system**, not world space. Always assign it to `localPosition` of a Transform parented under `UnityContext.MapRoot`, never to `transform.position`. This composes correctly with any parent transformations (useful for AR/MR scenarios where the map is anchored to a real-world surface).

For optional **terrain elevation** (drop your object onto the terrain surface instead of at Y=0), use `MapboxMap.TryGetElevation(latlng, out float elevation)`:

```csharp
if (MapBehaviour.MapboxMap.TryGetElevation(coord, out var elevation))
{
    localPos.y = elevation;
}
Marker.localPosition = localPos;
```

If the map moves (via `MapboxMap.ChangeView`, `MapInformation.SetInformation`, or `ShiftMapForDistance`), object positions need to be re-computed. Subscribe to `IMapInformation.ViewChanged` and re-run the conversion when it fires.

See also `Documentation~/CoordinateConversions.md` for the full coordinate-conversion contract, and `Documentation~/WorkingWithPois.md` if your data lives in a Mapbox vector tile layer (e.g. `poi_label`) rather than from your own backend.

---
