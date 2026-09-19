using System;
using Mapbox.UnityMapService.TileProviders;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Mapbox.Example.Scripts.TileProviderBehaviours
{
    public class UnityTileProviderBehaviour : TileProviderBehaviour
    {
        public UnityTileProviderSettings Settings;
        [NonSerialized] public UnityTileProvider TileProvider;
        public override TileProvider Core => TileProvider ??= new UnityTileProvider(Settings);
    }
}