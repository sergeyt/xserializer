using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Xml;
using System.Xml.Linq;

namespace TsvBits.XmlSerialization
{
	public static class XmlExtensions
	{
		public static XName CurrentXName(this XmlReader reader)
		{
			return XNamespace.Get(reader.NamespaceURI).GetName(reader.LocalName);
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

		public static string GetPropertyName<T,TValue>(this Expression<Func<T, TValue>> propertyGetter)
		{
			var me = (MemberExpression)propertyGetter.Body;
			return me.Member.Name;
		}

		
	}
}