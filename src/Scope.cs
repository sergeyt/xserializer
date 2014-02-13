using System;
using System.Collections.Generic;
using System.Xml.Linq;
using TsvBits.Serialization.Utils;

namespace TsvBits.Serialization
{
	public class Scope : IScope
	{
		private readonly IScope _parent;
		private readonly SimpleTypeCollection _simpleTypes = new SimpleTypeCollection();
		private readonly IDictionary<Type, IElementDef> _elementDefs = new Dictionary<Type, IElementDef>();
		private readonly IDictionary<Type, List<XNamespace>> _typeNamespaces = new Dictionary<Type, List<XNamespace>>();
		private readonly DefCollection<IElementDef> _elements = new DefCollection<IElementDef>();
		private readonly IDictionary<Type, IXmlSurrogate> _xmlSurrogates = new Dictionary<Type, IXmlSurrogate>();

		protected Scope(IScope parent)
		{
			_parent = parent;
		}

		public Scope(XNamespace defaultNamespace)
		{
			Namespace = defaultNamespace ?? XNamespace.None;
		}

		public Scope(string defaultNamespace)
			: this(string.IsNullOrEmpty(defaultNamespace) ? XNamespace.None : XNamespace.Get(defaultNamespace))
		{
		}

		public Scope() : this(XNamespace.None)
		{
		}

		/// <summary>
		/// Default namespace.
		/// </summary>
		public XNamespace Namespace { get; private set; }
		
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

		public ElementDef<T> Element<T>(XName name)
		{
			var def = new ElementDef<T>(this, name);
			Register(def);
			return def;
		}

		public ElementDef<T> Element<T>()
		{
			return Element<T>(GetName<T>(Namespace));
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
			return _xmlSurrogates.TryGetValue(type, out surrogate) ? surrogate : null;
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
	}
}
