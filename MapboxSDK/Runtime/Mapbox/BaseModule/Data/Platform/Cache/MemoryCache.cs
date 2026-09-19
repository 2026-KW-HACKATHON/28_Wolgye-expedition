using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.DataFetchers;

namespace Mapbox.BaseModule.Data.Platform.Cache
{
	public interface IMemoryCache
	{
		void OnDestroy();
		TypeMemoryCache<T> RegisterType<T>(int owner, int cacheSize = 100) where T : MapboxTileData;
	}

	public class MemoryCache : IMemoryCache
	{
		//private Dictionary<Type, ITypeCache> _subCaches;
		private Dictionary<int, ITypeCache> _subCaches;

		public MemoryCache()
		{
			_subCaches = new Dictionary<int, ITypeCache>();
		}

		public TypeMemoryCache<T> RegisterType<T>(int owner, int cacheSize = 100) where T : MapboxTileData
		{
			var dataType = typeof(T);
			if (_subCaches.ContainsKey(owner))
			{
				return (TypeMemoryCache<T>) _subCaches[owner];
			}
			else
			{
				var subcache = new TypeMemoryCache<T>(cacheSize);
				_subCaches.Add(owner, subcache);
				return subcache;
			}
		}

		public void OnDestroy()
		{
			foreach (var subCache in _subCaches)
			{
				subCache.Value.OnDestroy();
			}
		}
	}
}