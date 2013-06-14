using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace TsvBits.Serialization.Json
{
	/// <summary>
	/// JSON ML reader.
	/// </summary>
	/// <remarks>
	/// <seealso cref="http://www.jsonml.org/"/>
	/// </remarks>
	internal sealed class JsonMLReader : IReader
	{
		private sealed class Element
		{
			public Element(XName name, XNamespace defaultNs,
			               IEnumerable<KeyValuePair<XName, string>> attributes,
			               IDictionary<string, XNamespace> xmlns)
			{
				Name = name;
				DefaultNamespace = defaultNs;
				Attributes = (attributes ?? Enumerable.Empty<KeyValuePair<XName, string>>()).ToList().AsReadOnly();
				Namespaces = xmlns;
			}

			public XName Name { get; private set; }
			public XNamespace DefaultNamespace { get; private set; }
			public IList<KeyValuePair<XName, string>> Attributes { get; private set; }
			// prefix -> ns
			public IDictionary<string, XNamespace> Namespaces { get; private set; }

			public override string ToString()
			{
				return Name.ToString();
			}
		}

		private readonly JsonReader _reader;
		private readonly bool _dispose;
		private readonly Stack<Element> _elementStack = new Stack<Element>();
		private Element _peekElement;
		
		private JsonMLReader(JsonReader reader, bool dispose)
		{
			_reader = reader;
			_dispose = dispose;
		}

		public static IReader Create(TextReader input)
		{
			return new JsonMLReader(JsonReaderImpl.CreateJsonReader(input), true);
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
			get { return Format.JsonML; }
		}

		private Element CurrentElement
		{
			get { return _elementStack.Peek(); }
		}

		public XName CurrentName
		{
			get { return CurrentElement.Name; }
		}

		public IEnumerable<KeyValuePair<XName, string>> ReadAttributes()
		{
			return CurrentElement.Attributes;
		}

		public void Skip()
		{
			_reader.Skip();
		}

		public string ReadString()
		{
			var value = ReadObject();
			return value == null ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
		}

		public object ReadObject()
		{
			var value = _reader.Value;
			_reader.Read();
			return value;
		}

		public bool ReadStartElement(XName name)
		{
			var element = _peekElement ?? ReadElement();
			_peekElement = null;
			_elementStack.Push(element);
			return Equals(element.Name, name);
		}

		public IEnumerable<XName> ReadChildElements()
		{
			if (_reader.TokenType == JsonToken.EndArray)
			{
				_reader.Read();
				yield break;
			}

			int depth = _reader.Depth - 1;
			while (true)
			{
				if (_reader.TokenType == JsonToken.EndArray && depth == _reader.Depth)
				{
					_reader.Read();
					break;
				}

				if (_reader.TokenType == JsonToken.StartArray)
				{
					_peekElement = ReadElement();
					yield return _peekElement.Name;
				}
				else if (JsonReaderImpl.IsPrimitive(_reader.TokenType))
				{
					// primitive collection item
					yield return null;
				}
				else
				{
					_reader.Read();
				}
			}
		}

		private Element ReadElement()
		{
			if (!_reader.MoveTo(JsonToken.StartArray))
				throw new InvalidOperationException();
			_reader.MustRead();

			var qname = ReadString();
			if (string.IsNullOrEmpty(qname))
				throw new InvalidOperationException();

			var attrs = new List<KeyValuePair<string, string>>();
			var xmlns = new Dictionary<string, XNamespace>();
			XNamespace dns = null;
			foreach (var attr in ReadAttrsObject())
			{
				if (attr.Key.StartsWith("xmlns"))
				{
					if (attr.Key == "xmlns")
					{
						dns = XNamespace.Get(attr.Value);
					}
					else
					{
						var prefix = attr.Key.Substring(attr.Key.IndexOf(':') + 1);
						xmlns.Add(prefix, attr.Value);
					}
				}
				else
				{
					attrs.Add(new KeyValuePair<string, string>(attr.Key, attr.Value));
				}
			}

			if (dns == null)
			{
				dns = _elementStack.Count > 0 ? _elementStack.Peek().DefaultNamespace : XNamespace.None;
			}

			var elementName = qname.IndexOf(':') >= 0 ? ResolveQName(xmlns, qname, false) : dns.GetName(qname);

			return new Element(
				elementName, dns,
				attrs.Select(x => new KeyValuePair<XName, string>(ResolveQName(xmlns, x.Key, true), x.Value)),
				xmlns);
		}

		private IEnumerable<KeyValuePair<string, string>> ReadAttrsObject()
		{
			if (_reader.TokenType == JsonToken.EndArray) // empty element
				yield break;
			if (_reader.TokenType == JsonToken.String) // content of primitive element
				yield break;
			
			if (_reader.TokenType == JsonToken.StartObject)
			{
				_reader.MustRead();

				while (true)
				{
					if (_reader.TokenType == JsonToken.EndObject)
					{
						_reader.Read();
						break;
					}

					var name = (string)_reader.Value;
					_reader.Read();

					var value = Convert.ToString(_reader.Value, CultureInfo.InvariantCulture);
					_reader.Read();

					yield return new KeyValuePair<string, string>(name, value);
				}
			}
		}

		private XName ResolveQName(IDictionary<string,XNamespace> xmlns, string qname, bool forAttribute)
		{
			var i = qname.IndexOf(':');
			if (i >= 0)
			{
				var prefix = qname.Substring(0, i);
				var localName = qname.Substring(i + 1);
				var ns = xmlns.Get(prefix);
				if (ns != null) return ns.GetName(localName);

				ns = (from e in _elementStack.ToArray()
				      select e.Namespaces.Get(prefix))
					.FirstOrDefault(x => x != null);

				if (ns == null)
					throw new InvalidOperationException(string.Format("Unable to resolve namespace for QName: {0}", qname));

				return ns + localName;
			}

			if (forAttribute)
			{
				return XNamespace.None + qname;
			}

			throw new InvalidOperationException("You hit bad code path.");
		}
	}
}
