using System;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	public interface IScope
	{
		XNamespace Namespace { get; }

		IElementDef GetElementDef(Type type);
		IElementDef GetElementDef(XName name);

		bool TryConvert(object value, out string result);
		bool TryRead(Func<string> reader, Type type, out object value);
	}
}
