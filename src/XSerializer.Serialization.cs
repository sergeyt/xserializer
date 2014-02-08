using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	partial class XSerializer
	{
		private void WriteElement(IWriter writer, object obj, IElementDef def, XName name)
		{
			if (name == null) name = def.Name;

			var attributes = from attr in def.Attributes
							 let value = attr.GetValue(obj)
							 where value != null && !attr.IsDefaultValue(value)
							 let stringValue = ToString(value)
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
				WriteValue(writer, elem.Definition, elem.Name, elem.Value);
			}

			writer.WriteEndElement();
		}

		private void WriteValue(IWriter writer, IPropertyDef property, XName name, object value)
		{
			if (value == null) return;

			if (value is Enum && WriteStringElement(writer, property, name, value))
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

			if (WriteStringElement(writer, property, name, value))
				return;

			var type = value.GetType();
			var elementDef = _rootScope.ElemDef(type);
			if (elementDef != null)
			{
				WriteElement(writer, value, elementDef, elementDef.Name);
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
					WriteValue(writer, itemDef, property.ItemName, item);
				}
				if (!empty) writer.WriteEndCollection();
				return;
			}

			throw new InvalidOperationException(string.Format("Unknown element. Name: {0}. Type: {1}.", name, type));
		}

		private bool WriteStringElement(IWriter writer, IPropertyDef property, XName name, object value)
		{
			string s;
			if (!_rootScope.TryConvertToString(value, out s))
				return false;

			if (string.IsNullOrEmpty(s))
				return true;

			if (property.Type == typeof(object))
				writer.WriteObjectElement(name, value);
			else
				writer.WritePrimitiveElement(name, s);

			return true;
		}

		private string ToString(object value)
		{
			if (value == null) return string.Empty;

			string s;
			if (_rootScope.TryConvertToString(value, out s))
				return s;

			return Convert.ToString(value, CultureInfo.InvariantCulture);
		}

		private Type GetElementType(XName name)
		{
			var def = _rootScope.ElemDef(name);
			return def != null ? def.Type : null;
		}
	}
}
