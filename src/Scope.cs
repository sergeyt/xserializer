﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	public sealed class Scope
	{
		private static readonly IDictionary<Type, TypeDef> CoreTypes = new Dictionary<Type, TypeDef>();
		private readonly IDictionary<Type, TypeDef> _types = new Dictionary<Type, TypeDef>();
		private readonly IDictionary<Type, IElementDef> _elementDefs = new Dictionary<Type, IElementDef>();
		private readonly IDictionary<XName, IElementDef> _elementDefsByName = new Dictionary<XName, IElementDef>();

		static Scope()
		{
			CoreType(x => x, x => x);
			CoreType(s => Convert.ToBoolean(s, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			CoreType(s => Convert.ToSByte(s, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			CoreType(s => Convert.ToByte(s, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			CoreType(s => Convert.ToInt16(s, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			CoreType(s => Convert.ToUInt16(s, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			CoreType(s => Convert.ToInt32(s, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			CoreType(s => Convert.ToUInt32(s, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			CoreType(s => Convert.ToInt64(s, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			CoreType(s => Convert.ToUInt64(s, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			CoreType(s => Convert.ToSingle(s, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			CoreType(s => Convert.ToDouble(s, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			CoreType(s => Convert.ToDecimal(s, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x));
			CoreType(s => Convert.ToDateTime(s, CultureInfo.InvariantCulture), x => XmlConvert.ToString(x, XmlDateTimeSerializationMode.Utc));
			CoreType(s => Convert.FromBase64String(s), x => Convert.ToBase64String(x));
		}

		private Scope(XNamespace ns)
		{
			Namespace = ns ?? XNamespace.None;
		}

		public static Scope New(XNamespace ns)
		{
			return new Scope(ns);
		}

		public XNamespace Namespace { get; private set; }

		public static Scope New(string ns)
		{
			return new Scope(string.IsNullOrEmpty(ns) ? XNamespace.None : XNamespace.Get(ns));
		}

		private static void CoreType<T>(Func<string, T> read, Func<T, string> write)
		{
			CoreTypes.Add(typeof(T), new TypeDef(s => read(s), v => write((T)v)));
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
			return Elem<T>(Namespace + name);
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
			var parser = GetParser(type, true);
			if (parser != null)
			{
				return parser(s);
			}

			throw new NotSupportedException(string.Format("Unknown type: {0}", type));
		}

		internal bool TryReadString(Func<string> stringReader, Type type, out object value)
		{
			var parser = GetParser(type, false);
			if (parser != null)
			{
				var s = stringReader();
				value = parser(s);
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

			var type = value.GetType();
			if (type.IsNullable())
			{
				type = type.GetGenericArguments()[0];
				value = value.UnboxNullable();
			}

			var def = FindType(type);
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

		private Func<string, object> GetParser(Type type, bool withEnumSupport)
		{
			if (type.IsNullable())
			{
				type = type.GetGenericArguments()[0];
			}

			var def = FindType(type);
			if (def != null) return s => def.Read(s);

			if (withEnumSupport && type.IsEnum)
			{
				return s => System.Enum.Parse(type, s, true);
			}

			return null;
		}

		private TypeDef FindType(Type type)
		{
			TypeDef def;
			return _types.TryGetValue(type, out def) ? def : CoreTypes.TryGetValue(type, out def) ? def : null;
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