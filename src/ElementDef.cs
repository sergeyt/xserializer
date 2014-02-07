using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	public sealed class ElementDef<T> : IElementDef
	{
		private readonly Scope _scope;
		private readonly DefCollection<IPropertyDef> _attributes = new DefCollection<IPropertyDef>();
		private readonly DefCollection<IPropertyDef> _elements = new DefCollection<IPropertyDef>();
		private readonly IDictionary<int,string> _propertyIndex = new Dictionary<int, string>();
		private Func<IDictionary<string, object>, T> _create;

		internal ElementDef(Scope scope, XName name)
		{
			if (scope == null) throw new ArgumentNullException("scope");
			if (name == null) throw new ArgumentNullException("name");

			_scope = scope;
			Name = name;
		}

		private static IPropertyDef CreateProperty<TValue>(Expression<Func<T, TValue>> property, XNamespace ns, Func<TValue, bool> isDefaultValue)
		{
			var member = property.ResolveMember();
			var name = ns + member.Name;

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

			var getter = property.Compile();
			var setter = MethodGenerator.Set(property);
			return new PropertyDef<TValue>(member.Name, name, elementName, getter, setter, isDefaultValue);
		}

		public ElementDef<T> Attr<TValue>(Expression<Func<T, TValue>> property, int initIndex)
		{
			var def = CreateProperty(property, XNamespace.None, null);
			_attributes.Add(def.Name, def);
			if (initIndex >= 0) _propertyIndex[initIndex] = def.PropertyName;
			return this;
		}

		public ElementDef<T> Attr<TValue>(Expression<Func<T, TValue>> property)
		{
			Attr(property, -1);
			return this;
		}

		public ElementDef<T> Elem<TValue>(Expression<Func<T, TValue>> property, int initIndex)
		{
			var def = CreateProperty(property, Name.Namespace, null);
			_elements.Add(def.Name, def);
			if (initIndex >= 0) _propertyIndex[initIndex] = def.PropertyName;
			return this;
		}

		public ElementDef<T> Elem<TValue>(Expression<Func<T, TValue>> property)
		{
			Elem(property, -1);
			return this;
		}

		public ElementDef<TElement> Sub<TElement>(XName name)
		{
			var elem = _scope.Elem<TElement>(name);
			// copy only attributes and elements
			elem._attributes.AddRange(_attributes);
			elem._elements.AddRange(_elements);
			return elem;
		}

		public ElementDef<TElement> Sub<TElement>()
		{
			return Sub<TElement>(Name.Namespace + typeof(TElement).Name);
		}

		public ElementDef<T> Init(Func<IDictionary<string, object>, T> create)
		{
			if (create == null) throw new ArgumentNullException("create");
			_create = create;
			return this;
		}

		private string GetPropertyName(int initIndex)
		{
			string name;
			if (!_propertyIndex.TryGetValue(initIndex, out name))
				throw new InvalidOperationException("This ElementDef is not complete!");
			return name;
		}

		public ElementDef<T> Init<T1>(Func<T1, T> create)
		{
			if (create == null) throw new ArgumentNullException("create");
			_create = d =>
				{
					var v1 = d.Get<T1>(GetPropertyName(0));
					return create(v1);
				};
			return this;
		}

		public ElementDef<T> Init<T1, T2>(Func<T1, T2, T> create)
		{
			if (create == null) throw new ArgumentNullException("create");
			_create = d =>
				{
					var v1 = d.Get<T1>(GetPropertyName(0));
					var v2 = d.Get<T2>(GetPropertyName(1));
					return create(v1, v2);
				};
			return this;
		}

		public ElementDef<T> Init<T1, T2, T3>(Func<T1, T2, T3, T> create)
		{
			if (create == null) throw new ArgumentNullException("create");
			_create = d =>
				{
					var v1 = d.Get<T1>(GetPropertyName(0));
					var v2 = d.Get<T2>(GetPropertyName(1));
					var v3 = d.Get<T3>(GetPropertyName(2));
					return create(v1, v2, v3);
				};
			return this;
		}

		public ElementDef<T> Init<T1, T2, T3, T4>(Func<T1, T2, T3, T4, T> create)
		{
			if (create == null) throw new ArgumentNullException("create");
			_create = d =>
				{
					var v1 = d.Get<T1>(GetPropertyName(0));
					var v2 = d.Get<T2>(GetPropertyName(1));
					var v3 = d.Get<T3>(GetPropertyName(2));
					var v4 = d.Get<T4>(GetPropertyName(3));
					return create(v1, v2, v3, v4);
				};
			return this;
		}

		public XName Name { get; private set; }
		public Type Type { get { return typeof(T); } }
		public bool IsImmutable { get { return _create != null; } }
		public IDefCollection<IPropertyDef> Attributes { get { return _attributes; } }
		public IDefCollection<IPropertyDef> Elements { get { return _elements; } }

		public object Create(IDictionary<string, object> properties)
		{
			if (_create == null) throw new NotSupportedException();
			return _create(properties);
		}

		public override string ToString()
		{
			return Name.ToString();
		}

		private sealed class PropertyDef<TValue> : IPropertyDef
		{
			private readonly Func<T, TValue> _getter;
			private readonly Action<T, TValue> _setter;
			private readonly Func<TValue, bool> _isDefaultValue;

			public PropertyDef(string propertyName, XName name, XName itemName,
				Func<T, TValue> getter, Action<T, TValue> setter, Func<TValue, bool> isDefaultValue)
			{
				if (name == null) throw new ArgumentNullException("name");
				if (getter == null) throw new ArgumentNullException("getter");

				_getter = getter;
				_setter = setter;
				_isDefaultValue = isDefaultValue;

				PropertyName = propertyName;
				Name = name;
				ItemName = itemName;
			}

			public string PropertyName { get; private set; }
			public XName Name { get; private set; }
			public XName ItemName { get; private set; }
			public Type Type { get { return typeof(TValue); } }
			public bool IsReadOnly { get { return _setter == null; } }

			public object GetValue(object target)
			{
				return _getter((T)target);
			}

			public void SetValue(object target, object value)
			{
				_setter((T)target, (TValue)value);
			}

			public bool IsDefaultValue(object value)
			{
				return _isDefaultValue != null && _isDefaultValue((TValue) value);
			}

			public override string ToString()
			{
				return Name.ToString();
			}
		}
	}
}