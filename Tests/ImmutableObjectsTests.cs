#if NUNIT
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture]
	public class ImmutableObjectsTests
	{
		[Test]
		public void JustAttr()
		{
			var scope = Scope.New(XNamespace.None);

			scope.Elem<Item>()
			     .Attr(x => x.Name, 0)
			     .Init<string>(name => new Item(name));

			var serializer = XSerializer.New(scope);

			var item1 = new Item("test");
			var xml = serializer.ToXmlString(item1, true);
			Assert.AreEqual("<Item Name=\"test\" />", xml);

			var item2 = serializer.Parse<Item>(xml, Format.Xml);
			Assert.AreEqual(item1.Name, item2.Name);
		}

		[Test]
		public void TestContainer()
		{
			var scope = Scope.New(XNamespace.None);

			scope.Elem<Item>()
				 .Attr(x => x.Name, 0)
				 .Init<string>(name => new Item(name));

			scope.Elem<Container>()
			     .Elem(x => x.Items, 0)
			     .Init<IEnumerable<Item>>(items => new Container(items));

			var serializer = XSerializer.New(scope);

			var container = new Container(new[] {new Item("a"), new Item("b")});
			var xml = serializer.ToXmlString(container, true);
			Assert.AreEqual("<Container><Items><Item Name=\"a\" /><Item Name=\"b\" /></Items></Container>", xml);

			var container2 = serializer.Parse<Container>(xml, Format.Xml);
			Assert.AreEqual(container.Items.Count, container2.Items.Count);
			Assert.AreEqual(container.Items[0].Name, container2.Items[0].Name);
			Assert.AreEqual(container.Items[1].Name, container2.Items[1].Name);
		}

		class Item
		{
			public Item(string name)
			{
				Name = name;
			}

			public string Name { get; private set; }
		}

		class Container
		{
			public Container(IEnumerable<Item> items)
			{
				Items = (items ?? Enumerable.Empty<Item>()).ToList().AsReadOnly();
			}

			public IList<Item> Items { get; private set; }
		}
	}
}
#endif