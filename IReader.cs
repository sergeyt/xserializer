using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	public interface IReader : IDisposable
	{
		Format Format { get; }
		XName CurrentName { get; }

		IEnumerable<KeyValuePair<XName, string>> ReadAttributes();

		void Skip();

		string ReadString();
		object ReadObject();

		bool ReadStartElement(XName name);

		IEnumerable<XName> ReadChildElements();
	}
}
