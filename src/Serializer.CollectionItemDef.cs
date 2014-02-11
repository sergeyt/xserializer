using System;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	partial class Serializer
	{
		private sealed class CollectionItemDef : IPropertyDef
		{
			public CollectionItemDef(XName name, Type type)
			{
				Name = name;
				Type = type;
				ItemName = name;
			}

			public string PropertyName { get { return "Item"; } }
			public Type Type { get; private set; }
			public XName Name { get; private set; }
			public XName ItemName { get; private set; }
			public bool IsReadOnly { get { return true; } }

			public object GetValue(object target)
			{
				throw new NotSupportedException();
			}

			public void SetValue(object target, object value)
			{
				throw new NotSupportedException();
			}

			public bool IsDefaultValue(object value)
			{
				return false;
			}
		}
	}
}
