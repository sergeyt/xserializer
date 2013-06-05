using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace TsvBits.XmlSerialization
{
	public sealed class Scope
	{
		private readonly XNamespace _ns;
		private static readonly IDictionary<Type, TypeDef> PrimitiveTypes = new Dictionary<Type, TypeDef>();
		private readonly IDictionary<Type, TypeDef> _types = new Dictionary<Type, TypeDef>();
		private readonly IDictionary<Type, IElementDef> _elementDefs = new Dictionary<Type, IElementDef>();
		private readonly IDictionary<XName, IElementDef> _elementDefsByName = new Dictionary<XName, IElementDef>();

		static Scope()
		{
			PrimitiveType(x => x, x => x);
			PrimitiveType(x => Convert.ToBoolean(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			PrimitiveType(x => Convert.ToSByte(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			PrimitiveType(x => Convert.ToByte(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			PrimitiveType(x => Convert.ToInt16(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			PrimitiveType(x => Convert.ToUInt16(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			PrimitiveType(x => Convert.ToInt32(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			PrimitiveType(x => Convert.ToUInt32(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			PrimitiveType(x => Convert.ToInt64(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			PrimitiveType(x => Convert.ToUInt64(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			PrimitiveType(x => Convert.ToSingle(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			PrimitiveType(x => Convert.ToDouble(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			PrimitiveType(x => Convert.ToDecimal(x, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			PrimitiveType(x => Convert.ToDateTime(x, CultureInfo.InvariantCulture),
			     x => XmlConvert.ToString(x, XmlDateTimeSerializationMode.Utc));
		}

		private Scope(XNamespace ns)
		{
			_ns = ns ?? XNamespace.None;
		}

		public static Scope New(XNamespace ns)
		{
			return new Scope(ns);
		}

		public static Scope New(string ns)
		{
			return new Scope(string.IsNullOrEmpty(ns) ? XNamespace.None : XNamespace.Get(ns));
		}

		private static void PrimitiveType<T>(Func<string, T> read, Func<T, string> write)
		{
			PrimitiveTypes.Add(typeof(T), new TypeDef(s => read(s), v => write((T)v)));
		}

		/// <summary>
		/// Registers simple type serializable to string.
		/// </summary>
		/// <typeparam name="T">The type to register.</typeparam>
		/// <param name="read">The parser.</param>
		/// <param name="write">The writer.</param>
		public Scope Type<T>(Func<string, T> read, Func<T, string> write)
		{
			_types.Add(typeof(T), new TypeDef(s => read(s), v => write((T)v)));
			return this;
		}

		public Scope Enum<T>(T defval, bool ignoreCase)
		{
			var type = typeof(T);
			_types.Add(type, new TypeDef(s => System.Enum.Parse(type, s, ignoreCase), v => Equals(v, defval) ? "" : v.ToString()));
			return this;
		}

		public Scope Enum<T>(T defval)
		{
			return Enum(defval, true);
		}

		private void Register(IElementDef def)
		{
			_elementDefs.Add(def.Type, def);
			_elementDefsByName.Add(def.Name, def);
		}

		public ElementDef<T> Elem<T>(XName name)
		{
			var def = new ElementDef<T>(this, name);
			Register(def);
			return def;
		}

		public ElementDef<T> Elem<T>()
		{
			var type = typeof(T);
			var name = type.Name;
			if (type.IsGenericType)
			{
				var i = name.LastIndexOf('`');
				if (i >= 0) name = name.Substring(0, i);
			}
			return Elem<T>(_ns + name);
		}

		internal IElementDef ElemDef(Type type)
		{
			IElementDef def;
			return _elementDefs.TryGetValue(type, out def) ? def : null;
		}

		internal IElementDef ElemDef(XName name)
		{
			return _elementDefsByName[name];
		}

		internal object Parse(Type type, string s)
		{
			var def = FindType(type);
			if (def != null) return def.Read(s);

			if (type.IsEnum)
			{
				return System.Enum.Parse(type, s, true);
			}

			throw new NotSupportedException(string.Format("Unknown type: {0}", type));
		}

		internal bool TryReadString(XmlReader reader, Type type, out object value)
		{
			var def = FindType(type);
			if (def != null)
			{
				if (reader.IsEmptyElement)
				{
					value = def.Read(null);
					reader.Read();
				}
				else
				{
					var s = reader.ReadString();
					value = def.Read(s);
				}
				return true;
			}
			value = null;
			return false;
		}

		internal bool TryConvertToString(object value, out string result)
		{
			if (value == null)
			{
				result = "";
				return false;
			}

			var def = FindType(value.GetType());
			if (def != null)
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

		private TypeDef FindType(Type type)
		{
			TypeDef def;
			return _types.TryGetValue(type, out def) ? def : PrimitiveTypes.TryGetValue(type, out def) ? def : null;
		}

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
	}
}
