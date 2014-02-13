using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace TsvBits.Serialization.Xml
{
	internal sealed class XmlReaderImpl : IReader
	{
		private readonly XmlReader _reader;
		private readonly bool _dispose;

		private XmlReaderImpl(XmlReader reader, bool dispose)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			reader.MoveToFirstElement();

			_reader = reader;
			_dispose = dispose;
		}

		public XmlReader XmlReader
		{
			get { return _reader; }
		}

		public static IReader Create(TextReader input)
		{
			return new XmlReaderImpl(XmlReader.Create(input), true);
		}

		public static IReader Create(XmlReader reader)
		{
			return new XmlReaderImpl(reader, false);
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
			get { return Format.Xml; }
		}

		public XName CurrentName
		{
			get { return _reader.CurrentXName(); }
		}

		public IEnumerable<KeyValuePair<XName, string>> ReadAttributes()
		{
			if (_reader.MoveToFirstAttribute())
			{
				do
				{
					yield return new KeyValuePair<XName, string>(CurrentName, _reader.Value);
				} while (_reader.MoveToNextAttribute());

				_reader.MoveToElement();
			}
		}

		public void Skip()
		{
			_reader.Skip();
		}

		public string ReadString()
		{
			return _reader.ReadStringOrNull();
		}

		public object ReadObject()
		{
			var xsiType = _reader.GetAttribute("type", Xsi.Uri);

			var s = ReadString();
			if (string.IsNullOrEmpty(xsiType)) return null;

			xsiType = xsiType.Substring(xsiType.IndexOf(':') + 1);
			Type valueType;
			if (Xsi.Name2Type.TryGetValue(xsiType, out valueType))
			{
				return Parse(valueType, s);
			}

			return null;
		}

		public bool ReadStartElement(XName name)
		{
			return _reader.IsStartElement(name.LocalName, name.NamespaceName);
		}

		public IEnumerable<XName> ReadChildElements()
		{
			return _reader.ReadChildElements();
		}

		private static readonly Scope EmptyScope = new Scope();

		private static object Parse(Type type, string s)
		{
			object result;
			if (!EmptyScope.TryRead(() => s, type, out result))
				throw new NotSupportedException(string.Format("Unknown type: {0}", type));
			return result;
		}
	}
}
