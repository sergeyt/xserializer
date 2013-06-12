using System;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace TsvBits.Serialization.Xml
{
	internal sealed class XmlWriterImpl : IWriter
	{
		private readonly XmlWriter _writer;

		public XmlWriterImpl(XmlWriter writer)
		{
			_writer = writer;
		}

		public static IWriter Create(StringBuilder output, XmlWriterSettings settings)
		{
			return new XmlWriterImpl(XmlWriter.Create(output, settings));
		}

		public void Dispose()
		{
			((IDisposable)_writer).Dispose();
		}

		public bool SupportAttributes
		{
			get { return true; }
		}

		public void WriteAttributeString(XName name, string value)
		{
			_writer.WriteAttributeString(name.LocalName, name.NamespaceName, value);
		}

		public void WriteStartElement(XName name)
		{
			_writer.WriteStartElement(name.LocalName, name.NamespaceName);
		}

		public void WriteEndElement()
		{
			_writer.WriteEndElement();
		}

		public void WritePrimitiveElement(XName name, object value)
		{
			WriteStartElement(name);
			WriteValue(value);
			WriteEndElement();
		}

		public void WriteNullElement(XName name)
		{
			WriteStartElement(name);
			_writer.WriteAttributeString("nil", Xsi.Uri, "true");
			WriteEndElement();
		}

		public void WriteObjectElement(XName name, object value)
		{
			WriteStartElement(name);
			var xsiType = Xsi.TypeOf(value);
			_writer.WriteAttributeString("type", Xsi.Uri, xsiType);
			WriteValue(value);
			WriteEndElement();
		}

		private void WriteValue(object value)
		{
			_writer.WriteValue(value);
		}
	}
}
