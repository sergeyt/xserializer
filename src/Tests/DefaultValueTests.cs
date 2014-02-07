#if NUNIT
using System.Xml.Linq;
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture]
	public class DefaultValueTests
	{
		[Test]
		public void Test()
		{
			Test<string>("test");
		}

		private void Test<T>(T defaultValue)
		{
			var schema = Scope.New(XNamespace.None);
			var item = schema.Elem<Item<string>>();
			// TODO property descriptor structure to define DefaultValue, etc
			// item.Attr(x => x.Value, defaultValue);
		}

		private class Item<T>
		{
			public T Value { get; set; }
		}
	}
}
#endif