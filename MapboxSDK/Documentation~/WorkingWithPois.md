## Working with POIs

In the Mapbox API, a POI (Point of Interest) refers to a specific location or feature on the map that represents something meaningful to users—such as a restaurant, park, landmark, or shop. POIs are part of Mapbox’s vector tile datasets, especially the Mapbox Streets source, and include metadata like name, category, and sometimes additional attributes. Developers can use POIs to display labeled map features, create interactive markers, or power search and navigation features in their applications.

In the Unity SDK, we consume the Vector API and generate visuals for POIs under the vector module, similar to how buildings are rendered.  
While buildings are polygon-type data and roads are line-type data, POIs are point-type data. The Unity SDK processes this information, providing higher-level access for developers to use however they wish.  
A common usage for POIs is spawning a prefab at the location—this could be a label showing the name, a 3D model representing the place, or even a gameplay element like an enemy spawn point.

### Setting up a Layer Visualizer for POIs
To process POI data, we first need a `LayerVisualizer` object.  
If you are creating your map in the Unity editor, you can create one from the context menu: `Create > Mapbox > Modifiers`. This creates a scriptable object asset, which can then be added to the Vector Module in your map setup.

The `LayerVisualizer` has a few key settings. The most important is the name of the vector layer to process.  
For the default Mapbox Streets v8 tileset, you can find the full layer list here: https://docs.mapbox.com/data/tilesets/reference/mapbox-streets-v8.  
In this tutorial, we’ll use the `poi_label` layer, since it represents POI data.  
Because this is point-based data, make sure the `Layer Type` is set to `Point`. Setting this incorrectly can cause positioning and scaling errors in the generated visuals.

You can leave the following settings at their default values for now.  
The next major configuration is the `Modifier Stack Objects` section. Modifier stacks are like “styles” — they define how your map objects will appear.

### Creating a Modifier Stack
Similar to creating the Layer Visualizer, create a `Modifier Stack` and assign it to the visualizer.  
The Modifier Stack script has options for visibility range and object merging. Leave the visibility range unchanged for now and disable object merging.  
Below that are three key sections: Filters, Mesh Modifiers, and GameObject Modifiers.

#### Filters
Filters help define which POIs appear on the map. Built-in filter types correspond to Mapbox data properties, but you can also write custom filters for more advanced needs.  
For now, let’s add one simple filter to reduce the number of POIs.  
Referring again to the [Streets v8 documentation](https://docs.mapbox.com/data/tilesets/reference/mapbox-streets-v8), we know that `poi_labels` are ranked by importance through the `filterrank` property.  
We’ll add a `Number Property Filter`, set the operation to “Less Than or Equal,” set the property name to `filterrank`, and assign a value of `1`.  
This ensures that only the most important POIs are processed and displayed.

#### Mesh Modifiers
Mesh modifiers are not needed in this case because POIs are point data and do not require procedural mesh generation.  
We’ll skip this section.

#### GameObject Modifiers
In the GameObject Modifiers section, add one modifier — the `Prefab Modifier`.  
This modifier receives a GameObject representing the POI and spawns a prefab as its child.  
As a result, an empty GameObject marks each POI location, and your chosen prefab is instantiated under it.

The Prefab Modifier has a single setting: `Prefab`. Assign any prefab here — for example, a simple cube or a custom marker prefab for testing.

### Running the Setup
Once the visualizer and modifier stack are configured, run your map in Unity.  
You should now see your chosen prefab appearing at each POI location on the map, aligned with data from the Mapbox Streets vector tileset.
