using System;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	public interface IWriter : IDisposable
	{
		bool SupportAttributes { get; }

		void WriteAttributeString(XName name, string value);

		void WriteStartElement(XName name);
		void WriteEndElement();

		void WritePrimitiveElement(XName name, object value);
		void WriteNullElement(XName name);
		void WriteObjectElement(XName name, object value);
	}
}
