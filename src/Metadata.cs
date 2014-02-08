using System;

namespace TsvBits.Serialization
{
	/// <summary>
	/// Allows customization of XML name for the property/field.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public sealed class NameAttribute : Attribute
	{
		public NameAttribute(string name)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			Name = name;
		}

		public NameAttribute(string name, string ns)
			: this(name)
		{
			Namespace = ns;
		}

		public string Name { get; private set; }
		public string Namespace { get; private set; }
	}

	/// <summary>
	/// Allows customization of XML name for item of the collection.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public sealed class ItemNameAttribute : Attribute
	{
		public ItemNameAttribute(string name)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			Name = name;
		}

		public ItemNameAttribute(string name, string ns) : this(name)
		{
			Namespace = ns;
		}

		public string Name { get; private set; }
		public string Namespace { get; private set; }
	}

	/// <summary>
	/// Specifies index in argument constructor.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]	
	public sealed class ArgAttribute : Attribute
	{
		public ArgAttribute(int index)
		{
			Index = index;
		}

		public int Index { get; private set; }
	}
}