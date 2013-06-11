using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace TsvBits.XmlSerialization
{
	public static class Extensions
	{
		public static XName CurrentXName(this XmlReader reader)
		{
			return XNamespace.Get(reader.NamespaceURI).GetName(reader.LocalName);
		}

		internal static string ReadStringOrNull(this XmlReader reader)
		{
			if (reader.IsEmptyElement)
			{
				reader.Read();
				return null;
			}
			return reader.ReadString();
		}

		internal static void MoveToFirstElement(this XmlReader reader)
		{
			while (reader.NodeType != XmlNodeType.Element && reader.Read()){}
		}

		internal static bool MoveToNextElement(this XmlReader reader, int depth)
		{
			do
			{
				if (reader.NodeType == XmlNodeType.Element)
					return true;
				if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
					return false;
			} while (reader.Read());
			return false;
		}

		public static T Get<T>(this IDictionary<string, object> dictionary, string name)
		{
			object value;
			return dictionary.TryGetValue(name, out value) ? (T)value : default(T);
		}

		public static TValue Get<T, TValue>(this IDictionary<string, object> dictionary, Expression<Func<T, TValue>> propertyGetter)
		{
			return dictionary.Get<TValue>(propertyGetter.GetPropertyName());
		}

		public static MemberInfo ResolveMember<T, TValue>(this Expression<Func<T, TValue>> propertyGetter)
		{
			var me = (MemberExpression)propertyGetter.Body;
			return me.Member;
		}

		public static string GetPropertyName<T,TValue>(this Expression<Func<T, TValue>> propertyGetter)
		{
			return propertyGetter.ResolveMember().Name;
		}

		public static TR IfNotNull<T, TR>(this T value, Func<T, TR> selector)
		{
			return Equals(value, default(T)) ? default(TR) : selector(value);
		}

		public static object UnboxNullable(this object value)
		{
			if (value == null) return null;

			var type = value.GetType();
			if (!type.IsNullable()) return value;

			var valueType = type.GetGenericArguments()[0];
			switch (Type.GetTypeCode(valueType))
			{
				case TypeCode.Boolean:
					return (bool)value;
				case TypeCode.Char:
					return (char)value;
				case TypeCode.SByte:
					return (sbyte)value;
				case TypeCode.Byte:
					return (byte)value;
				case TypeCode.Int16:
					return (Int16)value;
				case TypeCode.UInt16:
					return (UInt16)value;
				case TypeCode.Int32:
					return (int)value;
				case TypeCode.UInt32:
					return (uint)value;
				case TypeCode.Int64:
					return (Int64)value;
				case TypeCode.UInt64:
					return (UInt64)value;
				case TypeCode.Single:
					return (Single)value;
				case TypeCode.Double:
					return (Double)value;
				case TypeCode.Decimal:
					return (Decimal)value;
				case TypeCode.DateTime:
					return (DateTime)value;
				case TypeCode.String:
					return value;
				default:
					return MethodGenerator.UnboxNullable(type)(value);
			}
		}
	}
}