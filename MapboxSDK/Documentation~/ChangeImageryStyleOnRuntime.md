# Change Imagery Style at Runtime

To change the imagery style at runtime, follow these steps:

1. Get a reference to the `MapboxMap`.
2. Access the `StaticApiLayerModule` from the `MapVisualizer`.
3. Change the `tilesetId` (asynchronous operation).
4. Reload all currently active tiles to apply the new imagery.

Changing the `tilesetId` is not instantaneous, so it must be handled as a coroutine. After the update completes, explicitly reload the active tiles to refresh the visible content.

```csharp
private IEnumerator ReloadImageModule(MapboxMap map, string tilesetId)
{
    if (map.MapVisualizer.TryGetLayerModule<StaticApiLayerModule>(out var imageModule))
    {
        yield return imageModule.ChangeTilesetId(tilesetId);

        foreach (var tilePair in map.MapVisualizer.ActiveTiles)
        {
            var tile = tilePair.Value;
            imageModule.LoadInstant(tile);
        }
    }
}
```

This ensures the imagery source is updated and all visible tiles are refreshed immediately without requiring user interaction.