using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	public class Scope
	{
		private readonly SimpleTypeCollection _simpleTypes = new SimpleTypeCollection();
		private readonly IDictionary<Type, IElementDef> _elementDefs = new Dictionary<Type, IElementDef>();
		private readonly DefCollection<IElementDef> _elements = new DefCollection<IElementDef>();

		protected Scope()
		{
		}

		private Scope(XNamespace ns)
		{
			Namespace = ns ?? XNamespace.None;
		}

		public static Scope New(XNamespace ns)
		{
			return new Scope(ns);
		}

		public XNamespace Namespace { get; private set; }

		public static Scope New(string ns)
		{
			return new Scope(string.IsNullOrEmpty(ns) ? XNamespace.None : XNamespace.Get(ns));
		}

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
			_elementDefs.Add(def.Type, def);
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
			// TODO support Name attribute
			var type = typeof(T);
			var name = type.Name;
			if (type.IsGenericType)
			{
				var i = name.LastIndexOf('`');
				if (i >= 0) name = name.Substring(0, i);
			}
			return Element<T>(Namespace + name);
		}

		internal IElementDef ElemDef(Type type)
		{
			IElementDef def;
			return _elementDefs.TryGetValue(type, out def) ? def : null;
		}

		internal IElementDef ElemDef(XName name)
		{
			return _elements[name];
		}

		internal SimpleTypeCollection SimpleTypes
		{
			get { return _simpleTypes; }
		}
	}
}
