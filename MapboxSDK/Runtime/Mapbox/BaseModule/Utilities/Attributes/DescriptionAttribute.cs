using System;

namespace Mapbox.BaseModule.Utilities.Attributes
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class DescriptionAttribute : Attribute
	{
		private readonly string description;
		public string Description { get { return description; } }
		public DescriptionAttribute(string description)
		{
			this.description = description;
		}
	}
}
