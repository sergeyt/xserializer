using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using TsvBits.Serialization.Xml;

namespace TsvBits.Serialization
{
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

		public string ToString<T>(T obj, Format format)
		{
			var output = new StringBuilder();
			using (var textWriter = new StringWriter(output))
			using (var writer = FormatFactory.CreateWriter(textWriter, format))
				Write(writer, obj);
			return output.ToString();
		}

		private object ReadElement(IReader reader, IElementDef def, Func<object> create)
		{
			if (def.IsImmutable)
			{
				// TODO pass original property name rather than xml name
				var props = ReadProperties(reader, null, def).ToDictionary(x => x.Key.Name.LocalName, x => x.Value);
				return def.Create(props);
			}

			var obj = create != null ? create() : Activator.CreateInstance(def.Type);
			ReadElement(reader, def, obj);
			return obj;
		}

		private void ReadElement(IReader reader, IElementDef def, object obj)
		{
			foreach (var p in ReadProperties(reader, obj, def))
			{
				var property = p.Key;
				if (!property.IsReadOnly)
					property.SetValue(obj, p.Value);
			}
		}

		private IEnumerable<KeyValuePair<IPropertyDef, object>> ReadProperties(IReader reader, object obj, IElementDef def)
		{
			if (!reader.ReadStartElement(def.Name))
				throw new XmlException(string.Format("Xml element not foud: {0}", def.Name));

			// read attributes
			if (def.Attributes.Any())
			{
				foreach (var attr in reader.ReadAttributes())
				{
					var property = def.Attributes[attr.Key];
					if (property != null)
					{
						var value = _rootScope.Parse(property.Type, attr.Value);
						yield return new KeyValuePair<IPropertyDef, object>(property, value);
					}
				}
			}

			bool json = reader.Format == Format.Json;

			// read child elements
			foreach (var name in reader.ReadChildElements())
			{
				var property = def.Elements[name];
				object value;

				if (json && property == null)
				{
					property = def.Attributes[XNamespace.None + name.LocalName];
					if (property != null)
					{
						value = _rootScope.Parse(property.Type, reader.ReadString());
						yield return new KeyValuePair<IPropertyDef, object>(property, value);
						continue;
					}
				}

				if (property == null) // unknown type
				{
					// todo: trace warning
					reader.Skip();
					continue;
				}

				if (ReadValue(reader, obj, def, property, out value))
				{
					yield return new KeyValuePair<IPropertyDef, object>(property, value);
				}
				else
				{
					// todo: trace warning
					reader.Skip();
				}
			}
		}

		private bool ReadValue(IReader reader, object obj, IElementDef def, IPropertyDef property, out object value)
		{
			var type = property.Type;

			if (type == typeof(object))
			{
				value = reader.ReadObject();
				return true;
			}

			if (_rootScope.TryReadString(() => reader.ReadString(), type, out value))
				return true;

			var elementDef = _rootScope.ElemDef(type);
			if (elementDef != null)
			{
				value = ReadElement(reader, elementDef, () => CreateElement(property, obj));
				return true;
			}

			if (ReadEnumElement(reader, type, out value))
				return true;

			var ienum = Reflector.FindIEnumerable(type);
			if (ienum != null)
			{
				var elementType = ienum.GetGenericArguments()[0];
				elementDef = new CollectionDef(this, property.Name, type, elementType);
				value = def.IsImmutable ? CreateList(elementType) : CreateElement(property, obj);
				ReadElement(reader, elementDef, value);
				return true;
			}

			value = null;
			return false;
		}

		private static bool ReadEnumElement(IReader reader, Type type, out object value)
		{
			if (type.IsNullable())
			{
				type = type.GetGenericArguments()[0];
			}

			if (type.IsEnum)
			{
				var s = reader.ReadString();
				value = Enum.Parse(type, s);
				return true;
			}

			value = null;
			return false;
		}

		private static object CreateList(Type elementType)
		{
			var listType = typeof(List<>).MakeGenericType(elementType);
			return Activator.CreateInstance(listType);
		}

		private static object CreateElement(IPropertyDef def, object target)
		{
			if (def == null) throw new NotSupportedException();
			var element = target != null ? def.GetValue(target) : null;
			return element ?? Activator.CreateInstance(def.Type);
		}

		private void WriteElement(IWriter writer, object obj, IElementDef def, XName name)
		{
			if (name == null) name = def.Name;

			var attributes = from attr in def.Attributes
				let value = attr.GetValue(obj)
				where value != null && !attr.IsDefaultValue(value)
				let stringValue = ToString(value)
				where !string.IsNullOrEmpty(stringValue)
				select new {attr.Name, Value = stringValue};

			var elements = from elem in def.Elements
				let value = elem.GetValue(obj)
				where value != null && !elem.IsDefaultValue(value)
				select new {elem.Name, Value = value, Definition = elem};

			// TODO do not write non-root empty elements

			writer.WriteStartElement(name);

			foreach (var attr in attributes)
			{
				writer.WriteAttributeString(attr.Name, attr.Value);
			}

			foreach (var elem in elements)
			{
				WriteValue(writer, elem.Definition, elem.Name, elem.Value);
			}

			writer.WriteEndElement();
		}

		private void WriteValue(IWriter writer, IPropertyDef property, XName name, object value)
		{
			if (value == null) return;

			if (value is Enum && WriteStringElement(writer, property, name, value))
				return;

			if (value.IsPrimitive())
			{
				if (property.Type == typeof(object))
				{
					writer.WriteObjectElement(name, value);
				}
				else
				{
					writer.WritePrimitiveElement(name, value);
				}
				return;
			}

			if (WriteStringElement(writer, property, name, value))
				return;
			
			var type = value.GetType();
			var elementDef = _rootScope.ElemDef(type);
			if (elementDef != null)
			{
				WriteElement(writer, value, elementDef, elementDef.Name);
				return;
			}

			var collection = value as IEnumerable;
			if (collection != null)
			{
				var itemDef = new CollectionItemDef(property.ItemName, Reflector.GetItemType(type));
				var empty = true;
				foreach (var item in collection)
				{
					if (empty)
					{
						writer.WriteStartCollection(name);
						empty = false;
					}
					if (item == null)
					{
						writer.WriteNullItem(property.ItemName);
						continue;
					}
					WriteValue(writer, itemDef, property.ItemName, item);
				}
				if (!empty) writer.WriteEndCollection();
				return;
			}

			throw new InvalidOperationException(string.Format("Unknown element. Name: {0}. Type: {1}.", name, type));
		}

		private bool WriteStringElement(IWriter writer, IPropertyDef property, XName name, object value)
		{
			string s;
			if (!_rootScope.TryConvertToString(value, out s))
				return false;

			if (string.IsNullOrEmpty(s))
				return true;

			if (property.Type == typeof(object))
				writer.WriteObjectElement(name, value);
			else
				writer.WritePrimitiveElement(name, s);

			return true;
		}

		private string ToString(object value)
		{
			if (value == null) return string.Empty;

			string s;
			if (_rootScope.TryConvertToString(value, out s))
				return s;

			return Convert.ToString(value, CultureInfo.InvariantCulture);
		}

		private Type GetElementType(XName name)
		{
			var def = _rootScope.ElemDef(name);
			return def != null ? def.Type : null;
		}

		private sealed class CollectionItemDef : IPropertyDef
		{
			public CollectionItemDef(XName name, Type type)
			{
				Name = name;
				Type = type;
				ItemName = name;
			}

			public string PropertyName { get { return "Item"; } }
			public Type Type { get; private set; }
			public XName Name { get; private set; }
			public XName ItemName { get; private set; }
			public bool IsReadOnly { get { return true; } }

			public object GetValue(object target)
			{
				throw new NotSupportedException();
			}

			public void SetValue(object target, object value)
			{
				throw new NotSupportedException();
			}

			public bool IsDefaultValue(object value)
			{
				return false;
			}
		}
	}
}
