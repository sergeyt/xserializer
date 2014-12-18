#if NUNIT
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture]
	public class SubScopeTest
	{
		[Test]
		public void Test()
		{
			var schema = new Scope();

			var entity = schema.Element<Entity>()
				.Elements()
				.Add(x => x.Name)
				.Add(x => x.Element)
				.End();

			entity.Element<Element>()
				.Elements()
				.Add(x => x.Text)
				.End();

			var obj1 = new Entity
			{
				Name = "test",
				Element = new Element {Text = "hi"}
			};

			var xml = schema.ToXmlString(obj1);

			var obj2 = new Entity();
			schema.ReadXmlString(xml, obj2);

			Assert.AreEqual(obj1.Name, obj2.Name);
			Assert.AreEqual(obj1.Element.Text, obj2.Element.Text);
		}

		private class Entity
		{
			public string Name { get; set; }
			public Element Element { get; set; }
		}

		private class Element
		{
			public string Text { get; set; }
		}
	}
}
#endif
