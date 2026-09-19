using System;
using System.Collections.Generic;
using System.ComponentModel;
using Mapbox.BaseModule.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mapbox.VectorModule.Filters
{
	[CreateAssetMenu(menuName = "Mapbox/Filters/Number Filter")]
	[DisplayName("Number Property Filter")]
	public class FeatureNumberPropertyFilterObject : FilterBaseObject
	{
		[NonSerialized] private FeatureNumberPropertyFilter _filter;
		public FeatureNumberPropertyFilterSettings NumberFilterSettings;

		public override ILayerFeatureFilterComparer Filter
		{
			get
			{
				if (_filter == null)
					_filter = new FeatureNumberPropertyFilter(NumberFilterSettings);
				return _filter;
			}
		}
	}

	[Serializable]
	public class FeatureNumberPropertyFilter : FilterBase
	{
		public FeatureNumberPropertyFilterSettings NumberFilterSettings;

		public FeatureNumberPropertyFilter(FeatureNumberPropertyFilterSettings numberFilterSettings)
		{
			NumberFilterSettings = numberFilterSettings;
		}

		public override bool Try(VectorFeatureUnity feature)
		{
			var value = float.Parse(feature.Properties[NumberFilterSettings.PropertyName].ToString());
			switch (NumberFilterSettings.checkOperation)
			{
				case FilterCheckOperation.Equals: return value == NumberFilterSettings.FilterValue;
				case FilterCheckOperation.NotEquals: return value != NumberFilterSettings.FilterValue;
				case FilterCheckOperation.LessThan: return value < NumberFilterSettings.FilterValue;
				case FilterCheckOperation.LessThanOrEquals: return value <= NumberFilterSettings.FilterValue;
				case FilterCheckOperation.MoreThan: return value > NumberFilterSettings.FilterValue;
				case FilterCheckOperation.MoreThanOrEquals: return value >= NumberFilterSettings.FilterValue;
			}

			return false;
		}
	}

	[Serializable]
	public class FeatureNumberPropertyFilterSettings
	{
		public FilterCheckOperation checkOperation = FilterCheckOperation.Equals;
		public string PropertyName;
		public float FilterValue;
	}
	
	public enum FilterCheckOperation
	{
		NotEquals,
		Equals,
		LessThan,
		LessThanOrEquals,
		MoreThan,
		MoreThanOrEquals
	}
}