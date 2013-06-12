using System;
using System.IO;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace TsvBits.Serialization.Json
{
	internal sealed class JsonWriterImpl : IWriter
	{
		private readonly JsonWriter _writer;
		private bool _root = true;

		public JsonWriterImpl(TextWriter writer)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			_writer = CreateJsonWriter(writer);
		}

		private static JsonWriter CreateJsonWriter(TextWriter output)
		{
			return new JsonTextWriter(output)
				{
					CloseOutput = false,
					// Formatting = Formatting.Indented,
					DateFormatHandling = DateFormatHandling.IsoDateFormat,
					DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFZ"
				};
		}

		public void Dispose()
		{
		}

		public void WriteAttributeString(XName name, string value)
		{
			WritePrimitiveElement(name, value);
		}

		public void WriteStartElement(XName name)
		{
			if (_root)
			{
				_root = false;
			}
			else
			{
				WritePropertyName(name);
			}

			_writer.WriteStartObject();
		}

		public void WriteEndElement()
		{
			_writer.WriteEndObject();
		}

		public void WriteStartCollection(XName name)
		{
			WritePropertyName(name);
			_writer.WriteStartArray();
		}

		public void WriteEndCollection()
		{
			_writer.WriteEndArray();
		}

		public void WritePrimitiveElement(XName name, object value)
		{
			WritePropertyName(name);
			_writer.WriteValue(value);
		}

		public void WriteNullItem(XName name)
		{
			// TODO: check that we write collection now
			_writer.WriteNull();
		}

		public void WriteObjectElement(XName name, object value)
		{
			WritePrimitiveElement(name, value);
		}

		private void WritePropertyName(XName name)
		{
			_writer.WritePropertyName(name.LocalName);
		}
	}
}
