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
			var def = schema.Elem<Entity>();

			if (asAttr)
			{
				def.Attr(x => x.StringProperty);
			}
			else
			{
				def.Elem(x => x.StringProperty);
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
			var def = schema.Elem<Entity>();

			if (asAttr)
			{
				def.Attr(x => x.StringProperty, "custom");
			}
			else
			{
				def.Elem(x => x.StringProperty, "custom");
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