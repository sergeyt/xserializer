﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace TsvBits.XmlSerialization
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

		public ElementDef<T> Attr<TValue>(XName name, Func<T, TValue> getter, Action<T, TValue> setter)
		{
			var attr = new PropertyDef<TValue>(name, getter, setter);
			_attributes.Add(name, attr);
			return this;
		}

		public ElementDef<T> Attr<TValue>(Expression<Func<T, TValue>> property)
		{
			var name = property.GetPropertyName();
			var getter = property.Compile();
			var setter = MethodGenerator.Set(property);
			return Attr(XNamespace.None + name, getter, setter);
		}

		public ElementDef<T> Attr<TValue>(Expression<Func<T, TValue>> property, int initIndex)
		{
			var name = property.GetPropertyName();
			var getter = property.Compile();
			Attr(XNamespace.None + name, getter, null);
			_propertyIndex[initIndex] = name;
			return this;
		}

		public ElementDef<T> Elem<TValue>(XName name, Func<T, TValue> getter, Action<T, TValue> setter)
		{
			var elem = new PropertyDef<TValue>(name, getter, setter);
			_elements.Add(name, elem);
			return this;
		}

		public ElementDef<T> Elem<TValue>(XName name, Func<T, TValue> getter)
		{
			return Elem(name, getter, null);
		}

		public ElementDef<T> Elem<TValue>(Expression<Func<T, TValue>> property)
		{
			var name = property.GetPropertyName();
			var getter = property.Compile();
			return Elem(Name.Namespace + name, getter, MethodGenerator.Set(property));
		}

		public ElementDef<T> Elem<TValue>(XName name, Func<T, TValue> getter, int initIndex)
		{
			Elem(name, getter, null);
			_propertyIndex[initIndex] = name.LocalName;
			return this;
		}

		public ElementDef<T> Elem<TValue>(Expression<Func<T, TValue>> property, int initIndex)
		{
			var name = property.GetPropertyName();
			var getter = property.Compile();
			return Elem(Name.Namespace + name, getter, initIndex);
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

			public PropertyDef(XName name, Func<T, TValue> getter, Action<T, TValue> setter)
			{
				if (name == null) throw new ArgumentNullException("name");
				if (getter == null) throw new ArgumentNullException("getter");

				_getter = getter;
				_setter = setter;

				Name = name;
			}

			public XName Name { get; private set; }

			public Type Type
			{
				get { return typeof(TValue); }
			}

			public bool IsReadOnly
			{
				get { return _setter == null; }
			}

			public object GetValue(object target)
			{
				return _getter((T)target);
			}

			public void SetValue(object target, object value)
			{
				_setter((T)target, (TValue)value);
			}

			public override string ToString()
			{
				return Name.ToString();
			}
		}
	}
}