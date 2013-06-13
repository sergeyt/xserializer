#if NUNIT
using System;
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture(Format.Xml)]
	[TestFixture(Format.Json)]
	public class NullableTests
	{
		private readonly Format _format;

		public NullableTests(Format format)
		{
			_format = format;
		}

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

		private void TestItem<T>(T value, bool asAttr) where T : struct
		{
			var scope = Scope.New("");
			var elem = scope.Elem<Item<T>>();
			if (asAttr) elem.Attr(x => x.Value);
			else elem.Elem(x => x.Value);

			var serializer = XSerializer.New(scope);

			var item1 = new Item<T>();
			var xml = serializer.ToString(item1, _format);
			var item2 = serializer.Parse<Item<T>>(xml, _format);
			Assert.AreEqual(item1.Value, item2.Value);

			item1 = new Item<T> {Value = value};
			xml = serializer.ToString(item1, _format);
			item2 = serializer.Parse<Item<T>>(xml, _format);
			Assert.AreEqual(item1.Value, item2.Value);
		}

		private class Item<T> where T:struct 
		{
			public T? Value { get; set; }
		}
	}
}
#endif