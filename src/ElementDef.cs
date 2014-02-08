using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	public sealed partial class ElementDef<T> : IElementDef
	{
		private readonly Scope _scope;
		private readonly DefCollection<IPropertyDef> _attributes = new DefCollection<IPropertyDef>();
		private readonly DefCollection<IPropertyDef> _elements = new DefCollection<IPropertyDef>();
		// property name -> index of constructor argument
		private readonly IDictionary<string,int> _ctorIndex = new Dictionary<string, int>();
		private Func<IDictionary<string, object>, T> _create;

		internal ElementDef(Scope scope, XName name)
		{
			if (scope == null) throw new ArgumentNullException("scope");
			if (name == null) throw new ArgumentNullException("name");

			_scope = scope;
			Name = name;
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
		
		private IPropertyDef CreateProperty<TValue>(Expression<Func<T, TValue>> property, XNamespace ns, Func<TValue, bool> isDefaultValue)
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
				_ctorIndex[name.LocalName] = argAttr.Index;
			}

			var getter = property.Compile();
			var setter = MethodGenerator.GenerateSetter(property);
			return new PropertyDef<TValue>(member.Name, name, elementName, getter, setter, isDefaultValue);
		}

		public ElementDef<T> Attr<TValue>(Expression<Func<T, TValue>> property)
		{
			var def = CreateProperty(property, XNamespace.None, null);
			_attributes.Add(def.Name, def);
			return this;
		}

		public ElementDef<T> Attr<TValue>(Expression<Func<T, TValue>> property, Func<TValue,bool> isDefaultValue)
		{
			var def = CreateProperty(property, XNamespace.None, isDefaultValue);
			_attributes.Add(def.Name, def);
			return this;
		}

		public ElementDef<T> Attr<TValue>(Expression<Func<T, TValue>> property, TValue defaultValue)
		{
			return Attr(property, value => Equals(value, defaultValue));
		}

		public ElementDef<T> Elem<TValue>(Expression<Func<T, TValue>> property)
		{
			var def = CreateProperty(property, Name.Namespace, null);
			_elements.Add(def.Name, def);
			return this;
		}

		public ElementDef<T> Elem<TValue>(Expression<Func<T, TValue>> property, Func<TValue, bool> isDefaultValue)
		{
			var def = CreateProperty(property, Name.Namespace, isDefaultValue);
			_elements.Add(def.Name, def);
			return this;
		}

		public ElementDef<T> Elem<TValue>(Expression<Func<T, TValue>> property, TValue defaultValue)
		{
			return Elem(property, value => Equals(value, defaultValue));
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

		/// <summary>
		/// Auto generates create function.
		/// </summary>
		public ElementDef<T> Init()
		{
			_create = d =>
			{
				// TODO optimize using generated dynamic methods
				var ctors = typeof(T).GetConstructors();
				var ctor = ctors.FirstOrDefault(x => x.GetParameters().Length == d.Count);
				if (ctor == null)
					throw new InvalidOperationException(
						string.Format("Type '{0}' has no appropriate constructor to create object.",
							typeof(T))
						);

				var args = (from p in d
					let index = _ctorIndex[p.Key]
					orderby index
					select p.Value).ToArray();

				return (T) ctor.Invoke(args);
			};
			return this;
		}

		public override string ToString()
		{
			return Name.ToString();
		}
	}
}