using System;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Tiles;
using UnityEngine;

namespace Mapbox.CustomImageryModule
{
    /// <summary>
    /// TMS tile in the name means Y axis is inverted in the tile ids
    /// </summary>
    public class CustomTMSTile : RasterTile
    {
        private string _urlFormat;
        private bool _invertY;
        private bool _isMapboxService;

        // Legacy 4-arg constructor preserved so external subclasses compiled against the
        // 3.0.6 API keep building. Defaults match the pre-3.0.7 behavior (invertY = true,
        // non-Mapbox-service URL request path).
        public CustomTMSTile(string urlFormat, CanonicalTileId tileId, string tilesetId, bool useNonReadableTexture)
            : this(urlFormat, tileId, tilesetId, useNonReadableTexture, invertY: true, isMapboxService: false) { }

        public CustomTMSTile(string urlFormat, CanonicalTileId tileId, string tilesetId, bool useNonReadableTexture, bool invertY, bool isMapboxService) : base(tileId, tilesetId, useNonReadableTexture)
        {
            _urlFormat = urlFormat;
            _invertY = invertY;
            _isMapboxService = isMapboxService;
        }

        public override void Initialize(IFileSource fileSource, Action<DataFetchingResult> p)
        {
            TileState = TileState.Loading;
            _callback = p;

            var y = _invertY ? (int)(Mathf.Pow(2, Id.Z) - Id.Y - 1) : Id.Y;
            _generatedUrl = string.Format(_urlFormat, Id.Z, Id.X, y);
            DoTheRequest(fileSource);
        }

        protected override void DoTheRequest(IFileSource fileSource)
        {
            _webRequest = _isMapboxService
                ? fileSource.MapboxImageRequest(_generatedUrl, HandleTileResponse, ETag, 10, IsTextureNonreadable)
                : fileSource.CustomImageRequest(_generatedUrl, HandleTileResponse, ETag, 10, IsTextureNonreadable);
        }
    }
}