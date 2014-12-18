#if NUNIT
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture]
	public class ImmutableObjectsTests
	{
		[TestCase(Format.Xml, "<Item Name=\"test\" />")]
#if FULL
		[TestCase(Format.Json, "{\"Name\":\"test\"}")]
		[TestCase(Format.JsonML, "[\"Item\",{\"Name\":\"test\"}]")]
#endif
		public void JustAttr(Format format, string expectedOutput)
		{
			var schema = new Scope();

			schema.Element<Item>()
				.Attributes()
				.Add(x => x.Name)
				.End()
				.Init();

			var item1 = new Item("test");
			var output = schema.ToString(item1, format);
			Assert.AreEqual(expectedOutput, output);
			
			var item2 = schema.Parse<Item>(output, format);
			Assert.AreEqual(item1.Name, item2.Name);
		}

		[TestCase(Format.Xml, "<Container><Items><Item Name=\"a\" /><Item Name=\"b\" /></Items></Container>")]
#if FULL
		[TestCase(Format.Json, "{\"Items\":[new Item({\"Name\":\"a\"}),new Item({\"Name\":\"b\"})]}")]
		[TestCase(Format.JsonML, "[\"Container\",[\"Items\",[\"Item\",{\"Name\":\"a\"}],[\"Item\",{\"Name\":\"b\"}]]]")]
#endif
		public void TestContainer(Format format, string expectedOutput)
		{
			var schema = new Scope();

			schema.Element<Item>()
				.Attributes()
				.Add(x => x.Name)
				.End()
				.Init();

			schema.Element<Container>()
				.Elements()
				.Add(x => x.Items)
				.End()
				.Init();

			var container = new Container(new[] {new Item("a"), new Item("b")});
			var output = schema.ToString(container, format);
			Assert.AreEqual(expectedOutput, output);
			
			var container2 = schema.Parse<Container>(output, format);
			Assert.AreEqual(container.Items.Count, container2.Items.Count);
			Assert.AreEqual(container.Items[0].Name, container2.Items[0].Name);
			Assert.AreEqual(container.Items[1].Name, container2.Items[1].Name);
		}

		private class Item
		{
			public Item(string name)
			{
				Name = name;
			}

			[Arg(0)]
			public string Name { get; private set; }
		}

		private class Container
		{
			public Container(IEnumerable<Item> items)
			{
				Items = (items ?? Enumerable.Empty<Item>()).ToList().AsReadOnly();
			}

			[Arg(0)]
			public IList<Item> Items { get; private set; }
		}
	}
}
#endif