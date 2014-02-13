using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using TsvBits.Serialization.Utils;
using TsvBits.Serialization.Xml;

namespace TsvBits.Serialization.Core
{
	internal static partial class Serializer
	{
		public static void WriteElement(IScope scope, IWriter writer, object obj)
		{
			var type = obj.GetType();

			var xmlWriter = writer as XmlWriterImpl;
			if (xmlWriter != null)
			{
				var surrogate = scope.GetSurrogate(type);
				if (surrogate != null)
				{
					surrogate.Write(xmlWriter.XmlWriter, obj);
					return;
				}
			}

			var def = scope.GetElementDef(type);
			if (def != null)
			{
				var subScope = def as IScope ?? scope;
				WriteElement(subScope, writer, obj, def, def.Name);
				return;
			}

			throw new NotSupportedException();
		}

		public static void WriteElement(IScope scope, IWriter writer, object obj, IElementDef def, XName name)
		{
			if (name == null) name = def.Name;

			var attributes = from attr in def.Attributes
							 let value = attr.GetValue(obj)
							 where value != null && !attr.IsDefaultValue(value)
							 let stringValue = ToString(scope, value)
							 where !string.IsNullOrEmpty(stringValue)
							 select new { attr.Name, Value = stringValue };

			var elements = from elem in def.Elements
						   let value = elem.GetValue(obj)
						   where value != null && !elem.IsDefaultValue(value)
						   select new { elem.Name, Value = value, Definition = elem };

			// TODO do not write non-root empty elements

			writer.WriteStartElement(name);

			foreach (var attr in attributes)
			{
				writer.WriteAttributeString(attr.Name, attr.Value);
			}

			foreach (var elem in elements)
			{
				WriteValue(scope, writer, elem.Definition, elem.Name, elem.Value);
			}

			writer.WriteEndElement();
		}

		private static void WriteValue(IScope scope, IWriter writer, IPropertyDef property, XName name, object value)
		{
			if (value == null) return;

			if (value is Enum && WriteStringElement(scope, writer, property, name, value))
				return;

			if (value.IsPrimitive())
			{
				if (property.Type == typeof(object))
				{
					writer.WriteObjectElement(name, value);
				}
				else
				{
					writer.WritePrimitiveElement(name, value);
				}
				return;
			}

			if (WriteStringElement(scope, writer, property, name, value))
				return;

			var type = value.GetType();
			var xmlWriter = writer as XmlWriterImpl;
			if (xmlWriter != null)
			{
				var surrogate = scope.GetSurrogate(type);
				if (surrogate != null)
				{
					surrogate.Write(xmlWriter.XmlWriter, value);
					return;
				}
			}

			var elementDef = scope.GetElementDef(type);
			if (elementDef != null)
			{
				var subScope = elementDef as IScope;
				WriteElement(subScope, writer, value, elementDef, elementDef.Name);
				return;
			}

			var collection = value as IEnumerable;
			if (collection != null)
			{
				var itemDef = new CollectionItemDef(property.ItemName, Reflector.GetItemType(type));
				var empty = true;
				foreach (var item in collection)
				{
					if (empty)
					{
						writer.WriteStartCollection(name);
						empty = false;
					}
					if (item == null)
					{
						writer.WriteNullItem(property.ItemName);
						continue;
					}
					WriteValue(scope, writer, itemDef, property.ItemName, item);
				}
				if (!empty) writer.WriteEndCollection();
				return;
			}

			throw new InvalidOperationException(string.Format("Unknown element. Name: {0}. Type: {1}.", name, type));
		}

		private static bool WriteStringElement(IScope scope, IWriter writer, IPropertyDef property, XName name, object value)
		{
			string s;
			if (!scope.TryConvert(value, out s))
				return false;

			if (string.IsNullOrEmpty(s))
				return true;

			if (property.Type == typeof(object))
				writer.WriteObjectElement(name, value);
			else
				writer.WritePrimitiveElement(name, s);

			return true;
		}

		private static string ToString(IScope scope, object value)
		{
			if (value == null) return string.Empty;

			string s;
			if (scope.TryConvert(value, out s))
				return s;

			return Convert.ToString(value, CultureInfo.InvariantCulture);
		}
	}
}
