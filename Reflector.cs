using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TsvBits.XmlSerialization
{
	/// <summary>
	/// Reflection helpers.
	/// </summary>
	internal static class Reflector
	{
		public static bool IsNullable(this Type type)
		{
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		public static T ResolveAttribute<T>(this ICustomAttributeProvider provider, bool inherit)
			where T : Attribute
		{
			var attrs = (T[])provider.GetCustomAttributes(typeof(T), inherit);
			return attrs.Length > 0 ? attrs[0] : null;
		}

		public static Type FindIEnumerable(Type type)
		{
			if (type == null || type == typeof(string))
				return null;

			if (type.IsArray)
				return typeof(IEnumerable<>).MakeGenericType(type.GetElementType());

			if (type.IsGenericType)
			{
				var ienum = type.GetGenericArguments()
				                .Select(x => typeof(IEnumerable<>).MakeGenericType(x))
				                .FirstOrDefault(x => x.IsAssignableFrom(type));
				if (ienum != null)
				{
					return ienum;
				}
			}

			var ifaces = type.GetInterfaces();
			if (ifaces.Length > 0)
			{
				var ienum = ifaces.Select(x => FindIEnumerable(x)).FirstOrDefault(x => x != null);
				if (ienum != null)
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

		public static Type GetItemType(Type type)
		{
			if (type.IsArray) return type.GetElementType();
			var ienum = FindIEnumerable(type);
			return ienum.GetGenericArguments()[0];
		}
	}
}
