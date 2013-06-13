using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace TsvBits.Serialization.Json
{
	internal sealed class JsonReaderImpl : IReader
	{
		private readonly XNamespace _ns;
		private readonly JsonReader _reader;
		private readonly bool _dispose;
		private bool _root = true;
		private enum ElementKind { Object, Array }
		private readonly Stack<ElementKind> _elementStack = new Stack<ElementKind>();

		private JsonReaderImpl(XNamespace ns, JsonReader reader, bool dispose)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			_ns = ns;
			_reader = reader;
			_dispose = dispose;
		}

		public static IReader Create(XNamespace ns, TextReader input)
		{
			var reader = new JsonTextReader(input)
				{
					CloseInput = false,
					DateParseHandling = DateParseHandling.DateTime,
					DateTimeZoneHandling = DateTimeZoneHandling.Local,
					Culture = CultureInfo.InvariantCulture,
					FloatParseHandling = FloatParseHandling.Double,
				};
			return new JsonReaderImpl(ns, reader, true);
		}

		public void Dispose()
		{
			if (_dispose)
			{
				((IDisposable)_reader).Dispose();
			}
		}

		public Format Format
		{
			get { return Format.Json; }
		}

		public XName CurrentName
		{
			get
			{
				if (_reader.TokenType == JsonToken.PropertyName || _reader.TokenType == JsonToken.StartConstructor)
					return _ns.GetName((string)_reader.Value);
				return _ns.GetName("Item");
			}
		}

		public IEnumerable<KeyValuePair<XName, string>> ReadAttributes()
		{
			return Enumerable.Empty<KeyValuePair<XName, string>>();
		}

		public void Skip()
		{
			_reader.Skip();
		}

		public string ReadString()
		{
			return Convert.ToString(ReadObject(), CultureInfo.InvariantCulture);
		}

		public object ReadObject()
		{
			var value = _reader.Value;
			_reader.Read();
			return value;
		}

		public bool ReadStartElement(XName name)
		{
			if (_reader.TokenType == JsonToken.StartArray)
			{
				_elementStack.Push(ElementKind.Array);
				_reader.Read();
				return true;
			}

			_elementStack.Push(ElementKind.Object);
			_reader.MoveToPropertyName();

			if (_root)
			{
				_root = false;
				return true;
			}

			// return Equals(CurrentName, name);
			return true;
		}

		public IEnumerable<XName> ReadChildElements()
		{
			// primitive | object | array
			var kind = _elementStack.Peek();
			if (kind == ElementKind.Object)
			{
				int depth = _reader.Depth - 1;
				while (true)
				{
					if (_reader.TokenType == JsonToken.EndObject && _reader.Depth == depth)
					{
						EndElement();
						yield break;
					}
						

					if (_reader.TokenType == JsonToken.PropertyName)
					{
						var name = CurrentName;
						_reader.Read();
						yield return name;
					}
					else
					{
						_reader.Read();
					}
				}
			}

			if (kind == ElementKind.Array)
			{
				int depth = _reader.Depth - 1;
				while (true)
				{
					if (_reader.TokenType == JsonToken.EndArray && _reader.Depth == depth)
					{
						EndElement();
						yield break;
					}

					if (_reader.TokenType == JsonToken.StartConstructor || IsPrimitive(_reader.TokenType))
					{
						var name = CurrentName;
						_reader.Read();
						yield return name;
					}
					else
					{
						_reader.Read();
					}
				}
			}
		}

		private void EndElement()
		{
			_elementStack.Pop();

			_reader.Read();

			if (_reader.TokenType == JsonToken.EndConstructor)
				_reader.Read();
		}

		private static bool IsPrimitive(JsonToken token)
		{
			switch (token)
			{
				case JsonToken.Integer:
				case JsonToken.Float:
				case JsonToken.String:
				case JsonToken.Boolean:
				case JsonToken.Null:
				case JsonToken.Undefined:
				case JsonToken.Date:
				case JsonToken.Bytes:
					return true;
				default:
					return false;
			}
		}
	}

	internal static class JsonReaderExtensions
	{
		public static void MoveToPropertyName(this JsonReader reader)
		{
			while (reader.TokenType != JsonToken.PropertyName && reader.Read())
			{
			}
		}
	}
}
