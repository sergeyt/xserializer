using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace XmlSerialization
{
	/// <summary>
	/// Implements XML (de)serialization based on schema specified by <see cref="IElementDef"/> definitions.
	/// </summary>
	public sealed class XSerializer
	{
		private sealed class TypeDef
		{
			private readonly Func<string, object> _read;
			private readonly Func<object, string> _write;

			public TypeDef(Func<string, object> read, Func<object, string> write)
			{
				_read = read;
				_write = write;
			}

			public object Read(string value)
			{
				return _read(value);
			}

			public string Write(object value)
			{
				return _write(value);
			}
		}

		private readonly IDictionary<Type, TypeDef> _types = new Dictionary<Type, TypeDef>();
		private readonly IDictionary<Type, IElementDef> _elementDefs = new Dictionary<Type, IElementDef>();
		private readonly IDictionary<XName, IElementDef> _elementDefsByName = new Dictionary<XName, IElementDef>();
		
		private XSerializer(IEnumerable<IElementDef> elements)
		{
			RegisterTypes();

			foreach (var element in elements)
				Elem(element);
		}

		private void RegisterTypes()
		{
			Type(x => x, x => x);
			Type(x => Convert.ToBoolean(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			Type(x => Convert.ToSByte(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			Type(x => Convert.ToByte(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			Type(x => Convert.ToInt16(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			Type(x => Convert.ToUInt16(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			Type(x => Convert.ToInt32(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			Type(x => Convert.ToUInt32(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			Type(x => Convert.ToInt64(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			Type(x => Convert.ToUInt64(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			Type(x => Convert.ToSingle(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			Type(x => Convert.ToDouble(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			Type(x => Convert.ToDecimal(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			Type(x => Convert.ToDateTime(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x, XmlDateTimeSerializationMode.Utc));
		}

		public static XSerializer New(params IElementDef[] defs)
		{
			return new XSerializer(defs);
		}

		/// <summary>
		/// Registers simple type serializable to string.
		/// </summary>
		/// <typeparam name="T">The type to register.</typeparam>
		/// <param name="read">The parser.</param>
		/// <param name="write">The writer.</param>
		public XSerializer Type<T>(Func<string, T> read, Func<T, string> write)
		{
			_types.Add(typeof(T), new TypeDef(s => read(s), o => write((T)o)));
			return this;
		}

		public XSerializer Elem(IElementDef def)
		{
			_elementDefs.Add(def.Type, def);

			if (!_elementDefsByName.ContainsKey(def.Name))
				_elementDefsByName.Add(def.Name, def);

			return this;
		}

		private IElementDef ResolveElementDef(Type type)
		{
			IElementDef def;
			if (!_elementDefs.TryGetValue(type, out def))
				throw new XmlException(string.Format("Unknown type: {0}", type));
			return def;
		}

		/// <summary>
		/// Parses specified xml string.
		/// </summary>
		/// <typeparam name="T">The object type to create.</typeparam>
		/// <param name="xml">The xml string to parse.</param>
		public T Parse<T>(string xml)
		{
			using (var input = new StringReader(xml))
			using (var reader = XmlReader.Create(input))
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
			using (var reader = XmlReader.Create(input))
			{
				Read(reader, obj);
			}
		}

		/// <summary>
		/// Reads specified object with given reader.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="reader">The xml reader.</param>
		/// <param name="obj">The object to deserialize.</param>
		public void Read<T>(XmlReader reader, T obj)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			var def = ResolveElementDef(obj.GetType());
			ReadElement(reader, obj, def);
		}

		/// <summary>
		/// Reads object with given reader.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="reader">The xml reader.</param>
		public T Read<T>(XmlReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			while (reader.NodeType != XmlNodeType.Element && reader.Read()){}

			var def = _elementDefsByName[reader.CurrentXName()];
			// TODO: support immutable objects
			var obj = Activator.CreateInstance(def.Type);
			ReadElement(reader, obj, def);

			return (T)obj;
		}

		/// <summary>
		/// Serializes given object.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="writer">The xml writer.</param>
		/// <param name="obj">The object to serialize.</param>
		public void Write<T>(XmlWriter writer, T obj)
		{
			var def = ResolveElementDef(obj.GetType());
			WriteElement(writer, obj, def, def.Name);
		}
		
		/// <summary>
		/// Serializes given object as XML string.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="obj">The object to serialize.</param>
		/// <param name="asFragment">Specifies whether to omit xml declaration.</param>
		/// <returns>XML string representing the object.</returns>
		public string ToXmlString<T>(T obj, bool asFragment)
		{
			var output = new StringBuilder();
			var xws = new XmlWriterSettings();
			if (asFragment) xws.ConformanceLevel = ConformanceLevel.Fragment;
			using (var writer = XmlWriter.Create(output, xws))
				Write(writer, obj);
			return output.ToString();
		}

		private void ReadElement(XmlReader reader, object obj, IElementDef def)
		{
			if (!reader.IsStartElement(def.Name.LocalName, def.Name.NamespaceName))
				throw new XmlException(string.Format("Xml element not foud: {0}", def.Name));

			// read attributes
			if (def.Attributes.Any() && reader.MoveToFirstAttribute())
			{
				do
				{
					var name = reader.CurrentXName();
					var attr = def.Attributes[name];
					if (attr != null)
					{
						var value = Parse(attr.Type, reader.Value);
						attr.SetValue(obj, value);
					}
				} while (reader.MoveToNextAttribute());

				reader.MoveToElement();
			}

			if (reader.IsEmptyElement) return;

			// read child elements
			int depth = reader.Depth;
			reader.Read(); // move to first child node

			while (!(reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth))
			{
				if (reader.NodeType != XmlNodeType.Element)
				{
					reader.Read();
					continue;
				}

				var name = reader.CurrentXName();
				var property = def.Elements[name];
				if (property == null) // unknown type
				{
					// todo: trace warning
					reader.Skip();
					continue;
				}

				if (ReadValue(reader, def, obj, property))
					continue;

				// todo: trace warning
				reader.Skip();
			}

			reader.ReadEndElement();
		}

		private bool ReadValue(XmlReader reader, IElementDef def, object obj, IPropertyDef property)
		{
			var type = property.Type;

			TypeDef simpleType;
			if (_types.TryGetValue(type, out simpleType))
			{
				var s = reader.ReadString();
				var value = simpleType.Read(s);
				property.SetValue(obj, value);
				return true;
			}

			IElementDef elementDef;
			if (_elementDefs.TryGetValue(type, out elementDef))
			{
				var element = CreateElement(property, obj);
				ReadElement(reader, element, elementDef);
				return true;
			}

			if (type.IsEnum)
			{
				var s = reader.ReadString();
				var value = Enum.Parse(type, s);
				property.SetValue(obj, value);
				return true;
			}

			var ienum = FindIEnumerable(type);
			if (ienum != null)
			{
				var collection = CreateElement(property, obj);
				if (collection == null) throw new NotSupportedException();
				var elementType = ienum.GetGenericArguments()[0];
				elementDef = new CollectionDef(this, property.Name, type, elementType);
				ReadElement(reader, collection, elementDef);
				return true;
			}

			return false;
		}

		private static object CreateElement(IPropertyDef def, object target)
		{
			if (def == null) throw new NotSupportedException();
			if (def.IsReadOnly)
			{
				return def.GetValue(target);
			}
			var value = Activator.CreateInstance(def.Type);
			def.SetValue(target, value);
			return value;
		}

		private void WriteElement(XmlWriter writer, object obj, IElementDef def, XName name)
		{
			if (name == null) name = def.Name;

			writer.WriteStartElement(name.LocalName, name.NamespaceName);

			foreach (var attr in def.Attributes)
			{
				var value = attr.GetValue(obj);
				if (value == null) continue;
				var s = ToString(value);
				if (string.IsNullOrEmpty(s)) continue;
				writer.WriteAttributeString(attr.Name.LocalName, attr.Name.NamespaceName, s);
			}

			var collection = obj as IEnumerable;
			if (collection != null)
			{
				// TODO: support custom collections with own properties
				foreach (var item in collection)
				{
					WriteValue(writer, null, item);
				}
			}
			else
			{
				foreach (var elem in def.Elements)
				{
					var value = elem.GetValue(obj);
					if (value == null) continue;
					WriteValue(writer, elem, value);
				}
			}
			
			writer.WriteEndElement();
		}

		private void WriteValue(XmlWriter writer, IPropertyDef def, object value)
		{
			if (value == null) return;

			var name = def != null ? def.Name : null;

			string s;
			if (TryConvertToString(value, out s))
			{
				if (string.IsNullOrEmpty(s)) return;
				writer.WriteElementString(name.LocalName, name.NamespaceName, s);
				return;
			}

			var collection = value as IEnumerable;
			if (collection != null)
			{
				bool empty = true;
				foreach (var item in collection)
				{
					if (empty)
					{
						writer.WriteStartElement(name.LocalName, name.NamespaceName);
						empty = false;
					}
					WriteValue(writer, null, item);
				}
				if (!empty) writer.WriteEndElement();
				return;
			}

			IElementDef elementDef;
			if (_elementDefs.TryGetValue(value.GetType(), out elementDef))
			{
				WriteElement(writer, value, elementDef, name);
				return;
			}

			throw new InvalidOperationException(string.Format("Unknown element. Name: {0}. Type: {1}.", def.Name, def.Type));
		}

		private object Parse(Type type, string s)
		{
			TypeDef def;
			if (!_types.TryGetValue(type, out def))
				throw new NotSupportedException();
			return def.Read(s);
		}

		private string ToString(object value)
		{
			if (value == null) return string.Empty;

			string s;
			if (TryConvertToString(value, out s))
				return s;

			return Convert.ToString(value, CultureInfo.InvariantCulture);
		}

		private bool TryConvertToString(object value, out string result)
		{
			if (value == null)
			{
				result = null;
				return false;
			}

			TypeDef def;
			var type = value.GetType();
			if (_types.TryGetValue(type, out def))
			{
				result = def.Write(value);
				return true;
			}

			if (value is Enum)
			{
				result = value.ToString();
				return true;
			}

			result = null;
			return false;
		}

		private static Type FindIEnumerable(Type type)
		{
			if (type == null || type == typeof(string))
				return null;

			if (type.IsArray)
				return typeof(IEnumerable<>).MakeGenericType(type.GetElementType());

			if (type.IsGenericType)
			{
				foreach (var arg in type.GetGenericArguments())
				{
					var ienum = typeof(IEnumerable<>).MakeGenericType(arg);
					if (ienum.IsAssignableFrom(type))
					{
						return ienum;
					}
				}
			}

			var ifaces = type.GetInterfaces();
			if (ifaces.Length > 0)
			{
				foreach (var ienum in ifaces.Select(iface => FindIEnumerable(iface)).Where(ienum => ienum != null))
				{
					return ienum;
				}
			}

			if (type.BaseType != null && type.BaseType != typeof(object))
			{
				return FindIEnumerable(type.BaseType);
			}

			return null;
		}

		private Type GetElementType(XName name)
		{
			IElementDef def;
			return _elementDefsByName.TryGetValue(name, out def) ? def.Type : null;
		}

		private sealed class CollectionDef : IElementDef
		{
			private readonly XSerializer _serializer;
			private readonly Type _elementType;
			private readonly ItemDefCollection _elements;

			public CollectionDef(XSerializer serializer, XName name, Type type, Type elementType)
			{
				_serializer = serializer;
				_elementType = elementType;
				Name = name;
				Type = type;
				_elements = new ItemDefCollection(this);
			}

			public XName Name { get; private set; }
			public Type Type { get; private set; }
			public IDefCollection<IPropertyDef> Attributes { get { return DefCollection<IPropertyDef>.Empty; } }
			public IDefCollection<IPropertyDef> Elements { get { return _elements; } }

			private Type GetElementType(XName name)
			{
				if (_elementType.IsSealed) return _elementType;

				// TODO: determine whether there are elementDefs for subclasses
				if (_elementType.IsAbstract)
				{
					return _serializer.GetElementType(name);
				}
				
				return _elementType;
			}

			private sealed class ItemDefCollection : IDefCollection<IPropertyDef>
			{
				private readonly CollectionDef _collectionDef;

				public ItemDefCollection(CollectionDef collectionDef)
				{
					_collectionDef = collectionDef;
				}

				public IEnumerator<IPropertyDef> GetEnumerator()
				{
					yield break;
				}

				IEnumerator IEnumerable.GetEnumerator()
				{
					return GetEnumerator();
				}

				public IPropertyDef this[XName name]
				{
					get { return new ItemDef(_collectionDef, name); }
				}
			}

			private sealed class ItemDef : IPropertyDef
			{
				private readonly CollectionDef _collectionDef;

				public ItemDef(CollectionDef collectionDef, XName name)
				{
					_collectionDef = collectionDef;
					Name = name;
				}

				public XName Name { get; private set; }
				public Type Type { get { return _collectionDef.GetElementType(Name); } }

				public bool IsReadOnly { get { return false; } }

				public object GetValue(object target)
				{
					throw new NotSupportedException();
				}

				public void SetValue(object target, object value)
				{
					var list = target as IList;
					if (list != null)
					{
						list.Add(value);
						return;
					}

					// TODO: optimize with expression tree or reflection emit
					target.GetType().GetMethod("Add").Invoke(target, new [] {value});
				}
			}
		}
	}
}
