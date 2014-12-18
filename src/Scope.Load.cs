using System;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	public sealed class LoadSpec
	{
		public Func<Type, bool> TypeFilter;
		public Func<PropertyInfo, bool> PropertyFilter;
		public Func<Type, TypeSpec> ForType;
		public Func<PropertyInfo, PropertySpec> ForProperty;
	}

	public struct TypeSpec
	{
		// first name is primary
		public XName[] Names;
	}

	public struct PropertySpec
	{
		public bool IsAttribute;
		public XName[] Names;
	}

	partial class Scope
	{
		public void LoadFrom(Assembly assembly, LoadSpec spec)
		{
			if (assembly == null) throw new ArgumentNullException("assembly");
			if (spec == null) throw new ArgumentNullException("spec");
			assembly.GetTypes().ToList().ForEach(type => LoadType(type, spec));
		}

		private IElementDef LoadType(Type type, LoadSpec spec)
		{
			if (!spec.TypeFilter(type)) return null;

			// skip primitive types
			if (IsPrimitive(type)) return null;
			
			// skip if defined
			var def = GetElementDef(type);
			if (def != null) return def;

			if (type.BaseType != null && type.BaseType != typeof(object))
			{
				LoadType(type.BaseType, spec);
			}

			var typeSpec = spec.ForType(type);
			var elem = Element(type, typeSpec.Names);

			type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.Where(spec.PropertyFilter ?? (p => true))
				.ToList()
				.ForEach(property =>
				{
					// TODO handle collections
					LoadType(property.PropertyType, spec);

					var propertySpec = spec.ForProperty(property);
					// add IPropertyDef
				});

			return elem;
		}

		private bool IsPrimitive(Type type)
		{
			if (_converters.FindType(type) != null) return true;
			return _parent != null && _parent.IsPrimitive(type);
		}
	}
}
