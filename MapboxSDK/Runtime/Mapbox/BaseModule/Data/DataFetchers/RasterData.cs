using System;
using UnityEngine;

namespace Mapbox.BaseModule.Data.DataFetchers
{
    [Serializable]
    public class RasterData : MapboxTileData
    {
        public Texture2D Texture;
        
        public virtual void Clear()
        {
            //cant null texture here as native will reuse it
            //this is just for debugging unity impl
            Texture = null;
        }

        public override void Dispose()
        {
            //no idea if this'll work with native
#if UNITY_EDITOR
            if (!Application.isPlaying)
                GameObject.DestroyImmediate(Texture);
            else
#endif
            GameObject.Destroy(Texture);
            Texture = null;
            base.Dispose();
        }
    }
}