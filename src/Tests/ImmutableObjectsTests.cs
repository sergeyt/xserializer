#if NUNIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture(Format.Xml)]
	[TestFixture(Format.Json)]
	[TestFixture(Format.JsonML)]
	public class ImmutableObjectsTests
	{
		private readonly Format _format;

		public ImmutableObjectsTests(Format format)
		{
			_format = format;
		}

		[Test]
		public void JustAttr()
		{
			var scope = Scope.New(XNamespace.None);

			scope.Element<Item>()
				.Attributes()
				.Add(x => x.Name)
				.End()
				.Init();

			var serializer = XSerializer.New(scope);

			var item1 = new Item("test");
			var serial = serializer.ToString(item1, _format);
			switch (_format)
			{
				case Format.Xml:
					Assert.AreEqual("<Item Name=\"test\" />", serial);
					break;
				case Format.Json:
					Assert.AreEqual("{\"Name\":\"test\"}", serial);
					break;
				case Format.JsonML:
					Assert.AreEqual("[\"Item\",{\"Name\":\"test\"}]", serial);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			var item2 = serializer.Parse<Item>(serial, _format);
			Assert.AreEqual(item1.Name, item2.Name);
		}

		[Test]
		public void TestContainer()
		{
			var scope = Scope.New(XNamespace.None);

			scope.Element<Item>()
				.Attributes()
				.Add(x => x.Name)
				.End()
				.Init();

			scope.Element<Container>()
				.Elements()
				.Add(x => x.Items)
				.End()
				.Init();

			var serializer = XSerializer.New(scope);

			var container = new Container(new[] {new Item("a"), new Item("b")});
			var serial = serializer.ToString(container, _format);
			switch (_format)
			{
				case Format.Xml:
					Assert.AreEqual("<Container><Items><Item Name=\"a\" /><Item Name=\"b\" /></Items></Container>", serial);
					break;
				case Format.Json:
					Assert.AreEqual("{\"Items\":[new Item({\"Name\":\"a\"}),new Item({\"Name\":\"b\"})]}", serial);
					break;
				case Format.JsonML:
					Assert.AreEqual("[\"Container\",[\"Items\",[\"Item\",{\"Name\":\"a\"}],[\"Item\",{\"Name\":\"b\"}]]]", serial);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			var container2 = serializer.Parse<Container>(serial, _format);
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