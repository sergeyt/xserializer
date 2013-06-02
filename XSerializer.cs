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

			var def = _rootScope.ElemDef(reader.CurrentXName());
			if (def.IsImmutable)
			{
				return (T)ReadImmutable(reader, def);
			}

			var obj = Activator.CreateInstance(typeof(T));
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

		private void ReadElement(XmlReader reader, object obj, IElementDef def)
		{
			foreach (var p in ReadProperties(reader, obj, def))
			{
				var property = p.Key;
				if (!property.IsReadOnly)
					property.SetValue(obj, p.Value);
			}
		}

		private object ReadImmutable(XmlReader reader, IElementDef def)
		{
			var props = ReadProperties(reader, null, def).ToDictionary(x => x.Key.Name.LocalName, x => x.Value);
			return def.Create(props);
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

			if (reader.IsEmptyElement) yield break;

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

			if (_rootScope.TryReadString(reader, type, out value))
				return true;

			var elementDef = _rootScope.ElemDef(type);
			if (elementDef != null)
			{
				if (elementDef.IsImmutable)
				{
					value = ReadImmutable(reader, elementDef);
				}
				else
				{
					value = CreateElement(property, obj);
					ReadElement(reader, value, elementDef);
				}
				return true;
			}

			if (type.IsEnum)
			{
				var s = reader.ReadString();
				value = System.Enum.Parse(type, s);
				return true;
			}

			var ienum = FindIEnumerable(type);
			if (ienum != null)
			{
				var elementType = ienum.GetGenericArguments()[0];
				elementDef = new CollectionDef(this, property.Name, type, elementType);
				value = def.IsImmutable ? CreateCollection(elementType) : CreateElement(property, obj);
				ReadElement(reader, value, elementDef);
				return true;
			}

			value = null;
			return false;
		}

		private static object CreateCollection(Type elementType)
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
			if (_rootScope.TryConvertToString(value, out s))
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

			var elementDef = _rootScope.ElemDef(value.GetType());
			if (elementDef != null)
			{
				WriteElement(writer, value, elementDef, name);
				return;
			}

			throw new InvalidOperationException(string.Format("Unknown element. Name: {0}. Type: {1}.", def.Name, def.Type));
		}

		private string ToString(object value)
		{
			if (value == null) return string.Empty;

			string s;
			if (_rootScope.TryConvertToString(value, out s))
				return s;

			return Convert.ToString(value, CultureInfo.InvariantCulture);
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
			var def = _rootScope.ElemDef(name);
			return def != null ? def.Type : null;
		}
	}
}
