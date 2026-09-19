using System;
using Mapbox.BaseModule.Data.Interfaces;

namespace Mapbox.BaseModule.Utilities
{
	public static class MapboxDefaultVector
	{
		public static Style GetParameters(VectorSourceType defaultElevation)
		{
			Style defaultStyle = null;
			switch (defaultElevation)
			{
				// case VectorSourceType.MapboxStreets:
				// 	defaultStyle = new Style
				// 	{
				// 		Id = "mapbox.mapbox-streets-v7",
				// 		Name = "Mapbox Streets v7"
				// 	};
				//
				// 	break;
				case VectorSourceType.MapboxStreetsV8:
					defaultStyle = new Style
					{
						Id = "mapbox.mapbox-streets-v8",
						Name = "Mapbox Streets v8"
					};

					break;
				case VectorSourceType.Custom:
					throw new Exception("Invalid type : Custom");
				default:
					break;
			}

			return defaultStyle;
		}
	}
}
