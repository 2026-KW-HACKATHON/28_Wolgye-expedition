using System;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Map;
using Mapbox.ImageModule.Terrain.Settings;
using UnityEngine;

namespace Mapbox.ImageModule.Terrain
{
    /// <summary>
    /// User-facing settings for the <see cref="TerrainLayerModule"/>. Controls how terrain
    /// raster tiles are fetched, whether CPU elevation data is decoded, and how tiles are
    /// rendered (shader-displaced vs. CPU-displaced meshes).
    /// </summary>
    [Serializable]
    public class TerrainLayerModuleSettings
    {
        /// <summary>Runtime-only selector for the terrain tileset (not serialized).</summary>
        [NonSerialized] public ElevationSourceType SourceType = ElevationSourceType.MapboxTerrain;

        [Tooltip("Pre-fetch low-zoom world tiles on startup so the map has fallback imagery while higher-zoom tiles load. Leave off to minimize cold-start bandwidth; enable for polished first-paint. Default: off.")]
        public bool LoadBackgroundTextures = false;

        [Tooltip("ON (recommended): the GPU vertex shader displaces a flat grid using the terrain-RGB texture — cheapest visual elevation. OFF: the CPU writes displaced vertices before render, which forces ExtractCpuElevationData on and costs more per-tile CPU. Default: on.")]
        public bool UseShaderTerrain = true;

        [Tooltip("Decode each terrain-RGB tile into a CPU float[] so features can query ground height. ON (default) is required for: vector snap-to-terrain (buildings, roads, POIs), terrain colliders, CPU elevation rendering, and TryGetElevation/QueryElevation. Turn OFF only for purely-terrain-rendered apps to save ~65K pixel decodes and a ~256KB float[] per tile. Force-enabled (and shown disabled) whenever UseShaderTerrain is off or colliderOptions.addCollider is on.")]
        public bool ExtractCpuElevationData = true;

        [Tooltip("Tileset id, retina flag, cache size, and data-zoom clamp for the terrain raster source.")]
        public ImageSourceSettings DataSettings;

        [Tooltip("Tiles whose zoom level is outside this [min, max] range will be skipped. Default (12, 16) keeps terrain loaded only in typical map-viewing zooms.")]
        public Vector2 RejectTilesOutsideZoom = new Vector2(12, 16);

        [Tooltip("Terrain mesh grid density, skirts, collider, and Unity layer assignment. See the nested fields for details.")]
        public ElevationLayerProperties ElevationLayerProperties = new ElevationLayerProperties();

        /// <summary>
        /// True when CPU elevation extraction is required by at least one consumer: the user
        /// explicitly enabled <see cref="ExtractCpuElevationData"/>, or shader rendering is
        /// off, or a terrain collider is requested. The module force-enables extraction on
        /// the raster source at Initialize time based on this value.
        /// </summary>
        public bool NeedsCpuElevation =>
            ExtractCpuElevationData
            || !UseShaderTerrain
            || (ElevationLayerProperties != null && ElevationLayerProperties.colliderOptions.addCollider);
    }
}
