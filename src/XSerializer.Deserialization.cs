using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	partial class XSerializer
	{
		private object ReadElement(IReader reader, IElementDef def, Func<object> create)
		{
			if (def.IsImmutable)
			{
				// TODO pass original property name rather than xml name
				var props = ReadProperties(reader, null, def).ToDictionary(x => x.Key.Name.LocalName, x => x.Value);
				return def.Create(props);
			}

			var obj = create != null ? create() : Activator.CreateInstance(def.Type);
			ReadElement(reader, def, obj);
			return obj;
		}

		private void ReadElement(IReader reader, IElementDef def, object obj)
		{
			foreach (var p in ReadProperties(reader, obj, def))
			{
				var property = p.Key;
				if (!property.IsReadOnly)
					property.SetValue(obj, p.Value);
			}
		}

		private IEnumerable<KeyValuePair<IPropertyDef, object>> ReadProperties(IReader reader, object obj, IElementDef def)
		{
			if (!reader.ReadStartElement(def.Name))
				throw new XmlException(string.Format("Xml element not foud: {0}", def.Name));

			// read attributes
			if (def.Attributes.Any())
			{
				foreach (var attr in reader.ReadAttributes())
				{
					var property = def.Attributes[attr.Key];
					if (property != null)
					{
						var value = _rootScope.SimpleTypes.Parse(property.Type, attr.Value);
						yield return new KeyValuePair<IPropertyDef, object>(property, value);
					}
				}
			}

			bool json = reader.Format == Format.Json;

			// read child elements
			foreach (var name in reader.ReadChildElements())
			{
				var property = def.Elements[name];
				object value;

				if (json && property == null)
				{
					property = def.Attributes[XNamespace.None + name.LocalName];
					if (property != null)
					{
						value = _rootScope.SimpleTypes.Parse(property.Type, reader.ReadString());
						yield return new KeyValuePair<IPropertyDef, object>(property, value);
						continue;
					}
				}

				if (property == null) // unknown type
				{
					// todo: trace warning
					reader.Skip();
					continue;
				}

				if (ReadValue(reader, obj, def, property, out value))
				{
					yield return new KeyValuePair<IPropertyDef, object>(property, value);
				}
				else
				{
					// todo: trace warning
					reader.Skip();
				}
			}
		}

		private bool ReadValue(IReader reader, object obj, IElementDef def, IPropertyDef property, out object value)
		{
			var type = property.Type;

			if (type == typeof(object))
			{
				value = reader.ReadObject();
				return true;
			}

			if (_rootScope.SimpleTypes.TryRead(() => reader.ReadString(), type, out value))
				return true;

			var elementDef = _rootScope.ElemDef(type);
			if (elementDef != null)
			{
				value = ReadElement(reader, elementDef, () => CreateElement(property, obj));
				return true;
			}

			if (ReadEnumElement(reader, type, out value))
				return true;

			var ienum = Reflector.FindIEnumerable(type);
			if (ienum != null)
			{
				var elementType = ienum.GetGenericArguments()[0];
				elementDef = new CollectionDef(this, property.Name, type, elementType);
				value = def.IsImmutable ? CreateList(elementType) : CreateElement(property, obj);
				ReadElement(reader, elementDef, value);
				return true;
			}

			value = null;
			return false;
		}

		private static bool ReadEnumElement(IReader reader, Type type, out object value)
		{
			if (type.IsNullable())
			{
				type = type.GetGenericArguments()[0];
			}

			if (type.IsEnum)
			{
				var s = reader.ReadString();
				value = Enum.Parse(type, s);
				return true;
			}

			value = null;
			return false;
		}

		private static object CreateList(Type elementType)
		{
			var listType = typeof(List<>).MakeGenericType(elementType);
			return Activator.CreateInstance(listType);
		}

		private static object CreateElement(IPropertyDef def, object target)
		{
			if (def == null) throw new NotSupportedException();
			var element = target != null ? def.GetValue(target) : null;
			return element ?? Activator.CreateInstance(def.Type);
		}
	}
}
