using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace XmlSerialization
{
	public interface INodeDef
	{
		XName Name { get; }
		Type Type { get; }
	}

	public interface IPropertyDef : INodeDef
	{
		bool IsReadOnly { get; }

		object GetValue(object target);

		void SetValue(object target, object value);
	}

	public interface IElementDef : INodeDef
	{
		IEnumerable<IPropertyDef> Attributes { get; }
		IPropertyDef GetAttribute(XName name);

		// TODO: reuse IAttributeDef
		IEnumerable<INodeDef> Elements { get; }
		
		Type GetElementType(XName name);

		object GetValue(XName name, object target);
		void SetValue(XName name, object target, object value);

		object CreateElement(XName name, object target);
	}
}