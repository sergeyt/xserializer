using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace TsvBits.Serialization.Xml
{
	internal static class XmlExtensions
	{
		public static XName CurrentXName(this XmlReader reader)
		{
			return XNamespace.Get(reader.NamespaceURI).GetName(reader.LocalName);
		}

		public static string ReadStringOrNull(this XmlReader reader)
		{
			if (reader.IsEmptyElement)
			{
				reader.Read();
				return null;
			}
			return reader.ReadString();
		}

		public static IEnumerable<XName> ReadChildElements(this XmlReader reader)
		{
			if (reader.IsEmptyElement)
			{
				reader.Read();
				yield break;
			}

			var depth = reader.Depth;
			reader.Read(); // move to first child node

			while (reader.MoveToNextElement(depth))
			{
				var name = reader.CurrentXName();
				yield return name;
			}

			reader.ReadEndElement();
		}

		public static void MoveToFirstElement(this XmlReader reader)
		{
			while (reader.NodeType != XmlNodeType.Element && reader.Read()){}
		}

		public static bool MoveToNextElement(this XmlReader reader, int depth)
		{
			do
			{
				if (reader.NodeType == XmlNodeType.Element)
					return true;
				if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
					return false;
			} while (reader.Read());
			return false;
		}
	}
}
