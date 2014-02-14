#if NUNIT
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture(Format.Xml)]
#if FULL
	[TestFixture(Format.Json)]
	[TestFixture(Format.JsonML)]
#endif
	public class CollectionTests
	{
		private readonly Format _format;

		public CollectionTests(Format format)
		{
			_format = format;
		}

		[TestCase("a", "b", "c")]
		[TestCase("a", null, "c")]
		[TestCase(1, 2, 3)]
		[TestCase(1, null, 3)]
		public void TestObjectContainer(object a, object b, object c)
		{
			TestCore(a, b, c);
		}

		[TestCase("a", "b", "c")]
		[TestCase("a", null, "c")]
		public void TestStringContainer(string a, string b, string c)
		{
			TestCore(a, b, c);
		}

		[TestCase(1, 2, 3)]
		public void TestIntContainer(int a, int b, int c)
		{
			TestCore(a, b, c);
		}

		private void TestCore<T>(params T[] items)
		{
			TestContainer1(items);
			TestContainer2(items);
		}

		private void TestContainer1<T>(params T[] items)
		{
			var schema = new Scope();

			schema.Element<Container<T>>()
				.Elements()
				.Add(x => x.Items);

			var serializer = XSerializer.New(schema);

			var container = new Container<T>();
			foreach (var item in items)
			{
				container.Items.Add(item);
			}
			
			var serial = serializer.ToString(container, _format);

			var container2 = serializer.Parse<Container<T>>(serial, _format);
			Assert.AreEqual(container.Items.Count, container2.Items.Count);
			for (int i = 0; i < container.Items.Count; i++)
			{
				Assert.AreEqual(container.Items[i], container2.Items[i]);
			}
		}

		private void TestContainer2<T>(params T[] items)
		{
			var schema = new Scope();

			schema.Element<Container2<T>>()
				.Elements()
				.Add(x => x.Items);

			var serializer = XSerializer.New(schema);

			var container = new Container2<T>();
			foreach (var item in items)
			{
				container.Items.Add(item);
			}

			var serial = serializer.ToString(container, _format);

			var container2 = serializer.Parse<Container2<T>>(serial, _format);
			Assert.AreEqual(container.Items.Count, container2.Items.Count);
			for (int i = 0; i < container.Items.Count; i++)
			{
				Assert.AreEqual(container.Items[i], container2.Items[i]);
			}
		}

		class Container<T>
		{
			public Container()
			{
				Items = new List<T>();
			}

			public IList<T> Items { get; private set; }
		}

		class Container2<T>
		{
			public Container2()
			{
				Items = new Collection<T>();
			}

			public Collection<T> Items { get; private set; }
		}

		class Collection<T> : CollectionBase
		{
			public T this[int index]
			{
				get { return (T)List[index]; }
			}

			public void Add(T item)
			{
				List.Add(item);
			}

			protected override void OnValidate(object value)
			{
			}
		}
	}
}
#endif