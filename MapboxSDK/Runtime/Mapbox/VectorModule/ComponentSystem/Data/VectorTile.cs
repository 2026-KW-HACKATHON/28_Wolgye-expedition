using System;

namespace Mapbox.VectorModule.ComponentSystem.Data
{
    public ref struct VectorTile
    {
        private VectorTileReader _VTR;
        
        public VectorTile(ReadOnlySpan<byte> data)
        {
            _VTR = new VectorTileReader(ref data);
        }

        public bool TryGetLayer(string layerName, out VectorTileLayer layer)
        {
	        return _VTR.TryGetLayer(layerName, out layer);
        }
    }
}