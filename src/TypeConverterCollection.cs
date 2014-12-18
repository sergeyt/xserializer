using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using TsvBits.Serialization.Utils;

namespace TsvBits.Serialization
{
	internal sealed class TypeConverterCollection
	{
		private static readonly IDictionary<Type, Converter> CoreTypes = new Dictionary<Type, Converter>();
		private readonly IDictionary<Type, Converter> _types = new Dictionary<Type, Converter>();

		static TypeConverterCollection()
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

		private static void CoreType<T>(Func<string, T> read, Func<T, string> write)
		{
			CoreTypes.Add(typeof(T), new Converter(s => read(s), v => write((T)v)));
		}

		public void Add<T>(Func<string, T> read, Func<T, string> write)
		{
			_types.Add(typeof(T), new Converter(s => read(s), v => write((T)v)));
		}

		public void Enum<T>(T defval, bool ignoreCase)
		{
			var type = typeof(T);
			_types.Add(type, new Converter(
				s => System.Enum.Parse(type, s, ignoreCase),
				v => Equals(v, defval) ? "" : v.ToString()
				));
		}

		public void Enum<T>(T defval)
		{
			Enum(defval, true);
		}

		internal bool TryRead(Func<string> reader, Type type, out object value)
		{
			var parser = GetParser(type, true);
			if (parser != null)
			{
				var s = reader();
				value = parser(s);
				return true;
			}
			value = null;
			return false;
		}

		internal bool TryConvert(object value, out string result, bool withEnumSupport)
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

			if (value is Enum && withEnumSupport)
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

		internal Converter FindType(Type type)
		{
			Converter def;
			return _types.TryGetValue(type, out def) ? def : CoreTypes.TryGetValue(type, out def) ? def : null;
		}

		internal sealed class Converter
		{
			private readonly Func<string, object> _read;
			private readonly Func<object, string> _write;

			public Converter(Func<string, object> read, Func<object, string> write)
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
