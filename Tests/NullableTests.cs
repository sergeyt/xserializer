#if NUNIT
using System;
using NUnit.Framework;

namespace TsvBits.XmlSerialization.Tests
{
	[TestFixture]
	public class NullableTests
	{
		[TestCase(false)]
		[TestCase(true)]
		public void TestItem(bool asAttr)
		{
			TestItem(true, asAttr);
			TestItem(false, asAttr);
			TestItem(0, asAttr);
			TestItem(int.MinValue, asAttr);
			TestItem(int.MaxValue, asAttr);
			TestItem(3.14, asAttr);
			TestItem(StringComparison.Ordinal, asAttr);
		}

		private static void TestItem<T>(T value, bool asAttr) where T : struct
		{
			var scope = Scope.New("");
			var elem = scope.Elem<Item<T>>();
			if (asAttr) elem.Attr(x => x.Value);
			else elem.Elem(x => x.Value);

			var serializer = XSerializer.New(scope);

			var item1 = new Item<T>();
			var xml = serializer.ToXmlFragment(item1);
			var item2 = serializer.Parse<Item<T>>(xml);
			Assert.AreEqual(item1.Value, item2.Value);

			item1 = new Item<T> {Value = value};
			xml = serializer.ToXmlFragment(item1);
			item2 = serializer.Parse<Item<T>>(xml);
			Assert.AreEqual(item1.Value, item2.Value);
		}

		private class Item<T> where T:struct 
		{
			public T? Value { get; set; }
		}
	}
}
#endif