using Mapbox.UnityMapService.TileProviders;
using UnityEngine;

namespace Mapbox.Example.Scripts.Map
{
    public class UnityRangeTileProviderBehaviour : MonoBehaviour
    {
        public Transform Transform;

        public UnityRangeTileProvider GetTileProvider()
        {
            return new UnityRangeTileProvider(Transform);
        }
    }
}
