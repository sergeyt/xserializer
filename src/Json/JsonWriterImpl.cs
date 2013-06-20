using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace TsvBits.Serialization.Json
{
	internal sealed class JsonWriterImpl : IWriter
	{
		private readonly JsonWriter _writer;
		private bool _root = true;
		private enum  ElementKind { Obj, Ctor }
		private readonly Stack<ElementKind> _elementStack = new Stack<ElementKind>();

		private JsonWriterImpl(JsonWriter writer)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			_writer = writer;
		}

		public static IWriter Create(TextWriter output)
		{
			return new JsonWriterImpl(CreateJsonWriter(output));
		}

		internal static JsonWriter CreateJsonWriter(TextWriter output)
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
				_elementStack.Push(ElementKind.Obj);
			}
			else
			{
				if (_writer.WriteState == WriteState.Array)
				{
					_elementStack.Push(ElementKind.Ctor);
					_writer.WriteStartConstructor(name.LocalName);
				}
				else
				{
					_elementStack.Push(ElementKind.Obj);
					WritePropertyName(name);
				}
			}

			_writer.WriteStartObject();
		}

		public void WriteEndElement()
		{
			switch (_elementStack.Pop())
			{
				case ElementKind.Obj:
					_writer.WriteEndObject();
					break;
				case ElementKind.Ctor:
					_writer.WriteEndObject();
					_writer.WriteEndConstructor();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
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
			if (_writer.WriteState != WriteState.Array)
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
