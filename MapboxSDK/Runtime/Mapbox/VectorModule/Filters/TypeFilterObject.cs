// using System;
// using System.Collections.Generic;
// using System.ComponentModel;
// using System.Linq;
// using Mapbox.BaseModule.Utilities;
// using UnityEngine;
// using UnityEngine.Serialization;
//
// namespace Mapbox.VectorModule.Filters
// {
// 	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Type Filter")]
// 	[DisplayName("Type Filter")]
// 	public class TypeFilterObject : FilterBaseObject
// 	{
// 		[NonSerialized] private TypeFilter _filter;
// 		public TypeFilterSettings TypeFilterSettings;
//
// 		public override ILayerFeatureFilterComparer Filter
// 		{
// 			get
// 			{
// 				if (_filter == null)
// 					_filter = new TypeFilter(TypeFilterSettings);
// 				return _filter;
// 			}
// 		}
// 	}
// 	
// 	[Serializable]
// 	public class TypeFilter : FilterBase
// 	{
// 		public TypeFilterSettings TypeFilterSettings;
// 		private HashSet<string> _types;
//
// 		public TypeFilter(TypeFilterSettings typeFilterSettings)
// 		{
// 			TypeFilterSettings = typeFilterSettings;
// 		}
// 		
// 		public override void Initialize()
// 		{
// 			base.Initialize();
// 			_types = new HashSet<string>();
// 			foreach (var s in TypeFilterSettings.FilterString.Split(','))
// 			{
// 				_types.Add(s.Trim().ToLowerInvariant());
// 			}
// 		}
//
// 		public override bool Try(VectorFeatureUnity feature)
// 		{
// 			var featureStuff = feature.Properties[TypeFilterSettings.PropertyName].ToString().ToLowerInvariant().Split(',');
// 			var reverseCheck = _types.Any(x => featureStuff.Contains(x));
// 			return _types.Contains(feature.Properties[TypeFilterSettings.PropertyName].ToString().ToLowerInvariant()) ||
// 			       reverseCheck;
// 		}
// 	}
//
// 	[Serializable]
// 	public class TypeFilterSettings
// 	{
// 		public string PropertyName;
// 		public string FilterString;
// 	}
// }
