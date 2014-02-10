using System;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	public interface IScope
	{
		XNamespace Namespace { get; }

		SimpleTypeCollection SimpleTypes { get; }

		IElementDef GetElementDef(Type type);
		IElementDef GetElementDef(XName name);
	}
}
