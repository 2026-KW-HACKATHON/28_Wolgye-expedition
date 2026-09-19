// using System;
// using System.ComponentModel;
// using Mapbox.BaseModule.Utilities;
// using UnityEngine;
//
// namespace Mapbox.VectorModule.Filters
// {
//     [CreateAssetMenu(menuName = "Mapbox/Filters/Feature Id Filter")]
//     [DisplayName("Feature Id Filter")]
//     public class FeatureIdFilterObject : FilterBaseObject
//     {
//         public ulong Value;
//         public bool IsEqual;
//         [NonSerialized] private FeatureIdFilter _filter;
//
//         public override ILayerFeatureFilterComparer Filter
//         {
//             get
//             {
//                 if (_filter == null)
//                     _filter = new FeatureIdFilter(Value, IsEqual);
//                 return _filter;
//             }
//         }
//     }
//     
//     [Serializable]
//     public class FeatureIdFilter : FilterBase
//     {
//         private ulong _value;
//         private bool _isEqual;
//
//         public FeatureIdFilter(ulong value, bool isEqual)
//         {
//             _value = value;
//             _isEqual = isEqual;
//         }
//
//         public override bool Try(VectorFeatureUnity feature)
//         {
//             return _isEqual == (_value == feature.Data.Id);
//         }
//     }
// }