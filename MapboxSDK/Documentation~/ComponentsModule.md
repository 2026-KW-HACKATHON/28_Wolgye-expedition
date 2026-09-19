11## Components Module

The Components Module is a high-performance alternative to the Vector Layer Module, designed for rendering large-scale maps with significantly reduced memory allocations and improved frame rates.

Like the Vector Layer Module, it uses the [Mapbox Vector Tiles API](https://docs.mapbox.com/api/maps/vector-tiles/) to fetch map data. However, it processes this data through a completely redesigned pipeline optimized for zero-allocation mesh generation and background threading.

---

### Why Components Module Exists

The original Vector Layer Module, while flexible, was not optimized for performance-critical applications. The Components Module was created to address performance limitations through:

**Performance Improvements:**
- **Custom PBF Parser**: A zero-allocation Protocol Buffer parser using `ref struct` and `Span<T>` for direct memory access
- **Optimized Data Structures**: Pre-sized buffers and stack-allocated arrays minimize GC pressure
- **Merged Meshes**: Buildings and roads within each tile are combined into single meshes, dramatically reducing draw calls compared to the Vector Layer Module's per-feature mesh approach

---

### Comparison: Components Module vs Vector Layer Module

| Feature | Vector Layer Module | Components Module |
|---------|---------------------|-------------------|
| **Flexibility** | Highly flexible with modifier stacks | Fixed pipeline per component type |
| **Extensibility** | Easy to add custom modifiers | Requires C# coding to extend |
| **Mesh Output** | Individual meshes per feature | Merged meshes per tile (buildings/roads combined) |
| **Performance** | Adequate for small-medium maps | Optimized for large-scale maps |
| **Memory** | Moderate allocation rate | Minimal allocations |

**When to Use Components Module:**
- Large maps with many visible tiles
- Mobile or low-end hardware targets
- Applications requiring stable 60 FPS
- Dense urban environments with thousands of buildings

**When to Use Vector Layer Module:**
- Rapid prototyping
- Custom rendering workflows
- Non-performance-critical applications
- Need for runtime modifier configuration

---

### Architecture Overview

The Components Module processes vector tiles through these stages:

1. **Tile Download**: Vector tiles are fetched and decompressed (shared with Vector Layer Module)
2. **Background Parsing**: Custom PBF parser decodes tile data on background threads using zero-allocation techniques
3. **Mesh Generation**: Component visualizers generate meshes in background tasks using `Span<T>` for direct vertex manipulation
4. **Main Thread Finalization**: Generated meshes are transferred to GameObjects on the main thread

**Key Technical Components:**
- `VectorTileReader`: Zero-allocation PBF parser using `ref struct` and `ReadOnlySpan<byte>`
- `PbfReader`: Low-level varint decoder with optimized packed array handling
- `MapboxComponentVisualizer`: Base class for component visualizers with stable ID assignment
- `MapboxComponentsModule`: Coordinates background mesh generation and main-thread finalization
- `ArrayPolygon`, `ArrayHeight`, `ArrayChamferHeight`: Stack-optimized mesh modifiers

---

### Module Setup

To use the Components Module instead of the Vector Layer Module:

1. Create a new `MapboxComponentsModuleScript` component on your map GameObject
2. Configure the module settings (similar to Vector Layer Module):
   - **Source Type**: Mapbox Streets V8 or custom tileset
   - **Cache Size**: Number of tiles to keep in memory
   - **Clamp Data Level**: Maximum zoom level for data fetching
   - **Reject Tiles Outside Zoom**: Discard tiles outside zoom range

3. Add component visualizers:
   - **Building Component**: For rendering 3D buildings
   - **Road Component**: For rendering road networks
   - **Area Component**: For rendering land use, water, parks, etc.

---

### Component Visualizers

The Components Module provides three specialized visualizers, each optimized for a specific feature type.

#### 1. Building Component Visualizer

Renders 3D buildings from the `building` layer.

**Configuration Options:**

- **Material**
  The material applied to all building meshes in this component.

- **Enable Terrain Snapping**
  When enabled, building vertices are adjusted to match terrain elevation.
  Requires a terrain-enabled map. Useful for hilly or mountainous areas.

- **Round Building Corners**
  Applies chamfered (rounded) corners to building geometry.
  Creates more realistic building shapes but increases vertex count.

- **Extrusion Settings**
  Controls how building heights are determined:
  - **Property Height**: Uses the `height` property from vector data (default)
  - **Absolute Height**: Sets all buildings to a fixed height
  - **Max Height**: Caps building heights at a maximum value
  - **Min Height**: Ensures buildings are at least a minimum height
  - **Range Height**: Clamps heights between min and max values
  - **Extrusion Scale Factor**: Multiplies all height values (e.g., 0.5 for half height)

- **Chamfer Offset** (when Round Building Corners is enabled)
  The distance in meters to inset corners. Typical values: 0.5 - 2.0 meters.

**Creating a Building Component Visualizer:**
1. Right-click in Project window: `Create → Mapbox → Layer Visualizers → Building Component Visualizer`
2. Configure the settings
3. Add the visualizer asset to your `MapboxComponentsModuleScript`

---

#### 2. Road Component Visualizer

Renders road networks from the `road` layer with style-based filtering.

**Configuration Options:**

- **Game Object Offset**
  Vertical offset applied to the entire road layer.
  Useful for raising roads slightly above terrain to prevent z-fighting.

- **Random Offset Range**
  Small random vertical offset per road segment (default: 0.0001).
  Prevents z-fighting when multiple road segments overlap.

- **Road Style Sheet**
  A `RoadStyleSheet` asset that defines how different road types are rendered.

**Road Style Sheet:**

A Road Style Sheet contains multiple road styles, each with:

- **Name**: Descriptive name (for organization only)
- **Filters**: Determines which road features this style applies to
- **Width**: Road width in meters
- **Push Up**: Additional vertical offset for this style
- **Material**: Material applied to roads matching this style

**Filter Stack:**

Filters determine which roads match a style. Common filters:
- **Class Filter**: Match by road class (e.g., "motorway", "primary", "residential")
- **Type Filter**: Match by road type (e.g., "trunk", "link")
- **Structure Filter**: Match by structure (e.g., "bridge", "tunnel")

Filters can be combined using:
- **Any**: Feature matches if any filter passes
- **All**: Feature matches only if all filters pass
- **None**: Feature matches if no filters pass

**Example Style Setup:**
```
Style: "Highways"
  Filters: Class = "motorway", Type = "Any"
  Width: 12.0
  Material: HighwayMaterial

Style: "Main Roads"
  Filters: Class = "primary" OR "secondary"
  Width: 8.0
  Material: MainRoadMaterial

Style: "Local Streets"
  Filters: Class = "residential"
  Width: 4.0
  Material: LocalStreetMaterial
```

**Creating a Road Component Visualizer:**
1. Right-click in Project: `Create → Mapbox → Road Style Sheet`
2. Configure styles and filters in the sheet
3. Right-click: `Create → Mapbox → Layer Visualizers → Road Component Visualizer`
4. Assign the style sheet to the visualizer
5. Add visualizer to `MapboxComponentsModuleScript`

---

#### 3. Area Component Visualizer

Renders polygonal areas such as water, parks, landuse, etc.

**Configuration Options:**

- **Layer Name**
  The vector layer to render (e.g., "water", "landuse", "park").
  Unlike building/road components which are fixed to their layers, area components are generic and can target any polygon layer.

- **Game Object Offset**
  Vertical offset applied to the entire area layer.
  Useful for layering (e.g., water at 0, parks at 0.1, landuse at 0.2).

- **Styles**
  A list of area styles, each defining how features with a specific class are rendered.

**Area Style:**

Each area style has:
- **Class Name**: The feature class to match (e.g., "park", "residential", "commercial")
- **Material**: Material applied to matching features
- **Push Up**: Additional vertical offset for this class

**Example Setup for Landuse:**
```
Layer Name: "landuse"
Styles:
  - Class: "residential", Material: ResidentialMat, Push Up: 0.0
  - Class: "commercial", Material: CommercialMat, Push Up: 0.1
  - Class: "industrial", Material: IndustrialMat, Push Up: 0.2
  - Class: "park", Material: ParkMat, Push Up: 0.3
```

**Example Setup for Water:**
```
Layer Name: "water"
Styles:
  - Class: "*", Material: WaterMat, Push Up: 0.0
```
(Water features typically don't have class variations, so a single catch-all style works)

**Creating an Area Component Visualizer:**
1. Right-click: `Create → Mapbox → Layer Visualizers → Area Component Visualizer`
2. Set the layer name
3. Add styles for each class you want to render
4. Add visualizer to `MapboxComponentsModuleScript`

---

### Best Practices

**Material Configuration:**
- Use shared materials across components to reduce draw calls
- Enable GPU instancing on materials when possible
- Consider mobile shader variants for cross-platform apps

**Performance Tuning:**
- Use **Clamp Data Level** to limit detail at high zoom (e.g., clamp to 15 for dense cities)
- Reduce **Cache Size** if memory is constrained (default is usually fine)
- Disable **Terrain Snapping** if terrain isn't used (saves CPU)
- Use **Random Offset Range** sparingly—smaller values are better

**Styling Tips:**
- Keep road style sheets simple—fewer styles mean faster filtering
- Use **Push Up** offsets to layer features (water → roads → buildings)
- Test materials with both day/night lighting for consistent appearance

---

### Limitations

The Components Module sacrifices flexibility for performance:

1. **Fixed Component Types**: Only buildings, roads, and areas are supported out of the box
2. **Less Extensible**: Adding new component types requires C# programming and understanding of the performance architecture
3. **No Per-Feature Callbacks**: Vector Layer Module's per-feature game object modifiers aren't available
4. **Simplified Filtering**: Road/area filtering is less powerful than full modifier stacks

For most production use cases, these limitations are acceptable trade-offs for the performance gains. If you need the flexibility, use the Vector Layer Module instead.

---

### Troubleshooting

**Buildings don't appear:**
- Verify Building Component Visualizer is active (`IsActive = true`)
- Check material is assigned
- Ensure map zoom level is 13+ (building data starts at zoom 13)
- Verify tileset includes `building` layer

**Roads render incorrectly:**
- Check Road Style Sheet is assigned
- Verify styles have filters that match road classes in your area
- Test with a single catch-all style first: `Filters: Class = Any`
- Confirm road width is reasonable (1-20 meters)

**Z-fighting between layers:**
- Increase `GameObjectOffset` for each component (e.g., roads: 0.1, areas: 0.0)
- Increase `RandomOffsetRange` for roads if segments overlap
- Use `PushUp` to layer features within a component

**Performance still poor:**
- Profile with Unity Profiler—confirm mesh generation is the bottleneck
- Reduce visible tile count via camera distance/zoom constraints
- Lower `CacheSize` if memory is limited (won't improve FPS but reduces memory)
- Simplify Road Style Sheet—fewer styles = faster filtering
- Disable terrain snapping if not needed

---

### API Events

Components expose events for integration with custom systems:

**BuildingComponentVisualizer:**
- `OnBuildingMeshCreated(CanonicalTileId tileId, GameObject gameObject, MeshData meshData)`: Fired when a building tile mesh is created
- `OnBuildingMeshDestroyed(CanonicalTileId tileId, GameObject gameObject)`: Fired when a building tile is unregistered

**RoadComponentVisualizer:**
- `OnRoadMeshCreated(CanonicalTileId tileId, GameObject gameObject, MeshData meshData)`: Fired when a road tile mesh is created
- `OnRoadMeshDestroyed(CanonicalTileId tileId, GameObject gameObject)`: Fired when a road tile is unregistered

**AreaComponentVisualizer:**
- `OnAreaMeshCreated(CanonicalTileId tileId, GameObject gameObject, MeshData meshData)`: Fired when an area tile mesh is created
- `OnAreaMeshDestroyed(CanonicalTileId tileId, GameObject gameObject)`: Fired when an area tile is unregistered

**Usage Example:**
```csharp
buildingVisualizer.OnBuildingMeshCreated += (tileId, gameObject, meshData) => {
    // Add custom components, colliders, etc.
    gameObject.AddComponent<BuildingInteraction>();
};
```

---

### Conclusion

The Components Module provides a high-performance alternative to the Vector Layer Module for production applications that need stable frame rates with large-scale maps. While it sacrifices some flexibility, the performance gains—particularly on mobile and low-end hardware—make it the preferred choice for performance-critical projects.

For rapid prototyping or custom rendering workflows, the Vector Layer Module remains the better option. Choose the module that best fits your project's needs.
