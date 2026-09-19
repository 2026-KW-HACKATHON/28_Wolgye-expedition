using System;

namespace Mapbox.BaseModule.Utilities.Attributes
{
	public class NodeEditorElementAttribute : Attribute
	{
		public string Name;

		public NodeEditorElementAttribute(string s)
		{
			Name = s;
		}
	}
}