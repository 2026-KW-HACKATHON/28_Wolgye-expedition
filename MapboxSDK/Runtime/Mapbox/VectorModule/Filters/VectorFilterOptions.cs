using System;
using System.Collections.Generic;

namespace Mapbox.VectorModule.Filters
{
	[Serializable]
	public class VectorFilterOptions
	{
		public List<FilterBase> Filters = new List<FilterBase>();
		public LayerFilterCombinerOperationType combinerType = LayerFilterCombinerOperationType.All;
	}

	public enum LayerFilterCombinerOperationType
	{
		All,
		Any,
		None
	}
}
