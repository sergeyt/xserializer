using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace TsvBits.XmlSerialization
{
	/// <summary>
	/// Implements XML (de)serialization based on schema specified by <see cref="IElementDef"/> definitions.
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

			var def = _rootScope.ElemDef(obj.GetType());
			ReadElement(reader, def, obj);
		}

		/// <summary>
		/// Reads object with given reader.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="reader">The xml reader.</param>
		public T Read<T>(XmlReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			reader.MoveToFirstElement();
			
			var def = _rootScope.ElemDef(reader.CurrentXName());
			return (T)ReadElement(reader, def, null);
		}

		/// <summary>
		/// Serializes given object.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="writer">The xml writer.</param>
		/// <param name="obj">The object to serialize.</param>
		public void Write<T>(XmlWriter writer, T obj)
		{
			var def = _rootScope.ElemDef(obj.GetType());
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

		public string ToXmlFragment<T>(T obj)
		{
			return ToXmlString(obj, true);
		}

		private object ReadElement(XmlReader reader, IElementDef def, Func<object> create)
		{
			if (def.IsImmutable)
			{
				var props = ReadProperties(reader, null, def).ToDictionary(x => x.Key.Name.LocalName, x => x.Value);
				return def.Create(props);
			}

			var obj = create != null ? create() : Activator.CreateInstance(def.Type);
			ReadElement(reader, def, obj);
			return obj;
		}

		private void ReadElement(XmlReader reader, IElementDef def, object obj)
		{
			foreach (var p in ReadProperties(reader, obj, def))
			{
				var property = p.Key;
				if (!property.IsReadOnly)
					property.SetValue(obj, p.Value);
			}
		}

		private IEnumerable<KeyValuePair<IPropertyDef, object>> ReadProperties(XmlReader reader, object obj, IElementDef def)
		{
			if (!reader.IsStartElement(def.Name.LocalName, def.Name.NamespaceName))
				throw new XmlException(string.Format("Xml element not foud: {0}", def.Name));

			// read attributes
			if (def.Attributes.Any() && reader.MoveToFirstAttribute())
			{
				do
				{
					var name = reader.CurrentXName();
					var property = def.Attributes[name];
					if (property != null)
					{
						var value = _rootScope.Parse(property.Type, reader.Value);
						yield return new KeyValuePair<IPropertyDef, object>(property, value);
					}
				} while (reader.MoveToNextAttribute());

				reader.MoveToElement();
			}

			if (reader.IsEmptyElement)
			{
				reader.Read();
				yield break;
			}

			// read child elements
			int depth = reader.Depth;
			reader.Read(); // move to first child node

			while (reader.MoveToNextElement(depth))
			{
				var name = reader.CurrentXName();
				var property = def.Elements[name];
				if (property == null) // unknown type
				{
					// todo: trace warning
					reader.Skip();
					continue;
				}

				object value;
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

			reader.ReadEndElement();
		}

		private bool ReadValue(XmlReader reader, object obj, IElementDef def, IPropertyDef property, out object value)
		{
			var type = property.Type;

			if (type == typeof(object))
			{
				value = null;
				var xsiType = reader.GetAttribute("type", Xsi.Uri);

				if (reader.IsEmptyElement)
				{
					reader.Read();
					return true;
				}

				var s = reader.ReadString();
				if (string.IsNullOrEmpty(xsiType)) return true;

				xsiType = xsiType.Substring(xsiType.IndexOf(':') + 1);
				Type valueType;
				if (Xsi.Name2Type.TryGetValue(xsiType, out valueType))
				{
					value = _rootScope.Parse(valueType, s);
				}
				return true;
			}

			if (_rootScope.TryReadString(reader, type, out value))
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

		private static bool ReadEnumElement(XmlReader reader, Type type, out object value)
		{
			if (type.IsNullable())
			{
				type = type.GetGenericArguments()[0];
			}

			if (type.IsEnum)
			{
				var s = reader.ReadStringOrNull();
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

			foreach (var elem in def.Elements)
			{
				var value = elem.GetValue(obj);
				if (value == null) continue;
				WriteValue(writer, elem, elem.Name, value);
			}
			
			writer.WriteEndElement();
		}

		private void WriteValue(XmlWriter writer, IPropertyDef property, XName name, object value)
		{
			if (value == null) return;

			string s;
			if (_rootScope.TryConvertToString(value, out s))
			{
				if (string.IsNullOrEmpty(s)) return;
				if (property.Type == typeof(object))
				{
					writer.WriteStartElement(name.LocalName, name.NamespaceName);
					var xsiType = Xsi.TypeOf(value);
					writer.WriteAttributeString("type", Xsi.Uri, xsiType);
					writer.WriteString(s);
					writer.WriteEndElement();
				}
				else
				{
					writer.WriteElementString(name.LocalName, name.NamespaceName, s);
				}				
				return;
			}

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
				var itemDef = new CollectionItemDef(property.ElementName, Reflector.GetItemType(type));
				bool empty = true;
				foreach (var item in collection)
				{
					if (empty)
					{
						writer.WriteStartElement(name.LocalName, name.NamespaceName);
						empty = false;
					}
					if (item == null)
					{
						WriteNullElement(writer, property.ElementName);
						continue;
					}
					WriteValue(writer, itemDef, property.ElementName, item);
				}
				if (!empty) writer.WriteEndElement();
				return;
			}

			throw new InvalidOperationException(string.Format("Unknown element. Name: {0}. Type: {1}.", name, type));
		}

		private static void WriteNullElement(XmlWriter writer, XName name)
		{
			writer.WriteStartElement(name.LocalName, name.NamespaceName);
			writer.WriteAttributeString("nil", Xsi.Uri, "true");
			writer.WriteEndElement();
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

		private static class Xsi
		{
			public const string Uri = "http://www.w3.org/2001/XMLSchema-instance";

			public static readonly IDictionary<string, Type> Name2Type = new Dictionary<string, Type>
				{
					{"boolean", typeof(bool)},
					{"unsignedByte", typeof(byte)},
					{"byte", typeof(sbyte)},
					{"short", typeof(short)},
					{"xsd:unsignedShort", typeof(ushort)},
					{"int", typeof(int)},
					{"unsignedInt", typeof(uint)},
					{"long", typeof(long)},
					{"unsignedLong", typeof(ulong)},
					{"float", typeof(float)},
					{"double", typeof(double)},
					{"decimal", typeof(decimal)},
					{"dateTime", typeof(DateTime)},
					{"string", typeof(string)}
				};

			public static string TypeOf(object value)
			{
				if (value == null) return null;

				switch (Type.GetTypeCode(value.GetType()))
				{
					case TypeCode.Empty:
					case TypeCode.DBNull:
						return null;
					case TypeCode.Object:
						throw new NotSupportedException(string.Format("Unsupported type: {0}", value.GetType()));
					case TypeCode.Boolean:
						return "xsd:boolean";
					case TypeCode.Char:
						return "xsd:unsignedByte";
					case TypeCode.SByte:
						return "xsd:byte";
					case TypeCode.Byte:
						return "xsd:unsignedByte";
					case TypeCode.Int16:
						return "xsd:short";
					case TypeCode.UInt16:
						return "xsd:unsignedShort";
					case TypeCode.Int32:
						return "xsd:int";
					case TypeCode.UInt32:
						return "xsd:unsignedInt";
					case TypeCode.Int64:
						return "xsd:long";
					case TypeCode.UInt64:
						return "xsd:unsignedLong";
					case TypeCode.Single:
						return "xsd:float";
					case TypeCode.Double:
						return "xsd:double";
					case TypeCode.Decimal:
						return "xsd:decimal";
					case TypeCode.DateTime:
						return "xsd:dateTime";
					case TypeCode.String:
						return "xsd:string";
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		private sealed class CollectionItemDef : IPropertyDef
		{
			public CollectionItemDef(XName name, Type type)
			{
				Name = name;
				Type = type;
				ElementName = name;
			}

			public string PropertyName { get { return "Item"; } }
			public Type Type { get; private set; }
			public XName Name { get; private set; }
			public XName ElementName { get; private set; }
			public bool IsReadOnly { get { return true; } }

			public object GetValue(object target)
			{
				throw new NotSupportedException();
			}

			public void SetValue(object target, object value)
			{
				throw new NotSupportedException();
			}
		}
	}
}
