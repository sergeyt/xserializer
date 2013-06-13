using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace TsvBits.Serialization.Xml
{
	internal sealed class XmlWriterImpl : IWriter
	{
		private readonly XmlWriter _writer;
		private readonly bool _dispose;

		private XmlWriterImpl(XmlWriter writer, bool dispose)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			_writer = writer;
			_dispose = dispose;
		}

		public static IWriter Create(TextWriter output, XmlWriterSettings settings)
		{
			return new XmlWriterImpl(XmlWriter.Create(output, settings), true);
		}

		public static IWriter Create(XmlWriter writer)
		{
			return new XmlWriterImpl(writer, false);
		}

		public void Dispose()
		{
			if (_dispose)
			{
				((IDisposable)_writer).Dispose();
			}
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

		public void WriteStartCollection(XName name)
		{
			WriteStartElement(name);
		}

		public void WriteEndCollection()
		{
			WriteEndElement();
		}

		public void WritePrimitiveElement(XName name, object value)
		{
			WriteStartElement(name);
			WriteValue(value);
			WriteEndElement();
		}

		public void WriteNullItem(XName name)
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
