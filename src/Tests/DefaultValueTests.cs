#if NUNIT
using System.ComponentModel;
using System.Xml.Linq;
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture]
	public class DefaultValueTests
	{
		[TestCase(false, Result = "<Entity />")]
		[TestCase(true, Result = "<Entity />")]
		public string DefaultValueAttribute(bool asAttr)
		{
			var schema = Scope.New(XNamespace.None);
			var def = schema.Element<Entity>();

			if (asAttr)
			{
				def.Attributes().Add(x => x.StringProperty);
			}
			else
			{
				def.Elements().Add(x => x.StringProperty);
			}
				
			var serializer = XSerializer.New(schema);
			var entity = new Entity
			{
				StringProperty = "test"
			};

			return serializer.ToXmlString(entity);
		}

		[TestCase(false, Result = "<Entity />")]
		[TestCase(true, Result = "<Entity />")]
		public string CustomDefaultValue(bool asAttr)
		{
			var schema = Scope.New(XNamespace.None);
			var def = schema.Element<Entity>();

			if (asAttr)
			{
				def.Attributes().Add(x => x.StringProperty, "custom");
			}
			else
			{
				def.Elements().Add(x => x.StringProperty, "custom");
			}
			
			var serializer = XSerializer.New(schema);
			var entity = new Entity
			{
				StringProperty = "custom"
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