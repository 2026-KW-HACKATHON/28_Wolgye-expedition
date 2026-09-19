
# Coordinate System Conversions

Your map is both a GameObject and a system within your Unity scene.  
It can have its own position, rotation, and scale, with various map visualization settings.  
Because of this, coordinate system conversions depend on multiple parameters.  

To handle these parameters, there are helper functions available under the `IMapInformation` interface.

---

## Understanding `IMapInformation`

The `IMapInformation` interface (or the `MapInformation` class that implements it) contains the core parameters of a map like center location (latitude and longitude) and scale.

This information is created during the map’s initialization process by `MapboxMapBehaviour`.  
It is passed to the `MapboxMap` constructor and stored there for the lifetime of the map.

In other words, every initialized map has its own `IMapInformation` instance.

To access it, you first need the `MapboxMap` object.  
You can review how to obtain it in the *Getting Started with the Mapbox Map Script* tutorial.

Once you have the `MapboxMap` object, you can access its information via `
MapboxMap.MapInformation` property.

---

## Converting Latitude/Longitude to Unity Coordinates

One of the most common conversions is transforming a latitude/longitude pair into Unity world (or local) coordinates.  
This is useful when you want to place a GameObject, such as a marker or model, at a specific geographic point on your map.

The helper method for this is:

```csharp
public static Vector3 ConvertLatLngToPosition(this IMapInformation mapInfo, LatitudeLongitude latlng)
```

This is an extension method, which means the first parameter (`mapInfo`) is provided automatically when called on an object that implements `IMapInformation`.

It returns a `Vector3` representing the position in Unity space, relative to the map’s local coordinate system.

Here’s a sample usage:

```csharp
public void PlaceObject(LatitudeLongitude latlng)
{
    var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); // Create a simple marker
    go.transform.SetParent(_map.UnityContext.MapRoot); // Moving it under the map object because our conversion will work in maps local space
    var unityPosition = _map.MapInformation.ConvertLatLngToPosition(latlng);
    go.transform.localPosition = unityPosition;
}
```

---

## Converting Unity Coordinates to Latitude/Longitude

You can also convert a Unity-space position (`Vector3`) back to a latitude/longitude coordinate.  
This is useful for identifying where a GameObject is located on the map in geographic terms.

The method is:

```csharp
public static LatitudeLongitude ConvertPositionToLatLng(this IMapInformation mapInfo, Vector3 position)
```

As before, you’ll need both the map and its `MapInformation` object.  
Pass in a Unity `Vector3` position, and you’ll receive a `LatitudeLongitude` result.

Example:

```csharp
public void GetLocationFor(GameObject go)
{
    var latitudeLongitude = _map.MapInformation.ConvertPositionToLatLng(go.transform.position);
    Debug.Log(latitudeLongitude);
}
```
