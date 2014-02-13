using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using TsvBits.Serialization.Utils;
using TsvBits.Serialization.Xml;

namespace TsvBits.Serialization.Core
{
	internal static partial class Deserializer
	{
		public static void ReadElement(IScope scope, IReader reader, object obj)
		{
			var type = obj.GetType();

			var xmlReader = reader as XmlReaderImpl;
			if (xmlReader != null)
			{
				var surrogate = scope.GetSurrogate(type);
				if (surrogate != null)
				{
					surrogate.Read(xmlReader.XmlReader, obj);
					return;
				}
			}

			var def = ResolveElementDef(scope, reader, type);
			if (def != null)
			{
				ReadElement(scope, reader, def, obj);
				return;
			}

			throw new NotSupportedException();
		}

		private static IElementDef ResolveElementDef(IScope scope, IReader reader, Type type)
		{
			if (reader.Format == Format.Json)
			{
				return scope.GetElementDef(type);
			}
			return scope.GetElementDef(reader.CurrentName) ?? scope.GetElementDef(type);
		}

		public static void ReadElement(IScope scope, IReader reader, IElementDef def, object obj)
		{
			foreach (var p in ReadProperties(scope, reader, obj, def))
			{
				var property = p.Key;
				if (!property.IsReadOnly)
					property.SetValue(obj, p.Value);
			}
		}

		public static object ReadElement(IScope scope, IReader reader, IElementDef def, Func<object> create)
		{
			if (def.IsImmutable)
			{
				// TODO pass original property name rather than xml name
				var props = ReadProperties(scope, reader, null, def).ToDictionary(x => x.Key.Name.LocalName, x => x.Value);
				return def.Create(props);
			}

			var obj = create != null ? create() : Activator.CreateInstance(def.Type);
			ReadElement(scope, reader, def, obj);
			return obj;
		}

		private static void ReadStartElement(IScope scope, IReader reader, IElementDef def)
		{
			if (reader.ReadStartElement(def.Name))
				return;

			var namespaces = scope.GetNamespaces(def.Type);
			if (namespaces.Any(ns => reader.ReadStartElement(ns + def.Name.LocalName)))
			{
				return;
			}

			throw new XmlException(string.Format("Xml element not foud: {0}", def.Name));
		}

		private static IEnumerable<KeyValuePair<IPropertyDef, object>> ReadProperties(IScope scope, IReader reader, object obj, IElementDef def)
		{
			ReadStartElement(scope, reader, def);

			// read attributes
			if (def.Attributes.Any())
			{
				foreach (var attr in reader.ReadAttributes())
				{
					var property = def.Attributes[attr.Key];
					if (property != null)
					{
						var value = Parse(scope, property.Type, attr.Value);
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
						value = Parse(scope, property.Type, reader.ReadString());
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

				if (ReadValue(scope, reader, obj, def, property, out value))
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

		private static bool ReadValue(IScope scope, IReader reader, object obj, IElementDef def, IPropertyDef property, out object value)
		{
			var type = property.Type;

			if (type == typeof(object))
			{
				value = reader.ReadObject();
				return true;
			}

			if (scope.TryRead(() => reader.ReadString(), type, out value))
				return true;

			var xmlReader = reader as XmlReaderImpl;
			if (xmlReader != null)
			{
				var surrogate = scope.GetSurrogate(type);
				if (surrogate != null)
				{
					value = CreateElement(property, obj);
					surrogate.Read(xmlReader.XmlReader, value);
					return true;
				}
			}

			var elementDef = scope.GetElementDef(type);
			if (elementDef != null)
			{
				var subScope = elementDef as IScope;
				value = ReadElement(subScope ?? scope, reader, elementDef, () => CreateElement(property, obj));
				return true;
			}

			if (ReadEnumElement(reader, type, out value))
				return true;

			var ienum = Reflector.FindIEnumerable(type);
			if (ienum != null)
			{
				var elementType = ienum.GetGenericArguments()[0];
				elementDef = new CollectionDef(scope, property.Name, type, elementType);
				value = def.IsImmutable ? CreateList(elementType) : CreateElement(property, obj);
				ReadElement(scope, reader, elementDef, value);
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

		private static object Parse(IScope scope, Type type, string s)
		{
			object result;
			if (!scope.TryRead(() => s, type, out result))
				throw new NotSupportedException(string.Format("Unknown type: {0}", type));
			return result;
		}
	}
}
