using System;
using System.IO;
using System.Text;
using System.Xml;
using TsvBits.Serialization.Xml;

namespace TsvBits.Serialization
{
	// TODO consider to make XSerialize to be internal class exposing serialization API in schema class

	/// <summary>
	/// Implements (de)serialization based on schema specified by <see cref="IElementDef"/> definitions.
	/// </summary>
	public sealed partial class XSerializer
	{
		private readonly Scope _rootScope;

		private XSerializer(Scope scope)
		{
			if (scope == null) throw new ArgumentNullException("scope");

			_rootScope = scope;
		}

		public static XSerializer New(Scope scope)
		{
			return new XSerializer(scope);
		}

		/// <summary>
		/// Parses specified string.
		/// </summary>
		/// <typeparam name="T">The object type to create.</typeparam>
		/// <param name="s">The string to parse.</param>
		/// <param name="format">Specifies string format.</param>
		public T Parse<T>(string s, Format format)
		{
			using (var input = new StringReader(s))
			using (var reader = FormatFactory.CreateReader(input, format, _rootScope.Namespace))
			{
				return Read<T>(reader);
			}
		}

		/// <summary>
		/// Reads specified object from given xml string.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="xml">The xml string to parse.</param>
		/// <param name="obj">The object to deserialize.</param>
		public void ReadXmlString<T>(string xml, T obj)
		{
			using (var input = new StringReader(xml))
			using (var reader = XmlReaderImpl.Create(input))
			{
				Read(reader, obj);
			}
		}

		/// <summary>
		/// Reads specified object with given reader.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="reader">The reader.</param>
		/// <param name="obj">The object to deserialize.</param>
		public void Read<T>(IReader reader, T obj)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			var def = _rootScope.ElemDef(obj.GetType());
			ReadElement(reader, def, obj);
		}

		/// <summary>
		/// Reads object with given reader.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="reader">The reader.</param>
		public T Read<T>(IReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			var def = _rootScope.ElemDef(typeof(T));
			return (T)ReadElement(reader, def, null);
		}

		/// <summary>
		/// Reads specified object with given reader.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="reader">The xml reader.</param>
		/// <param name="obj">The object to deserialize.</param>
		public void Read<T>(XmlReader reader, T obj)
		{
			using (var impl = XmlReaderImpl.Create(reader))
				Read(impl, obj);
		}

		/// <summary>
		/// Reads object with given reader.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="reader">The xml reader.</param>
		public T Read<T>(XmlReader reader)
		{
			using (var impl = XmlReaderImpl.Create(reader))
				return Read<T>(impl);
		}

		/// <summary>
		/// Serializes given object.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="writer">The writer.</param>
		/// <param name="obj">The object to serialize.</param>
		public void Write<T>(IWriter writer, T obj)
		{
			var def = _rootScope.ElemDef(obj.GetType());
			WriteElement(writer, obj, def, def.Name);
		}

		/// <summary>
		/// Serializes given object.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="writer">The xml writer.</param>
		/// <param name="obj">The object to serialize.</param>
		public void Write<T>(XmlWriter writer, T obj)
		{
			using (var impl = XmlWriterImpl.Create(writer))
				Write(impl, obj);
		}
		
		/// <summary>
		/// Serializes given object as XML string.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="obj">The object to serialize.</param>
		/// <returns>XML string representing the object.</returns>
		public string ToXmlString<T>(T obj)
		{
			return ToString(obj, Format.Xml);
		}

		/// <summary>
		/// Serializes given object as JSON string.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="obj">The object to serialize.</param>
		/// <returns>JSON string representing the object.</returns>
		public string ToJsonString<T>(T obj)
		{
			return ToString(obj, Format.Json);
		}

		/// <summary>
		/// Serializes given object to string of specified format.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="obj">The object to serialize.</param>
		/// <param name="format">The output format.</param>
		/// <returns>Output string.</returns>
		public string ToString<T>(T obj, Format format)
		{
			var output = new StringBuilder();
			using (var textWriter = new StringWriter(output))
			using (var writer = FormatFactory.CreateWriter(textWriter, format))
				Write(writer, obj);
			return output.ToString();
		}
	}
}
