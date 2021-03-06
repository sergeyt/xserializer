﻿using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
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

			public PropertyCollection Add<TValue>(Expression<Func<T, TValue>> property, Func<TValue, bool> isDefaultValue, params XName[] names)
			{
				IPropertyDef prop = null;
				if (names != null && names.Length > 0)
				{
					foreach (var name in names)
					{
						if (prop == null)
						{
							prop = Create(property, isDefaultValue, null, name);
							_properties.Add(name, prop);
						}
						else
						{
							_properties.Alias(name, new PropertyFork(prop, name));
						}
					}
				}
				else
				{
					foreach (var ns in _namespaces)
					{
						if (prop == null)
						{
							prop = Create(property, isDefaultValue, ns, null);
							var name = ns + prop.Name.LocalName;
							_properties.Add(name, prop);
						}
						else
						{
							var name = ns + prop.Name.LocalName;
							_properties.Alias(name, new PropertyFork(prop, name));
						}
					}
				}
				return this;
			}

			public PropertyCollection Add<TValue>(Expression<Func<T, TValue>> property, TValue defaultValue, params XName[] names)
			{
				return Add(property, value => Equals(value, defaultValue), names);
			}

			public PropertyCollection Add<TValue>(Expression<Func<T, TValue>> property, params XName[] names)
			{
				return Add(property, null, names);
			}

			public ElementDef<T> End()
			{
				return _elementDef;
			}

			private IPropertyDef Create<TValue>(Expression<Func<T, TValue>> property, Func<TValue, bool> isDefaultValue, XNamespace ns, XName name)
			{
				var member = property.ResolveMember();
				if (name == null)
				{
					name = GetDefaultName(member, ns);
				}

				ns = name.Namespace;
				var elementName = ns + name.LocalName.ToSingular();
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
				var setter = DynamicMethods.Setter(property);
				return new PropertyDef<TValue>(member.Name, name, elementName, getter, setter, isDefaultValue);
			}

			private static XName GetDefaultName(MemberInfo member, XNamespace ns)
			{
				var name = ns + member.Name;
				var nameAttr = member.ResolveAttribute<NameAttribute>(true);
				if (nameAttr != null)
				{
					name = string.IsNullOrEmpty(nameAttr.Namespace)
						? ns + nameAttr.Name
						: XNamespace.Get(nameAttr.Namespace) + nameAttr.Name;
				}
				return name;
			}
		}

		private class PropertyFork : IPropertyDef
		{
			private readonly IPropertyDef _property;

			public PropertyFork(IPropertyDef property, XName name)
			{
				_property = property;
				Name = name;
			}

			public XName Name { get; private set; }
			public Type Type { get { return _property.Type; } }
			public string PropertyName { get { return _property.PropertyName; } }
			// TODO fork could have different ItemName
			public XName ItemName { get { return _property.ItemName; } }
			public bool IsReadOnly { get { return _property.IsReadOnly; } }

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
