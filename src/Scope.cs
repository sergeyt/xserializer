using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TsvBits.Serialization.Utils;

namespace TsvBits.Serialization
{
	public class Scope : IScope
	{
		private readonly Scope _parent;
		private readonly SimpleTypeCollection _simpleTypes = new SimpleTypeCollection();
		private readonly IDictionary<Type, IElementDef> _elementDefs = new Dictionary<Type, IElementDef>();
		private readonly IDictionary<Type, List<XNamespace>> _typeNamespaces = new Dictionary<Type, List<XNamespace>>();
		private readonly DefCollection<IElementDef> _elements = new DefCollection<IElementDef>();
		private readonly IDictionary<Type, IXmlSurrogate> _xmlSurrogates = new Dictionary<Type, IXmlSurrogate>();

		protected Scope(Scope parent)
		{
			if (parent == null) throw new ArgumentNullException("parent");

			_parent = parent;
			Namespaces = parent.Namespaces;
		}

		public Scope(params XNamespace[] namespaces)
		{
			if (namespaces == null || namespaces.Length == 0)
			{
				namespaces = new[] {XNamespace.None};
			}

			Namespaces = namespaces;
		}

		public Scope() : this(XNamespace.None)
		{
		}

		/// <summary>
		/// Default namespace.
		/// </summary>
		public XNamespace Namespace { get { return Namespaces.First(); } }
		public XNamespace[] Namespaces { get; private set; }

		/// <summary>
		/// Registers simple type serializable to string.
		/// </summary>
		/// <typeparam name="T">The type to register.</typeparam>
		/// <param name="read">The parser.</param>
		/// <param name="write">The writer.</param>
		public Scope Type<T>(Func<string, T> read, Func<T, string> write)
		{
			_simpleTypes.Add(read, write);
			return this;
		}

		public Scope Enum<T>(T defval, bool ignoreCase)
		{
			_simpleTypes.Enum(defval, ignoreCase);
			return this;
		}

		public Scope Enum<T>(T defval)
		{
			_simpleTypes.Enum(defval);
			return this;
		}

		private void Register(IElementDef def)
		{
			var type = def.Type;
			if (!_elementDefs.ContainsKey(type))
			{
				_elementDefs.Add(type, def);
			}

			List<XNamespace> namespaces;
			var ns = def.Name.Namespace;
			if (_typeNamespaces.TryGetValue(type, out namespaces))
			{
				if (!namespaces.Contains(ns))
					namespaces.Add(ns);
			}
			else
			{
				_typeNamespaces.Add(type, new List<XNamespace> {ns});
			}
			
			_elements.Add(def.Name, def);
		}

		public ElementDef<T> Element<T>(params XName[] names)
		{
			if (names == null || names.Length == 0)
			{
				names = Namespaces.Select(ns => GetName<T>(ns)).ToArray();
			}

			var def = new ElementDef<T>(this, names[0]);
			Register(def);

			for (var i = 1; i < names.Length; i++)
			{
				Register(new ElementFork(def, names[i]));
			}

			return def;
		}

		public IElementDef GetElementDef(Type type)
		{
			IElementDef def;
			if (_elementDefs.TryGetValue(type, out def))
				return def;
			return _parent != null ? _parent.GetElementDef(type) : null;
		}

		public IElementDef GetElementDef(XName name)
		{
			var def = _elements[name];
			if (def != null) return def;
			return _parent != null ? _parent.GetElementDef(name) : null;
		}

		public bool TryConvert(object value, out string result)
		{
			if (_simpleTypes.TryConvert(value, out result, _parent == null))
				return true;
			return _parent != null && _parent.TryConvert(value, out result);
		}

		public bool TryRead(Func<string> reader, Type type, out object value)
		{
			if (_simpleTypes.TryRead(reader, type, out value))
				return true;
			return _parent != null && _parent.TryRead(reader, type, out value);
		}

		public IList<XNamespace> GetNamespaces(Type type)
		{
			List<XNamespace> namespaces;
			if (_typeNamespaces.TryGetValue(type, out namespaces))
				return namespaces.AsReadOnly();
			return _parent != null ? _parent.GetNamespaces(type) : new XNamespace[0];
		}

		public Scope Element<T>(IXmlSurrogate surrogate)
		{
			if (surrogate == null) throw new ArgumentNullException("surrogate");
			_xmlSurrogates.Add(typeof(T), surrogate);
			return this;
		}

		public IXmlSurrogate GetSurrogate(Type type)
		{
			IXmlSurrogate surrogate;
			if (_xmlSurrogates.TryGetValue(type, out surrogate))
				return surrogate;
			return _parent != null ? _parent.GetSurrogate(type) : null;
		}

		internal static XName GetName<T>(XNamespace defaultNamespace)
		{
			var type = typeof(T);

			var attr = type.ResolveAttribute<NameAttribute>(true);
			if (attr != null)
			{
				if (!string.IsNullOrEmpty(attr.Namespace))
				{
					return XNamespace.Get(attr.Namespace) + attr.Name;
				}
				return defaultNamespace + attr.Name;
			}

			var name = type.Name;
			if (type.IsGenericType)
			{
				var i = name.LastIndexOf('`');
				if (i >= 0) name = name.Substring(0, i);
			}

			return defaultNamespace + name;
		}

		private class ElementFork : IElementDef
		{
			private readonly IElementDef _element;

			public ElementFork(IElementDef element, XName name)
			{
				_element = element;
				Name = name;
			}

			public XName Name { get; private set; }
			public Type Type { get { return _element.Type; } }
			public bool IsImmutable { get { return _element.IsImmutable; } }
			public IDefCollection<IPropertyDef> Elements {  get { return _element.Elements; } }
			public IDefCollection<IPropertyDef> Attributes { get { return _element.Attributes; } }

			public object Create(IDictionary<string, object> properties)
			{
				return _element.Create(properties);
			}
		}
	}
}
