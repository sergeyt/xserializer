using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TsvBits.Serialization.Utils
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

		public static Type FindIEnumerableT(Type type)
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
				var ienum = ifaces.Select(x => FindIEnumerableT(x)).FirstOrDefault(x => x != null);
				if (ienum != null)
				{
					return ienum;
				}
			}

			if (type.BaseType != null && type.BaseType != typeof(object))
			{
				return FindIEnumerableT(type.BaseType);
			}

			return null;
		}

		public static Type GetItemType(Type type)
		{
			if (type == null) return null;

			if (type.IsArray) return type.GetElementType();

			// IEnumerable<T>
			var i = FindIEnumerableT(type);
			if (i != null) return i.GetGenericArguments()[0];

			// support non-generic collections like old .NET 1 collections based on CollectionBase
			if (!typeof(IEnumerable).IsAssignableFrom(type))
				return null;

			var add = type.GetMethod("Add");
			if (add == null)
				return null;

			var parameters = add.GetParameters();
			if (parameters.Length != 1)
				return null;

			return parameters[0].ParameterType;
		}
	}
}
