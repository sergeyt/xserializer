#if NUNIT
using System.Collections.Generic;
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture]
	public class CollectionTests
	{
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

		private static void TestCore<T>(params T[] items)
		{
			var scope = Scope.New("");

			scope.Elem<Container<T>>()
			     .Elem(x => x.Items);

			var serializer = XSerializer.New(scope);

			var container = new Container<T>();
			foreach (var item in items)
			{
				container.Items.Add(item);
			}
			
			var xml = serializer.ToXmlString(container, true);

			var container2 = serializer.Parse<Container<T>>(xml);
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
	}
}
#endif