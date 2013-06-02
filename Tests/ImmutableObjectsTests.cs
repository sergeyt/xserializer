#if NUNIT
using System.Xml.Linq;
using NUnit.Framework;

namespace XmlSerialization.Tests
{
	[TestFixture]
	public class ImmutableObjectsTests
	{
		[Test]
		public void JustAttr()
		{
			var ns = XNamespace.None;
			var item = ElementDef.New<Item>(ns + "Item")
			                     .Attr(x => x.Name, 0)
			                     .Init<string>(name => new Item(name));

			var serializer = XSerializer.New(item);

			var item1 = new Item("test");
			var xml = serializer.ToXmlString(item1, true);
			Assert.AreEqual("<Item Name=\"test\" />", xml);

			var item2 = serializer.Parse<Item>(xml);
			Assert.AreEqual(item1.Name, item2.Name);
		}

		class Item
		{
			public Item(string name)
			{
				Name = name;
			}

			public string Name { get; private set; }
		}
	}
}
#endif