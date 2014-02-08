#if NUNIT
using System.ComponentModel;
using System.Xml.Linq;
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture]
	public class DefaultValueTests
	{
		[TestCase(Result = "<Entity />")]
		public string Test()
		{
			var schema = Scope.New(XNamespace.None);
			schema.Elem<Entity>()
				.Elem(x => x.StringProperty);

			var serializer = XSerializer.New(schema);
			var entity = new Entity
			{
				StringProperty = "test"
			};

			return serializer.ToXmlString(entity);
		}

		private class Entity
		{
			[DefaultValue("test")]
			public string StringProperty { get; set; }
		}
	}
}
#endif