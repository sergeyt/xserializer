#if NUNIT
using System.Xml.Linq;
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture]
	public class MiscTests
	{
		[TestCase(@"<Entity><Value>test</Value><Unknown>abc</Unknown></Entity>", Result = "test")]
		[TestCase(@"<Entity><Unknown><nested>abc</nested></Unknown><Value>test</Value></Entity>", Result = "test")]
		public string SkipUnknownElement(string xml)
		{
			var scope = new Scope();
			scope.Element<Entity>()
				.Elements()
				.Add(x => x.Value)
				.End();

			var serializer = XSerializer.New(scope);
			var obj = new Entity();
			serializer.ReadXmlString(xml, obj);
			return obj.Value;
		}

		[TestCase(@"<Entity><Value xmlns=""A"">test</Value></Entity>", Result = "test")]
		[TestCase(@"<Entity><Value xmlns=""B"">test</Value></Entity>", Result = "test")]
		[TestCase(@"<Entity><Text xmlns=""A"">test</Text></Entity>", Result = "test")]
		[TestCase(@"<Entity><Text xmlns=""B"">test</Text></Entity>", Result = "test")]
		[TestCase(@"<Entity><Value xmlns=""C"">test</Value></Entity>", Result = null)]
		[TestCase(@"<Entity><Text xmlns=""C"">test</Text></Entity>", Result = null)]
		[TestCase(@"<Entity><Value>test</Value></Entity>", Result = null)]
		[TestCase(@"<Entity><Text>test</Text></Entity>", Result = null)]
		public string MultiplePropertyNames(string xml)
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

		[TestCase(@"<Entity><Value>test</Value></Entity>", Result = "test")]
		[TestCase(@"<Ent><Value>test</Value></Ent>", Result = "test")]
		[TestCase(@"<E><Value>test</Value></E>", Result = "test")]
		public string MultipleElementNames(string xml)
		{
			var scope = new Scope();
			scope.Element<Entity>("Entity", "Ent", "E")
				.Elements()
				.Add(x => x.Value)
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
