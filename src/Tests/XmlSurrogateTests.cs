#if NUNIT
using System.Xml;
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture]
	public class XmlSurrogateTests
	{
		[Test]
		public void Test()
		{
			var scope = new Scope();
			scope.Element<Custom>(new CustomSurrogate());

			scope.Element<Item>()
				.Elements()
				.Add(x => x.Name)
				.Add(x => x.Custom)
				.Add(x => x.Value)
				.End();

			var obj = new Item
			{
				Name = "item",
				Value = "test",
				Custom = new Custom
				{
					InnerXml = "<test></test>"
				}
			};

			var serializer = XSerializer.New(scope);
			var xml = serializer.ToXmlString(obj);

			var obj2 = new Item();
			serializer.ReadXmlString(xml, obj2);

			Assert.AreEqual(obj.Name, obj2.Name);
			Assert.AreEqual(obj.Value, obj2.Value);
			Assert.AreEqual(obj.Custom.InnerXml, obj2.Custom.InnerXml);
		}

		class Item
		{
			public string Name { get; set; }
			public Custom Custom { get; set; }
			public string Value { get; set; }
		}

		class Custom
		{
			public string InnerXml { get; set; }
		}

		class CustomSurrogate : IXmlSurrogate
		{
			public void Read(XmlReader reader, object instance)
			{
				((Custom) instance).InnerXml = reader.ReadInnerXml();
			}

			public void Write(XmlWriter writer, object instance)
			{
				writer.WriteStartElement("Custom");
				writer.WriteRaw(((Custom) instance).InnerXml);
				writer.WriteEndElement();
			}
		}
	}
}
#endif
