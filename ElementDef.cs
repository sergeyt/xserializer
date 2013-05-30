using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace XmlSerialization
{
	public sealed class ElementDef<T> : IElementDef
	{
		private readonly IDictionary<XName, IPropertyDef> _attributeDefs = new Dictionary<XName, IPropertyDef>();
		private readonly IDictionary<XName, IPropertyDef> _elementDefs = new Dictionary<XName, IPropertyDef>();

		public ElementDef(XName name)
		{
			if (name == null) throw new ArgumentNullException("name");

			Name = name;
		}

		public ElementDef<T> Attr<TValue>(XName name, Func<T, TValue> getter, Action<T, TValue> setter)
		{
			var attr = new PropertyDef<TValue>(name, getter, setter);
			_attributeDefs.Add(name, attr);
			return this;
		}

		public ElementDef<T> Attr<TValue>(Expression<Func<T, TValue>> property)
		{
			var name = GetPropertyName(property);
			var getter = property.Compile();
			var setter = ResolveSetter(property);
			// TODO: support readonly attributes
			if (setter == null) throw new NotSupportedException();
			return Attr(XNamespace.None + name, getter, setter);
		}

		public ElementDef<T> Elem<TValue>(XName name, Func<T, TValue> getter, Action<T, TValue> setter)
		{
			var elem = new PropertyDef<TValue>(name, getter, setter);
			_elementDefs.Add(name, elem);
			return this;
		}

		public ElementDef<T> Elem<TValue>(XName name, Func<T, TValue> getter)
		{
			return Elem(name, getter, null);
		}

		public ElementDef<T> Elem<TValue>(Expression<Func<T, TValue>> property)
		{
			var name = GetPropertyName(property);
			var getter = property.Compile();
			return Elem(Name.Namespace + name, getter, ResolveSetter(property));
		}

		public ElementDef<TElement> Sub<TElement>(XName name)
		{
			var elem = new ElementDef<TElement>(name);
			foreach (var p in _attributeDefs)
			{
				elem._attributeDefs.Add(p.Key, p.Value);
			}
			foreach (var p in _elementDefs)
			{
				elem._elementDefs.Add(p.Key, p.Value);
			}
			return elem;
		}

		public XName Name { get; private set; }

		public Type Type
		{
			get { return typeof(T); }
		}

		public IEnumerable<IPropertyDef> Attributes
		{
			get { return _attributeDefs.Values; }
		}

		public IPropertyDef GetAttribute(XName name)
		{
			IPropertyDef def;
			return _attributeDefs.TryGetValue(name, out def) ? def : null;
		}
		
		public IEnumerable<INodeDef> Elements
		{
			get { return _elementDefs.Values.Cast<INodeDef>(); }
		}

		public Type GetElementType(XName name)
		{
			var def = GetPropertyDef(name);
			return def != null ? def.Type : null;
		}

		public object GetValue(XName name, object target)
		{
			var def = GetPropertyDef(name);
			if (def == null) throw new NotSupportedException();
			return def.GetValue(target);
		}

		public void SetValue(XName name, object target, object value)
		{
			var def = GetPropertyDef(name);
			if (def == null) throw new NotSupportedException();
			if (def.IsReadOnly) throw new NotSupportedException();
			def.SetValue(target, value);
		}

		public object CreateElement(XName name, object target)
		{
			var def = GetPropertyDef(name);
			if (def == null) throw new NotSupportedException();
			if (def.IsReadOnly)
			{
				return def.GetValue(target);
			}
			var value = Activator.CreateInstance(def.Type);
			def.SetValue(target, value);
			return value;
		}

		public override string ToString()
		{
			return Name.ToString();
		}

		private IPropertyDef GetPropertyDef(XName name)
		{
			IPropertyDef def;
			return _elementDefs.TryGetValue(name, out def) ? def : null;
		}

		private static string GetPropertyName<TValue>(Expression<Func<T, TValue>> expression)
		{
			var me = (MemberExpression)expression.Body;
			return me.Member.Name;
		}

		private static Action<T, TValue> ResolveSetter<TValue>(Expression<Func<T, TValue>> expression)
		{
			var me = (MemberExpression)expression.Body;
			var pi = me.Member as PropertyInfo;
			if (pi != null)
			{
				// TODO: handle collections

				var setMethod = pi.GetSetMethod();
				if (setMethod == null) return null;
				
				var target = Expression.Parameter(typeof(T), "target");
				var value = Expression.Parameter(typeof(TValue), "value");
				
				var setter = Expression.Call(target, setMethod, value);
				return Expression.Lambda<Action<T, TValue>>(setter, target, value).Compile();
			}

			var fi = me.Member as FieldInfo;
			if (fi != null)
			{
				// TODO: optimize with reflection emit
				return (target, value) => fi.SetValue(target, value);
			}

			throw new NotSupportedException();
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

	public static class ElementDef
	{
		public static ElementDef<T> New<T>(XName name)
		{
			return new ElementDef<T>(name);
		}
	}
}