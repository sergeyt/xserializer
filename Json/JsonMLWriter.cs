using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace TsvBits.Serialization.Json
{
	using Pair = KeyValuePair<string, string>;

	/// <summary>
	/// JSON ML writer.
	/// </summary>
	/// <remarks>
	/// <seealso cref="http://www.jsonml.org/"/>
	/// </remarks>
	internal sealed class JsonMLWriter : IWriter
	{
		private sealed class Element
		{
			public Element(XName name)
			{
				Name = name;
				Namespaces = new HashSet<Pair>();
			}

			private XName Name { get; set; }
			public XNamespace Namespace { get { return Name.Namespace; } }
			public HashSet<Pair> Namespaces { get; private set; }
		}

		private readonly JsonWriter _writer;
		private readonly Stack<Element> _elementStack = new Stack<Element>();
		private readonly IDictionary<XNamespace, string> _nsPrefixes = new Dictionary<XNamespace, string>();
		private readonly List<Pair> _xmlnsQueue = new List<Pair>();

		private JsonMLWriter(JsonWriter writer)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			_writer = writer;
		}

		public static IWriter Create(TextWriter output)
		{
			return new JsonMLWriter(JsonWriterImpl.CreateJsonWriter(output));
		}

		public void Dispose()
		{
			((IDisposable)_writer).Dispose();
		}

		public void WriteAttributeString(XName name, string value)
		{
			if (_elementStack.Count == 0)
				throw new InvalidOperationException();

			if (_writer.WriteState != WriteState.Object)
				_writer.WriteStartObject();
			
			// attribute-name
			var prefix = PushNamespace(name.Namespace, true);
			var qn = QName(prefix, name.LocalName);

			WriteXmlns();

			_writer.WritePropertyName(qn);
			_writer.WriteValue(value);
		}

		public void WriteStartElement(XName name)
		{
			_elementStack.Push(new Element(name));

			// end attribute-list
			if (_writer.WriteState == WriteState.Object)
				_writer.WriteEndObject();

			_writer.WriteStartArray();
			
			var prefix = PushNamespace(name.Namespace, false);
			var qn = QName(prefix, name.LocalName);
			_writer.WriteValue(qn);

			WriteXmlns();
		}

		public void WriteEndElement()
		{
			// end attribute-list
			if (_writer.WriteState == WriteState.Object)
				_writer.WriteEndObject();

			_writer.WriteEndArray();

			_elementStack.Pop();
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
			if (value != null) _writer.WriteValue(value);
			WriteEndElement();
		}

		public void WriteObjectElement(XName name, object value)
		{
//			WriteStartElement(name);
//			var xsiType = Xsi.TypeOf(value);
//			WriteAttributeString(Xsi.Namespace + "type", xsiType);
//			_writer.WriteValue(value);
//			WriteEndElement();
			WritePrimitiveElement(name, value);
		}

		public void WriteNullItem(XName name)
		{
//			WriteStartElement(name);
//			WriteAttributeString(Xsi.Namespace + "nil", "true");
//			WriteEndElement();
			_writer.WriteNull();
		}

		private string PushNamespace(XNamespace ns, bool forAttribute)
		{
			if (string.IsNullOrEmpty(ns.NamespaceName))
			{
				if (forAttribute) return string.Empty;
				if (_elementStack.Count == 1) return string.Empty;
			}

			if (forAttribute)
			{
				var prefix = RegisterPrefix(ns);
				if (!InScope(prefix, ns))
				{
					_xmlnsQueue.Add(new Pair(prefix, ns.NamespaceName));
				}
				return prefix;
			}

			if (_elementStack.Count == 1 || !Equals(_elementStack.Peek().Namespace, ns))
			{
				_xmlnsQueue.Add(new Pair(string.Empty, ns.NamespaceName));
			}

			return string.Empty;
		}

		private bool InScope(string prefix, XNamespace ns)
		{
			var p = new Pair(prefix, ns.NamespaceName);
			return (from e in _elementStack.ToArray()
			        select e.Namespaces.Contains(p)).Any();
		}

		private string RegisterPrefix(XNamespace ns)
		{
			string prefix;
			if (_nsPrefixes.TryGetValue(ns, out prefix))
				return prefix;

			prefix = ((char)('a' + _nsPrefixes.Count)).ToString();
			_nsPrefixes.Add(ns, prefix);

			return prefix;
		}

		private void WriteXmlns()
		{
			if (_xmlnsQueue.Count == 0) return;

			// start attribute-list
			if (_writer.WriteState != WriteState.Object)
				_writer.WriteStartObject();

			foreach (var pair in _xmlnsQueue)
			{
				var qn = string.IsNullOrEmpty(pair.Key) ? "xmlns" : "xmlns:" + pair.Key;
				_writer.WritePropertyName(qn);
				_writer.WriteValue(pair.Value);
			}

			_xmlnsQueue.Clear();
		}

		private static string QName(string prefix, string localName)
		{
			return string.IsNullOrEmpty(prefix) ? localName : prefix + ":" + localName;
		}
	}
}
