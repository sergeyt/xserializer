using System.Xml;
using System.Xml.Linq;

namespace XmlSerialization
{
	internal static class XmlExtensions
	{
		public static XName CurrentXName(this XmlReader reader)
		{
			return XNamespace.Get(reader.NamespaceURI).GetName(reader.LocalName);
		}
	}
}