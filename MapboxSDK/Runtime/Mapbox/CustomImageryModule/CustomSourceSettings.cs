using System;
using UnityEngine;

namespace Mapbox.CustomImageryModule
{
    [Serializable]
    public class CustomSourceSettings
    {
        [Tooltip("Url format string, structured as C# string format with '{}' fields for X/Y/Z coordinates. {0}=Z, {1}=X, {2}=Y")]
        public string UrlFormat;
        [Tooltip("Invert Y axis coordinates for TMS coordinate system, which starts from bottom left and grows to top-right")]
        public bool InvertY;
        [Tooltip("Enable when the URL points to a Mapbox-hosted tileset (api.mapbox.com). Appends access_token and sku query parameters automatically.")]
        public bool IsMapboxService;
    }
}