using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	partial class XSerializer
	{
		private sealed class CollectionDef : IElementDef
		{
			private readonly XSerializer _serializer;
			private readonly Type _elementType;
			private readonly ItemDefCollection _elements;
			private readonly Dictionary<Type, Action<object, object>> _addMethods = new Dictionary<Type, Action<object, object>>();

			public CollectionDef(XSerializer serializer, XName name, Type type, Type elementType)
			{
				_serializer = serializer;
				_elementType = elementType;
				Name = name;
				Type = type;
				_elements = new ItemDefCollection(this);
			}

			public XName Name { get; private set; }
			public Type Type { get; private set; }
			public bool IsImmutable { get { return false; } }
			public IDefCollection<IPropertyDef> Attributes { get { return DefCollection<IPropertyDef>.Empty; } }
			public IDefCollection<IPropertyDef> Elements { get { return _elements; } }

			public object Create(IDictionary<string, object> properties)
			{
				throw new NotSupportedException();
			}

			private Type GetElementType(XName name)
			{
				if (_elementType.IsSealed) return _elementType;

				// TODO: determine whether there are elementDefs for subclasses
				if (_elementType.IsAbstract)
				{
					return _serializer.GetElementType(name);
				}

				return _elementType;
			}

			private void Add(object target, object item)
			{
				var list = target as IList;
				if (list != null)
				{
					list.Add(item);
					return;
				}

				var add = ResolveAddMethod(target, item);

				add(target, item);
			}

			private Action<object, object> ResolveAddMethod(object target, object item)
			{
				var itemType = item != null ? item.GetType() : _elementType;

				Action<object, object> action;
				if (_addMethods.TryGetValue(itemType, out action))
					return action;

				action = MethodGenerator.GenerateAdder(target, item, _elementType);
				_addMethods.Add(itemType, action);
				return action;
			}

			private sealed class ItemDefCollection : IDefCollection<IPropertyDef>
			{
				private readonly CollectionDef _collectionDef;

				public ItemDefCollection(CollectionDef collectionDef)
				{
					_collectionDef = collectionDef;
				}

				public IEnumerator<IPropertyDef> GetEnumerator()
				{
					yield break;
				}

				IEnumerator IEnumerable.GetEnumerator()
				{
					return GetEnumerator();
				}

				public IPropertyDef this[XName name]
				{
					get { return new ItemDef(_collectionDef, name); }
				}
			}

			private sealed class ItemDef : IPropertyDef
			{
				private readonly CollectionDef _collectionDef;

				public ItemDef(CollectionDef collectionDef, XName name)
				{
					_collectionDef = collectionDef;
					Name = name;
					ItemName = name;
				}

				public XName Name { get; private set; }
				public XName ItemName { get; private set; }
				public string PropertyName { get { return "Item"; } }
				public Type Type { get { return _collectionDef.GetElementType(Name); } }

				public bool IsReadOnly { get { return false; } }

				public object GetValue(object target)
				{
					return Activator.CreateInstance(Type);
				}

				public void SetValue(object target, object value)
				{
					_collectionDef.Add(target, value);
				}

				public bool IsDefaultValue(object value)
				{
					return false;
				}

				public override string ToString()
				{
					return Name.ToString();
				}
			}
		}
	}
}