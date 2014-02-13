#if NUNIT
using System.Xml.Linq;
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture]
	public class MultiplePropertyNamesTest
	{
		[TestCase(@"<Entity><Value xmlns=""A"">test</Value></Entity>", Result = "test")]
		[TestCase(@"<Entity><Value xmlns=""B"">test</Value></Entity>", Result = "test")]
		[TestCase(@"<Entity><Text xmlns=""A"">test</Text></Entity>", Result = "test")]
		[TestCase(@"<Entity><Text xmlns=""B"">test</Text></Entity>", Result = "test")]
		[TestCase(@"<Entity><Value xmlns=""C"">test</Value></Entity>", Result = null)]
		[TestCase(@"<Entity><Text xmlns=""C"">test</Text></Entity>", Result = null)]
		[TestCase(@"<Entity><Value>test</Value></Entity>", Result = null)]
		[TestCase(@"<Entity><Text>test</Text></Entity>", Result = null)]
		public string Test(string xml)
		{
			var ns1 = XNamespace.Get("A");
			var ns2 = XNamespace.Get("B");

			var scope = new Scope();
			scope.Element<Entity>()
				.Elements()
				.Add(x => x.Value, ns1 + "Value", ns2 + "Value", ns1 + "Text", ns2 + "Text")
				.End();

			var serializer = XSerializer.New(scope);
			var obj = new Entity();
			serializer.ReadXmlString(xml, obj);
			return obj.Value;
		}

		class Entity
		{
			public string Value;
		}
	}
}
#endif
