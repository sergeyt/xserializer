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
			                     .Attr(x => x.Name);

			var serializer = XSerializer.New(item);

			var xml = serializer.ToXmlString(new Item("test"), true);
			Assert.AreEqual("<Item Name=\"test\" />", xml);

			var obj = serializer.Parse<Item>(xml);
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