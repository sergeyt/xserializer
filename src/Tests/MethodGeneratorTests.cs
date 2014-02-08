﻿#if NUNIT
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture]
	public class MethodGeneratorTests
	{
		[Test]
		public void TestAdd()
		{
			{
				var c1 = new MyCollection();
				var item1 = new Item {Name = "item1"};
				var adder = MethodGenerator.GenerateAdder(c1, item1, typeof(Item));
				Assert.AreEqual(0, c1.Count);
				adder(c1, item1);
				Assert.AreEqual(1, c1.Count);
				Assert.AreSame(item1, c1[0]);
			}
			{
				var c1 = new MyCollection();
				var item1 = new Item {Name = "item1"};
				var adder = MethodGenerator.GenerateAdder(c1, item1, typeof(object));
				Assert.AreEqual(0, c1.Count);
				adder(c1, item1);
				Assert.AreEqual(1, c1.Count);
				Assert.AreSame(item1, c1[0]);
			}
		}

		[Test]
		public void TestSetProperty()
		{
			var setter = MethodGenerator.GenerateSetter<Item, string>(x => x.Name);
			var item = new Item();
			setter(item, "test");
			Assert.AreEqual("test", item.Name);
			Assert.IsNull(item.Field);
			Assert.IsNull(MethodGenerator.GenerateSetter<Item, string>(x => x.FullName));
		}

		[Test]
		public void TestSetField()
		{
			var setter = MethodGenerator.GenerateSetter<Item, string>(x => x.Field);
			var item = new Item {Field = "abc"};
			setter(item, "test");
			Assert.AreEqual("test", item.Field);
			Assert.IsNull(item.Name);
		}

		[Test]
		public void TestSetReadOnlyField()
		{
			var setter = MethodGenerator.GenerateSetter<Item2, string>(x => x.Name);
			Assert.IsNull(setter);
		}

		private class Item
		{
			public string Field;
			public string Name { get; set; }

			public string FullName
			{
				get { return Name; }
			}
		}

		private class Item2
		{
			public readonly string Name;

			public Item2(string name)
			{
				Name = name;
			}
		}

		private class MyCollection : IEnumerable<Item>
		{
			private readonly IList<Item> _items = new List<Item>();

			public int Count
			{
				get { return _items.Count; }
			}

			public Item this[int index]
			{
				get { return _items[index]; }
			}

			public void Add(object item)
			{
				_items.Add((Item)item);
			}

			public void Add(Item item)
			{
				_items.Add(item);
			}

			public IEnumerator<Item> GetEnumerator()
			{
				return _items.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}
	}
}
#endif