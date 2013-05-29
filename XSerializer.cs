using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace XmlSerialization
{
	public interface INodeDef
	{
		XName Name { get; }
		Type Type { get; }
	}

	public interface IPropertyDef : INodeDef
	{
		bool IsReadOnly { get; }

		object GetValue(object target);

		void SetValue(object target, object value);
	}

	public interface IElementDef : INodeDef
	{
		IEnumerable<IPropertyDef> Attributes { get; }
		IPropertyDef GetAttribute(XName name);

		// TODO: reuse IAttributeDef
		IEnumerable<INodeDef> Elements { get; }
		
		Type GetElementType(XName name);

		object GetValue(XName name, object target);
		void SetValue(XName name, object target, object value);

		object CreateElement(XName name, object target);
	}

	public sealed class ElementDef<T> : IElementDef
	{
		private readonly IDictionary<XName, IPropertyDef> _attributeDefs = new Dictionary<XName, IPropertyDef>();
		private readonly IDictionary<XName, IPropertyDef> _elementDefs = new Dictionary<XName, IPropertyDef>();

		public ElementDef(XName name)
		{
			if (name == null) throw new ArgumentNullException("name");

			Name = name;
		}

		public ElementDef<T> Attr<TValue>(XName name, Func<T, TValue> getter, Action<T, TValue> setter)
		{
			var attr = new PropertyDef<TValue>(name, getter, setter);
			_attributeDefs.Add(name, attr);
			return this;
		}

		public ElementDef<T> Attr<TValue>(Expression<Func<T, TValue>> property)
		{
			var name = GetPropertyName(property);
			var getter = property.Compile();
			var setter = ResolveSetter(property);
			// TODO: support readonly attributes
			if (setter == null) throw new NotSupportedException();
			return Attr(XNamespace.None + name, getter, setter);
		}

		public ElementDef<T> Elem<TValue>(XName name, Func<T, TValue> getter, Action<T, TValue> setter)
		{
			var elem = new PropertyDef<TValue>(name, getter, setter);
			_elementDefs.Add(name, elem);
			return this;
		}

		public ElementDef<T> Elem<TValue>(XName name, Func<T, TValue> getter)
		{
			return Elem(name, getter, null);
		}

		public ElementDef<T> Elem<TValue>(Expression<Func<T, TValue>> property)
		{
			var name = GetPropertyName(property);
			var getter = property.Compile();
			return Elem(Name.Namespace + name, getter, ResolveSetter(property));
		}

		public ElementDef<TElement> Sub<TElement>(XName name)
		{
			var elem = new ElementDef<TElement>(name);
			foreach (var p in _attributeDefs)
			{
				elem._attributeDefs.Add(p.Key, p.Value);
			}
			foreach (var p in _elementDefs)
			{
				elem._elementDefs.Add(p.Key, p.Value);
			}
			return elem;
		}

		public XName Name { get; private set; }

		public Type Type
		{
			get { return typeof(T); }
		}

		public IEnumerable<IPropertyDef> Attributes
		{
			get { return _attributeDefs.Values; }
		}

		public IPropertyDef GetAttribute(XName name)
		{
			IPropertyDef def;
			return _attributeDefs.TryGetValue(name, out def) ? def : null;
		}
		
		public IEnumerable<INodeDef> Elements
		{
			get { return _elementDefs.Values.Cast<INodeDef>(); }
		}

		public Type GetElementType(XName name)
		{
			var def = GetPropertyDef(name);
			return def != null ? def.Type : null;
		}

		public object GetValue(XName name, object target)
		{
			var def = GetPropertyDef(name);
			if (def == null) throw new NotSupportedException();
			return def.GetValue(target);
		}

		public void SetValue(XName name, object target, object value)
		{
			var def = GetPropertyDef(name);
			if (def == null) throw new NotSupportedException();
			if (def.IsReadOnly) throw new NotSupportedException();
			def.SetValue(target, value);
		}

		public object CreateElement(XName name, object target)
		{
			var def = GetPropertyDef(name);
			if (def == null) throw new NotSupportedException();
			if (def.IsReadOnly)
			{
				return def.GetValue(target);
			}
			var value = Activator.CreateInstance(def.Type);
			def.SetValue(target, value);
			return value;
		}

		public override string ToString()
		{
			return Name.ToString();
		}

		private IPropertyDef GetPropertyDef(XName name)
		{
			IPropertyDef def;
			return _elementDefs.TryGetValue(name, out def) ? def : null;
		}

		private static string GetPropertyName<TValue>(Expression<Func<T, TValue>> expression)
		{
			var me = (MemberExpression)expression.Body;
			return me.Member.Name;
		}

		private static Action<T, TValue> ResolveSetter<TValue>(Expression<Func<T, TValue>> expression)
		{
			var me = (MemberExpression)expression.Body;
			var pi = me.Member as PropertyInfo;
			if (pi != null)
			{
				// TODO: handle collections

				var setMethod = pi.GetSetMethod();
				if (setMethod == null) return null;
				
				var target = Expression.Parameter(typeof(T), "target");
				var value = Expression.Parameter(typeof(TValue), "value");
				
				var setter = Expression.Call(target, setMethod, value);
				return Expression.Lambda<Action<T, TValue>>(setter, target, value).Compile();
			}

			var fi = me.Member as FieldInfo;
			if (fi != null)
			{
				// TODO: optimize with reflection emit
				return (target, value) => fi.SetValue(target, value);
			}

			throw new NotSupportedException();
		}

		private sealed class PropertyDef<TValue> : IPropertyDef
		{
			private readonly Func<T, TValue> _getter;
			private readonly Action<T, TValue> _setter;

			public PropertyDef(XName name, Func<T, TValue> getter, Action<T, TValue> setter)
			{
				if (name == null) throw new ArgumentNullException("name");
				if (getter == null) throw new ArgumentNullException("getter");

				_getter = getter;
				_setter = setter;

				Name = name;
			}

			public XName Name { get; private set; }

			public Type Type
			{
				get { return typeof(TValue); }
			}

			public bool IsReadOnly
			{
				get { return _setter == null; }
			}

			public object GetValue(object target)
			{
				return _getter((T)target);
			}

			public void SetValue(object target, object value)
			{
				_setter((T)target, (TValue)value);
			}

			public override string ToString()
			{
				return Name.ToString();
			}
		}
	}

	public static class ElementDef
	{
		public static ElementDef<T> New<T>(XName name)
		{
			return new ElementDef<T>(name);
		}
	}

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
		
		public XSerializer()
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
		public T Parse<T>(string xml) where T : new()
		{
			var obj = new T();
			ReadXmlString(xml, obj);
			return obj;
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
					var attr = def.GetAttribute(name);
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
				var type = def.GetElementType(name);
				if (type == null) // unknown type
				{
					// todo: trace warning
					reader.Skip();
					continue;
				}

				if (ReadValue(reader, def, obj, type, name))
					continue;

				// todo: trace warning
				reader.Skip();
			}

			reader.ReadEndElement();
		}

		private bool ReadValue(XmlReader reader, IElementDef def, object obj, Type type, XName name)
		{
			TypeDef simpleType;
			if (_types.TryGetValue(type, out simpleType))
			{
				var s = reader.ReadString();
				var value = simpleType.Read(s);
				def.SetValue(name, obj, value);
				return true;
			}

			IElementDef elementDef;
			if (_elementDefs.TryGetValue(type, out elementDef))
			{
				var element = def.CreateElement(name, obj);
				ReadElement(reader, element, elementDef);
				return true;
			}

			var ienum = FindIEnumerable(type);
			if (ienum != null)
			{
				// TODO: support non-IList collections
				var collection = def.CreateElement(name, obj) as IList;
				if (collection == null) throw new NotSupportedException();
				var elementType = ienum.GetGenericArguments()[0];
				elementDef = new CollectionDef(this, name, type, elementType);
				ReadElement(reader, collection, elementDef);
				return true;
			}

			return false;
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
					var value = def.GetValue(elem.Name, obj);
					if (value == null) continue;
					WriteValue(writer, elem, value);
				}
			}
			
			writer.WriteEndElement();
		}

		private void WriteValue(XmlWriter writer, INodeDef def, object value)
		{
			var name = def != null ? def.Name : null;

			TypeDef simpleType;
			var type = value.GetType();
			if (_types.TryGetValue(type, out simpleType))
			{
				var s = simpleType.Write(value);
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
			if (_elementDefs.TryGetValue(type, out elementDef))
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

			TypeDef def;
			var type = value.GetType();
			if (_types.TryGetValue(type, out def))
			{
				return def.Write(value);
			}

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
			IElementDef def;
			return _elementDefsByName.TryGetValue(name, out def) ? def.Type : null;
		}

		private sealed class CollectionDef : IElementDef
		{
			private readonly XSerializer _serializer;
			private readonly Type _elementType;

			public CollectionDef(XSerializer serializer, XName name, Type type, Type elementType)
			{
				_serializer = serializer;
				_elementType = elementType;
				Name = name;
				Type = type;
			}

			public XName Name { get; private set; }
			public Type Type { get; private set; }
			public IEnumerable<IPropertyDef> Attributes { get { return Enumerable.Empty<IPropertyDef>(); } }
			public IPropertyDef GetAttribute(XName name) { return null; }
			public IEnumerable<INodeDef> Elements { get { return Enumerable.Empty<INodeDef>(); } }

			public Type GetElementType(XName name)
			{
				if (_elementType.IsSealed) return _elementType;

				// TODO: determine whether there are elementDefs for subclasses
				if (_elementType.IsAbstract)
				{
					return _serializer.GetElementType(name);
				}
				
				return _elementType;
			}

			public object GetValue(XName name, object target)
			{
				throw new NotSupportedException();
			}

			public void SetValue(XName name, object target, object value)
			{
				// TODO: support non-IList collections
				((IList)target).Add(value);
			}

			public object CreateElement(XName name, object target)
			{
				var type = GetElementType(name);
				var item = Activator.CreateInstance(type);
				SetValue(name, target, item);
				return item;
			}
		}
	}

	internal static class XmlExtensions
	{
		public static XName CurrentXName(this XmlReader reader)
		{
			return XNamespace.Get(reader.NamespaceURI).GetName(reader.LocalName);
		}
	}
}
