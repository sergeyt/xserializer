using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
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

	public interface IAttributeDef : INodeDef
	{
		bool IsReadOnly { get; }

		object GetValue(object target);

		void SetValue(object target, object value);
	}

	public interface IElementDef : INodeDef
	{
		IEnumerable<IAttributeDef> Attributes { get; }
		IAttributeDef GetAttribute(XName name);

		IEnumerable<INodeDef> Elements { get; }
		
		Type GetElementType(XName name);

		object GetValue(XName name, object target);
		void SetValue(XName name, object target, object value);

		object CreateElement(XName name, object target);
	}

	public sealed class ElementDef<T> : IElementDef
	{
		private readonly IDictionary<XName, IAttributeDef> _attributeDefs = new Dictionary<XName, IAttributeDef>();
		private readonly IDictionary<XName, IAttributeDef> _elementDefs = new Dictionary<XName, IAttributeDef>();

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

		public ElementDef<T> Attr<TValue>(Expression<Func<T, TValue>> property, Action<T, TValue> setter)
		{
			var name = GetPropertyName(property);
			var getter = property.Compile();
			return Attr(Name.Namespace + name, getter, setter);
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

		public ElementDef<T> Elem<TValue>(Expression<Func<T, TValue>> property, Action<T, TValue> setter)
		{
			var name = GetPropertyName(property);
			var getter = property.Compile();
			return Elem(Name.Namespace + name, getter, setter);
		}

		public ElementDef<T> Elem<TValue>(Expression<Func<T, TValue>> property)
		{
			return Elem(property, null);
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

		public IEnumerable<IAttributeDef> Attributes
		{
			get { return _attributeDefs.Values; }
		}

		public IAttributeDef GetAttribute(XName name)
		{
			IAttributeDef def;
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
			if (def.IsReadOnly)
			{
				return def.GetValue(target);
			}
			var value = Activator.CreateInstance(def.Type);
			def.SetValue(target, value);
			return value;
		}

		private IAttributeDef GetPropertyDef(XName name)
		{
			IAttributeDef def;
			return _elementDefs.TryGetValue(name, out def) ? def : null;
		}

		private static string GetPropertyName<TValue>(Expression<Func<T, TValue>> expression)
		{
			var memberAssignment = (MemberExpression)expression.Body;
			var memberName = memberAssignment.Member.Name;
			return memberName;
		}

		private sealed class PropertyDef<TValue> : IAttributeDef
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
		}
	}

	public static class ElementDef
	{
		public static ElementDef<T> New<T>(XName name)
		{
			return new ElementDef<T>(name);
		}
	}

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
			Type(x => Convert.ToDateTime(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x, XmlDateTimeSerializationMode.Utc));
		}

		public XSerializer Type<T>(Func<string, T> read, Func<T, string> write)
		{
			_types.Add(typeof(T), new TypeDef(s => read(s), o => write((T)o)));
			return this;
		}

		public XSerializer Elem(IElementDef def)
		{
			_elementDefs.Add(def.Type, def);
			return this;
		}

		private IElementDef ResolveElementDef(Type type)
		{
			IElementDef def;
			if (!_elementDefs.TryGetValue(type, out def))
				throw new XmlException(string.Format("Unknown type: {0}", type));
			return def;
		}

		public void Read<T>(XmlReader reader, T obj)
		{
			var def = ResolveElementDef(obj.GetType());
			ReadElement(reader, obj, def);
		}

		public void Write<T>(XmlWriter writer, T obj)
		{
			var def = ResolveElementDef(obj.GetType());
			WriteElement(writer, obj, def, def.Name);
		}

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
			reader.ReadStartElement(def.Name.LocalName, def.Name.NamespaceName);

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
			}

			int depth = reader.Depth;

			// read elements
			while (reader.Read())
			{
				if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
					break;

				if (reader.NodeType != XmlNodeType.Element) continue;

				var name = reader.CurrentXName();
				var type = def.GetElementType(name);
				if (type == null) // unknown type
				{
					// todo: trace warning
					reader.Skip();
					continue;
				}

				TypeDef simpleType;
				if (_types.TryGetValue(type, out simpleType))
				{
					var s = reader.ReadElementString(reader.LocalName, reader.NamespaceURI);
					var value = simpleType.Read(s);
					def.SetValue(name, obj, value);
					continue;
				}

				IElementDef elementDef;
				if (_elementDefs.TryGetValue(type, out elementDef))
				{
					var element = def.CreateElement(name, obj);
					ReadElement(reader, element, elementDef);
				}
			}

			reader.ReadEndElement();
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
				writer.WriteAttributeString(attr.Name.LocalName, attr.Name.NamespaceName, s);
			}

			var collection = obj as IEnumerable;
			if (collection != null)
			{
				// TODO: support custom collections with own properties
				foreach (var item in collection)
				{
					WriteValue(writer, item, null);
				}
			}
			else
			{
				foreach (var elem in def.Elements)
				{
					var value = def.GetValue(elem.Name, obj);
					if (value == null) continue;
					WriteValue(writer, value, elem);
				}
			}
			
			writer.WriteEndElement();
		}

		private void WriteValue(XmlWriter writer, object value, INodeDef def)
		{
			var name = def != null ? def.Name : null;
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
					WriteValue(writer, item, null);
				}
				if (!empty) writer.WriteEndElement();
				return;
			}

			TypeDef simpleType;
			var type = value.GetType();
			if (_types.TryGetValue(type, out simpleType))
			{
				var s = simpleType.Write(value);
				writer.WriteElementString(name.LocalName, name.NamespaceName, s);
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
	}

	public static class XmlExtensions
	{
		public static XName CurrentXName(this XmlReader reader)
		{
			return XNamespace.Get(reader.NamespaceURI).GetName(reader.LocalName);
		}
	}
}
