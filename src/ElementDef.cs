using System;
using System.Collections.Generic;
using System.Linq;
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
		IDefCollection<IPropertyDef> IElementDef.Attributes { get { return _attributes; } }
		IDefCollection<IPropertyDef> IElementDef.Elements { get { return _elements; } }

		public object Create(IDictionary<string, object> properties)
		{
			if (_create == null) throw new NotSupportedException();
			return _create(properties);
		}
		
		public PropertyCollection Attributes(params XNamespace[] namespaces)
		{
			if (namespaces == null || namespaces.Length == 0)
			{
				namespaces = new[] {XNamespace.None};
			}
			return new PropertyCollection(this, namespaces, _attributes);
		}

		public PropertyCollection Elements(params XNamespace[] namespaces)
		{
			if (namespaces == null || namespaces.Length == 0)
			{
				namespaces = new[] {Name.Namespace};
			}
			return new PropertyCollection(this, namespaces, _elements);
		}

		public ElementDef<TElement> Sub<TElement>(XName name)
		{
			var elem = _scope.Element<TElement>(name);
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