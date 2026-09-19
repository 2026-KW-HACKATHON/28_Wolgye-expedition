using System;
using System.Collections.Generic;
using System.ComponentModel;
using Mapbox.BaseModule.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mapbox.VectorModule.Filters
{
	[CreateAssetMenu(menuName = "Mapbox/Filters/Type Filter")]
	[DisplayName("String Property Filter")]
	public class FeatureStringPropertyFilterObject : FilterBaseObject
	{
		[NonSerialized] private FeatureStringPropertyFilter _filter;
		public FeatureStringPropertyFilterSettings PropertyFilterSettings;

		public override ILayerFeatureFilterComparer Filter
		{
			get
			{
				if (_filter == null)
					_filter = new FeatureStringPropertyFilter(PropertyFilterSettings);
				return _filter;
			}
		}
	}
	
	[Serializable]
	public class FeatureStringPropertyFilter : FilterBase
	{
		public FeatureStringPropertyFilterSettings PropertyFilterSettings;
		private HashSet<string> _types;

		public FeatureStringPropertyFilter(FeatureStringPropertyFilterSettings propertyFilterSettings)
		{
			PropertyFilterSettings = propertyFilterSettings;
		}
		
		public override void Initialize()
		{
			base.Initialize();
			if (PropertyFilterSettings.CheckOperation == StringCheckOperation.Contains)
			{
				_types = new HashSet<string>();
				foreach (var s in PropertyFilterSettings.FilterString.Split(','))
				{
					_types.Add(s.Trim().ToLowerInvariant());
				}
			}
		}

		public override bool Try(VectorFeatureUnity feature)
		{
			//this is a slightly cheesy way to negate by a flag
			//so right side is simply negated by the first part and equality check

			var result = false;
			if (PropertyFilterSettings.CheckOperation == StringCheckOperation.Equals)
			{
				result = PropertyFilterSettings.FilterString.ToLowerInvariant() == feature.Properties[PropertyFilterSettings.PropertyName].ToString().ToLowerInvariant();
			}
			else if (PropertyFilterSettings.CheckOperation == StringCheckOperation.Contains)
			{
				result = _types.Contains(feature.Properties[PropertyFilterSettings.PropertyName].ToString().ToLowerInvariant());
			}

			return (!PropertyFilterSettings.Invert) ? result : !result; 
		}
	}

	[Serializable]
	public class FeatureStringPropertyFilterSettings
	{
		public string PropertyName;
		public StringCheckOperation CheckOperation = StringCheckOperation.Equals;
		public string FilterString;
		public bool Invert = false;
	}

	public enum StringCheckOperation
	{
		Equals,
		Contains
	}
}
