using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	/// <summary>
	/// Property definition.
	/// </summary>
	public interface IPropertyDef
	{
		/// <summary>
		/// Gets the original property name.
		/// </summary>
		string PropertyName { get; }

		/// <summary>
		/// Gets property type.
		/// </summary>
		Type Type { get; }

		/// <summary>
		/// Gets XML name of the property.
		/// </summary>
		XName Name { get; }

		/// <summary>
		/// Gets XML name of collection element.
		/// </summary>
		XName ElementName { get; }
		
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
	}

	/// <summary>
	/// Definition collection.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IDefCollection<T> : IEnumerable<T>
	{
		/// <summary>
		/// Finds definition by name.
		/// </summary>
		/// <param name="name">The name of definition to get.</param>
		T this[XName name] { get; }
	}

	/// <summary>
	/// Element definition.
	/// </summary>
	public interface IElementDef
	{
		/// <summary>
		/// Gets element name.
		/// </summary>
		XName Name { get; }

		/// <summary>
		/// Gets element type.
		/// </summary>
		Type Type { get; }

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