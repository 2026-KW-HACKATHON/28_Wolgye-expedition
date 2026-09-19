using System;
using Mapbox.BaseModule.Data.Platform.SQLite;
using UnityEngine.Scripting;

namespace Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache
{

	/// <summary>
	/// Don't change the class name: sqlite-net uses it for table creation.
	/// [Preserve] forces IL2CPP managed-code-stripping to keep the property
	/// accessors that sqlite-net invokes via reflection.
	/// </summary>
	[Preserve]
	public class tiles
	{
		

		[PrimaryKey, AutoIncrement, Preserve]
		public int id { get; set; }

		[Preserve]
		public int tile_set { get; set; }

		//hrmpf: multiple PKs not supported by sqlite.net
		//https://github.com/praeclarum/sqlite-net/issues/282
		//TODO: do it via plain SQL
		//[PrimaryKey]
		[Preserve]
		public int zoom_level { get; set; }

		//[PrimaryKey]
		[Preserve]
		public long tile_column { get; set; }

		//[PrimaryKey]
		[Preserve]
		public long tile_row { get; set; }

		[Preserve]
		public byte[] tile_data { get; set; }

		[Preserve]
		public string tile_path { get; set; }

		/// <summary>Unix epoch for simple FIFO pruning </summary>
		[Preserve]
		public int timestamp { get; set; }

		/// <summary> ETag Header value of the reponse for auto updating cache</summary>
		[Preserve]
		public string etag { get; set; }

		/// <summary>Expiration date of cached data </summary>
		[Preserve]
		public int? expirationDate { get; set; }

		[Ignore]
		public DateTime expirationDateFormatted  { get; set; }
	}

	[Preserve]
	public class offlineMaps
	{
		[PrimaryKey, AutoIncrement, Preserve]
		public int id { get; set; }
		[Preserve]
		public string name { get; set; }
	}

	[Preserve]
	public class tile2offline
	{
		[Preserve]
		public int tileId { get; set; }
		[Preserve]
		public int mapId { get; set; }
	}
}
