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
			var schema = new Scope();
			var def = schema.Element<Entity>();

			if (asAttr)
			{
				def.Attributes().Add(x => x.StringProperty);
			}
			else
			{
				def.Elements().Add(x => x.StringProperty);
			}

			var entity = new Entity
			{
				StringProperty = "test"
			};

			return schema.ToXmlString(entity);
		}

		[TestCase(false, Result = "<Entity />")]
		[TestCase(true, Result = "<Entity />")]
		public string CustomDefaultValue(bool asAttr)
		{
			var schema = new Scope();
			var def = schema.Element<Entity>();

			if (asAttr)
			{
				def.Attributes().Add(x => x.StringProperty, "custom");
			}
			else
			{
				def.Elements().Add(x => x.StringProperty, "custom");
			}

			var entity = new Entity
			{
				StringProperty = "custom"
			};

			return schema.ToXmlString(entity);
		}

		private class Entity
		{
			[DefaultValue("test")]
			public string StringProperty { get; set; }
		}
	}
}
#endif