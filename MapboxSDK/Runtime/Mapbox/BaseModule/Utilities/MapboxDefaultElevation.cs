using System;
using Mapbox.BaseModule.Data.Interfaces;

namespace Mapbox.BaseModule.Utilities
{
	public static class MapboxDefaultElevation
	{
		public static Style GetParameters(ElevationSourceType defaultElevation)
		{
			Style defaultStyle = new Style();
			switch (defaultElevation)
			{
				case ElevationSourceType.MapboxTerrain:
					defaultStyle = new Style
					{
						Id = "mapbox.terrain-rgb",
						Name = "Mapbox Terrain"
					};

					break;
				// case ElevationSourceType.MapboxTerrainDemV1:
				// 	defaultStyle = new Style
				// 	{
				// 		Id = "mapbox.mapbox-terrain-dem-v1",
				// 		Name = "Mapbox Terrain Dem V1"
				// 	};
				//
				// 	break;
				case ElevationSourceType.Custom:
					throw new Exception("Invalid type : Custom");
				default:
					break;
			}

			return defaultStyle;
		}
	}
}
