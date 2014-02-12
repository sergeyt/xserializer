using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Xml.Linq;
using TsvBits.Serialization.Utils;

namespace TsvBits.Serialization
{
	partial class ElementDef<T>
	{
		public class PropertyCollection
		{
			private readonly ElementDef<T> _elementDef;
			private readonly XNamespace[] _namespaces;
			private readonly DefCollection<IPropertyDef> _properties;

			internal PropertyCollection(ElementDef<T> elementDef, XNamespace[] namespaces, DefCollection<IPropertyDef> properties)
			{
				_elementDef = elementDef;
				_namespaces = namespaces;
				_properties = properties;
			}

			public PropertyCollection Add<TValue>(Expression<Func<T, TValue>> property, Func<TValue, bool> isDefaultValue)
			{
				IPropertyDef prop = null;
				foreach (var ns in _namespaces)
				{
					if (prop == null)
					{
						prop = Create(property, ns, isDefaultValue);
						var name = ns + prop.Name.LocalName;
						_properties.Add(name, prop);
					}
					else
					{
						var name = ns + prop.Name.LocalName;
						_properties.RegisterSynonym(name, new PropertyProxy(prop, name));
					}
				}
				return this;
			}

			public PropertyCollection Add<TValue>(Expression<Func<T, TValue>> property, TValue defaultValue)
			{
				return Add(property, value => Equals(value, defaultValue));
			}

			public PropertyCollection Add<TValue>(Expression<Func<T, TValue>> property)
			{
				return Add(property, null);
			}

			public ElementDef<T> End()
			{
				return _elementDef;
			}

			private IPropertyDef Create<TValue>(Expression<Func<T, TValue>> property, XNamespace ns, Func<TValue, bool> isDefaultValue)
			{
				var member = property.ResolveMember();
				var name = ns + member.Name;

				// TODO support multiple xml names or namespaces configured via custom attributes 
				var nameAttr = member.ResolveAttribute<NameAttribute>(true);
				if (nameAttr != null)
				{
					name = string.IsNullOrEmpty(nameAttr.Namespace)
							   ? ns + nameAttr.Name
							   : XNamespace.Get(nameAttr.Namespace) + nameAttr.Name;
				}

				var elementName = ns + member.Name.ToSingular();
				var itemAttr = member.ResolveAttribute<ItemNameAttribute>(true);
				if (itemAttr != null)
				{
					elementName = string.IsNullOrEmpty(itemAttr.Namespace)
									  ? ns + itemAttr.Name
									  : XNamespace.Get(itemAttr.Namespace) + itemAttr.Name;
				}

				if (isDefaultValue == null)
				{
					var defaultValueAttr = member.ResolveAttribute<DefaultValueAttribute>(true);
					if (defaultValueAttr != null)
					{
						var defaultValue = defaultValueAttr.Value;
						isDefaultValue = value => Equals(value, defaultValue);
					}
				}

				var argAttr = member.ResolveAttribute<ArgAttribute>(true);
				if (argAttr != null)
				{
					_elementDef._ctorIndex[name.LocalName] = argAttr.Index;
				}

				var getter = property.Compile();
				var setter = MethodGenerator.GenerateSetter(property);
				return new PropertyDef<TValue>(member.Name, name, elementName, getter, setter, isDefaultValue);
			}
		}

		private class PropertyProxy : IPropertyDef
		{
			private readonly IPropertyDef _property;

			public PropertyProxy(IPropertyDef property, XName name)
			{
				_property = property;
				Name = name;
			}

			public XName Name { get; private set; }

			public Type Type
			{
				get { return _property.Type; }
			}

			public string PropertyName
			{
				get { return _property.PropertyName; }
			}

			public XName ItemName
			{
				get { return _property.ItemName; }
			}

			public bool IsReadOnly
			{
				get { return _property.IsReadOnly; }
			}

			public object GetValue(object target)
			{
				return _property.GetValue(target);
			}

			public void SetValue(object target, object value)
			{
				_property.SetValue(target, value);
			}

			public bool IsDefaultValue(object value)
			{
				return _property.IsDefaultValue(value);
			}
		}
	}
}
