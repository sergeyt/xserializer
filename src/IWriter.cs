using System;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	/// <summary>
	/// Common writer interface to support various output formats (XML, JSON, etc).
	/// </summary>
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
