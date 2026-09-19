using Mapbox.BaseModule.Data.Platform.SQLite;
using UnityEngine.Scripting;

namespace Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache
{

	/// <summary>
	/// Don't change the class name: sqlite-net uses it for table creation.
	/// [Preserve] forces IL2CPP managed-code-stripping to keep the property
	/// accessors that sqlite-net invokes via reflection — without it, set_id is
	/// stripped on Android Medium/High stripping and every row maps to id=0.
	/// </summary>
	[Preserve]
	public class tilesets
	{

		//hrmpf: multiple PKs not supported by sqlite.net
		//https://github.com/praeclarum/sqlite-net/issues/282
		//TODO: do it via plain SQL
		[PrimaryKey, AutoIncrement, Preserve]
		public int id { get; set; }

		[Preserve]
		public string name { get; set; }
	}
}
