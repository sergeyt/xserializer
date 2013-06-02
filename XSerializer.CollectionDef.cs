using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

namespace XmlSerialization
{
	partial class XSerializer
	{
		private sealed class CollectionDef : IElementDef
		{
			private readonly XSerializer _serializer;
			private readonly Type _elementType;
			private readonly ItemDefCollection _elements;

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
				}

				public XName Name { get; private set; }
				public Type Type { get { return _collectionDef.GetElementType(Name); } }

				public bool IsReadOnly { get { return false; } }

				public object GetValue(object target)
				{
					return Activator.CreateInstance(Type);
				}

				public void SetValue(object target, object value)
				{
					var list = target as IList;
					if (list != null)
					{
						list.Add(value);
						return;
					}

					// TODO: optimize with expression tree or reflection emit
					target.GetType().GetMethod("Add").Invoke(target, new[] { value });
				}
			}
		}
	}
}