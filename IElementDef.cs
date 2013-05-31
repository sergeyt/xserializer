using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace XmlSerialization
{
	public interface IPropertyDef
	{
		XName Name { get; }
		Type Type { get; }

		bool IsReadOnly { get; }

		object GetValue(object target);

		void SetValue(object target, object value);
	}

	public interface IDefCollection<T> : IEnumerable<T>
	{
		T this[XName name] { get; }
	}

	public interface IElementDef
	{
		XName Name { get; }
		Type Type { get; }

		IDefCollection<IPropertyDef> Attributes { get; }
		IDefCollection<IPropertyDef> Elements { get; }
	}
}