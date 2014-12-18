using System;
using System.IO;
using System.Text;
using System.Xml;
using TsvBits.Serialization.Core;
using TsvBits.Serialization.Xml;

namespace TsvBits.Serialization
{
	/// <summary>
	/// Exposes (de)serialization API based on schema of <see cref="IElementDef"/> definitions.
	/// </summary>
	public static class ScopeExtensions
	{
		#region Parse, Read

		/// <summary>
		/// Parses specified string.
		/// </summary>
		/// <typeparam name="T">The object type to create.</typeparam>
		/// <param name="schema">Serialization schema.</param>
		/// <param name="s">The string to parse.</param>
		/// <param name="format">Specifies string format.</param>
		public static T Parse<T>(this IScope schema, string s, Format format)
		{
			using (var input = new StringReader(s))
			using (var reader = FormatFactory.CreateReader(input, format, schema.Namespace))
			{
				return Read<T>(schema, reader);
			}
		}

		/// <summary>
		/// Reads specified object from given xml string.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="schema">Serialization schema.</param>
		/// <param name="xml">The xml string to parse.</param>
		/// <param name="obj">The object to deserialize.</param>
		public static void ReadXmlString<T>(this IScope schema, string xml, T obj)
		{
			using (var input = new StringReader(xml))
			using (var reader = XmlReaderImpl.Create(input))
			{
				Read(schema, reader, obj);
			}
		}		

		/// <summary>
		/// Reads specified object with given reader.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="schema">Serialization schema.</param>
		/// <param name="reader">The reader.</param>
		/// <param name="obj">The object to deserialize.</param>
		public static void Read<T>(this IScope schema, IReader reader, T obj)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			Deserializer.ReadElement(schema, reader, obj);
		}

		/// <summary>
		/// Reads object with given reader.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="schema">The serialization schema.</param>
		/// <param name="reader">The reader.</param>
		public static T Read<T>(this IScope schema, IReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			// TODO move to Deserializer
			var def = ResolveElementDef(schema, reader, typeof(T));
			return (T)Deserializer.ReadElement(schema, reader, def, null);
		}

		private static IElementDef ResolveElementDef(IScope schema, IReader reader, Type type)
		{
#if FULL
			if (reader.Format == Format.Json)
			{
				return schema.GetElementDef(type);
			}
#endif
			return schema.GetElementDef(reader.CurrentName) ?? schema.GetElementDef(type);
		}

		/// <summary>
		/// Reads specified object with given reader.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="schema">Serialization schema.</param>
		/// <param name="reader">The xml reader.</param>
		/// <param name="obj">The object to deserialize.</param>
		public static void Read<T>(this IScope schema, XmlReader reader, T obj)
		{
			using (var impl = XmlReaderImpl.Create(reader))
				Read(schema, impl, obj);
		}

		/// <summary>
		/// Reads object with given reader.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="schema">Serialization schema.</param>
		/// <param name="reader">The xml reader.</param>
		public static T Read<T>(this IScope schema, XmlReader reader)
		{
			using (var impl = XmlReaderImpl.Create(reader))
				return Read<T>(schema, impl);
		}

		#endregion

		#region Write

		/// <summary>
		/// Serializes given object.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="schema">Serialization schema.</param>
		/// <param name="writer">The writer.</param>
		/// <param name="obj">The object to serialize.</param>
		public static void Write<T>(this IScope schema, IWriter writer, T obj)
		{
			Serializer.WriteElement(schema, writer, obj);
		}

		/// <summary>
		/// Serializes given object.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="schema">Serialization schema.</param>
		/// <param name="writer">The xml writer.</param>
		/// <param name="obj">The object to serialize.</param>
		public static void Write<T>(this IScope schema, XmlWriter writer, T obj)
		{
			using (var impl = XmlWriterImpl.Create(writer))
				Write(schema, impl, obj);
		}

		#endregion

		#region ToString

		/// <summary>
		/// Serializes given object as XML string.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="schema">Serialization schema.</param>
		/// <param name="obj">The object to serialize.</param>
		/// <returns>XML string representing the object.</returns>
		public static string ToXmlString<T>(this IScope schema, T obj)
		{
			return ToString(schema, obj, Format.Xml);
		}

#if FULL
		/// <summary>
		/// Serializes given object as JSON string.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="schema">Serialization schema.</param>
		/// <param name="obj">The object to serialize.</param>
		/// <returns>JSON string representing the object.</returns>
		public static string ToJsonString<T>(this IScope schema, T obj)
		{
			return ToString(schema, obj, Format.Json);
		}
#endif

		/// <summary>
		/// Serializes given object to string of specified format.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="schema">Serialization schema.</param>
		/// <param name="obj">The object to serialize.</param>
		/// <param name="format">The output format.</param>
		/// <returns>Output string.</returns>
		public static string ToString<T>(this IScope schema, T obj, Format format)
		{
			var output = new StringBuilder();
			using (var textWriter = new StringWriter(output))
			using (var writer = FormatFactory.CreateWriter(textWriter, format))
				Write(schema, writer, obj);
			return output.ToString();
		}

		#endregion

		#region BSON

#if FULL
		public static byte[] ToBson<T>(this IScope schema, T obj)
		{
			var output = new MemoryStream();
			Write(schema, FormatFactory.CreateWriter(output, Format.Bson), obj);
			output.Close();
			return output.ToArray();
		}

		public static void ReadBson<T>(this IScope schema, Stream input, T obj)
		{
			Read(schema, FormatFactory.CreateReader(input, Format.Bson, schema.Namespace), obj);
		}
#endif

		#endregion
	}
}
