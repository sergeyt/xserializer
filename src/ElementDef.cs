using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	public class ElementDef : Scope, IElementDef
	{
		internal readonly DefCollection<IPropertyDef> _attributes = new DefCollection<IPropertyDef>();
		internal readonly DefCollection<IPropertyDef> _elements = new DefCollection<IPropertyDef>();

		internal ElementDef(Scope scope, Type type, XName name)
			: base(scope)
		{
			if (scope == null) throw new ArgumentNullException("scope");
			if (name == null) throw new ArgumentNullException("name");

			Scope = scope;
			Name = name;
			Type = type;
		}

		internal Scope Scope { get; private set; }
		public XName Name { get; private set; }
		public Type Type { get; private set; }
		public virtual bool IsImmutable { get { return false; } }
		IDefCollection<IPropertyDef> IElementDef.Attributes { get { return _attributes; } }
		IDefCollection<IPropertyDef> IElementDef.Elements { get { return _elements; } }

		public virtual object Create(IDictionary<string, object> properties)
		{
			throw new NotSupportedException();
		}
	}

	public sealed partial class ElementDef<T> : ElementDef
	{
		// property name -> index of constructor argument
		private readonly IDictionary<string,int> _ctorIndex = new Dictionary<string, int>();
		private Func<IDictionary<string, object>, T> _create;

		internal ElementDef(Scope scope, XName name)
			: base(scope, typeof(T), name)
		{
		}

		public override bool IsImmutable
		{
			get { return _create != null; }
		}

		public override object Create(IDictionary<string, object> properties)
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
				namespaces = Scope.Namespace == Name.Namespace
					? Scope.Namespaces
					: new[] {Name.Namespace};
			}
			return new PropertyCollection(this, namespaces, _elements);
		}

		public ElementDef<TElement> Sub<TElement>(params XName[] names)
		{
			var elem = Scope.Element<TElement>(names);
			// copy only attributes and elements
			elem._attributes.AddRange(_attributes);
			elem._elements.AddRange(_elements);
			return elem;
		}

		public ElementDef<T> Fork(XNamespace ns)
		{
			return Sub<T>(ns + Name.LocalName);
		}

		public ElementDef<T> Fork(XName name)
		{
			return Sub<T>(name);
		}

		public ElementDef<T> Use(params XNamespace[] namespaces)
		{
			foreach (var ns in namespaces.Where(x => x != Namespace))
			{
				Fork(ns);
			}
			return this;
		}

		#region Init for Immutable Types

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

		#endregion

		public override string ToString()
		{
			return Name.ToString();
		}
	}
}