using System;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	public interface IWriter : IDisposable
	{
		void WriteAttributeString(XName name, string value);

		void WriteStartElement(XName name);
		void WriteEndElement();

		void WriteStartCollection(XName name);
		void WriteEndCollection();

		void WritePrimitiveElement(XName name, object value);
		void WriteObjectElement(XName name, object value);

		void WriteNullItem(XName name);
	}
}
