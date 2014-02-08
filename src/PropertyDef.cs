using System;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	partial class ElementDef<T>
	{
		private sealed class PropertyDef<TValue> : IPropertyDef
		{
			private readonly Func<T, TValue> _getter;
			private readonly Action<T, TValue> _setter;
			private readonly Func<TValue, bool> _isDefaultValue;

			public PropertyDef(string propertyName, XName name, XName itemName,
				Func<T, TValue> getter, Action<T, TValue> setter, Func<TValue, bool> isDefaultValue)
			{
				if (name == null) throw new ArgumentNullException("name");
				if (getter == null) throw new ArgumentNullException("getter");

				_getter = getter;
				_setter = setter;
				_isDefaultValue = isDefaultValue;

				PropertyName = propertyName;
				Name = name;
				ItemName = itemName;
			}

			public string PropertyName { get; private set; }
			public XName Name { get; private set; }
			public XName ItemName { get; private set; }
			public Type Type { get { return typeof(TValue); } }
			public bool IsReadOnly { get { return _setter == null; } }

			public object GetValue(object target)
			{
				return _getter((T)target);
			}

			public void SetValue(object target, object value)
			{
				_setter((T)target, (TValue)value);
			}

			public bool IsDefaultValue(object value)
			{
				return _isDefaultValue != null && _isDefaultValue((TValue)value);
			}

			public override string ToString()
			{
				return Name.ToString();
			}
		}
	}
}
