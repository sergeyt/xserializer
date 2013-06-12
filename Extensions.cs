using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace TsvBits.Serialization
{
	public static class Extensions
	{
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

		public static bool IsPrimitive(this object value)
		{
			if (value == null) return true;

			switch (Type.GetTypeCode(value.GetType()))
			{
				case TypeCode.Empty:
				case TypeCode.DBNull:
				case TypeCode.Boolean:
				case TypeCode.Char:
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
				case TypeCode.DateTime:
				case TypeCode.String:
					return true;
				default:
					return false;
			}
		}
	}
}