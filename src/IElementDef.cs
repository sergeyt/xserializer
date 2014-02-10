using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	public interface IDef
	{
		/// <summary>
		/// Gets XML name of the definition.
		/// </summary>
		XName Name { get; }

		/// <summary>
		/// Gets definition type.
		/// </summary>
		Type Type { get; }
	}

	/// <summary>
	/// Definition collection.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IDefCollection<T> : IEnumerable<T> where T : IDef
	{
		/// <summary>
		/// Finds definition by name.
		/// </summary>
		/// <param name="name">The name of definition to get.</param>
		T this[XName name] { get; }
	}

	/// <summary>
	/// Property definition.
	/// </summary>
	public interface IPropertyDef : IDef
	{
		/// <summary>
		/// Gets the original property name.
		/// </summary>
		string PropertyName { get; }

		/// <summary>
		/// Gets XML name of collection element. Applicable for collection properties.
		/// </summary>
		XName ItemName { get; }
		
		/// <summary>
		/// Specifies whether the property is readonly.
		/// </summary>
		bool IsReadOnly { get; }

		/// <summary>
		/// Gets property value.
		/// </summary>
		/// <param name="target">The target object to get property of.</param>
		/// <returns></returns>
		object GetValue(object target);

		/// <summary>
		/// Sets property value to specified object.
		/// </summary>
		/// <param name="target">The target object to modify.</param>
		/// <param name="value">The value to set.</param>
		void SetValue(object target, object value);

		/// <summary>
		/// Specifies whether given value is default for this property.
		/// </summary>
		/// <param name="value">The value to check.</param>
		bool IsDefaultValue(object value);
	}

	/// <summary>
	/// Element definition.
	/// </summary>
	public interface IElementDef : IDef
	{
		/// <summary>
		/// Specifies whether the elemnt is immutable.
		/// </summary>
		bool IsImmutable { get; }

		/// <summary>
		/// Gets element attribute definitions.
		/// </summary>
		IDefCollection<IPropertyDef> Attributes { get; }

		/// <summary>
		/// Gets child element definitions.
		/// </summary>
		IDefCollection<IPropertyDef> Elements { get; }

		/// <summary>
		/// Creates new instance with specified properties.
		/// </summary>
		/// <param name="properties">The properties to initialize the object with.</param>
		object Create(IDictionary<string, object> properties);
	}
}