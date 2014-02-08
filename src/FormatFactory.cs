using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using TsvBits.Serialization.Json;
using TsvBits.Serialization.Xml;

namespace TsvBits.Serialization
{
	// TODO load Json writer/reader from separate assembly

	internal static class FormatFactory
	{
		public static IWriter CreateWriter(TextWriter output, Format format)
		{
			switch (format)
			{
				case Format.Xml:
					var xws = new XmlWriterSettings {ConformanceLevel = ConformanceLevel.Fragment};
					return XmlWriterImpl.Create(output, xws);
				case Format.Json:
					return JsonWriterImpl.Create(output);
				case Format.JsonML:
					return JsonMLWriter.Create(output);
				default:
					throw new ArgumentOutOfRangeException("format");
			}
		}

		public static IReader CreateReader(TextReader input, Format format, XNamespace rootNamespace)
		{
			switch (format)
			{
				case Format.Xml:
					return XmlReaderImpl.Create(input);
				case Format.Json:
					return JsonReaderImpl.Create(rootNamespace, input);
				case Format.JsonML:
					return JsonMLReader.Create(input);
				default:
					throw new ArgumentOutOfRangeException("format");
			}
		}
	}
}
